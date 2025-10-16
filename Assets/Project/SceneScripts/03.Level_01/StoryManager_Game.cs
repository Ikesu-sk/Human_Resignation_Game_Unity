using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System;
#if UNITY_EDITOR
using UnityEditor.U2D.Animation;
#endif

public class StoryManager_Game : MonoBehaviour
{
    [SerializeField] private StoryData[] storyDatas; // StoryData[] = StoryData型の配列。配列にすることで、何個もデータを入れることが出来る

    // -------------　画面上の各要素の参照先を作成 ---------------- 
    // 画面上の各要素の参照先を作成
    [SerializeField] private Image background;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image windowText;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private Image characterName;
    [SerializeField] private TextMeshProUGUI characterText;

    public GameObject gameFinishPanel; //終了用画面

    // --------------------------------------------------

    public int storyIndex { get; private set; } //get; private set;：外から参照できるけど値は入れられないよ　
    public int textIndex { get; private set; }

    // 一回目の文章時のみ、Enterキーを一回押せばfulltextが表示されるようにする。
    private int enterPressCount = 1;
    private bool typingtext = false;

    // ------------- 各スクリプトとの連携 ---------------- 
    [SerializeField] private SoundManager soundManager;
    // RunYOLoクラスの（以下同文）
    [SerializeField] private RunYOLO runYOLO;
    [SerializeField] private GameManager gameManager;
    // --------------------------------------------------

    private void Start()
    {
        // ゲームクリアイベントを購読する　※ +=でイベントに処理を登録すること＝購読
        // gameManager.cs内にある OnGameClear が発火したとき、HandleGameCler関数を実行するという意味
        gameManager.OnGameClear += HandleGameClear;
        gameFinishPanel.gameObject.SetActive(false);
        storyText.text = "";
        characterText.text = "";
        // 初期値は０から呼ばれることになってるとのこと
        SetStoryElement(storyIndex, textIndex);
    }

    void HandleGameClear()
    {
        //ストーリーUIを再表示
        SetUIVisible(true);
        ChangeStoryElement();
        runYOLO.StopYOLO();
        Debug.Log("StoryIndexは" + storyIndex + "に更新されました。ゲームクリアしたみたいです");
    }

    private void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame && !typingtext)
        {
            textIndex++;
            storyText.text = "";
            StartCoroutine(Progressionstory(storyIndex));
        }
    }

    private void SetStoryElement(int _storyIndex, int _textIndex)
    {
        // 自作関数(soundmanager.cs)を実行するコード
        soundManager.PlayBGM(storyDatas[_storyIndex].bgm);

        // storyDatasのx番目のstoryIndexの、Y番目のstoriesの中身を取り出す
        var storyElement = storyDatas[_storyIndex].stories[_textIndex];

        // 画面上にある各要素にstoryDatsから取得したデータを格納
        background.sprite = storyElement.Background;

        // characterimageの中身（画像）が存在した場合、characterImageを表示させる
        if (storyElement.CharacterImage != null)
        {
            characterImage.gameObject.SetActive(true);
            characterImage.sprite = storyElement.CharacterImage;
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }
        characterImage.sprite = storyElement.CharacterImage;
        characterText.text = storyElement.CharacterName;
        StartCoroutine(TypeSentence(_storyIndex, _textIndex));
    }

    // 次のstoryIndex（会話のまとまり）へ進んでいるのか確認する関数
    private IEnumerator Progressionstory(int _storyIndex)
    {
        // textIndexの数が、storyIndexのstoriesの数より小さいときは
        if (textIndex < storyDatas[_storyIndex].stories.Count)
        {
            SetStoryElement(storyIndex, textIndex);
            // Debug.Log("更新しました。" + _storyIndex);

        }
        // textIndexの数が超えたとき、storyIndexが0だった場合
        else if (storyIndex == 0)
        {
            // ゲームを開始させる関数
            gameManager.StartGame();
            runYOLO.StartYOLO();
            // ゲーム中はストーリーUIを非表示にする
            SetUIVisible(false);
        }
        else if (storyIndex == 1)
        {
            gameFinishPanel.gameObject.SetActive(true);
        }
        yield break;
    }

    private void ChangeStoryElement()
    {
        textIndex = 0;
        storyIndex++;
        SetStoryElement(storyIndex, textIndex);
    }

    // タイプライターのように、文字を一文字ずつ表示させる機能
    private IEnumerator TypeSentence(int _storyIndex, int _textIndex)
    {

        // 全文を取得しておく
        string fullSentence = storyDatas[_storyIndex].stories[_textIndex].StoryText;

        // 一文字ずつ分割したものをletterに入れている
        foreach (var letter in fullSentence.ToCharArray())
        {
            typingtext = true;

            storyText.text += letter;
            for (int i = 0; i < 8; i++) // 10フレーム待つ
            {
                // もし、このフレーム中にEnterキーが押されたら
                if (Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    enterPressCount++;

                    if (enterPressCount >= 2)
                    {
                        // 全文を即座に表示
                        storyText.text = fullSentence;
                        break;
                    }
                }
                yield return null;
            }
        }

        storyText.text = fullSentence;
        enterPressCount = 0;
        typingtext = false;
    }

    // ストーリーUIの表示/非表示の制御をする関数
    private void SetUIVisible(bool isVisible)
    {
        windowText.gameObject.SetActive(isVisible);
        storyText.gameObject.SetActive(isVisible);
        characterName.gameObject.SetActive(isVisible);
        characterText.gameObject.SetActive(isVisible);
        characterImage.gameObject.SetActive(isVisible);
    }

    public void ReturnTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
