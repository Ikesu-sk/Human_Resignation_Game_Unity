using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem.LowLevel;

// 敵エネミーを動かす＆判定時の処理を記述したスクリプト
public class Enemy2D : MonoBehaviour
{
    // -------------　敵キャラ生成用 ----------------
    private Transform targetPoint;
    private float moveDuration;
    private float endScale;
    public bool isAlien;

    private Renderer rend;// Cube のRendererを取得
    public Sprite[] alienSprites;

    public Sprite[] humanSprites;

    // プレハブ自身が持っている SpriteRenderer（spriteを表示する機能）を割り当てておく必要がある
    [SerializeField] SpriteRenderer spriteRenderer;
    // -------------　yolo推論用 ----------------

    private RunYOLO runYOLO;

    // GameManager.csからRunYOLO classを受け取る関数
    public void SetRunYOLO(RunYOLO r)
    {
        runYOLO = r;
        Debug.Log("RunYOLOをEnemyにセットしました: " + (runYOLO != null));
    }

    // -------------　HP管理用 ----------------
    private HealthUI healthUI;

    // GameManager.csからHealthUI classを受け取る関数
    public void SetHealthUI(HealthUI r)
    {
        healthUI = r;
    }

    // -------------　効果音 ----------------

    private AudioSource audioSource;
    // GameManager.csからRunYOLO classを受け取る関数
    public void SetAudioSource(AudioSource r)
    {
        audioSource = r;
        Debug.Log("RunYOLOをEnemyにセットしました: " + (runYOLO != null));
    }

    [Header("効果音クリップ")]
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip apperClip;

    // --------------------------------------------------

    // 配列の前にあるスプライトが出やすくなるランダム選択を実現する関数
    // Spriteが戻り値のため、関数の先頭に Sprite と記述されている
    Sprite GetWeightedRandomSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;

        // 0〜1のランダム値
        float r = Random.value;

        // 配列のインデックスを決める（前の方ほど出やすい）
        int idx = Mathf.FloorToInt(Mathf.Pow(r, 2) * sprites.Length);
        if (idx >= sprites.Length) idx = sprites.Length - 1; // 安全策
        return sprites[idx];
    }


    // GameManager.csから敵が生成されるたびに実行
    public void Initialize(Transform target, bool alienFlag, float duration, float scale)
    {

            if (audioSource != null && damageClip != null)
            {
                Debug.Log("出現時に音を鳴らします");
                audioSource.PlayOneShot(apperClip);
            }
    
        targetPoint = target; // どこまで移動するのか
        isAlien = alienFlag;
        moveDuration = duration; //移動にかかる時間（例：3f → 3秒かけて移動）
        endScale = scale; // 移動後のscaleの大きさ

        // cubeのrendrerを取得
        rend = GetComponent<Renderer>();

        // isAlienに応じてテクスチャを変更
        if (isAlien)
        {
            int idx = UnityEngine.Random.Range(0, alienSprites.Length);
            spriteRenderer.sprite = alienSprites[idx];
        }
        else
        {
            // 先頭が出やすくなる関数を使用
            spriteRenderer.sprite = GetWeightedRandomSprite(humanSprites);
        }

        // 移動前のscaleを設定
        transform.localScale = Vector3.one * 1f;

        // targetが渡されたタイミングで移動開始
        if (targetPoint != null)
        {
            transform.DOMove(targetPoint.position, moveDuration).SetEase(Ease.Linear);
            transform.DOScale(Vector3.one * endScale, moveDuration)
                // .OnComplete(() => Destroy(gameObject)); //終了後に削除
                .OnComplete(EnemyReachedTarget);
        }
    }

    // TargetPointに到達したときの処理
    void EnemyReachedTarget()
    {
        if (runYOLO == null)
        {
            Debug.LogError("RunYOLOがnullのままです！");
            return;
        }

        // Enemy到達時のYOLO検出数を取得
        int currentBoxes = runYOLO.BoxesFound;

        // 検出数が1以上（人間）かつEnemyが人外だった場合
        if (currentBoxes > 0 && isAlien)
        {
            healthUI.TakeDamage();
            PlaySound(damageClip);
            Debug.Log("相手が人外だったのにあなたは人間だった。１ダメージを受ける");
        }
        // 検出数が１以上（人間）かつEnemyが人間だった場合
        else if (currentBoxes > 0 && !isAlien)
        {
            PlaySound(successClip);
            Debug.Log("相手が人間だったがあなたも人間だった。上手く乗り切ることができた");
        }
        // 検出数が0（人外）かつEnemyが人外だった場合
        else if (currentBoxes == 0 && isAlien)
        {
            PlaySound(successClip);
            Debug.Log("相手は人外だったがあなたも人外だった。上手く乗り切ることができた");
        }
        // 検出数が0（人外）かつEnemyが人間だった場合
        else
        {
            healthUI.TakeDamage();
            PlaySound(damageClip);
            Debug.Log("相手が人間だったのにあなたは人外だった。１ダメージを受ける");
        }

        Destroy(gameObject);
    }
    
    private void PlaySound(AudioClip clip)
    {
        Debug.Log("音を鳴らしました");
            audioSource.PlayOneShot(clip);

    }
}
