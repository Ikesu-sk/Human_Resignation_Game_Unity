using Unity.VisualScripting;
using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    // スクロール速度(Inspecteorから調節できるように public にする)
    [SerializeField]
    private float scrollSpeed = 0.1f;

    // SpriteRenderコンポーネントを保持する変数
    private Renderer spriteRenderer;

    // 現在のオフセット値を保持する変数
    private Vector2 savedOffset;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // このスクリプトがアタッチされているオブジェクトのRendererコンポーネントを取得
        spriteRenderer = GetComponent<Renderer>();

        // 現在のマテリアルのテクスチャオフセットを初期値として保存
        savedOffset = spriteRenderer.material.mainTextureOffset;

    }

    // Update is called once per frame
    void Update()
    {
        // 時間の経過と共にx方向のオフセットを計算
        // Time.time を使うことで、アプリの実行時間に基づいた一定の動きになる
        // scrollSpeedをかけて速度を調節
        float x = Time.time * scrollSpeed;
        float y = Time.time * 0.5f;

        // y方向は動かさないので、元のオフセット値を維持
        Vector2 offset = new Vector2(x, y);

        // 計算したオフセット値をマテリアルに適用
        spriteRenderer.material.mainTextureOffset = offset;
    }

}
