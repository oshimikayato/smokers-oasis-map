using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// UnityのコンソールログをFlaskサーバーに転送するスクリプト
/// シーン内の任意のGameObjectにアタッチして使用
/// </summary>
public class LogForwarder : MonoBehaviour
{
    [Header("Server Settings")]
    [Tooltip("ログを送信するサーバーのURL (例: http://192.168.0.19:5000)")]
    public string serverUrlBase = "http://121.112.224.89:5000"; // グローバルIPに変更
    
    [Header("Filter Settings")]
    public bool sendErrors = true;
    public bool sendWarnings = true;
    public bool sendLogs = true;
    
    private static LogForwarder _instance;
    
    void Awake()
    {
        // シングルトンパターン（重複防止）
        // 重要: このスクリプトはSearchPanelに付く可能性があるため、
        // gameObject操作は一切しない（Destroy(gameObject)やDontDestroyOnLoadは禁止）
        if (_instance != null && _instance != this)
        {
            // 重複時はコンポーネントだけを削除
            Debug.Log("[LogForwarder] Duplicate detected, destroying self component only");
            Destroy(this);
            return;
        }
        _instance = this;
        
        // 注意: DontDestroyOnLoad は使用しない（SearchPanelを移動させてしまうため）
        // LogForwarder はシーン再読み込み時に再作成される
        
        // ログイベントを購読
        Application.logMessageReceived += HandleLog;
        Debug.Log("[LogForwarder] Started - logs will be forwarded to server");
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
    
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // フィルタリング
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (!sendErrors) return;
        }
        else if (type == LogType.Warning)
        {
            if (!sendWarnings) return;
        }
        else
        {
            if (!sendLogs) return;
        }
        
        // 自分自身のログは送信しない（無限ループ防止）
        if (logString.Contains("[LogForwarder]")) return;
        
        StartCoroutine(SendLogToServer(logString, stackTrace, type.ToString()));
    }
    
    IEnumerator SendLogToServer(string message, string stackTrace, string logType)
    {
        string url = $"{serverUrlBase}/log";
        
        // JSONペイロード作成
        string json = JsonUtility.ToJson(new LogData
        {
            type = logType,
            message = message,
            stack = stackTrace
        });
        
        System.Threading.Thread thread = new System.Threading.Thread(() =>
        {
            try
            {
                // Bypass SSL Validation for WebClient
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                    client.UploadData(url, "POST", bodyRaw);
                }
            }
            catch (System.Exception)
            {
                // 静かに失敗
            }
        });
        
        thread.Start();
        yield return null;
    }
    
    [System.Serializable]
    private class LogData
    {
        public string type;
        public string message;
        public string stack;
    }
}
