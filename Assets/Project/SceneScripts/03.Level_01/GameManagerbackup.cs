using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.Rendering.LookDev;
#endif
using System.Collections.Generic;
// using UnityEngine.UIElements; // UI Toolkit用のSliderを使いたい場合は、こちらを記述すればよいとのこと

public class GameManagerCopy : MonoBehaviour
{
    // -------------　スライダーバー等の設定用 ----------------

    public Slider progressBar; // 進捗バー用のUIスライダー
    public float clearCount; // クリアに必要なカウント。publicにしているため、Inspecterでの設定が反映される
    private float currentCount = 0;
    public bool gameCleard = true; // デフォルトはゲームが開始してないのでtrue

    // Inspecterで clearCount の数値を変更したら自動で変更される設定にする関数
    void OnValidate() 
    {
        progressBar.maxValue = clearCount;
    }

    // -------------　ゲームUI用 ----------------
    public TextMeshProUGUI countText; // カウント表示用のUIテキスト
    [SerializeField] private GameObject JudgeLine;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image HeartContainer;
    [SerializeField] private Image overlayImage;

    public Sprite humanSprite;          // 人間用スプライト
    public Sprite alienSprite;          // 人外用スプライト

    // -------------　ゲームオーバー用 ----------------
    public GameObject gameOverPanel;
    [SerializeField] private SoundManager soundManager;

    // -------------背景動画を再生するやつ ----------------
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoUI;

    // -------------　敵キャラ生成用 ----------------

    public GameObject enemyPrefab; //生成する敵キャラのプレハブ
    public Transform spawnPoint; // 敵の生成位置
    public Transform[] targetPoints; // 敵が到達する位置
    private Coroutine spawnCoroutine;

    private List<GameObject> enemies = new List<GameObject>();

    // -------------　推論スクリプトとの連携　 ----------------
    [SerializeField] private RunYOLO runYOLO;

    // -------------　webカメラテクスチャとの連携　 ----------------
    [SerializeField] private WebCamController webCamController;
    // ------------- HealthUIとの連携 -------------
    [SerializeField] private HealthUI healthUI;

    [SerializeField] private AudioSource audioSource;

    // -------------イベントの定義 ----------------
    public event Action OnGameClear; // ゲームクリア時のイベント

    // --------------------------------------------------

    void Start()
    {
        QualitySettings.vSyncCount = 0; //Vsyncをオフにする
        Application.targetFrameRate = 30; // 30FPSを目標に固定。理想は60だがPCのスペック的に30FPSが安定しそうだった

        countText.gameObject.SetActive(false); // 最初はカウントを非表示
        progressBar.gameObject.SetActive(false);// 進捗バーも非表示
        countdownText.gameObject.SetActive(false);// カウントダウンも非表示
        HeartContainer.gameObject.SetActive(false); // HPも非表示
        gameOverPanel.gameObject.SetActive(false);// ゲームオーバー用のパネルも非表示
        overlayImage.gameObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (!gameCleard)
        {
            // countText.gameObject.SetActive(true); // カウントを表示
            currentCount += 1; //毎フレームごとに1 増える
            if (countText != null) // InspecterでTextコンポーネントが割り当てられてなかったときは、以下のコードを実行しないように設定
            {
                countText.text = "Count:" + Mathf.Floor(currentCount).ToString(); //整数に丸めて表示
            }

            if (progressBar != null)
            {
                progressBar.value = currentCount; // スライダーの値を更新
            }

            if (currentCount >= clearCount)
            {
                ClearGame();
            }

            
            // Enemy到達時のYOLO検出数を取得
            int currentBoxes = runYOLO.BoxesFound;

            // 検出数が1以上（人間）だった場合
            if (currentBoxes > 0 )
            {
                overlayImage.gameObject.SetActive(false);
            }
            // 検出数が0（人外）だった場合、人外画像をwebカメラの上に映す
            else
            {
                overlayImage.gameObject.SetActive(true);
            }
        }
    }
    
    // ゲームUIの表示/非表示の制御をする関数
    private void SetGameUIVisible(bool isVisible)
    {
        HeartContainer.gameObject.SetActive(isVisible);
        progressBar.gameObject.SetActive(isVisible);
        countText.gameObject.SetActive(isVisible);
        JudgeLine.gameObject.SetActive(isVisible);
    }

    public void StartGame() // StoryManager_Game.csから呼び出されて実行される。
    {
        // まだゲームが開始してなかった場合のみに実行
        if (gameCleard == true)
        {
            // ゲームUIを表示させる
            SetGameUIVisible(true);
            // カウントダウンを開始
            StartCoroutine(GameStartCountdown());
        }
    }

    IEnumerator GameStartCountdown()
    {
        countdownText.gameObject.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            countdownText.text = count.ToString();
            yield return new WaitForSeconds(1f);
            count--;
        }

        // "START"表示
        countdownText.text = "START!";
        yield return new WaitForSeconds(1f);

        // カウントダウン非表示
        countdownText.gameObject.SetActive(false);

        if (count == 0)
        {
            gameCleard = false;
            videoUI.SetActive(true); // UIを表示
            videoPlayer.Play(); // videoを再生

            // 【カメラ映像サイズ調整】ゲーム画面に適したサイズに設定
            // 300x300サイズに設定（位置はUnity Editorで設定済み）
            runYOLO.SetCameraDisplaySize(300f, 300f);

            // 敵の生成開始。変数に格納しておくことで、いつでもコルーチンを停止させられるようにしておく
            spawnCoroutine = StartCoroutine(SpawnEnemies()); 
            Debug.Log("ゲームスタート！");
        }
    }

    // 敵を生成するための関数
    IEnumerator SpawnEnemies()
    {
        while (!gameCleard)
        {
            // 敵を生成する間隔を、1秒~5秒の間でランダムに設定
            float interval = UnityEngine.Random.Range(1f, 5f);
            Debug.Log("intervalは" + interval + "秒でした");

            yield return new WaitForSeconds(interval); //コルーチンを一時停止し、指定された時間後に処理を再開させる命令

            // enemyPrefabを生成（インスタンス化）するための関数
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity); //newEnemyは生成されたオブジェクトを格納するための変数。引数の中身 →（生成する敵のプレハブ, 生成する位置, 生成オブジェクトの回転をなしに設定）
            // newEnemy（↑これ）というGameObjectにアタッチされているEnemy（というクラス名の）コンポーネントを取得する
            Enemy2D enemy = newEnemy.GetComponent<Enemy2D>();

            // RunYOLOをEnemy.csに受け渡す関数
            //enemy.SetRunYOLO(runYOLO);
            enemy.SetAudioSource(audioSource);
            //enemy.SetHealthUI(healthUI);

            // targetPointsの中から（ランダムに）１つ到達座標を選ぶ
            int idx = UnityEngine.Random.Range(0, targetPoints.Length);
            Transform selectedTarget = targetPoints[idx];
            // Debug.Log(idx + "が到達座標に選ばれました");

            // 乱数で人外判定
            bool alienFlag = (UnityEngine.Random.Range(0, 2) == 0);//50%の確率で人外になる。数値が0なら人外、1なら人間になる

            // 到達時間を設定する
            // float duration = Random.Range(5f, 10f);
            float duration = 1f;

            // 移動後のscaleの大きさを設定
            float scale = 1f;

            // Enemy 側に初期化を渡す
            enemy.Initialize(selectedTarget, alienFlag, duration, scale);

            // 生成した敵をリストに追加していく
            enemies.Add(newEnemy);
                
            // Debug.Log("敵を生成しました");
        }
    }

    public void GameOver()
    {
        gameOverPanel.SetActive(true);

        // 敵の生成を停止
        StopCoroutine(spawnCoroutine);
        // コルーチンの参照を空にしておく。そうしないと再開時にコルーチンが2重でスタートする可能性があるから
        spawnCoroutine = null;

        // 敵を非表示にする
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false);
            }
        }
        // 敵リストを空にしておく
        enemies.Clear();

        //ゲーム全体を静止  
        Time.timeScale = 0f;
        // video動画も静止
        videoPlayer.Pause();

        // カウント、音楽変更、カメラ映像停止
        // カウント停止するため、一度クリア状態にしておく
        gameCleard = true;

        // 推論停止
        runYOLO.StopYOLO();

        // webカメラの映像更新を停止
        webCamController.StopCamera();

        // ゲームオーバー用の音楽に切り替え
        soundManager.PlayBGM(SoundManager.BGM.GameOver);
    }

    public void OnRetryButton()
    {
        // 先ほどまで使用した変数の中身をリセット。またカウントダウンから再スタートさせる
        // ゲームオーバーパネルを閉じる
        gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
        videoPlayer.Play();

        // 進捗やカウントをリセット
        currentCount = 0;
        progressBar.value = 0;
        countText.text = "Count:" + 0;

        // HealthUI のリセット
        healthUI.ResetHearts();

        // 推論再開
        runYOLO.StartYOLO();

        // 【カメラ映像サイズ再設定】リトライ時にもサイズを適切に設定
        runYOLO.SetCameraDisplaySize(300f, 300f);

        // webカメラの映像を元に戻す
        // webCamController.StartCamera();

        // ゲームプレイ時のBGMに戻す
        soundManager.PlayBGM(SoundManager.BGM.Electronic_Violence);

        // ゲームクリアフラグをtrueに戻す
        gameCleard = true;

        // カウントダウンから再開
        StartGame();
    }

    void ClearGame()
    {
        gameCleard = true;
        SetGameUIVisible(false);
        videoPlayer.Stop(); // videoを停止
        videoUI.SetActive(false); // Videoを非表示

        // 敵の生成を停止＆コルーチンを空にしておく
        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;

        // 敵を非表示にする
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false);
            }
        }
        // 敵リストを空にしておく
        enemies.Clear();

        // webカメラを非表示にする
        webCamController.gameObject.SetActive(false);
        overlayImage.gameObject.SetActive(false);
        
        Debug.Log("Game Clear!");
        if (countText != null)
        {
            countText.text = "Game Clear!";
        }
        // クリア時の処理を行うイベントを発火させる(StoryManager_Game側で発火時のイベントを追加している)
        OnGameClear?.Invoke();
    }
}
