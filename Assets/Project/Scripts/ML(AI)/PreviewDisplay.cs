using UnityEngine;
using UnityEngine.UI;

public class CameraDisplay : MonoBehaviour
{
    [Tooltip("シーンにRaw Imageを作成し、ここにリンクしてください")]
    public RawImage displayImage; // 表示先のRawImage

    [Tooltip("WebCamControllerスクリプトがアタッチされたオブジェクトをここにドラッグしてください")]
    public WebCamController webCamController;

    [Tooltip("画像サイズのデフォルトは640x640です")]
    // targetRTの画像サイズ（モデル入力や表示用の解像度を指定）
    public int imageWidth = 640;  // 画像の幅
    public int imageHeight = 640; // 画像の高さ

    // 出力用の画像を入れる箱(RenderTexture)
    private RenderTexture targetRT;

    void Start()
    {
        // displayImageの比率を参照して画像サイズを自動設定する
        if (displayImage == null) displayImage = GetComponent<RawImage>();

        // 640x640のRenderTextureを作成
        targetRT = new RenderTexture(imageWidth, imageHeight, 0);

        // webカメラから取得した映像のみを反転させる
        displayImage.uvRect = new Rect(1, 0, -1, 1);
    }

    // カメラ映像だけを表示する関数
    public void ImagePreview()
    {
        
        // webカメラのテクスチャを取得
        var CameraTexture = webCamController.CameraTexture;

        // これにより、アスペクト比を保ったまま、上下に黒帯が追加された640x640の画像が作られる。
        float aspect = (float)CameraTexture.width / CameraTexture.height; // aspectは「横 ÷ 縦」で、画像の横長さと縦長さの比率を表します

        Graphics.Blit(
            CameraTexture, // カメラからの画像
            targetRT,      // 表示する場所
            new Vector2(1f / aspect, 1), // Vector2(x方向 / y方向)：x方向を縮小させる割合を指定（yはそのまま）
            // ↑ この場合、1:1の正方形に収まる
            new Vector2((aspect - 1f) / (2f * aspect), 0) // 横方向の位置調整（真ん中にするため）
        );

        // UIのRawImageに結果を表示
        displayImage.texture = targetRT;
    }
}