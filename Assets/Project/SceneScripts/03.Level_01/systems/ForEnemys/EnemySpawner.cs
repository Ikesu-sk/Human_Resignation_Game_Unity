using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemySpawner - 敵生成管理クラス
/// 
/// 【主な役割】
/// <summary>
/// EnemySpawner - 敵生成管理クラス
/// 
/// 【主な役割】
/// 
/// 1. 敵キャラクターの定期的な生成を行うクラス
/// </summary>
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("敵キャラクターの定期的な生成を行うクラス")]
    public GameObject enemyPrefab; // 敵キャラのプレハブ
    public Transform spawnPoint; // 敵の生成位置
    public Transform[] targetPoints; // 敵が移動する目標地点
    public RunYOLO runYOLO; // YOLO推論スクリプト
    // GameManager.csからRunYOLO classを受け取る関数
    public void SetRunYOLO(RunYOLO r)
    {
        runYOLO = r;
    }

    public AudioSource audioSource; // オーディオソース
    private void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
    [SerializeField] private AudioClip appearClip;
    
    public EncounterJudge encounterJudge; // 敵との遭遇判定クラス

    private List<GameObject> enemies = new List<GameObject>(); // 生成された敵リスト

    


    //------------------------------

    // 敵を生成するための関数
    public IEnumerator SpawnEnemies(bool gameCleard)
    {
        // ゲームクリア状態にならない限り、敵を生成し続ける
        while (!gameCleard)
        {
            // 敵を生成する間隔を、1秒~5秒の間でランダムに設定
            float interval = UnityEngine.Random.Range(1f, 5f);
            Debug.Log("intervalは" + interval + "秒でした");

            yield return new WaitForSeconds(interval); //コルーチンを一時停止し、指定された時間後に処理を再開させる命令

            // enemyPrefabを生成（インスタンス化）するための関数
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity); //newEnemyは生成されたオブジェクトを格納するための変数。引数の中身 →（生成する敵のプレハブ, 生成する位置, 生成オブジェクトの回転をなしに設定）
            // newEnemy（↑これ）というGameObjectにアタッチされているEnemy（というクラス名の）コンポーネントを取得する
            Enemy2D enemy = newEnemy.GetComponent<Enemy2D>();

            // RunYOLOをEnemy.csに受け渡す（enemyに直接アタッチできないため、ここで渡す）
            //enemy.SetRunYOLO(runYOLO);

            // targetPointsの中から（ランダムに）１つ到達座標を選ぶ
            int idx = UnityEngine.Random.Range(0, targetPoints.Length);
            Transform selectedTarget = targetPoints[idx];
            // Debug.Log(idx + "が到達座標に選ばれました");

            // 乱数で人外判定
            bool alienFlag = (UnityEngine.Random.Range(0, 2) == 0);//50%の確率で人外になる。数値が0なら人外、1なら人間になる

            // 到達時間を設定する
            // float duration = Random.Range(5f, 10f);
            float duration = 1f;

            // 移動後のscaleの大きさを設定
            float scale = 1f;

            // 敵が出現したときの音を再生
            PlaySound(appearClip);

            // Enemy 側に初期化を渡す
            enemy.Initialize(selectedTarget, alienFlag, duration, scale);

            // EncounterJudgeが設定されている場合、敵のOnResolvedイベントに登録
            if (encounterJudge != null)
            {
                // enemyのOnResolvedイベントにEncounterJudgeのEnemyResolved関数を登録
                // OnResolvedが実行されたときに、EnemyResolvedが呼び出されるようになる
                enemy.OnResolved += encounterJudge.EnemyResolved;
            }
            else
            {
                Debug.LogWarning("EncounterJudgeが設定されていません。インスペクターで設定してください。");
            }

            // 生成した敵をリストに追加していく
            enemies.Add(newEnemy);
        }
    }

    public void ClearEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false);
            }
        }
        enemies.Clear();
    }
}