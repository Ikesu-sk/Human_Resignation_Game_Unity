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

public class GameManager : MonoBehaviour
{
    // ------------- UI関連の設定 ----------------
    [SerializeField] private UIManager uiManager; // UI管理クラス

    public float clearCount; // ゲームクリアに必要なカウント
    private float currentCount = 0; // 現在のカウント
    public bool gameCleard = true; // ゲームクリア状態のフラグ

    // Inspecterで clearCount の数値を変更したら自動で変更される設定にする関数
    void OnValidate()
    {
        uiManager.SetProgressBarMaxValue(clearCount);// InspectorでclearCount変更時にスライダーの最大値を更新
    }


    [SerializeField] private SoundManager soundManager; // サウンド管理


    // ------------- 敵キャラ生成 ----------------
    [SerializeField] private EnemySpawner enemySpawner;   
    private Coroutine spawnCoroutine; // 敵生成コルーチン
    private List<GameObject> enemies = new List<GameObject>(); // 生成された敵リスト

    // ------------- 推論スクリプト ----------------
    [SerializeField] private RunYOLO runYOLO; // YOLO推論スクリプト

    // ------------- Health UI ----------------
    [SerializeField] private HealthUI healthUI; // HP管理UI

    // ------------- イベント ----------------
    public event Action OnGameClear; // ゲームクリアイベント

    // --------------------------------------------------

    void Start()
    {
        QualitySettings.vSyncCount = 0; // VSyncを無効化

        uiManager.GameUI_InitializeUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameCleard)
        {
            currentCount += 1; // カウントを増加
            uiManager.UpdateCountText("Count:" + Mathf.Floor(currentCount).ToString()); // カウントを整数で表示


            uiManager.UpdateProgressBar(currentCount); // スライダーの値を更新
            

            if (currentCount >= clearCount)
            {
                ClearGame(); // ゲームクリア処理を呼び出し
            }

            // YOLO推論結果に応じたオーバーレイ表示
            int currentBoxes = runYOLO.BoxesFound;

            // 検出数が1以上（人間）だった場合
            if (currentBoxes > 0)
            {
                uiManager.ShowIsJingai(false);
            }
            // 検出数が0（人外）だった場合、人外画像をwebカメラの上に映す
            else
            {
                uiManager.ShowIsJingai(true);
            }
        }
    }

    public void StartGame() // StoryManager_Game.csから呼び出されて実行される。
    {
        // まだゲームが開始してなかった場合のみに実行
        if (gameCleard == true)
        {
            // ゲームUIを表示させる
            uiManager.SetGameUIVisible(true);
            // カウントダウンを開始
            StartCoroutine(GameStartCountdown());
        }
    }

    IEnumerator GameStartCountdown()
    {
        uiManager.CountdownTextVisible(true);

        int count = 3;
        while (count > 0)
        {
            uiManager.UpdateCountdownText(count.ToString()) ;
            yield return new WaitForSeconds(1f);
            count--;
        }

        // "START"表示
        uiManager.UpdateCountdownText("START!");
        yield return new WaitForSeconds(1f);

        // カウントダウン非表示
        uiManager.CountdownTextVisible(false);

        if (count == 0)
        {
            gameCleard = false;

            uiManager.BackgroundVideoVisible(true);
            uiManager.BackgroundVideoPlay(true);

            // 【カメラ映像サイズ調整】ゲーム画面に適したサイズに設定
            // 300x300サイズに設定（位置はUnity Editorで設定済み）
            runYOLO.SetCameraDisplaySize(300f, 300f);

            // 敵の生成開始。変数に格納しておくことで、いつでもコルーチンを停止させられるようにしておく
            spawnCoroutine = StartCoroutine(enemySpawner.SpawnEnemies(gameCleard));
            Debug.Log("ゲームスタート！");
        }
    }


    public void GameOver()
    {
        uiManager.GameOverPanel(true);

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

        // Videoのみ静止
        uiManager.BackgroundVideoPlay(false);


        // カウント、音楽変更、カメラ映像停止
        // カウント停止するため、一度クリア状態にしておく
        gameCleard = true;

        // 推論停止
        runYOLO.StopYOLO();

        // ゲームオーバー用の音楽に切り替え
        soundManager.PlayBGM(SoundManager.BGM.GameOver);
    }

    public void OnRetryButton()
    {
        // 先ほどまで使用した変数の中身をリセット。またカウントダウンから再スタートさせる
        // ゲームオーバーパネルを閉じる
        uiManager.GameOverPanel(false);

        Time.timeScale = 1f;
        uiManager.BackgroundVideoPlay(true);

        // 進捗やカウントをリセット
        currentCount = 0;
        uiManager.UpdateProgressBar(0);
        uiManager.UpdateCountText("Count:" + 0);

        // HealthUI のリセット
        healthUI.ResetHearts();

        // 推論再開
        runYOLO.StartYOLO();

        // 【カメラ映像サイズ再設定】リトライ時にもサイズを適切に設定
        runYOLO.SetCameraDisplaySize(300f, 300f);

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
        uiManager.SetGameUIVisible(false);
        uiManager.BackgroundVideoVisible(false);
        uiManager.BackgroundVideoPlay(false);

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
        uiManager.WebCamVisible(false);

        // 人外画像も非表示にする
        uiManager.ShowIsJingai(false);

        uiManager.UpdateCountText("Game Clear!");

        // クリア時の処理を行うイベントを発火させる(StoryManager_Game側で発火時のイベントを追加している)
        OnGameClear?.Invoke();
    }
}
