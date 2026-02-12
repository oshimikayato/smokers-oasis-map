using UnityEngine;
using NRKernal;

/// <summary>
/// シングルトンService Locator
/// FindObjectOfTypeの代わりに使用し、パフォーマンスと依存関係管理を改善
/// </summary>
public class ServiceLocator : MonoBehaviour
{
    private static ServiceLocator _instance;
    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ServiceLocator>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ServiceLocator");
                    _instance = go.AddComponent<ServiceLocator>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // ============ Cached References ============
    [Header("Core Managers")]
    public ImageUploader imageUploader;
    public SearchUIManager searchUIManager;
    public WeatherManager weatherManager;
    public TutorialManager tutorialManager;
    
    [Header("AR Components")]
    public ARSearchResultDisplay arSearchResultDisplay;
    public ARModeSwitcher arModeSwitcher;
    
    [Header("UI Controllers")]
    public NRKernal.BottomMenuController bottomMenuController;
    public NRKernal.SettingsPanelController settingsPanelController;
    
    [Header("Other")]
    public Camera mainCamera;
    
    private bool _initialized = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }

    /// <summary>
    /// 全参照を初期化（一度だけFindObjectOfTypeを使用）
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        
        Debug.Log("[ServiceLocator] Initializing...");
        
        // Find all references once
        if (imageUploader == null) imageUploader = FindObjectOfType<ImageUploader>();
        if (searchUIManager == null) searchUIManager = FindObjectOfType<SearchUIManager>();
        if (weatherManager == null) weatherManager = FindObjectOfType<WeatherManager>();
        if (tutorialManager == null) tutorialManager = FindObjectOfType<TutorialManager>();
        if (arSearchResultDisplay == null) arSearchResultDisplay = FindObjectOfType<ARSearchResultDisplay>();
        if (arModeSwitcher == null) arModeSwitcher = FindObjectOfType<ARModeSwitcher>();
        if (bottomMenuController == null) bottomMenuController = FindObjectOfType<NRKernal.BottomMenuController>();
        if (settingsPanelController == null) settingsPanelController = FindObjectOfType<NRKernal.SettingsPanelController>();
        if (mainCamera == null) mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        
        _initialized = true;
        
        LogStatus();
    }

    /// <summary>
    /// 手動で参照を登録（Inspectorから設定済みの場合不要）
    /// </summary>
    public void Register<T>(T service) where T : MonoBehaviour
    {
        if (service is ImageUploader uploader) imageUploader = uploader;
        else if (service is SearchUIManager searchUI) searchUIManager = searchUI;
        else if (service is WeatherManager weather) weatherManager = weather;
        else if (service is TutorialManager tutorial) tutorialManager = tutorial;
        else if (service is ARSearchResultDisplay arDisplay) arSearchResultDisplay = arDisplay;
        else if (service is ARModeSwitcher arSwitcher) arModeSwitcher = arSwitcher;
        else if (service is NRKernal.BottomMenuController bottom) bottomMenuController = bottom;
        else if (service is NRKernal.SettingsPanelController settings) settingsPanelController = settings;
    }

    /// <summary>
    /// 参照を取得（型安全）
    /// </summary>
    public T Get<T>() where T : class
    {
        if (typeof(T) == typeof(ImageUploader)) return imageUploader as T;
        if (typeof(T) == typeof(SearchUIManager)) return searchUIManager as T;
        if (typeof(T) == typeof(WeatherManager)) return weatherManager as T;
        if (typeof(T) == typeof(TutorialManager)) return tutorialManager as T;
        if (typeof(T) == typeof(ARSearchResultDisplay)) return arSearchResultDisplay as T;
        if (typeof(T) == typeof(ARModeSwitcher)) return arModeSwitcher as T;
        if (typeof(T) == typeof(NRKernal.BottomMenuController)) return bottomMenuController as T;
        if (typeof(T) == typeof(NRKernal.SettingsPanelController)) return settingsPanelController as T;
        if (typeof(T) == typeof(Camera)) return mainCamera as T;
        
        Debug.LogWarning($"[ServiceLocator] Unknown service type: {typeof(T).Name}");
        return null;
    }

    void LogStatus()
    {
        Debug.Log($"[ServiceLocator] Status:\n" +
            $"  ImageUploader: {(imageUploader != null ? "✓" : "✗")}\n" +
            $"  SearchUIManager: {(searchUIManager != null ? "✓" : "✗")}\n" +
            $"  WeatherManager: {(weatherManager != null ? "✓" : "✗")}\n" +
            $"  TutorialManager: {(tutorialManager != null ? "✓" : "✗")}\n" +
            $"  ARSearchResultDisplay: {(arSearchResultDisplay != null ? "✓" : "✗")}\n" +
            $"  BottomMenuController: {(bottomMenuController != null ? "✓" : "✗")}");
    }

    /// <summary>
    /// 参照をリフレッシュ（シーン変更時など）
    /// </summary>
    public void Refresh()
    {
        _initialized = false;
        Initialize();
    }
}
