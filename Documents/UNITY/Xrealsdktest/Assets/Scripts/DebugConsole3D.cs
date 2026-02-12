using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 3D空間に浮遊するデバッグコンソール
/// カメラの前方に常に表示され、ログメッセージを表示する
/// </summary>
public class DebugConsole3D : MonoBehaviour
{
    [Header("Display Settings")]
    public float distanceFromCamera = 2.0f;
    public float offsetY = -0.3f; // 少し下に配置
    public int maxLines = 15;
    public float fontSize = 0.02f;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
    public Color textColor = Color.green;
    
    [Header("Behavior")]
    public bool followCamera = true;
    public float smoothSpeed = 5f;
    
    private TextMesh _textMesh;
    private GameObject _backgroundQuad;
    private Queue<string> _logLines = new Queue<string>();
    private bool _isVisible = false;
    private static DebugConsole3D _instance;
    
    public static DebugConsole3D Instance => _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreateConsoleVisuals();
        
        // デフォルトで非表示
        gameObject.SetActive(false);
    }
    
    void CreateConsoleVisuals()
    {
        // Background Quad
        _backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _backgroundQuad.name = "ConsoleBackground";
        _backgroundQuad.transform.SetParent(transform);
        _backgroundQuad.transform.localPosition = Vector3.zero;
        _backgroundQuad.transform.localScale = new Vector3(0.8f, 0.5f, 1f);
        
        // Remove collider
        Destroy(_backgroundQuad.GetComponent<Collider>());
        
        // Semi-transparent material
        Renderer bgRenderer = _backgroundQuad.GetComponent<Renderer>();
        Material bgMat = new Material(Shader.Find("Sprites/Default"));
        bgMat.color = backgroundColor;
        bgRenderer.material = bgMat;
        
        // Text
        GameObject textObj = new GameObject("ConsoleText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.01f); // Slightly in front
        
        _textMesh = textObj.AddComponent<TextMesh>();
        _textMesh.fontSize = 50;
        _textMesh.characterSize = fontSize;
        _textMesh.anchor = TextAnchor.MiddleCenter;
        _textMesh.alignment = TextAlignment.Left;
        _textMesh.color = textColor;
        _textMesh.text = "[ Console Ready ]";
        
        // Title bar
        GameObject titleObj = new GameObject("ConsoleTitle");
        titleObj.transform.SetParent(transform);
        titleObj.transform.localPosition = new Vector3(0, 0.22f, -0.01f);
        
        TextMesh titleMesh = titleObj.AddComponent<TextMesh>();
        titleMesh.fontSize = 60;
        titleMesh.characterSize = fontSize * 1.2f;
        titleMesh.anchor = TextAnchor.MiddleCenter;
        titleMesh.alignment = TextAlignment.Center;
        titleMesh.color = Color.cyan;
        titleMesh.text = "🖥️ DEBUG CONSOLE";
    }
    
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        _isVisible = true;
        Debug.Log("[DebugConsole3D] Console enabled");
    }
    
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        _isVisible = false;
    }
    
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!_isVisible) return;
        
        // Format based on type
        string prefix = "";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                prefix = "❌ ";
                break;
            case LogType.Warning:
                prefix = "⚠️ ";
                break;
            default:
                prefix = ">> ";
                break;
        }
        
        // Truncate long messages
        string msg = logString;
        if (msg.Length > 60) msg = msg.Substring(0, 57) + "...";
        
        _logLines.Enqueue(prefix + msg);
        
        // Keep only last N lines
        while (_logLines.Count > maxLines)
        {
            _logLines.Dequeue();
        }
        
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        if (_textMesh == null) return;
        _textMesh.text = string.Join("\n", _logLines);
    }
    
    void LateUpdate()
    {
        if (!followCamera) return;
        
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Target position: in front of camera
        Vector3 targetPos = cam.transform.position 
            + cam.transform.forward * distanceFromCamera 
            + cam.transform.up * offsetY;
        
        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        
        // Face camera
        transform.LookAt(cam.transform);
        transform.Rotate(0, 180, 0); // TextMesh faces backwards
    }
    
    /// <summary>
    /// コンソールの表示/非表示を切り替え
    /// </summary>
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        Debug.Log($"[DebugConsole3D] Toggled: {gameObject.activeSelf}");
    }
    
    /// <summary>
    /// 明示的に表示
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 明示的に非表示
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 手動でログを追加
    /// </summary>
    public void Log(string message)
    {
        HandleLog(message, "", LogType.Log);
    }
    
    /// <summary>
    /// シングルトンを作成または取得
    /// </summary>
    public static DebugConsole3D CreateOrGet()
    {
        if (_instance != null) return _instance;
        
        GameObject consoleObj = new GameObject("DebugConsole3D");
        return consoleObj.AddComponent<DebugConsole3D>();
    }
}
