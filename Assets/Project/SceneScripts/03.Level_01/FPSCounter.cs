using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private int frameCount = 0;
    private float elapsedTime = 0f;

    void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime; // 経過時間（リアルタイム）

        if (elapsedTime >= 1f) // 1秒ごとに実行
        {
            int fps = Mathf.RoundToInt(frameCount / elapsedTime);
            Debug.Log("FPS: " + fps);

            // カウントをリセット
            frameCount = 0;
            elapsedTime = 0f;
        }
    }
}
