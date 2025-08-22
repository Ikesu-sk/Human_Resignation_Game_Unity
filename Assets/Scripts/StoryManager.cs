using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
using System;
using UnityEditor.U2D.Animation;

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

    private void Start()
    {
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
            // 3秒待ってから実行
            yield return new WaitForSeconds(3f);

            // BoxesFound = ボックス検出数が0の間は繰り返す
            while (runYOLO.BoxesFound == 0)
            {
                yield return null;
                runYOLO.StartYOLO();
                Debug.Log("人検出数：" + runYOLO.BoxesFound);
            }

            // 検出数が１以上になったらシーンチェンジを実行する
            if (runYOLO.BoxesFound > 0)
            {
                ChangeStoryElement();
                Debug.Log("yolo推論終了");
            }
        }
        else
        {
            // シーンチェンジ、選択肢出す、別のScriptableObjectを呼ぶ
            ChangeStoryElement();
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
                    Debug.Log("Enterキーが押されました");

                    if (enterPressCount >= 2)
                    {
                        // 全文を即座に表示
                        storyText.text = fullSentence;
                        Debug.Log("Enterキーが２回押されました");
                        break;
                    }
                }
                yield return null;
            }
        }

        storyText.text = fullSentence;
        Debug.Log("全表示storyText");
        enterPressCount = 0;
        typingtext = false;
    }    
}
