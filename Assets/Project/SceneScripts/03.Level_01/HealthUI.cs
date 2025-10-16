using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab; //一個分のハートUIのプレハブ
    [SerializeField] private Transform heartContainer;// ハートを並べる親（horizontal layout Group）
    [SerializeField] private int maxHealth = 3;// 体力の最大値

    // ------------- GamaManager.csとの連携 -------------
    [SerializeField] private GameManager gameManager;
    // --------------------------------------------------

    private int currentHealth;

    // List<GameObject>：ハートを入れるための入れ物
    // new List<GameObject>()：空っぽの箱を新しく作る
    private List<GameObject> hearts = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetHearts();
    }

    public void ResetHearts()
    {
        // 残ってるハートを全部削除
        foreach (var heart in hearts)
        {
            Destroy(heart);
        }
        hearts.Clear();

        // 体力を初期値に戻す
        currentHealth = maxHealth;

        // 最大体力ぶんのハートを生成して、リストに登録
        for (int i = 0; i < currentHealth; i++)
        {
            // Instantiate：複製して実際に画面に出す命令
            // heartPrefab：あらかじめ用意したハートの設計図
            // heartContainer：ハートの置き場所
            GameObject newHeart = Instantiate(heartPrefab, heartContainer);
            hearts.Add(newHeart);
        }
    }

    public void TakeDamage(int damage = 1)
    {
        currentHealth -= damage;

        if (currentHealth < 0) currentHealth = 0;// もし体力がマイナスになったら強制的に0にする

        // ダメージの数だけ処理を繰り返す
        for (int i = 0; i < damage; i++)
        {
            // heartsというリストの数が０より多い、つまりハートが残っている場合に処理を行う
            if (hearts.Count > 0)
            {
                int lastIndex = hearts.Count - 1; // リストの最後の要素のインデックス数を取得
                GameObject heartToRemove = hearts[lastIndex];// 削除するハートのゲームオブジェクトを取得
                hearts.RemoveAt(lastIndex);// リストから最後のハートを削除
                Destroy(heartToRemove); // そのゲームオブジェクトを破棄
            }
        }
        if (currentHealth == 0)
        {
            Debug.Log("Game over!");
            gameManager.GameOver();
        }
    }
}
