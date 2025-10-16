// ===============================
// StoryManager.cs
// ===============================
// このスクリプトはストーリー進行・UI表示・YOLO連携などを一括管理します。
// 初心者でも分かりやすいように、役割ごとに大きくコメントで区切っています。

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
    // 全文表示後に進行待ち状態かどうか
    private bool waitForNextProgress = false;
    // ======【ストーリーデータ・UI要素の参照】======
    [SerializeField] private StoryData[] storyDatas; // ストーリー進行用データ配列
    private static readonly WaitForSeconds waitForOneSecond = new WaitForSeconds(1f); // 1秒待機用

    // 画面上の各UI要素
    [SerializeField] private Image background;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private TextMeshProUGUI characterName;

    // ======【進行管理用の変数】======
    public int storyIndex { get; private set; } // 現在のストーリー番号
    public int textIndex { get; private set; }  // 現在のテキスト番号
    private int enterPressCount = 1;            // Enterキー押下回数
    private bool typingtext = false;            // タイプ中フラグ

    // ======【連携・効果音・カメラ】======
    [SerializeField] private SoundManager soundManager; // BGM・SE管理
    [SerializeField] private RunYOLO runYOLO;           // YOLO推論連携
    [SerializeField] private Image overlayImage;        // 人外時のオーバーレイ
    [SerializeField] private AudioSource seSoundManager = default; // SE再生用
    [SerializeField] private AudioClip apperClip;       // 人外SE


    // ===============================
    // 1. 初期化処理（Start）
    // ===============================
    private void Start()
    {
        // UI初期化
        if (overlayImage != null) overlayImage.gameObject.SetActive(false);
        if (storyText != null) storyText.text = "";
        if (characterName != null) characterName.text = "";
        // 最初のストーリー要素を表示
        SetStoryElement(storyIndex, textIndex);
    }


    // ===============================
    // 2. 毎フレームの入力監視（Update）
    // ===============================
    private void Update()
    {
        if (Keyboard.current == null) return;

        // Enterキーが押されたときの挙動を整理
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (typingtext)
            {
                // タイプ中のみ全文即表示フラグを立てる
                enterPressCount = 2;
            }
            else if (waitForNextProgress)
            {
                // 全文表示後の進行待ち状態なら、ここでストーリー進行
                waitForNextProgress = false;
                StartCoroutine(Progressionstory(storyIndex));
            }
            // typingtext==false かつ waitForNextProgress==false のときは何もしない
        }
    }


    // ===============================
    // 3. ストーリー要素のセット・UI反映
    // ===============================
    private void SetStoryElement(int _storyIndex, int _textIndex)
    {
        // BGM再生
        if (soundManager != null)
            soundManager.PlayBGM(storyDatas[_storyIndex].bgm);

        // storyDatasのx番目のstoryIndexの、Y番目のstoriesの中身を取り出す
        var storyElement = storyDatas[_storyIndex].stories[_textIndex];

        // 背景画像・キャラ画像・名前をUIに反映
        if (background != null)
            background.sprite = storyElement.Background;

        if (characterImage != null)
        {
            if (storyElement.CharacterImage != null)
            {
                characterImage.gameObject.SetActive(true);
                characterImage.sprite = storyElement.CharacterImage;
            }
            else
            {
                characterImage.gameObject.SetActive(false);
            }
        }

        if (characterName != null)
            characterName.text = storyElement.CharacterName;

        // テキストをタイプライター演出で表示
        StartCoroutine(TypeSentence(_storyIndex, _textIndex));
    }


    // ===============================
    // 4. ストーリー進行・YOLO判定コルーチン
    // yolo判定、changestoryElementへの移行を担当
    // ===============================
    private IEnumerator Progressionstory(int storyIndex)
    {
        if (storyIndex == 0)
        {
            // YOLOJudgmentManagerを使用して判定処理を実行
            yield return StartCoroutine(YOLOJudgmentManager.ExecuteYOLOJudgment(
                runYOLO,
                overlayImage,
                seSoundManager,
                apperClip,
                () => {
                    // 完了時のコールバック
                    Debug.Log("YOLO判定完了 - ChangeStoryElementを呼び出します");
                    ChangeStoryElement();
                }
            ));
        }
        else
        {
            // storyIndex==1以外は即ChangeStoryElement
            ChangeStoryElement();
            Debug.Log("即changeStoryElement呼び出しました");
        }
    }


    // ===============================
    // 5. ストーリー切り替え・シーン遷移
    // ===============================
    private async void ChangeStoryElement()
    {
        typingtext = true; // 入力ロック開始
        textIndex = 0;
        int nextStory = storyIndex + 1;
        Debug.Log("ストーリー番号: " + nextStory);

        // storyDatas がない。または nextStory が範囲を超えた場合は Game シーンへ遷移
        if (storyDatas == null || nextStory >= storyDatas.Length)
        {
            // 現在アクティブなシーンから GameBootstrap を探す
            GameBootstrap gb = FindFirstObjectByType<GameBootstrap>();
            if (gb != null)
            {
                await gb.LoadScenesFromInspector(); // 非同期メソッドとして呼び出し
            }
            return;
        }

        storyIndex = nextStory;
        SetStoryElement(storyIndex, textIndex);
    }


    // ===============================
    // 6. タイプライター演出（テキストを一文字ずつ表示）
    // ===============================
    private IEnumerator TypeSentence(int _storyIndex, int _textIndex)
    {
        if (storyDatas == null || _storyIndex < 0 || _storyIndex >= storyDatas.Length ||
            _textIndex < 0 || _textIndex >= storyDatas[_storyIndex].stories.Count)
        {
            yield break;
        }

        // 全文を取得しておく
        string fullSentence = storyDatas[_storyIndex].stories[_textIndex].StoryText ?? "";

        typingtext = true;
        Debug.Log("typingtextはtrue");
        bool skip = false;
        foreach (var letter in fullSentence.ToCharArray())
        {
            if (skip) break;
            if (storyText != null) storyText.text += letter;
            for (int i = 0; i < 8; i++) // 8フレーム待つ
            {
                // Enterキー即全文表示
                if (enterPressCount >= 2)
                {
                    Debug.Log("EnterキーがtypeSentenceシークエンス中に押されました");
                    skip = true;
                    break;
                }
                yield return null;
            }
        }

        if (storyText != null) storyText.text = fullSentence;
        enterPressCount = 1; // 次の全文即表示のために初期値1に戻す
        typingtext = false; // 入力ロック解除
        Debug.Log("タイプライター解除");

        // 次のテキストが存在するか判定し、なければ進行待ちフラグを立てる
        textIndex++;
        if (textIndex < storyDatas[storyIndex].stories.Count)
        {
            // 何もしない（次のEnter待ち）
            Debug.Log("次のテキストがあります。Enter待ちです");
        }
        else
        {
            // 全文表示後、次のEnter待ち状態にする
            waitForNextProgress = true;
            Debug.Log(storyIndex + "が現在のstoryIndexです");
            Debug.Log("次のテキストがないため、ストーリー進行フェーズへ進みます（Enter待ち）");
        }
    }
}
