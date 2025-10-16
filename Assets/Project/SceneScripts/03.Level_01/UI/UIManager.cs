using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIManager : MonoBehaviour
{
    // ------------- ゲームUI ----------------
    public TextMeshProUGUI countText; // ゲームplay中のカウント表示用テキスト
    [SerializeField] private GameObject judgeLine; // 判定ライン
    [SerializeField] private TextMeshProUGUI countdownText; // カウントダウン表示
    [SerializeField] private Image heartContainer; // HPバー
    [SerializeField] private Image overlayImage; // オーバーレイ画像
    public Slider progressBar; // ゲーム進捗バー
    public GameObject gameOverPanel; // ゲームオーバーパネル

    // ------------- 背景動画 ----------------
    [SerializeField] private VideoPlayer videoPlayer; // 背景動画プレイヤー
    [SerializeField] private GameObject BackgroundVideo; // 動画

    // ------------- Webカメラ ----------------
    [SerializeField] private WebCamController webCamController; // Webカメラ制御

    // ------------- 関数一覧 ----------------

    // UIの初期化。ゲーム関連のものは非表示にしておく
    public void GameUI_InitializeUI()
    {
        countText.gameObject.SetActive(false);
        progressBar.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(false);
        heartContainer.gameObject.SetActive(false);
        gameOverPanel.SetActive(false);
        overlayImage.gameObject.SetActive(false);
    }

    // 人外画像を表示するか否かのクラス
    public void ShowIsJingai(bool isVisible)
    {
        overlayImage.gameObject.SetActive(isVisible);
    }

    // ゲームUIの表示・非表示を切り替える
    public void SetGameUIVisible(bool isVisible)
    {
        heartContainer.gameObject.SetActive(isVisible);
        progressBar.gameObject.SetActive(isVisible);
        countText.gameObject.SetActive(isVisible);
        judgeLine.gameObject.SetActive(isVisible);
    }

    // ゲームオーバーパネルーーーーーーーーーーーーーーーー
    public void GameOverPanel(bool isVisible)
    {
        gameOverPanel.SetActive(isVisible);
        Debug.Log("GameOverPanel is " + isVisible);
    }

    // カウントダウンテキストーーーーーーーーーーーーーーーー
    public void CountdownTextVisible(bool isVisible)
    {
        countdownText.gameObject.SetActive(isVisible);
    }
    // カウントダウンテキストの更新
    public void UpdateCountdownText(string text)
    {
        countdownText.text = text;
    }

    // countText ーーーーーーーーーーーーーーーーーーー
    public void UpdateCountText(string text)
    {
        countText.text = text;
    }

    // BackgroundVideo ーーーーーーーーーーーーーーーーー
    public void BackgroundVideoVisible(bool isVisible)
    {
        BackgroundVideo.SetActive(isVisible);
    }
    // BackgroundVideoの再生・停止
    public void BackgroundVideoPlay(bool play)
    {
        // video再生と停止
        if (play == true)
        {
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Pause();
        }
    }

    // webカメラ ーーーーーーーーーーーーーーーー
    public void WebCamVisible(bool isVisible)
    {
        webCamController.gameObject.SetActive(isVisible);
    }

    // progressBar ーーーーーーーーーーーーーーーー

    // 進捗バーの最大値を設定
    public void SetProgressBarMaxValue(float maxValue)
    {
        progressBar.maxValue = maxValue;
    }
    // 進捗バーの値を更新
    public void UpdateProgressBar(float value)
    {
        progressBar.value = value;
    }



}