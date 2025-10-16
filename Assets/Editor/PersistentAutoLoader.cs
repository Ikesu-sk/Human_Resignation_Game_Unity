using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PersistentAutoLoader
{
    // Persistentシーンのパス（自分の環境に合わせて）
    private const string PERSISTENT_SCENE_PATH = "Assets/Project/Scenes/Common/Presistent.unity";

    static PersistentAutoLoader()
    {
        // Playモードの開始／終了時を監視
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 再生開始直前に呼ばれるタイミング
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // すでにPersistentが開かれていないならAdditiveで開く
            var scene = EditorSceneManager.GetSceneByPath(PERSISTENT_SCENE_PATH);
            if (!scene.isLoaded)
            {
                Debug.Log("[PersistentAutoLoader] Loading Persistent scene additively...");
                EditorSceneManager.OpenScene(PERSISTENT_SCENE_PATH, OpenSceneMode.Additive);
            }
        }
    }
}
