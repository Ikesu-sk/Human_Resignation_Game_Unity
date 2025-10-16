using System.Collections;
using UnityEngine;

/// <summary>
/// WebCamControllerの使い方:
/// 1. このスクリプトを空のGameObjectにアタッチしてください。
/// 2. ゲーム開始時に自動でWebカメラが初期化されます。
/// 3. WebCamController.Instance.CameraTexture でWebカメラの映像にアクセスできます。
/// 4. カメラの再起動は RestartCamera() を呼び出してください。
/// 5. カメラ停止は StopCamera() を呼び出してください。
/// </summary>
public class WebCamController : MonoBehaviour
{
    public static WebCamController Instance { get; private set; }

    public enum CameraType
    {
        Auto,       // 自動選択（OBS優先）
        WebCamera,  // 通常のWebカメラ
        OBS_Virtual // OBS/仮想カメラ
    }

    [Header("カメラ設定")]
    [Tooltip("使用するカメラの種類を選択してください")]
    public CameraType cameraType = CameraType.Auto;

    [Tooltip("手動でデバイス名を指定する場合（空の場合は自動選択）")]
    public string manualDeviceName = "";

    // webCamTexture用の入れ物を作っただけ
    public WebCamTexture CameraTexture { get; private set; }
    private bool isInitializing = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance == null || Instance.gameObject == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(InitializeCamera());
    }

    void OnDestroy()
    {
        StopCamera();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // カメラの初期化を行うコルーチン
    private IEnumerator InitializeCamera()
    {
        if (isInitializing) yield break;
        isInitializing = true;

        if (CameraTexture != null) StopCamera();
        yield return new WaitForSeconds(0.5f);

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("利用可能なカメラデバイスが見つかりません。");
            isInitializing = false;
            yield break;
        }

        string deviceName = GetDeviceName(devices);
        if (string.IsNullOrEmpty(deviceName))
        {
            Debug.LogError("指定された条件に合うカメラデバイスが見つかりません。");
            isInitializing = false;
            yield break;
        }

        Debug.Log($"使用するカメラデバイス: {deviceName}");

        bool isOBSCamera = IsOBSCamera(deviceName);

        if (isOBSCamera)
        {
            // WebCamTextureを初期化し「CameraTexture」変数に格納(どのカメラを使うか、解像度、フレームレート)
            CameraTexture = new WebCamTexture(deviceName, 1280, 720, 30);
        }
        else
        {
            CameraTexture = new WebCamTexture(deviceName, 640, 480, 15);
        }

        CameraTexture.Play();
        isInitializing = false;
    }

    private string GetDeviceName(WebCamDevice[] devices)
    {
        // 手動でデバイス名が指定されている場合
        if (!string.IsNullOrEmpty(manualDeviceName))
        {
            foreach (var device in devices)
            {
                if (device.name.Equals(manualDeviceName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return device.name;
                }
            }
            Debug.LogWarning($"指定されたデバイス '{manualDeviceName}' が見つかりません。自動選択に切り替えます。");
        }

        switch (cameraType)
        {
            case CameraType.Auto:
                // OBS/仮想カメラを優先して探す
                foreach (var device in devices)
                {
                    if (IsOBSCamera(device.name))
                    {
                        return device.name;
                    }
                }
                // 見つからない場合は最初のデバイスを使用
                return devices[0].name;

            case CameraType.WebCamera:
                // OBS/仮想カメラ以外を探す
                foreach (var device in devices)
                {
                    if (!IsOBSCamera(device.name))
                    {
                        return device.name;
                    }
                }
                // 見つからない場合は最初のデバイスを使用
                return devices[0].name;

            case CameraType.OBS_Virtual:
                // OBS/仮想カメラのみを探す
                foreach (var device in devices)
                {
                    if (IsOBSCamera(device.name))
                    {
                        return device.name;
                    }
                }
                // 見つからない場合はnullを返す
                return null;

            default:
                return devices[0].name;
        }
    }

    private bool IsOBSCamera(string deviceName)
    {
        string lowerName = deviceName.ToLower();
        return lowerName.Contains("obs") || lowerName.Contains("virtual") || lowerName.Contains("cam");
    }

    public void StopCamera()
    {
        if (CameraTexture != null)
        {
            if (CameraTexture.isPlaying) CameraTexture.Stop();
            Destroy(CameraTexture);
            CameraTexture = null;
        }
    }

    public void RestartCamera()
    {
        StopCamera();
        isInitializing = false;
        StartCoroutine(InitializeCamera());
    }

    public bool IsCameraWorking()
    {
        return CameraTexture != null && CameraTexture.isPlaying && CameraTexture.width > 16;
    }
}