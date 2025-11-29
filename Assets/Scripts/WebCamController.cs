using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.UI;

public class WebCamController : MonoBehaviour
{
    // webカメラのテクスチャを保持する変数。これは新しく作ったインスタンス変数（ただの設計図）
    // 左側：Unityにもともと備わっているクラス。右側：自分で作った変数名
    // この箱には WebCamTexture という種類のデータしか入れられません。この箱に CameraTexture という名前をつけます
    public WebCamTexture CameraTexture{ get; private set; } // { get; private set; } により、他のスクリプトからは読み取りのみ可能になります。
    
    void Start()
    {
        // PCに接続されているカメラデバイスを取得。（"WebCamTexture"クラスの .devices から）
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("カメラが見つかりません");
            return;
        }

        // 最初のカメラを使用する
        string deviceName = devices[0].name;

         // WebCamTextureを初期化し「CameraTexture」変数に格納(どのカメラを使うか、解像度、フレームレート)
        CameraTexture = new WebCamTexture(deviceName, 1280, 720, 30);

        // カメラを起動
        CameraTexture.Play();

        Debug.Log(deviceName + "のカメラを起動しました");
        
    }

    void OnDestroy()
    {
        //アプリケーション終了時に CameraTextureがnullではなく、かつカメラが起動中であれば停止させる
        if (CameraTexture != null && CameraTexture.isPlaying)
        {
            CameraTexture.Stop();
        }
    }
}
