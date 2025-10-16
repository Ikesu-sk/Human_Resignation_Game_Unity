using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem.LowLevel;
using System;

// 敵の設定＆エネミーを動かす＆判定バーに到達したことを知らせるスクリプト
public class Enemy2D : MonoBehaviour
{
    // 敵が到達した際の結果を通知するイベント。
    // 引数1: Enemy2D型 - イベントを発火した敵オブジェクト自身。
    // 引数2: EnemyOutcome型 - 敵が到達した際の結果（例: Alien, Human など）。
    public event Action<Enemy2D, EnemyOutcome> OnResolved; 
    
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


    // -------------　効果音 ----------------
    private AudioSource audioSource;
    // GameManager.csからRunYOLO classを受け取る関数
    public void SetAudioSource(AudioSource r)
    {
        audioSource = r;
    }

    [Header("効果音クリップ")]
    [SerializeField] private AudioClip apperClip;

    // --------------------------------------------------

    // 配列の前にあるスプライトが出やすくなるランダム選択を実現する関数
    // Spriteが戻り値のため、関数の先頭に Sprite と記述されている
    Sprite GetWeightedRandomSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;

        // 0〜1のランダム値
        float r = UnityEngine.Random.value;

        // 配列のインデックスを決める（前の方ほど出やすい）
        int idx = Mathf.FloorToInt(Mathf.Pow(r, 2) * sprites.Length);
        if (idx >= sprites.Length) idx = sprites.Length - 1; // 安全策
        return sprites[idx];
    }


    // GameManager.csから敵が生成されるたびに実行
    public void Initialize(Transform target, bool alienFlag, float duration, float scale)
    {

            if (audioSource != null && apperClip != null)
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
                .OnComplete(EnemyEncounter);
        }
    }

    /// エネミーが判定バーに到達したときの結果通知処理
    private void EnemyEncounter()
    {
        // 判定して結果を outcome にまとめる
        // isAlien が true の場合は人外（Alien）、false の場合は人間（Human）として結果を設定
        EnemyOutcome outcome = isAlien ? EnemyOutcome.Alien : EnemyOutcome.Human;

        // イベント購読者がいれば、引数に入れた情報を渡して呼び出す
        OnResolved?.Invoke(this, outcome);

        // 敵オブジェクトをシーンから削除
        Destroy(gameObject);
    }
    
    void OnDisable()
    {
        // DOTween使ってるなら保険で止めとく
        transform.DOKill();
    }
}
