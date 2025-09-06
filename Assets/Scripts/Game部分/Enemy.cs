using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem.LowLevel;

// 敵エネミーを動かす＆判定時の処理を記述したスクリプト
public class Enemy : MonoBehaviour
{
    // -------------　敵キャラ生成用 ----------------
    private Transform targetPoint;
    private float moveDuration;
    private float endScale;
    public bool isAlien;

    private Renderer rend;// Cube のRendererを取得
    public Sprite humanSprite;
    public Sprite alienSprite;
    private SpriteRenderer spriteRenderer;
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
    // --------------------------------------------------



    // GameManager.csから敵が生成されるたびに実行
    public void Initialize(Transform target, bool alienFlag, float duration, float scale)
    {
        targetPoint = target; // どこまで移動するのか
        isAlien = alienFlag;
        moveDuration = duration; //移動にかかる時間（例：3f → 3秒かけて移動）
        endScale = scale; // 移動後のscaleの大きさ

        // cubeのrendrerを取得
        rend = GetComponent<Renderer>();

        // isAlienに応じて色を変更
        if (isAlien)
            rend.material.color = Color.red; //人外なら赤

        else
            rend.material.color = Color.blue;// 人間なら青


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
            Debug.Log("相手が人外だったのにあなたは人間だった。１ダメージを受ける");
        }
        // 検出数が１以上（人間）かつEnemyが人間だった場合
        else if (currentBoxes > 0 && !isAlien)
        {
            Debug.Log("相手が人間だったがあなたも人間だった。上手く乗り切ることができた");
        }
        // 検出数が0（人外）かつEnemyが人外だった場合
        else if (currentBoxes == 0 && isAlien)
        {
            Debug.Log("相手は人外だったがあなたも人外だった。上手く乗り切ることができた");
        }
        // 検出数が0（人外）かつEnemyが人間だった場合
        else
        {
            healthUI.TakeDamage();
            Debug.Log("相手が人間だったのにあなたは人外だった。１ダメージを受ける");
        }

        Destroy(gameObject);
    }
}
