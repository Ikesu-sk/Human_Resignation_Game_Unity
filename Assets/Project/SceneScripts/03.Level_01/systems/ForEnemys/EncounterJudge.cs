using UnityEngine;

/// 敵が判定バーに到達したときの処理を実行するクラス
/// （Enemy2D の OnResolved イベントを受け取る）
public class EncounterJudge : MonoBehaviour
{
    [SerializeField, TextArea]
    private string infoText = "このスクリプトはYOLO推論を使用して敵の判定を行います。";

    [Header("推論スクリプト")]
    [SerializeField] private RunYOLO runYOLO; // YOLO推論スクリプト
    
    [Header("効果音クリップ")]
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioSource audioSource;
    private void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
    [Header("HP管理用")]
    [SerializeField] private HealthUI healthUI;
    
    void Start()
    {
        // healthUIが設定されていない場合、シーンから自動で見つける
        if (healthUI == null)
        {
            healthUI = FindFirstObjectByType<HealthUI>();
            if (healthUI == null)
            {
                Debug.LogError("HealthUIが見つかりません。インスペクターで設定するか、シーンにHealthUIオブジェクトを配置してください。");
            }
        }
    }

    // 防御判定の結果をほかのスクリプト通知したい場合に使うイベント
    //public event System.Action<Enemy2D, EnemyOutcome> OnEnemyResolved;

    /// エネミーが判定バーに到達したときの結果通知処理
    public void EnemyResolved(Enemy2D enemy, EnemyOutcome outcome)
    {
        // 判定して結果を outcome にまとめる
        int currentBoxes = runYOLO.BoxesFound;
        bool playerIsHuman = currentBoxes > 0;

        // outcome に応じた処理を実行
        if (outcome == EnemyOutcome.Alien && !playerIsHuman || outcome == EnemyOutcome.Human && playerIsHuman)
        {
            PlaySound(successClip);
            Debug.Log("Correct!");
            // ダメージを受けない処理
        }
        else
        {
            healthUI.TakeDamage();
            PlaySound(damageClip);
            Debug.Log("Alien detected! プレイヤーにダメージを与えます。");
            // プレイヤーにダメージを与える処理
        }
    }
}
