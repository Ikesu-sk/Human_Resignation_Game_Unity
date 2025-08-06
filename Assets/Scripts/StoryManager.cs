using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
using System;

public class StoryManager : MonoBehaviour
{
    [SerializeField] private StoryData[] storyDatas; // storyData[]を呼んでおく

    // 画面上の各要素の参照先を作成
    [SerializeField] private Image background;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private TextMeshProUGUI characterName;


    public int storyIndex { get; private set; } //get; private set;：外から参照できるけど値は入れられないよ　
    public int textIndex { get; private set; }

    private bool finishText = false;

    // SoundManagerクラスのscriptを受け付けるためのコード
    [SerializeField] private SoundManager soundManager;

    private void Start()
    {
        storyText.text = "";
        characterName.text = "";
        // 初期値だと０から呼ばれることになってるとのこと
        SetStoryElement(storyIndex, textIndex);
    }

    private void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame && finishText)
        {
            textIndex++;
            storyText.text = "";
            Progressionstory(storyIndex);
        }
    }

    private void SetStoryElement(int _storyIndex, int _textIndex)
    {
        // 自作関数(soundmanager.cs)を実行するコード
        soundManager.PlayBGM(storyDatas[_storyIndex].bgm);

        var storyElement = storyDatas[_storyIndex].stories[_textIndex];

        // 画面上にある各要素にstoryDatsから取得したデータを格納
        background.sprite = storyElement.Background;
        characterImage.sprite = storyElement.CharacterImage;
        characterName.text = storyElement.CharacterName;
        // storyText.text = storyElement.StoryText;
        StartCoroutine(TypeSentence(_storyIndex, _textIndex));
    }

    private void Progressionstory(int _storyIndex)
    {
        // textIndexの数が、storyIndexのstoriesの数より小さいときは
        if (textIndex < storyDatas[_storyIndex].stories.Count)
        {
            SetStoryElement(storyIndex, textIndex);
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

    private IEnumerator TypeSentence(int _storyIndex, int _textIndex)
    {

        finishText = false; // ← ここで一度リセット！

        // 一文字ずつ分割したものをletterに入れている
        foreach (var letter in storyDatas[_storyIndex].stories[_textIndex].StoryText.ToCharArray())
        {
            storyText.text += letter;
            yield return new WaitForSeconds(0.1f);
        }

        finishText = true;
    }    
}
