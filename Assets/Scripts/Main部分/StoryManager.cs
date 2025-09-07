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

public class StoryManager : MonoBehaviour
{
    [SerializeField] private StoryData[] storyDatas; // StoryData[] = StoryData型の配列。配列にすることで、何個もデータを入れることが出来る

    // 画面上の各要素の参照先を作成
    [SerializeField] private Image background;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private TextMeshProUGUI characterName;

    public int storyIndex { get; private set; } //get; private set;：外から参照できるけど値は入れられないよ　
    public int textIndex { get; private set; }

    // 一回目の文章時のみ、Enterキーを一回押せばfulltextが表示されるようにする。
    private int enterPressCount = 1;
    private bool typingtext = false;

    // SoundManagerクラスのscriptを受け付けるためのコード
    [SerializeField] private SoundManager soundManager;
    
    // RunYOLoクラスの（以下同文）
    [SerializeField] private RunYOLO runYOLO;
    // -------------　webカメラテクスチャとの連携　 ----------------
    [SerializeField] private WebCamController webCamController;
    [SerializeField] private Image overlayImage;

    // -------------　人外になったときの効果音　 ----------------
    [SerializeField] private AudioSource seSoundManager = default;

    [SerializeField] private AudioClip apperClip;
    // -----------------------------

    private void Start()
    {
        overlayImage.gameObject.SetActive(false);
        storyText.text = "";
        characterName.text = "";
        // 初期値は０から呼ばれることになってるとのこと
        SetStoryElement(storyIndex, textIndex);
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
        characterName.text = storyElement.CharacterName;
        StartCoroutine(TypeSentence(_storyIndex, _textIndex));
    }

    // 次のstoryIndex（会話のまとまり）へ進んでいるのか確認する関数
    private IEnumerator Progressionstory(int _storyIndex)
    {
        // textIndexの数が、storyIndexのstoriesの数より小さいときは
        if (textIndex < storyDatas[_storyIndex].stories.Count)
        {
            SetStoryElement(storyIndex, textIndex);
        }
        // textIndexの数がstoryIndexのstoriesの数を超えたかつ、storyIndexが0だった場合
        else if (_storyIndex == 0)
        {
            // 1秒待ってから実行
            yield return new WaitForSeconds(1f);
            
            runYOLO.StartYOLO();

            // ①BoxesFound = ボックス検出数が1以上になるまで待機
            while (runYOLO.BoxesFound < 1)
            {
                Debug.Log("人として検出されてないです");
                yield return null;
            }

            // ②BoxesFound が 3回連続で 0 になるまで待機
            int zeroCount = 0;

            while (zeroCount < 3)
            {
                if (runYOLO.BoxesFound == 0)
                {
                    zeroCount++;
                    Debug.Log($"人外カウント {zeroCount}/3");
                }
                else
                {
                    // 途中で1以上検出されたらリセット
                    if (zeroCount > 0)
                    {
                        Debug.Log("人が検出されたのでリセット");
                    }
                    zeroCount = 0;
                }

                yield return null; // 次のフレームまで待機
            }

            Debug.Log("人外になれました！（3回連続で0検出）");
            overlayImage.gameObject.SetActive(true);
            seSoundManager.clip = apperClip;
            seSoundManager.Play();

            // 検出数が0になったのでStoryチェンジを実行する
            // 推論停止
            runYOLO.StopYOLO();
            // webカメラの映像更新を停止
            webCamController.StopCamera();
            Debug.Log("webカメラの映像停止しました");
            // ↑こうすることでwebカメラを停止した際のバウンディングボックスを表示したまま、webカメラ画面を停止することができる

            ChangeStoryElement();

        }
        else if (_storyIndex == 1)
        {
            // 「Game」Sceneに移行
            SceneManager.LoadScene("Game");
        }
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
}
