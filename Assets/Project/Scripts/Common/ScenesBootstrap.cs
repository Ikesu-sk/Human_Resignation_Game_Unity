using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEditor;

/// <summary>
/// ゲーム開始時に共通シーン（UI, Audio）と最初のシーンをAdditiveで自動読み込みするクラス。
/// 他スクリプトからは以下のように呼び出せます:
    // --- 例 ---
    /*
    // 現在アクティブなシーンから GameBootstrap を探す
    GameBootstrap gb = FindFirstObjectByType<GameBootstrap>();
    if (gb != null)
    {
        await gb.LoadScenesFromInspector(); // gbが見つかっていた場合の処理
    }
    */
    // --- ここまで ---
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] SceneAsset[] sceneAssets; // シーンアセットをドラッグ＆ドロップ
    private string[] scenePaths; // シーンアセットのパスを格納する配列
    private int currentSceneIndex = 0; // 現在のシーンインデックスを追跡
    [SerializeField, Range(0, 5)] private int debugStartElementIndex = 0; // デバッグ用にスライダーで開始要素のインデックスを設定

    /// ゲーム開始時にasync/awaitを使ってシーンの読み込み完了を待つ
    async void Start()
    {
        // シーンアセットからパスを取得
        scenePaths = new string[sceneAssets.Length];
        for (int i = 0; i < sceneAssets.Length; i++)
        {
            scenePaths[i] = AssetDatabase.GetAssetPath(sceneAssets[i]);
        }

        // 全ゲームで共通して使用するシーンを配列で定義
        // UI: ユーザーインターフェース関連
        // Audio: 音響システム関連
        string[] commonScenes = {
            "Assets/Project/Scenes/Common/UI.unity",
            "Assets/Project/Scenes/Common/Audio.unity"
        };

        // （現在のシーンに）共通シーンを順番に読み込み
        foreach (var path in commonScenes)
        {
            // LoadSceneAsync: シーンを非同期で読み込む
            // LoadSceneMode.Additive: 既存のシーンを消さずに追加で読み込み
            var op = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);

            // null チェック: ビルド設定にシーンが登録されていない場合nullが返る
            if (op != null)
                await op; // 読み込み完了まで待機
        }

        // デバッグ用の開始インデックスを適用
        currentSceneIndex = debugStartElementIndex;

        // インスペクターで指定された最初のシーンを読み込み
        await LoadScenesFromInspector();
    }
    
    // インスペクターからシーンをAdditiveで読み込む＆前のシーンを削除するメソッド
    public async Task LoadScenesFromInspector()
    {
        Debug.Log($"今から行うLoading scene: {scenePaths[currentSceneIndex]}");
        // 新しく表示するシーンをロード
        var op = SceneManager.LoadSceneAsync(scenePaths[currentSceneIndex], LoadSceneMode.Additive);
        if (op != null)
            await op; // 読み込み完了まで待機

        // 最初のシーン以外の場合、前のシーンをアンロード
        if (currentSceneIndex != 0)
        {
            // 前回表示していたシーンを取得
            Scene beforeScene = SceneManager.GetSceneByPath(scenePaths[currentSceneIndex - 1]);
            // 前回のシーンをアンロード
            if (beforeScene.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(beforeScene);
                if (unloadOp != null)
                    await unloadOp; // アンロード完了まで待機
            }
        }
        // インデックスを次に進める
        currentSceneIndex++;
    }
}