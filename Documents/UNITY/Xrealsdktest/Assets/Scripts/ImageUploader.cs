using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using NRKernal;

// Certificate handler to allow insecure HTTP connections
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Accept all certificates
    }
}

[Serializable]
public class StreamResponse
{
    public string status;
    public List<string> objects;
    public List<DetectionData> detections;
    public string scene;
}

[Serializable]
public class DetectionData
{
    public string name;
    public float confidence;
    public float[] box; // [x1, y1, x2, y2]
}

public class ImageUploader : MonoBehaviour
{
    [Header("Server Settings")]
    [Tooltip("IP address of your PC running the Python server")]
    public string serverUrlBase = "http://192.168.0.19:5000"; // Home server IP
    
    [Header("Streaming Settings")]
    public float streamInterval = 2.0f; // Send image every 2 seconds (ngrok rate limit対策)
    public bool isStreaming = true;
    
    [Header("Display Settings")]
    public bool showLogByDefault = false; // Set to false to hide console log on startup

    [Header("Camera Settings")]
    public bool useARGlassCamera = true; // ARグラスのRGBカメラを使用するかどうか

    [Header("UI References")]
    public Text debugText;
    public TextMesh debugTextMesh;
    public GameObject settingsPanel;
    public GameObject debugPanel;
    public Text logToggleButtonText;
    public Button settingsButton;
    public Button logToggleButton;
    public Button closeSettingsButton;
    public Button setIpButton;
    public InputField ipInputField;
    
    [Header("Object Recognition UI")]
    public Button objectIdButton; // OBJECT button in top bar
    public Text objectResultText; // Display detected object names
    public GameObject objectIdPanel; // Panel for object registration
    public InputField objectNameInput;
    public Button registerObjectButton;
    public Button closeObjectIdPanelButton;
    public Text objectProgressText; // 進捗表示テキスト (e.g., "3/30")
    public Image objectProgressBar; // 進捗バー
    
    // Object Recognition state
    public bool isObjectIdMode = false; // Object ID mode toggle
    private Coroutine _objectIdCoroutine;
    private string _lastObjectDisplayText = "";
    private float _lastObjectDetectionTime = 0f;
    private const float OBJECT_RESULT_HOLD_TIME = 3.0f;
    private bool _isRegistering = false; // 登録中フラグ
    private const int REGISTRATION_FRAMES = 120; // 登録に使用するフレーム数 (User requested 120)
    
    [Header("Registration Selection")]
    public GameObject registrationSelectPanel; // 顔/物体選択パネル
    public Button closeRegistrationSelectButton; // Added reference
    
    [Header("Beacon Settings")]
    public bool beaconEnabled = true; // ビーコン機能のオン/オフ
    public bool isBeaconVisible = true; // 表示/非表示トグル (位置は更新し続ける)
    public float beaconDistance = 2.0f; // カメラ前方の固定距離
    public Color beaconColor = new Color(0f, 1f, 0.5f, 0.8f); // ネオングリーン
    private Dictionary<string, GameObject> _activeBeacons = new Dictionary<string, GameObject>();
    private const float BEACON_COOLDOWN = 5.0f; // 同じ物体のビーコン生成間隔
    private Dictionary<string, float> _lastBeaconTime = new Dictionary<string, float>();
    
    // WebCamTexture for Phone Camera (fallback)
    private WebCamTexture _webCamTexture;
    private Texture2D _tempTexture;
    
    // NRSDK RGB Camera for AR Glasses
    private NRRGBCamTextureYUV _nrRGBCamTexture;
    private bool _isNRCameraInitialized = false;

    // References
    private BeaconSelectionManager _beaconManager;
    
    // Detection Target Selection (max 3)
    public HashSet<string> SelectedTargets = new HashSet<string>();
    public const int MAX_SELECTIONS = 3;
    public Text selectionCountText;
    
    public bool ToggleTargetSelection(string targetName)
    {
        if (SelectedTargets.Contains(targetName))
        {
            SelectedTargets.Remove(targetName);
            UpdateSelectionCountText();
            Log($"[Target] Deselected: {targetName} ({SelectedTargets.Count}/{MAX_SELECTIONS})");
            return false; // Now deselected
        }
        else
        {
            if (SelectedTargets.Count >= MAX_SELECTIONS)
            {
                Log($"[Target] Cannot select {targetName} - max {MAX_SELECTIONS} reached!");
                return false; // Cannot add more
            }
            SelectedTargets.Add(targetName);
            UpdateSelectionCountText();
            Log($"[Target] Selected: {targetName} ({SelectedTargets.Count}/{MAX_SELECTIONS})");
            return true; // Now selected
        }
    }
    
    public bool IsTargetSelected(string targetName)
    {
        return SelectedTargets.Contains(targetName);
    }
    
    private void UpdateSelectionCountText()
    {
        if (selectionCountText != null)
        {
            selectionCountText.text = $"選択中: {SelectedTargets.Count}/{MAX_SELECTIONS}";
        }
    }
    


    // ============ BEACON VISUALIZATON ============
    // Beacon Prefab (dynamically created if null)
    private GameObject _beaconTextPrefab;

    private void ProcessDetections(List<DetectionData> detections)
    {
        if (detections == null) detections = new List<DetectionData>();
        
        // DEBUG: Show what we received
        Log($"[ProcessDetections] Received {detections.Count} detections, SelectedTargets: {string.Join(",", SelectedTargets)}");
        foreach(var d in detections)
        {
            Log($"[ProcessDetections] Detection: name={d.name}, conf={d.confidence}, hasBox={d.box != null && d.box.Length >= 4}");
        }
        
        // 1. Check for Matches
        foreach (string target in SelectedTargets)
        {
            // Find best match (highest confidence) for this target (case-insensitive)
            DetectionData bestMatch = null;
            float maxConf = 0f;
            
            foreach(var d in detections)
            {
                if (string.Equals(d.name, target, StringComparison.OrdinalIgnoreCase) && d.confidence > maxConf)
                {
                    maxConf = d.confidence;
                    bestMatch = d;
                }
            }

            if (bestMatch != null)
            {
                Log($"[ProcessDetections] MATCH: {target} -> {bestMatch.name} (conf={maxConf})");
                // Found! Show Beacon at specific position
                UpdateBeacon(target, true, bestMatch);
            }
            else
            {
                // Not found. Hide/Remove Beacon
                UpdateBeacon(target, false, null);
            }
        }
        
        // 2. Remove beacons for targets that are no longer selected (cleanup)
        List<string> keysToRemove = new List<string>();
        foreach(var kvp in _activeBeacons)
        {
            if (!SelectedTargets.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach(var key in keysToRemove)
        {
           DestroyBeacon(key);
        }
    }

    private void UpdateBeacon(string objectName, bool isDetected, DetectionData data)
    {
        if (isDetected)
        {
            if (!_activeBeacons.ContainsKey(objectName))
            {
                CreateBeacon(objectName);
            }
            
            // Update timestamp to keep it alive
            _lastBeaconTime[objectName] = Time.time;
            
            GameObject beacon = _activeBeacons[objectName];
            if (beacon != null)
            {
                beacon.SetActive(isBeaconVisible); // Visibility Toggle Loop
                
                // Update text content
                TextMesh tm = beacon.GetComponent<TextMesh>();
                 if (tm != null) tm.text = $"🚨 {objectName.ToUpper()} 🚨";
                 
                 // --- SPATIAL PLACEMENT --
                 if (data != null && data.box != null && data.box.Length >= 4 && Camera.main != null)
                 {
                     // YOLO Box: [x1, y1, x2, y2] normalized (0-1). Origin Top-Left.
                     float x1 = data.box[0];
                     float y1 = data.box[1];
                     float x2 = data.box[2];
                     float y2 = data.box[3];
                     
                     float cx = (x1 + x2) / 2.0f;
                     float cy = (y1 + y2) / 2.0f;
                     
                     // Unity Viewport: Origin Bottom-Left. Invert Y.
                     float vx = cx;
                     float vy = 1.0f - cy;
                     
                     // Raycast to find direction
                     Ray ray = Camera.main.ViewportPointToRay(new Vector3(vx, vy, 0));
                     
                     // Place at fixed distance (simulating depth)
                     Vector3 targetPos = ray.GetPoint(beaconDistance);
                     
                     // WORLD LOCKING: Detach from camera if attached
                     if (beacon.transform.parent == Camera.main.transform)
                     {
                         beacon.transform.SetParent(null); 
                     }
                     
                     // INSTANT teleport to target position
                     beacon.transform.position = targetPos;
                     
                     // Update BeaconPulse's base position so bobbing works from new location
                     BeaconPulse pulse = beacon.GetComponent<BeaconPulse>();
                     if (pulse != null) pulse.SetBasePosition(targetPos);
                     
                     // Always face camera
                     beacon.transform.LookAt(Camera.main.transform);
                     beacon.transform.Rotate(0, 180, 0); // TextMesh faces backwards by default
                     
                     Log($"[Beacon] {objectName} updated: box[{cx:F2},{cy:F2}] -> pos{beacon.transform.position}");
                 }
                 else
                 {
                     Log($"[Beacon] {objectName}: No box data (data={data != null}, box={data?.box != null})");
                 }
            }
        }
        else
        {
             // If not detected recently, do NOT hide it. Keep last known position.
             // User Request: "Always display when beacon is ON"
        }
    }

    public void ToggleBeaconVisibility()
    {
        isBeaconVisible = !isBeaconVisible;
        foreach(var kvp in _activeBeacons)
        {
            if (kvp.Value != null)
            {
                // Simple toggle. Real validity is checked in UpdateBeacon loop.
                kvp.Value.SetActive(isBeaconVisible); 
            }
        }
        Log($"Beacon Visibility: {isBeaconVisible}");
    }

    private void CreateBeacon(string objectName)
    {
         if (_activeBeacons.ContainsKey(objectName) && _activeBeacons[objectName] != null) return;

         // カメラの前方のワールド座標を計算（実空間固定）
         Vector3 beaconPosition;
         if (Camera.main != null)
         {
             beaconPosition = Camera.main.transform.position + Camera.main.transform.forward * 2.0f;
             beaconPosition.y -= 0.2f; // 少し下に配置
         }
         else
         {
             beaconPosition = transform.position + transform.forward * 2.0f;
         }

         // CreateBeaconObjectと同じビジュアルで作成
         GameObject beacon = CreateBeaconObject(objectName, beaconPosition);
         
         _activeBeacons[objectName] = beacon;
         _lastBeaconTime[objectName] = Time.time;
         Log($"[Beacon] Created for {objectName} at world position {beaconPosition}");
    }

    private void DestroyBeacon(string objectName)
    {
        if (_activeBeacons.ContainsKey(objectName))
        {
            if (_activeBeacons[objectName] != null) Destroy(_activeBeacons[objectName]);
            _activeBeacons.Remove(objectName);
        }
    }

    // ============ START & INITIALIZATION ============
    // ============ START & INITIALIZATION ============
    void Start()
    {
        // Fallback UI click helper (in case EventSystem raycast fails)
        if (FindObjectOfType<NRUIRaycastClicker>() == null)
        {
            gameObject.AddComponent<NRUIRaycastClicker>();
        }

        // 1. Load saved settings or use default
        // Default to mobile data IP as requested
        string defaultUrl = "http://10.218.149.69:5000";
        
        if (PlayerPrefs.HasKey("ServerUrl"))
        {
            serverUrlBase = PlayerPrefs.GetString("ServerUrl", defaultUrl);
        }
        else
        {
            serverUrlBase = defaultUrl;
            PlayerPrefs.SetString("ServerUrl", serverUrlBase);
            PlayerPrefs.Save();
        }
        
        Debug.Log($"[ImageUploader] Server URL loaded: {serverUrlBase}");


        // 2. Request Camera Permission
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        }
        
        // 3. Setup UI Button Listeners
        if (closeRegistrationSelectButton == null && registrationSelectPanel != null)
        {
            // Try to find a close button automatically
            var btn = registrationSelectPanel.transform.Find("CloseButton")?.GetComponent<Button>();
            if (btn == null) btn = registrationSelectPanel.transform.Find("CancelButton")?.GetComponent<Button>();
            if (btn == null) btn = registrationSelectPanel.GetComponentInChildren<Button>(); // Fallback to first button
            
            if (btn != null)
            {
                closeRegistrationSelectButton = btn;
                Debug.Log("[ImageUploader] Auto-assigned closeRegistrationSelectButton");
            }
        }
        
        SetupButtonListeners();

        // 4. Initial UI state
        if (!showLogByDefault && debugPanel != null) debugPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (objectIdPanel != null) objectIdPanel.SetActive(false);
        if (registrationSelectPanel != null) registrationSelectPanel.SetActive(false);
        if (registeredListPanel != null) registeredListPanel.SetActive(false);
        
        // 5. Initialize Camera
        InitializeCamera();
        
        _tempTexture = new Texture2D(1280, 720, TextureFormat.RGB24, false); // Backup init just in case

        // 6. Start Routines
        Log($"[SETUP] URL: {serverUrlBase}");
        StartCoroutine(PingRoutine());
        if (isStreaming)
        {
            StartCoroutine(StreamRoutine());
        }
        
        // 7. Auto-refresh registered list on startup (ensures object list is always up-to-date)
        StartCoroutine(AutoRefreshRegisteredListRoutine());
        
        // Auto-start Weather if manager exists (handled in WeatherManager itself)
    }

    void SetupButtonListeners()
    {
        // Settings & Log
        if (settingsButton != null) {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
        }
        if (closeSettingsButton != null) {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(() => settingsPanel.SetActive(false));
        }
        if (logToggleButton != null) {
            logToggleButton.onClick.RemoveAllListeners();
            logToggleButton.onClick.AddListener(ToggleLogPanel);
        }
        if (setIpButton != null) {
            setIpButton.onClick.RemoveAllListeners();
            setIpButton.onClick.AddListener(() => {
                if (ipInputField != null) serverUrlBase = ipInputField.text;
                Log("Server URL updated: " + serverUrlBase);
            });
        }

        // Registration Select
        if (closeRegistrationSelectButton != null) {
            closeRegistrationSelectButton.onClick.RemoveAllListeners();
            closeRegistrationSelectButton.onClick.AddListener(HideRegistrationSelectPanel);
        }

        // Object ID
        if (objectIdButton != null) {
            objectIdButton.onClick.RemoveAllListeners();
            objectIdButton.onClick.AddListener(ShowObjectIdPanel);
        }
        if (registerObjectButton != null) {
            registerObjectButton.onClick.RemoveAllListeners();
            registerObjectButton.onClick.AddListener(RegisterCurrentObject);
        }
        if (closeObjectIdPanelButton != null) {
            closeObjectIdPanelButton.onClick.RemoveAllListeners();
            closeObjectIdPanelButton.onClick.AddListener(HideObjectIdPanel);
        }

        // Registered List
        if (closeRegisteredListButton != null) {
            closeRegisteredListButton.onClick.RemoveAllListeners();
            closeRegisteredListButton.onClick.AddListener(HideRegisteredListPanel);
        }
    }
    public void ShowRegistrationSelectPanel()
    {
        if (registrationSelectPanel != null)
        {
            registrationSelectPanel.SetActive(true);
        }
    }

    public void HideRegistrationSelectPanel()
    {
        if (registrationSelectPanel != null)
        {
            registrationSelectPanel.SetActive(false);
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    public void ToggleLogPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
            if (logToggleButtonText != null)
            {
                logToggleButtonText.text = debugPanel.activeSelf ? "Console Log: ON" : "Console Log: OFF";
            }
        }
    }

    // ============ OBJECT RECOGNITION METHODS ============
    

    public void ShowObjectIdPanel()
    {
        if (objectIdPanel != null)
        {
            objectIdPanel.SetActive(true);
        }
    }

    public void HideObjectIdPanel()
    {
        if (objectIdPanel != null)
        {
            objectIdPanel.SetActive(false);
        }
    }
    
    public void RegisterCurrentObject()
    {
        Log("[RegisterCurrentObject] Method called!");
        
        if (objectNameInput == null)
        {
            Log("[RegisterCurrentObject] ERROR: objectNameInput is NULL!");
            return;
        }
        
        if (string.IsNullOrEmpty(objectNameInput.text))
        {
            Log("[RegisterCurrentObject] Please enter object name (input is empty)");
            return;
        }
        
        string name = objectNameInput.text.Trim();
        Log($"[RegisterCurrentObject] Starting registration for: {name}");
        StartCoroutine(RegisterObjectRoutine(name));
    }
    
    IEnumerator ObjectIdRoutine()
    {
        Log("Object ID Routine Started");
        yield return new WaitForSeconds(0.5f);
        
        while (isObjectIdMode)
        {
            if (_webCamTexture != null && _webCamTexture.isPlaying && _webCamTexture.width > 16)
            {
                yield return StartCoroutine(IdentifyObjectsRoutine());
            }
            yield return new WaitForSeconds(streamInterval); // Use same interval as streaming
        }
    }
    
    IEnumerator IdentifyObjectsRoutine()
    {
        // Capture frame
        if (_tempTexture.width != _webCamTexture.width || _tempTexture.height != _webCamTexture.height)
        {
            _tempTexture.Reinitialize(_webCamTexture.width, _webCamTexture.height);
        }
        _tempTexture.SetPixels(_webCamTexture.GetPixels());
        _tempTexture.Apply();
        
        byte[] jpgBytes = _tempTexture.EncodeToJPG(50);
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", jpgBytes, "object.jpg", "image/jpeg");
        
        string url = $"{serverUrlBase}/objects/identify";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.certificateHandler = new BypassCertificate();
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                ObjectIdentifyResponse response = JsonUtility.FromJson<ObjectIdentifyResponse>(json);
                
                if (response != null && response.objects != null && response.objects.Count > 0)
                {
                    string displayText = "";
                    List<DetectionData> detections = new List<DetectionData>();

                    foreach (var obj in response.objects)
                    {
                        int confidencePercent = Mathf.RoundToInt(obj.confidence * 100);
                        displayText += $"📦 {obj.name} ({confidencePercent}%)\n";
                        
                        // Convert to DetectionData for Beacon Update
                        DetectionData dd = new DetectionData();
                        dd.name = obj.name;
                        dd.confidence = obj.confidence;
                        dd.box = obj.box; // Normalized [x1, y1, x2, y2]
                        detections.Add(dd);
                    }
                    
                    _lastObjectDisplayText = displayText.Trim();
                    _lastObjectDetectionTime = Time.time;
                    
                    if (objectResultText != null)
                    {
                        objectResultText.text = _lastObjectDisplayText;
                    }
                    
                    // Unified Processing (Updates Beacon Position & Visibility)
                    if (beaconEnabled)
                    {
                        ProcessDetections(detections);
                    }
                    
                    Log($"Objects detected: {response.objects.Count}");
                }
            }
            else
            {
                Log($"Object identify error: {www.error}");
                // Clear detections if error or empty (optional, but sticking to existing flow)
                if (beaconEnabled) ProcessDetections(new List<DetectionData>());
            }
        }
    }
    
    // ============ BEACON METHODS ============
    
    void PlaceBeaconForObject(string objectName)
    {
        // ビーコン対象として選択されているかチェック
        BeaconSelectionManager beaconMgr = FindObjectOfType<BeaconSelectionManager>();
        if (beaconMgr != null && !beaconMgr.IsSelectedForBeacon(objectName))
        {
            // 選択されていないオブジェクトはビーコン対象外
            return;
        }
        
        // クールダウンチェック
        if (_lastBeaconTime.ContainsKey(objectName))
        {
            if (Time.time - _lastBeaconTime[objectName] < BEACON_COOLDOWN)
            {
                return; // まだクールダウン中
            }
        }
        
        // カメラの位置と向きを取得
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // NRSDKのカメラを探す
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Log("Beacon: Camera not found");
            return;
        }
        
        // カメラ前方にビーコンを配置
        Vector3 beaconPosition = mainCamera.transform.position + mainCamera.transform.forward * beaconDistance;
        
        // 既存のビーコンがあれば削除
        if (_activeBeacons.ContainsKey(objectName) && _activeBeacons[objectName] != null)
        {
            Destroy(_activeBeacons[objectName]);
        }
        
        // 新しいビーコンを作成
        GameObject beacon = CreateBeaconObject(objectName, beaconPosition);
        _activeBeacons[objectName] = beacon;
        _lastBeaconTime[objectName] = Time.time;
        
        Log($"📍 Beacon placed for: {objectName}");
    }
    
    GameObject CreateBeaconObject(string objectName, Vector3 position)
    {
        // ビーコンのルートオブジェクト
        GameObject beacon = new GameObject($"Beacon_{objectName}");
        beacon.transform.position = position;
        
        // 光る柱（Cylinder）
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "BeaconPillar";
        pillar.transform.SetParent(beacon.transform);
        pillar.transform.localPosition = new Vector3(0, 0.5f, 0);
        pillar.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        
        // コライダーを削除（視覚的な要素のみ）
        Destroy(pillar.GetComponent<Collider>());
        
        // マテリアル設定（光る効果）
        Renderer pillarRenderer = pillar.GetComponent<Renderer>();
        Material beaconMaterial = new Material(Shader.Find("Sprites/Default"));
        beaconMaterial.color = beaconColor;
        pillarRenderer.material = beaconMaterial;
        
        // 上部の球体（光るインジケータ）
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "BeaconTop";
        sphere.transform.SetParent(beacon.transform);
        sphere.transform.localPosition = new Vector3(0, 1.2f, 0);
        sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        Destroy(sphere.GetComponent<Collider>());
        
        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        Material sphereMaterial = new Material(Shader.Find("Sprites/Default"));
        sphereMaterial.color = new Color(beaconColor.r, beaconColor.g, beaconColor.b, 1f);
        sphereRenderer.material = sphereMaterial;
        
        // ラベル（物体名）
        GameObject labelObj = new GameObject("BeaconLabel");
        labelObj.transform.SetParent(beacon.transform);
        labelObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        TextMesh labelText = labelObj.AddComponent<TextMesh>();
        labelText.text = objectName;
        labelText.fontSize = 24;
        labelText.characterSize = 0.05f;
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.color = Color.white;
        
        // ラベルが常にカメラを向くようにBillboard効果を追加
        labelObj.AddComponent<BeaconBillboard>();
        
        // パルスアニメーション用コンポーネント追加
        beacon.AddComponent<BeaconPulse>();
        
        return beacon;
    }
    
    public void ToggleBeaconEnabled()
    {
        beaconEnabled = !beaconEnabled;
        Log($"Beacon: {(beaconEnabled ? "ON" : "OFF")}");
        
        // オフにした場合、既存のビーコンを全て削除
        if (!beaconEnabled)
        {
            ClearAllBeacons();
        }
    }
    
    public void ClearAllBeacons()
    {
        foreach (var kvp in _activeBeacons)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        _activeBeacons.Clear();
        _lastBeaconTime.Clear();
        Log("All beacons cleared");
    }
    
    // ============ REGISTERED LIST MANAGEMENT ============
    
    [Header("Registered List UI")]
    public GameObject registeredListPanel;
    public Text registeredListText;
    public Button closeRegisteredListButton; // Added reference
    public Action<string> onDeleteFace;
    public Action<string> onDeleteObject;
    public Action<List<string>> OnObjectListUpdated; // Publishes updated object list for other UIs
    
    public void ShowRegisteredListPanel()
    {
        // Auto-find panel if missing
        if (registeredListPanel == null)
        {
            var found = GameObject.Find("RegistrationListPanel");
            if (found != null) 
            {
                registeredListPanel = found;
                Log("[List] Auto-found RegistrationListPanel");
            }
            else
            {
                Log("[List] ERROR: RegistrationListPanel not found in scene!");
                return;
            }
        }
        
        registeredListPanel.SetActive(true);
        RefreshRegisteredList();
    }
    
    public void HideRegisteredListPanel()
    {
        if (registeredListPanel != null)
        {
            registeredListPanel.SetActive(false);
        }
    }
    
    public void RefreshRegisteredList()
    {
        // Auto-find references if missing
        if (registeredListPanel == null)
        {
            // Try to find by name in scene (assuming created by SceneSetupTool)
            var found = GameObject.Find("RegistrationListPanel");
            if (found != null) registeredListPanel = found;
        }

        if (registeredListPanel != null)
        {
             // Find Text if missing
             if (registeredListText == null)
             {
                 var t = registeredListPanel.transform.Find("ListContent");
                 if (t != null) registeredListText = t.GetComponent<Text>();
             }
             
             // Find BeaconManager if missing
             if (_beaconManager == null)
             {
                 _beaconManager = registeredListPanel.GetComponent<BeaconSelectionManager>();
                 if (_beaconManager == null) _beaconManager = FindObjectOfType<BeaconSelectionManager>();
             }
        }

        // Clear existing items using Manager
        if (_beaconManager != null)
        {
            _beaconManager.ClearList();
            Log("[List] Cleared existing list items.");
        }
        else
        {
            Log("[List] Warning: BeaconSelectionManager not found! List might duplicate.");
        }
        
        Log("[List] RefreshRegisteredList called. Starting coroutine.");
        StartCoroutine(FetchRegisteredListRoutine());
    }
    
    /// <summary>
    /// Auto-refresh on startup with delay to ensure UI is ready
    /// </summary>
    IEnumerator AutoRefreshRegisteredListRoutine()
    {
        Log("[AutoRefresh] Waiting 2 seconds before auto-refresh...");
        yield return new WaitForSeconds(2.0f);
        
        Log("[AutoRefresh] Starting auto-refresh of registered list");
        RefreshRegisteredList();
    }
    
    IEnumerator FetchRegisteredListRoutine()
    {
        Log("[List] Starting FetchRegisteredListRoutine...");
        if (registeredListText != null) registeredListText.text = "Loading: Connecting...";
        
        // Debug Test Item to prove UI works
        if (_beaconManager != null) {
            _beaconManager.CreateListItem("Loading Test", "object", "Please Wait...");
        }
        
        List<string> allFaces = new List<string>();
        List<string> allObjects = new List<string>();
        
        string headerText = "";
        
        // Fetch faces
        string facesUrl = $"{serverUrlBase}/list_persons";
        Log($"[List] Fetching faces from: {facesUrl}");
        if (registeredListText != null) registeredListText.text = "Loading: Fetching Faces...";
        
        using (UnityWebRequest www = UnityWebRequest.Get(facesUrl))
        {
            www.certificateHandler = new BypassCertificate();
            www.timeout = 5;
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Log($"[List] Face JSON received: {json}");
                PersonListResponse faceResponse = JsonUtility.FromJson<PersonListResponse>(json);
                
                if (faceResponse != null && faceResponse.persons != null)
                {
                    Log($"[List] Parsing success: {faceResponse.persons.Count} faces found.");
                    foreach (var person in faceResponse.persons)
                    {
                        allFaces.Add(person.name);
                        allObjects.Add(person.name); // Add faces to the combined list for search UI
                        
                        // BeaconSelectionManagerでUI作成
                        if (_beaconManager != null)
                        {
                            _beaconManager.CreateListItem(person.name, "face", $"{person.samples} samples");
                        }
                        else
                        {
                            Log("[List] _beaconManager is NULL! Cannot create face item.");
                        }
                    }
                }
                headerText += $"👤 Faces: {faceResponse?.count ?? 0}  ";
            }
            else
            {
                Log($"[List] Face Fetch Error: {www.error}");
                headerText += "⚠ Faces: Error  ";
            }
        }
        
        // Fetch objects
        string objectsUrl = $"{serverUrlBase}/objects/list";
        Log($"[List] Fetching objects from: {objectsUrl}");
        if (registeredListText != null) registeredListText.text = "Loading: Fetching Objects...";
        
        using (UnityWebRequest www = UnityWebRequest.Get(objectsUrl))
        {
            www.certificateHandler = new BypassCertificate();
            www.timeout = 5;
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Log($"[List] Object JSON received: {json}");
                ObjectListResponse objResponse = JsonUtility.FromJson<ObjectListResponse>(json);
                
                if (objResponse != null && objResponse.objects != null)
                {
                    Log($"[List] Parsing success: {objResponse.objects.Count} objects found.");
                    foreach (var obj in objResponse.objects)
                    {
                        allObjects.Add(obj.name);
                        if (_beaconManager != null) {
                            _beaconManager.CreateListItem(obj.name, "object", $"{obj.keypoints} features");
                        }
                    }
                    headerText += $"📦 Objects: {objResponse.count} (+Common)";
                }
            }
            else
            {
                Log($"[List] Object Fetch Error: {www.error}. Using offline list.");
                headerText += "⚠ Objects: Offline";
            }
        }
        
        // FORCE ADD 15 COMMON OBJECTS (Fallback/Default) - Always executed
        string[] commonObjects = {
            "person", "bicycle", "car", "dog", "cat", 
            "backpack", "umbrella", "bottle", "cup", "fork", 
            "spoon", "bowl", "chair", "laptop", "cell phone"
        };
        
        foreach (string common in commonObjects)
        {
            if (!allObjects.Contains(common))
            {
                allObjects.Add(common); // Add to list for search UI
                
                if (_beaconManager != null) {
                    _beaconManager.CreateListItem(common, "object", "Pre-installed");
                }
            }
        }

        // Ensure "All" is at the start
        if (!allObjects.Contains("All")) allObjects.Insert(0, "All");
        
        // Notify listeners (SearchUIManager) - Always executed so UI is never empty
        Log($"[List] Invoking OnObjectListUpdated with {allObjects.Count} items: " + string.Join(", ", allObjects));
        OnObjectListUpdated?.Invoke(allObjects);
        
        // ヘッダーテキスト更新
        if (registeredListText != null)
        {
            registeredListText.text = headerText + "\n\n<size=14>☑ Check up to 3 items for beacon tracking</size>";
        }
        else
        {
            Log("[List] registeredListText is NULL!");
        }
        
        // 選択数表示を更新
        if (_beaconManager != null && _beaconManager.selectionCountText != null)
        {
            _beaconManager.selectionCountText.text = $"Beacon Targets: {_beaconManager.SelectedObjects.Count}/{_beaconManager.maxSelections}";
        }
        Log("[List] Finished FetchRegisteredListRoutine.");
    }
    
    public void DeleteFace(string name)
    {
        StartCoroutine(DeleteFaceRoutine(name));
    }
    
    IEnumerator DeleteFaceRoutine(string name)
    {
        Log($"Deleting face: {name}");
        
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        
        string url = $"{serverUrlBase}/delete_person";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.certificateHandler = new BypassCertificate();
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Log($"✅ Face '{name}' deleted");
                RefreshRegisteredList();
            }
            else
            {
                Log($"❌ Failed to delete face: {www.error}");
            }
        }
    }
    
    public void DeleteObject(string name)
    {
        StartCoroutine(DeleteObjectRoutine(name));
    }
    
    IEnumerator DeleteObjectRoutine(string name)
    {
        Log($"Deleting object: {name}");
        
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        
        string url = $"{serverUrlBase}/objects/delete";
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.certificateHandler = new BypassCertificate();
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Log($"✅ Object '{name}' deleted");
                // ビーコンも削除
                if (_activeBeacons.ContainsKey(name) && _activeBeacons[name] != null)
                {
                    Destroy(_activeBeacons[name]);
                    _activeBeacons.Remove(name);
                }
                RefreshRegisteredList();
            }
            else
            {
                Log($"❌ Failed to delete object: {www.error}");
            }
        }
    }
    
    IEnumerator RegisterObjectRoutine(string name)
    {
        Log($"Starting object registration: {name}");
        _isRegistering = true;
        
        // パネルを非表示
        GameObject objectRegPanel = GameObject.Find("ObjectRegPanel");
        if (objectRegPanel != null)
        {
            objectRegPanel.SetActive(false);
            Log("[RegisterObject] Panel hidden");
        }
        
        // 3D進捗表示を作成
        GameObject progressDisplay3D = Create3DProgressDisplay();
        TextMesh progress3DText = progressDisplay3D?.GetComponentInChildren<TextMesh>();
        
        int successCount = 0;
        int totalFeatures = 0;
        float captureInterval = 0.2f; // 200ms間隔で撮影
        
        // Initial UI State
        if (objectProgressText != null)
        {
            objectProgressText.text = "0/" + REGISTRATION_FRAMES;
            objectProgressText.gameObject.SetActive(true);
        }
        if (objectProgressBar != null)
        {
            objectProgressBar.fillAmount = 0f;
            if (objectProgressBar.transform.parent != null)
            {
                objectProgressBar.transform.parent.gameObject.SetActive(true);
            }
            objectProgressBar.gameObject.SetActive(true);
        }

        // Start Animation
        Coroutine animCoroutine = StartCoroutine(AnimateRecordingUI());
        
        // 登録中は物体をゆっくり回転させるよう促すテキスト表示
        Log("📦 Slowly rotate the object...");
        
        // 3D進捗テキストの初期更新
        if (progress3DText != null)
        {
            progress3DText.text = $"📦 Registering: {name}\n0/{REGISTRATION_FRAMES}\nRotate object slowly...";
            Log("[RegisterObject] 3D progress text initialized");
        }
        else
        {
            Log("[RegisterObject] WARNING: progress3DText is null!");
        }
        
        for (int i = 0; i < REGISTRATION_FRAMES; i++)
        {
            if (!_isRegistering) break; // キャンセルされた場合
            
            // 3D進捗更新（ループの最初に更新）
            if (progress3DText != null)
            {
                progress3DText.text = $"📦 Registering: {name}\n{i + 1}/{REGISTRATION_FRAMES}\nRotate object slowly...";
            }
            
            // UIを更新するためにフレームを待つ
            yield return null;
            
            // フレームキャプチャ（ARカメラ優先、フォールバックあり）
            byte[] jpgBytes = null;
            if (_isNRCameraInitialized && _nrRGBCamTexture != null)
            {
                Log($"[RegisterObject] Frame {i + 1}: Using NR Camera");
                jpgBytes = GetARGlassCameraFrame();
            }
            else
            {
                Log($"[RegisterObject] Frame {i + 1}: Using WebCam fallback");
                jpgBytes = GetWebCamFallbackFrame();
            }
            
            Log($"[RegisterObject] Frame {i + 1}: Capture result = {(jpgBytes != null ? jpgBytes.Length + " bytes" : "NULL")}");
            
            if (jpgBytes != null && jpgBytes.Length > 0)
            {
                // サーバー通信（非ブロッキング）
                StartCoroutine(SendObjectSampleToServer(jpgBytes, name, i + 1, (success, features) => {
                    if (success)
                    {
                        successCount++;
                        totalFeatures = features;
                    }
                }));
            }
            else
            {
                Log($"[RegisterObject] Frame {i}: Failed to capture frame");
            }
            
            // UI更新
            float progress = (float)(i + 1) / REGISTRATION_FRAMES;
            if (objectProgressText != null)
            {
                objectProgressText.text = $"{i + 1}/{REGISTRATION_FRAMES} (Running...)"; // 1行に戻す
            }
            if (objectProgressBar != null)
            {
                objectProgressBar.fillAmount = progress;
            }
            
            // 3D進捗更新
            if (progress3DText != null)
            {
                progress3DText.text = $"📦 Registering: {name}\n{i + 1}/{REGISTRATION_FRAMES}\nRotate object slowly...";
            }
            
            yield return new WaitForSeconds(captureInterval);
        }
        
        // Stop Animation
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        CleanupRecordingUI();
        
        // 完了処理
        _isRegistering = false;
        
        // 進捗UI非表示
        if (objectProgressText != null) objectProgressText.gameObject.SetActive(false);
        if (objectProgressBar != null)
        {
            objectProgressBar.gameObject.SetActive(false);
            if (objectProgressBar.transform.parent != null)
            {
                objectProgressBar.transform.parent.gameObject.SetActive(false);
            }
        }
        
        // 3D進捗表示を破棄
        if (progressDisplay3D != null)
        {
            Destroy(progressDisplay3D);
        }
        
        if (successCount > 0)
        {
            Log($"✅ Object '{name}' registered! ({successCount} frames, {totalFeatures} features)");
            HideObjectIdPanel();
            if (objectNameInput != null) objectNameInput.text = "";
            
            // Wait a bit for server to finalize file I/O before refreshing list
            yield return new WaitForSeconds(1.5f);
            RefreshRegisteredList(); // Update UI immediately
        }
        else
        {
            Log($"❌ Registration failed: No successful captures");
        }
    }

    /// <summary>
    /// サーバーにオブジェクトサンプルを送信（非ブロッキング）
    /// </summary>
    IEnumerator SendObjectSampleToServer(byte[] jpgBytes, string name, int frameNum, System.Action<bool, int> callback)
    {
        Log($"[SendObjectSample] Frame {frameNum}: Starting send to server...");
        string url = $"{serverUrlBase}/objects/add_sample";
        bool isDone = false;
        bool success = false;
        int features = 0;

        System.Threading.Thread thread = new System.Threading.Thread(() =>
        {
            try
            {
                string boundary = "----UnityBoundary" + DateTime.Now.Ticks.ToString("x");
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // 1. Name Field
                        string nameHeader = $"--{boundary}\r\nContent-Disposition: form-data; name=\"name\"\r\n\r\n{name}\r\n";
                        byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(nameHeader);
                        ms.Write(nameBytes, 0, nameBytes.Length);

                        // 2. Image Field
                        string imageHeader = $"--{boundary}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"object.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
                        byte[] imageHeaderBytes = System.Text.Encoding.UTF8.GetBytes(imageHeader);
                        ms.Write(imageHeaderBytes, 0, imageHeaderBytes.Length);
                        
                        // Image Data
                        ms.Write(jpgBytes, 0, jpgBytes.Length);
                        
                        // Footer
                        string footer = $"\r\n--{boundary}--\r\n";
                        byte[] footerBytes = System.Text.Encoding.UTF8.GetBytes(footer);
                        ms.Write(footerBytes, 0, footerBytes.Length);

                        // Send
                        byte[] body = ms.ToArray();
                        byte[] responseBytes = client.UploadData(url, "POST", body);
                        string json = System.Text.Encoding.UTF8.GetString(responseBytes);
                        
                        // Parse Response
                        ObjectAddSampleResponse response = JsonUtility.FromJson<ObjectAddSampleResponse>(json);
                        if (response != null && response.success)
                        {
                            success = true;
                            features = response.current_features;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Thread-safe logging not possible here if it touches UI
                // Just capture error for later if needed, or rely on internal flag
                Debug.Log($"[SendObjectSample] Error in thread: {e.Message}");
            }
            finally
            {
                isDone = true;
            }
        });
        
        thread.Start();

        // Wait for thread
        while (!isDone)
        {
            yield return null;
        }

        if (success)
        {
            Log($"[SendObjectSample] Success! Features: {features}");
            callback?.Invoke(true, features);
        }
        else
        {
            Log($"[SendObjectSample] Failed! (Check console for thread errors if any)");
            callback?.Invoke(false, 0);
        }
    }

    /// <summary>
    /// 3D空間に進捗表示を作成
    /// </summary>
    GameObject Create3DProgressDisplay()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        
        GameObject display = new GameObject("ObjectRegistration3DProgress");
        
        // カメラ前方に配置
        Vector3 pos = cam.transform.position + cam.transform.forward * 1.2f;
        display.transform.position = pos;
        display.transform.LookAt(cam.transform);
        display.transform.Rotate(0, 180, 0);
        
        // テキスト表示
        GameObject textObj = new GameObject("ProgressText");
        textObj.transform.SetParent(display.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.01f;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = "📦 Preparing registration...";
        textMesh.fontSize = 50;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.5f;
        
        Log("[RegisterObject] 3D progress display created");
        return display;
    }

    // ============ ANIMATION HELPERS ============
    private GameObject _recordingIconObj;

    void EnsureRecordingIcon()
    {
        if (_recordingIconObj != null) return;
        if (objectProgressText == null) return;

        // Try to find existing icon first
        Transform existing = objectProgressText.transform.parent.Find("RecIcon");
        if (existing != null)
        {
            _recordingIconObj = existing.gameObject;
            return;
        }

        _recordingIconObj = new GameObject("RecIcon");
        _recordingIconObj.transform.SetParent(objectProgressText.transform.parent, false);
        Image img = _recordingIconObj.AddComponent<Image>();
        
        // Try built-in Knob (Circle)
        Sprite knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        if (knob != null) img.sprite = knob;
        img.color = Color.red;
        
        RectTransform rt = _recordingIconObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(24, 24);
        
        // Position top-right relative to anchor? Or next to text.
        // Assuming Panel is parent.
        // Let's put it top-right of the panel
        rt.anchorMin = new Vector2(0.92f, 0.92f);
        rt.anchorMax = new Vector2(0.98f, 0.98f);
        rt.anchoredPosition = Vector2.zero;
        
        _recordingIconObj.SetActive(false);
    }

    IEnumerator AnimateRecordingUI()
    {
        EnsureRecordingIcon();
        if (_recordingIconObj) _recordingIconObj.SetActive(true);
        
        while (true)
        {
             // Blink Icon
             if (_recordingIconObj) _recordingIconObj.SetActive(!_recordingIconObj.activeSelf);
             
             // Pulse Text Scale and Color
             if (objectProgressText) 
             {
                 bool isExpanded = objectProgressText.transform.localScale.x > 1.1f;
                 objectProgressText.transform.localScale = isExpanded ? Vector3.one : new Vector3(1.2f, 1.2f, 1f);
                 objectProgressText.color = isExpanded ? Color.white : new Color(1f, 0.8f, 0.8f);
             }
             
             yield return new WaitForSeconds(0.4f);
        }
    }

    void CleanupRecordingUI()
    {
        if (_recordingIconObj) _recordingIconObj.SetActive(false);
        if (objectProgressText) 
        {
            objectProgressText.transform.localScale = Vector3.one;
            objectProgressText.color = Color.white;
        }
    }

    IEnumerator PingRoutine()
    {
        Log("Ping Loop Starting...");
        yield return new WaitForSeconds(1.0f);
        
        while (true)
        {
            string url = $"{serverUrlBase}/ping";
            Log($"Pinging: {url}");
            
            bool success = false;
            string errorMsg = "";
            
            // Use Thread to avoid blocking main thread
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.Headers.Add("User-Agent", "UnityApp");
                        string result = client.DownloadString(url);
                        if (result.Contains("pong"))
                        {
                            success = true;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    errorMsg = ex.GetType().Name + ": " + ex.Message;
                }
            });
            
            thread.Start();
            
            // Wait for thread to complete (max 5 seconds)
            float startTime = Time.realtimeSinceStartup;
            while (thread.IsAlive && (Time.realtimeSinceStartup - startTime) < 5.0f)
            {
                yield return null;
            }
            
            if (thread.IsAlive)
            {
                Log("🔴 TIMEOUT (thread)");
                thread.Abort();
            }
            else if (success)
            {
                Log("🟢 CONNECTED!");
            }
            else
            {
                Log($"🔴 FAIL: {errorMsg}");
            }
            
            yield return new WaitForSeconds(5.0f);
        }
    }

    public void SetServerUrl(string newUrl)
    {
        if (string.IsNullOrEmpty(newUrl)) return;
        
        // Add http:// if missing
        if (!newUrl.StartsWith("http"))
        {
            newUrl = "http://" + newUrl;
        }
        // Remove trailing slash
        if (newUrl.EndsWith("/"))
        {
            newUrl = newUrl.Substring(0, newUrl.Length - 1);
        }

        serverUrlBase = newUrl;
        PlayerPrefs.SetString("ServerUrl", serverUrlBase);
        PlayerPrefs.Save();
        Log($"NEW URL: {serverUrlBase}");
    }

    public void Log(string message)
    {
        Debug.Log($"[ImageUploader] {message}");

        if (debugText != null)
        {
            // Keep only last 25 lines for visibility
            string currentText = debugText.text;
            string[] lines = currentText.Split('\n');
            if (lines.Length > 25)
            {
                currentText = string.Join("\n", lines, 0, 25);
            }
            debugText.text = $"{DateTime.Now:HH:mm:ss} {message}\n{currentText}";
        }
    }


    private void InitializeCamera()
    {
        try
        {
            // ARグラスのRGBカメラを使用する場合
            if (useARGlassCamera)
            {
                Log("Initializing AR Glass RGB Camera...");
                InitializeARGlassCamera();
                return;
            }
            
            // フォールバック: WebCamTexture（スマホ/Beam Proカメラ）
            InitializeWebCamera();
        }
        catch (Exception e)
        {
            Log($"Camera Init Error: {e.Message}");
        }
    }

    private void InitializeARGlassCamera()
    {
        try
        {
            // NRRGBCamTextureYUVを初期化
            _nrRGBCamTexture = new NRRGBCamTextureYUV();
            _nrRGBCamTexture.Play();
            _isNRCameraInitialized = true;
            
            // テンポラリテクスチャを準備
            _tempTexture = new Texture2D(640, 360, TextureFormat.RGB24, false);
            
            Log("AR Glass RGB Camera initialized successfully!");
        }
        catch (Exception e)
        {
            Log($"AR Glass Camera Init Error: {e.Message}");
            Log("Falling back to WebCamTexture...");
            _isNRCameraInitialized = false;
            InitializeWebCamera();
        }
    }

    private void InitializeWebCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Log("Error: No camera devices found.");
            return;
        }

        // Use the first available back-facing camera
        string cameraName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                cameraName = devices[i].name;
                break;
            }
        }

        Log($"Initializing WebCam: {cameraName}");
        // Request lower resolution for faster streaming
        _webCamTexture = new WebCamTexture(cameraName, 640, 360, 30);
        _webCamTexture.Play();
        
        _tempTexture = new Texture2D(640, 360, TextureFormat.RGB24, false);
    }

    /// <summary>
    /// Checks if the texture is predominantly black
    /// </summary>
    private bool IsImageBlack(Texture2D tex)
    {
        if (tex == null) return true;
        
        int w = tex.width;
        int h = tex.height;
        if (w == 0 || h == 0) return true;

        // Sample 5 points (Center and corners)
        Color[] samples = new Color[] {
            tex.GetPixel(w / 2, h / 2),
            tex.GetPixel(w / 4, h / 4),
            tex.GetPixel(w * 3 / 4, h / 4),
            tex.GetPixel(w / 4, h * 3 / 4),
            tex.GetPixel(w * 3 / 4, h * 3 / 4)
        };
        
        foreach(Color c in samples) {
            if (c.grayscale > 0.05f) return false; // Found a non-black pixel
        }
        
        return true;
    }

    /// <summary>
    /// ARグラスのRGBテクスチャからJPGバイトを取得
    /// </summary>
    private byte[] GetARGlassCameraFrame()
    {
        if (!_isNRCameraInitialized || _nrRGBCamTexture == null)
        {
            // Log("[ARCam] Not initialized, using WebCam fallback");
            return GetWebCamFallbackFrame();
        }

        try
        {
            // RGBテクスチャを取得
            RenderTexture rgbTexture = _nrRGBCamTexture.GetRGBTexture();
            if (rgbTexture == null)
            {
                // Log("[ARCam] GetRGBTexture returned null, using WebCam fallback");
                return GetWebCamFallbackFrame();
            }

            // RenderTextureからTexture2Dに変換
            RenderTexture.active = rgbTexture;
            if (_tempTexture == null || _tempTexture.width != rgbTexture.width || _tempTexture.height != rgbTexture.height)
            {
                _tempTexture = new Texture2D(rgbTexture.width, rgbTexture.height, TextureFormat.RGB24, false);
            }
            _tempTexture.ReadPixels(new Rect(0, 0, rgbTexture.width, rgbTexture.height), 0, 0);
            _tempTexture.Apply();
            RenderTexture.active = null;

            // Strict Black Frame Check
            if (IsImageBlack(_tempTexture))
            {
                Log("[ARCam] Black frame detected (Camera likely blocked/loading). Using WebCam fallback.");
                return GetWebCamFallbackFrame();
            }

            byte[] jpgBytes = _tempTexture.EncodeToJPG(80);
            
            // Save for Debugging - REMOVED
            // try {
            //     string debugPath = System.IO.Path.Combine(Application.persistentDataPath, "debug_frame.jpg");
            //     System.IO.File.WriteAllBytes(debugPath, jpgBytes);
            // } catch { }
            
            return jpgBytes;
        }
        catch (Exception e)
        {
            Log($"AR Glass Frame Error: {e.Message}");
            return GetWebCamFallbackFrame();
        }
    }
    
    /// <summary>
    /// WebCamTextureからフレームを取得（フォールバック用）
    /// </summary>
    private byte[] GetWebCamFallbackFrame()
    {
        if (_webCamTexture == null || !_webCamTexture.isPlaying)
        {
            // WebCamも使えない場合は初期化を試みる
            if (_webCamTexture == null)
            {
                Log("[Fallback] Initializing WebCamTexture...");
                InitializeWebCamera();
                return null; // 次のフレームで再試行
            }
            return null;
        }
        
        try
        {
            if (_tempTexture.width != _webCamTexture.width || _tempTexture.height != _webCamTexture.height)
            {
                _tempTexture.Reinitialize(_webCamTexture.width, _webCamTexture.height);
            }
            _tempTexture.SetPixels(_webCamTexture.GetPixels());
            _tempTexture.Apply();
            
            return _tempTexture.EncodeToJPG(80);
        }
        catch (Exception e)
        {
            Log($"[Fallback] WebCam Error: {e.Message}");
            return null;
        }
    }

    IEnumerator StreamRoutine()
    {
        Log("Streaming Started... Waiting for warmup.");
        yield return new WaitForSeconds(3.0f); // Warmup

        Log("Entering Stream Loop...");
        while (isStreaming)
        {
            yield return new WaitForSeconds(streamInterval);

            // ARグラスカメラを使用している場合
            if (_isNRCameraInitialized && _nrRGBCamTexture != null)
            {
                yield return StartCoroutine(SendFrameToServer());
            }
            // WebCamTextureを使用している場合
            else if (_webCamTexture != null)
            {
                if (!_webCamTexture.isPlaying || _webCamTexture.width <= 16)
                {
                    Log($"Cam Waiting... Playing:{_webCamTexture.isPlaying} Size:{_webCamTexture.width}x{_webCamTexture.height}");
                }
                else
                {
                    yield return StartCoroutine(SendFrameToServer());
                }
            }
            else
            {
                Log("Error: No camera available!");
            }
        }
    }

    IEnumerator SendFrameToServer()
    {
        byte[] jpgBytes = null;
        
        if (_isNRCameraInitialized && _nrRGBCamTexture != null)
             jpgBytes = GetARGlassCameraFrame();
        else
             jpgBytes = GetWebCamFallbackFrame();
        
        if (jpgBytes == null || jpgBytes.Length == 0) yield break;
        
        string url = $"{serverUrlBase}/stream";
        string jsonResult = null;
        string errorMsg = "";
        
        System.Threading.Thread thread = new System.Threading.Thread(() =>
        {
            try
            {
                string boundary = "----UnityBoundary" + DateTime.Now.Ticks.ToString("x");
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    using (var ms = new System.IO.MemoryStream())
                    {
                        string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"stream.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
                        byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
                        ms.Write(headerBytes, 0, headerBytes.Length);
                        ms.Write(jpgBytes, 0, jpgBytes.Length);
                        string footer = $"\r\n--{boundary}--\r\n";
                        byte[] footerBytes = System.Text.Encoding.UTF8.GetBytes(footer);
                        ms.Write(footerBytes, 0, footerBytes.Length);
                        
                        byte[] response = client.UploadData(url, "POST", ms.ToArray());
                        jsonResult = System.Text.Encoding.UTF8.GetString(response);
                    }
                }
            }
            catch (System.Exception ex) { errorMsg = ex.GetType().Name + ": " + ex.Message; }
        });

        thread.Start();
        float startTime = Time.realtimeSinceStartup;
        while (thread.IsAlive && (Time.realtimeSinceStartup - startTime) < 5.0f)
        {
            yield return null;
        }

        if (thread.IsAlive) thread.Abort();
        else if (!string.IsNullOrEmpty(jsonResult))
        {
             try
             {
                 // Parsing JSON on main thread
                 StreamResponse res = JsonUtility.FromJson<StreamResponse>(jsonResult);
                 if (res != null)
                 {
                     ProcessDetections(res.detections);
                 }
             }
             catch (Exception e)
             {
                 Log($"JSON Parse Error: {e.Message}");
             }
        }
        else if (!string.IsNullOrEmpty(errorMsg))
        {
            Log($"Upload Error: {errorMsg}");
        }
    }

    // --- Search Functionality ---

    // Called by SearchUIManager
    public void SearchObjects(string query, Action<List<SearchResultItem>> onCompleted)
    {
        StartCoroutine(SearchRoutine(query, onCompleted));
    }

    IEnumerator SearchRoutine(string query, Action<List<SearchResultItem>> onCompleted)
    {
        string url = $"{serverUrlBase}/search?q={System.Uri.EscapeDataString(query)}";
        Log($">>>>>> SEARCH START: {url} <<<<<<");

        string jsonResult = null;
        string errorMsg = "";

        // Use Thread to avoid blocking main thread
        System.Threading.Thread thread = new System.Threading.Thread(() =>
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    jsonResult = client.DownloadString(url);
                }
            }
            catch (System.Exception ex)
            {
                errorMsg = ex.GetType().Name + ": " + ex.Message;
            }
        });

        thread.Start();

        // Wait for thread (max 10 seconds)
        float startTime = Time.realtimeSinceStartup;
        while (thread.IsAlive && (Time.realtimeSinceStartup - startTime) < 10.0f)
        {
            yield return null;
        }

        if (thread.IsAlive)
        {
            Log(">>>>>> SEARCH TIMEOUT <<<<<<");
            thread.Abort();
            onCompleted?.Invoke(null);
        }
        else if (jsonResult != null)
        {
            Log($">>>>>> SEARCH GOT JSON (length={jsonResult.Length}) <<<<<<");
            try
            {
                var response = JsonUtility.FromJson<SearchResponse>(jsonResult);
                int count = response?.results?.Count ?? 0;
                Log($">>>>>> SEARCH PARSED: {count} results <<<<<<");
                Log($">>>>>> CALLING onCompleted CALLBACK <<<<<<");
                onCompleted?.Invoke(response.results);
                Log($">>>>>> onCompleted CALLBACK DONE <<<<<<");
            }
            catch (Exception e)
            {
                Log($">>>>>> JSON Error: {e.Message} <<<<<<");
                onCompleted?.Invoke(null);
            }
        }
        else
        {
            Log($">>>>>> SEARCH ERROR: {errorMsg} <<<<<<");
            onCompleted?.Invoke(null);
        }
    }

    public IEnumerator DownloadImage(string relativeUrl, Action<Texture2D> onCompleted)
    {
        string url = $"{serverUrlBase}{relativeUrl}";
        // Ensure no double slash if relativeUrl starts with /
        if (serverUrlBase.EndsWith("/") && relativeUrl.StartsWith("/"))
        {
            url = $"{serverUrlBase}{relativeUrl.Substring(1)}";
        }

        byte[] imageData = null;
        string errorMsg = "";

        // Use Thread to avoid blocking main thread
        System.Threading.Thread thread = new System.Threading.Thread(() =>
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    imageData = client.DownloadData(url);
                }
            }
            catch (System.Exception ex)
            {
                errorMsg = ex.GetType().Name + ": " + ex.Message;
            }
        });

        thread.Start();

        // Wait for thread (max 15 seconds for image download)
        float startTime = Time.realtimeSinceStartup;
        while (thread.IsAlive && (Time.realtimeSinceStartup - startTime) < 15.0f)
        {
            yield return null;
        }

        if (thread.IsAlive)
        {
            Log("🔴 Image Download Timeout");
            thread.Abort();
            onCompleted?.Invoke(null);
        }
        else if (imageData != null)
        {
            // Create texture from downloaded data
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(imageData))
            {
                onCompleted?.Invoke(tex);
            }
            else
            {
                Log("🔴 Failed to load image data");
                onCompleted?.Invoke(null);
            }
        }
        else
        {
            Log($"🔴 Image Download Error: {errorMsg}");
            onCompleted?.Invoke(null);
        }
    }

    /// <summary>
    /// サーバーURLを更新（設定画面から呼び出し）
    /// </summary>
    public void UpdateServerUrl(string newUrl)
    {
        if (string.IsNullOrEmpty(newUrl)) return;
        
        // 末尾のスラッシュ削除
        if (newUrl.EndsWith("/")) newUrl = newUrl.Substring(0, newUrl.Length - 1);
        
        serverUrlBase = newUrl;
        PlayerPrefs.SetString("ServerUrl", serverUrlBase);
        PlayerPrefs.Save();
        
        Debug.Log($"[ImageUploader] Server URL updated to: {serverUrlBase}");
    }

    void OnDestroy()
    {
        isStreaming = false;
        if (_webCamTexture != null) _webCamTexture.Stop();
    }
}

// JSON Classes
[Serializable]
public class SearchResponse
{
    public int count;
    public List<SearchResultItem> results;
}

[Serializable]
public class SearchResultItem
{
    public string filename;
    public string url;
    public string timestamp;
    public List<string> objects;
}

// Face Recognition JSON Classes
[Serializable]
public class FaceIdentifyResponse
{
    public List<FaceResult> faces;
    public int count;
}

[Serializable]
public class FaceResult
{
    public string name;
    public float confidence;
    public List<int> bbox;
}

// Object Recognition JSON Classes
[Serializable]
public class ObjectIdentifyResponse
{
    public bool success;
    public string message;
    public List<ObjectResult> objects;
}

[Serializable]
public class ObjectResult
{
    public string name;
    public int matches;
    public float confidence;
    public float[] box;
}

[Serializable]
public class ObjectRegisterResponse
{
    public bool success;
    public string message;
    public string name;
    public int features;
}

[Serializable]
public class ObjectAddSampleResponse
{
    public bool success;
    public string message;
    public int current_features;
    public int added_features;
}

// ビーコンが常にカメラを向くようにするコンポーネント
public class BeaconBillboard : MonoBehaviour
{
    private Camera _camera;
    
    void Start()
    {
        _camera = Camera.main;
        if (_camera == null)
        {
            _camera = FindObjectOfType<Camera>();
        }
    }
    
    void LateUpdate()
    {
        if (_camera != null)
        {
            transform.LookAt(_camera.transform);
            transform.Rotate(0, 180, 0); // テキストが正面を向くように
        }
    }
}

// ビーコンのパルスアニメーションコンポーネント
public class BeaconPulse : MonoBehaviour
{
    [Header("Pulse Animation")]
    public float pulseSpeed = 2f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    
    [Header("Rotation Animation")]
    public float rotationSpeed = 30f; // Y軸回転速度（度/秒）
    
    [Header("Bobbing Animation")]
    public float bobSpeed = 1.5f;      // 上下動作の速度
    public float bobAmount = 0.1f;     // 上下動作の幅
    
    private Vector3 _originalScale;
    private Vector3 _basePosition;
    private Transform _sphereTransform;
    private bool _positionInitialized = false;
    
    void Start()
    {
        _originalScale = transform.localScale;
        _basePosition = transform.position;
        _positionInitialized = true;
        
        // 球体を探す
        Transform sphere = transform.Find("BeaconTop");
        if (sphere != null)
        {
            _sphereTransform = sphere;
        }
    }
    
    /// <summary>
    /// 外部からベース位置を更新する
    /// </summary>
    public void SetBasePosition(Vector3 newPosition)
    {
        _basePosition = newPosition;
    }
    
    void Update()
    {
        // === Y軸回転アニメーション ===
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        
        // === 上下浮遊アニメーション (ベース位置からのオフセット) ===
        if (_positionInitialized)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = _basePosition + new Vector3(0f, bobOffset, 0f);
        }
        
        // === 球体のパルスアニメーション ===
        if (_sphereTransform != null)
        {
            float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            _sphereTransform.localScale = new Vector3(0.2f * pulse, 0.2f * pulse, 0.2f * pulse);
        }
    }
}

// 登録済みリスト用JSONクラス
[Serializable]
public class PersonListResponse
{
    public List<PersonInfo> persons;
    public int count;
}

[Serializable]
public class PersonInfo
{
    public string name;
    public int samples;
}

[Serializable]
public class ObjectListResponse
{
    public bool success;
    public List<ObjectInfo> objects;
    public int count;
}

[Serializable]
public class ObjectInfo
{
    public string name;
    public int keypoints;
    public string registered_at;
}
