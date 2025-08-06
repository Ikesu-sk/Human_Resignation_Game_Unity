using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.UI;

public class WebCamController : MonoBehaviour
{
    // webカメラ映像をRawImageに割り当てるための変数
    public RawImage webcamDisplay;
    // webカメラのテクスチャを保持する変数。これは新しく作ったインスタンス変数（ただの設計図）
    // 左側：Unityにもともと備わっているクラス。右側：自分で作った変数名
    // この箱には WebCamTexture という種類のデータしか入れられません。この箱に _cameraTexture という名前をつけます
    private WebCamTexture _cameraTexture;

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

         // WebCamTextureを初期化し「_cameraTexture」変数に格納(どのカメラを使うか、解像度、フレームレート)
        _cameraTexture = new WebCamTexture(deviceName, 1280, 720, 30);

        // RawImageのテクスチャにWebCamTextureを割り当てる
        webcamDisplay.texture = _cameraTexture;

        // 映像の向きが反転してしまうことがあるので補正する
        webcamDisplay.transform.localScale = new Vector3(-1, 1, 1);

        // カメラを起動
        _cameraTexture.Play();

        Debug.Log(deviceName + "のカメラを起動しました");
        
    }

    void OnDestroy()
    {
        //アプリケーション終了時に _cameraTextureがnullではなく、かつカメラが起動中であれば停止させる
        if (_cameraTexture != null && _cameraTexture.isPlaying)
        {
            _cameraTexture.Stop();
        }
    }
}
