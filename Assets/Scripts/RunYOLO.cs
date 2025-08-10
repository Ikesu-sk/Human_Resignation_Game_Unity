using System;
using System.Collections.Generic;
using System.IO;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/*
 *  YOLO Inference Script
 *  ========================
 *
 * このスクリプトは、YOLOモデルを使用してリアルタイムで物体検出を行います。
 * Main Cameraにアタッチし、インスペクターから必要なアセット（モデル、クラスラベルなど）を設定してください。
 *
 */

public class RunYOLO : MonoBehaviour
{
    [Tooltip("YOLOモデルの.onnxファイルをここにドラッグしてください")]
    public ModelAsset modelAsset;

    [Tooltip("クラス名が記述されたclasses.txtをここにドラッグしてください")]
    public TextAsset classesAsset;

    [Tooltip("シーンにRaw Imageを作成し、ここにリンクしてください")]
    public RawImage displayImage;

    [Tooltip("バウンディングボックス（枠線）用のテクスチャをここにドラッグしてください")]
    public Texture2D borderTexture;

    [Tooltip("ラベル表示に適したフォントを選択してください")]
    public Font font;

    [Tooltip("WebCamControllerスクリプトがアタッチされたオブジェクトをここにドラッグしてください")]
    public WebCamController webCamController;

    // 推論に使用するバックエンドを指定します。GPUが利用可能な場合はGPUComputeが高速です。
    const BackendType backend = BackendType.GPUCompute;

    // UI要素の親となるTransform
    private Transform displayLocation;
    // ONNXモデルを実行するためのワーカー
    private Worker worker;
    // 検出可能なクラスのラベルを格納する配列
    private string[] labels;
    // モデルの入力として使用するRenderTexture
    private RenderTexture targetRT;
    // 枠線表示用のスプライト
    private Sprite borderSprite;

    // モデルが要求する画像のサイズ
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    // バウンディングボックスのUIオブジェクトを再利用するためのプール
    List<GameObject> boxPool = new();

    [Tooltip("Non-Maximum Suppression（NMS）で使用されるIoU（Intersection over Union）の閾値")]
    [SerializeField, Range(0, 1)]
    float iouThreshold = 0.5f;

    [Tooltip("Non-Maximum Suppression（NMS）で使用される信頼度スコアの閾値")]
    [SerializeField, Range(0, 1)]
    float scoreThreshold = 0.5f;

    // YOLOの出力（中心座標、幅、高さ）を（左上、右下）の角座標に変換するための行列
    Tensor<float> centersToCorners;
    
    // バウンディングボックスの情報を保持するための構造体
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }

    // ゲーム開始時に一度だけ実行される関数。推論を始めるための初期設定を行う
    void Start()
    {
        // アプリケーションのフレームレートを60に設定
        Application.targetFrameRate = 60;
        // 画面の向きを横向き（左）に固定
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // classes.txtからクラスラベルを読み込み、改行で分割して配列に格納
        labels = classesAsset.text.Split('\n');

        // モデルのロードと後処理の設定
        LoadModel();

        // モデルの入力サイズでRenderTextureを初期化
        targetRT = new RenderTexture(imageWidth, imageHeight, 0);

        // 検出結果を表示するRawImageのTransformを取得
        displayLocation = displayImage.transform;

        // webカメラから取得した映像のみを反転させる
        displayImage.uvRect = new Rect(1, 0, -1, 1);

        // 枠線テクスチャからスプライトを作成
        borderSprite = Sprite.Create(borderTexture, new Rect(0, 0, borderTexture.width, borderTexture.height), new Vector2(borderTexture.width / 2, borderTexture.height / 2));
    }
    
    // ONNXモデルを読み込み、推論するための計算グラフを構築する関数
    void LoadModel()
    {
        // ONNXモデルファイルをロード
        var model1 = ModelLoader.Load(modelAsset);

        // YOLOの出力形式 (center_x, center_y, width, height) を、
        // NMS（Non-Maximum Suppression）が要求する (x_min, y_min, x_max, y_max) 形式に変換するための行列を定義
        centersToCorners = new Tensor<float>(new TensorShape(4, 4),
        new float[]
        {
                    1,      0,      1,      0,
                    0,      1,      0,      1,
                    -0.5f,  0,      0.5f,   0,
                    0,      -0.5f,  0,      0.5f
        });

        // ここでは、モデルの生出力を直接使うのではなく、FunctionalGraph API を使って後処理（NMS）を含む新しい計算グラフを構築します。
        // これにより、後処理をCPUではなくGPU上で高速に実行できます。
        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(model1);
        var modelOutput = Functional.Forward(model1, inputs)[0];                        // shape=(1, 84, 8400) - 84は(x,y,w,h)の4次元 + 80クラスのスコア
        var boxCoords = modelOutput[0, 0..4, ..].Transpose(0, 1);               // shape=(8400, 4) - バウンディングボックスの座標部分を抽出
        var allScores = modelOutput[0, 4.., ..];                                // shape=(80, 8400) - 全クラスのスコア部分を抽出
        var scores = Functional.ReduceMax(allScores, 0);                                // shape=(8400) - 各ボックスで最も高いスコアを計算
        var classIDs = Functional.ArgMax(allScores, 0);                                 // shape=(8400) - 最も高いスコアを持つクラスのIDを計算
        var boxCorners = Functional.MatMul(boxCoords, Functional.Constant(centersToCorners));   // shape=(8400, 4) - 座標形式を変換
        var indices = Functional.NMS(boxCorners, scores, iouThreshold, scoreThreshold); // shape=(N) - NMSを実行し、残すべきボックスのインデックスを取得
        var coords = Functional.IndexSelect(boxCoords, 0, indices);                     // shape=(N, 4) - NMSを通過したボックスの座標を選択
        var labelIDs = Functional.IndexSelect(classIDs, 0, indices);                    // shape=(N) - NMSを通過したボックスのクラスIDを選択

        // 構築したグラフと、最終的な出力（座標とラベルID）を指定して、推論ワーカーを作成
        worker = new Worker(graph.Compile(coords, labelIDs), backend);
    }

    // 毎フレーム(今回は1秒間に60回)呼び出される関数
    private void Update()
    {
        // 毎フレーム、機械学習の推論を実行
        ExecuteML();
    }

    // 毎フレーム呼び出され、物体検出の一連のプロセスを実行する関数
    public void ExecuteML()
    {
        // 前のフレームで描画したバウンディングボックスを非表示にする
        ClearAnnotations();

        // WebCamControllerが設定されていて、カメラのテクスチャが利用可能な状態かチェックします。
        if (webCamController == null && webCamController.CameraTexture == null && !webCamController.CameraTexture.isPlaying)
        {
            return;
        }

        // パフォーマンス向上のため、カメラ映像が更新されたフレームでのみ推論を実行します。
        if (!webCamController.CameraTexture.didUpdateThisFrame) //更新されていない場合はreturnを返すだけ
        {
            return;
        }

        // webカメラのテクスチャを取得
        var CameraTexture = webCamController.CameraTexture;

        // これにより、アスペクト比を保ったまま、上下に黒帯が追加された640x640の画像が作られる。
        float aspect = (float)CameraTexture.width / CameraTexture.height;
        Graphics.Blit(CameraTexture, targetRT, new Vector2(1f / aspect, 1), new Vector2((aspect - 1f) / (2f * aspect), 0));
        // UIのRawImageに結果を表示
        displayImage.texture = targetRT;

        // RenderTextureからTensor<float>形式に変換して、モデルへの入力データを作成
        using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
        TextureConverter.ToTensor(targetRT, inputTensor, default);

        // 作成した入力Tensorをワーカーに渡して、推論ジョブをスケジュール（非同期実行）
        worker.Schedule(inputTensor);

        // 推論結果を取得（ここでは同期的に待機）。`ReadbackAndClone`でGPUからCPUにデータをコピー
        using var output = (worker.PeekOutput("output_0") as Tensor<float>).ReadbackAndClone();
        using var labelIDs = (worker.PeekOutput("output_1") as Tensor<int>).ReadbackAndClone();

        // 表示用UIのサイズを取得
        float displayWidth = displayImage.rectTransform.rect.width;
        float displayHeight = displayImage.rectTransform.rect.height;

        // 座標のスケーリングも入力解像度に合わせる
        float scaleX = displayWidth / imageWidth;
        float scaleY = displayHeight / imageHeight;

        // 検出されたボックスの数
        int boxesFound = output.shape[0];

        // 検出された各ボックスに対して描画処理を行う（最大200個まで）
        for (int n = 0; n < Mathf.Min(boxesFound, 200); n++)
        {
            // BoundingBox構造体に結果を格納
            var box = new BoundingBox
            {
                // 座標をスケーリングし、UIの原点（中央）に合わせる
                centerX = output[n, 0] * scaleX - displayWidth / 2,
                centerY = output[n, 1] * scaleY - displayHeight / 2,
                width = output[n, 2] * scaleX,
                height = output[n, 3] * scaleY,
                label = labels[labelIDs[n]], // クラスIDに対応するラベル名を取得
            };

            // 背景映像が反転しているので、ボックスのX座標も手動で反転させる
            box.centerX *= -1; 

            // ボックスを描画
            DrawBox(box, n, displayHeight * 0.05f);
        }
    }

    // 検出結果を画面に表示するためのUI操作メゾッド（その１）
    public void DrawBox(BoundingBox box, int id, float fontSize)
    {
        GameObject panel;
        // オブジェクトプールに再利用可能なボックスがあるか確認
        if (id < boxPool.Count)
        {
            // あればそれを再利用（アクティブにする）
            panel = boxPool[id];
            panel.SetActive(true);
        }
        else
        {
            // なければ新しく作成
            panel = CreateNewBox(Color.yellow);
        }

        // ボックスの位置を設定（Y座標は上下反転させる）
        panel.transform.localPosition = new Vector3(box.centerX, -box.centerY);

        // ボックスのサイズを設定
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(box.width, box.height);

        // ラベルのテキストとフォントサイズを設定
        var label = panel.GetComponentInChildren<Text>();
        label.text = box.label;
        label.fontSize = (int)fontSize;
    }

    // UI操作メゾッド（その２）
    public GameObject CreateNewBox(Color color)
    {
        // ボックスの枠となるUIオブジェクト（Panel）を生成
        var panel = new GameObject("ObjectBox");
        panel.AddComponent<CanvasRenderer>();
        Image img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = borderSprite; // 枠線用のスプライトを設定
        img.type = Image.Type.Sliced; // Slicedモードで引き伸ばしても角が崩れないようにする
        panel.transform.SetParent(displayLocation, false); // displayImageの子要素にする

        // ラベル表示用のUIオブジェクト（Text）を生成
        var text = new GameObject("ObjectLabel");
        text.AddComponent<CanvasRenderer>();
        text.transform.SetParent(panel.transform, false); // Panelの子要素にする
        Text txt = text.AddComponent<Text>();
        txt.font = font;
        txt.color = color;
        txt.fontSize = 40;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow; // テキストがはみ出すのを許可

        // ラベルの位置とサイズを調整
        RectTransform rt2 = text.GetComponent<RectTransform>();
        rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
        rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
        rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
        rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
        rt2.anchorMin = new Vector2(0, 0);
        rt2.anchorMax = new Vector2(1, 1);

        // 作成したボックスをプールに追加
        boxPool.Add(panel);
        return panel;
    }

    // UI操作メゾッド（その３）
    public void ClearAnnotations()
    {
        // プール内のすべてのボックスを非アクティブにして、画面から消す
        foreach (var box in boxPool)
        {
            box.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // アプリケーション終了時に、確保したネイティブリソースを解放する
        // これを怠るとメモリリークの原因になる
        centersToCorners?.Dispose();
        worker?.Dispose();
    }
}