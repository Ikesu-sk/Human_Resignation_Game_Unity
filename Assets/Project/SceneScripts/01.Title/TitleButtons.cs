using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleBtton : MonoBehaviour
{
    public async void StartBtn()
    {
        // 現在アクティブなシーンから GameBootstrap を探す
        GameBootstrap gb = FindFirstObjectByType<GameBootstrap>();
        if (gb != null)
        {
            await gb.LoadScenesFromInspector(); // gbが見つかっていた場合の処理
        }
    }
}
