using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using DG.Tweening;
using NRKernal;

public class SearchUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Dropdown objectDropdown; // Legacy (optional)
    public Button searchButton; // Opens category panel
    public Button cancelButton; // Closes category panel
    public Button closeResultsButton; // Closes search results
    public Transform resultContainer; // Parent object for result images (Content of ScrollView)
    public GameObject resultImagePrefab; // Prefab for displaying a single result image
    public Text statusText;
    public Transform categoryButtonContainer; // Container for category buttons
    public GameObject loadingPanel; // Loading indicator panel
    public GameObject categoryPanel; // Category selection panel
    public GameObject resultScrollView; // Scroll view for results (hidden by default)

    [Header("AR Display")]
    public ARSearchResultDisplay arDisplay; // AR表示システム
    public bool useARDisplay = true; // AR表示を使用するか

    [Header("Settings")]
    public GameObject settingsPanel; // 設定パネル
    public GameObject controllerModeButton; // コントローラーモードボタン
    public GameObject handModeButton; // ハンドモードボタン
    
    [Header("Server Settings")]
    public InputField serverUrlInput; // サーバーURL入力欄
    public Button saveServerUrlButton; // URL保存ボタン
    
    public enum InputMode { Controller, HandTracking }
    public InputMode currentInputMode = InputMode.Controller;

    [Header("Dependencies")]
    public ImageUploader imageUploader;

    // Common objects in COCO dataset (YOLO default) - Top items for grid
    private readonly List<string> _gridCategories = new List<string>
    {
        "All", "person", "bottle", "cell phone", "laptop", 
        "chair", "bag", "book"
    };

    private string _currentSearchTerm = "All";
    private int _pendingImageDownloads = 0;
    private Tween _loadingSpinnerTween;
    private Tween _loadingPulseTween;
    
    // 複数カテゴリー選択用
    private HashSet<string> _selectedCategories = new HashSet<string>();
    private Dictionary<string, Button> _categoryButtons = new Dictionary<string, Button>();
    // Pagination
    private List<string> _allCategories = new List<string>();
    private int _currentPage = 0;
    private const int ITEMS_PER_PAGE = 12; // 3x4 grid per page
    private Text _pageIndicatorText;
    private Button _prevPageButton;
    private Button _nextPageButton;
    
    // ★ クリックデバウンス用
    private float _lastCategoryClickTime = 0f;
    private const float CLICK_DEBOUNCE_TIME = 0.3f; // 0.3秒間は連続クリック無視

    void ShowLoading(bool show)
    {
        if (loadingPanel == null) return;
        
        if (show)
        {
            loadingPanel.SetActive(true);
            
            // スピナーを取得（子オブジェクトとして想定）
            Transform spinner = loadingPanel.transform.Find("Spinner");
            if (spinner != null)
            {
                // 回転アニメーション（無限ループ）
                _loadingSpinnerTween?.Kill();
                _loadingSpinnerTween = spinner.DORotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Restart)
                    .SetEase(Ease.Linear);
                
                // パルス効果（スケールのパルス）
                _loadingPulseTween?.Kill();
                _loadingPulseTween = spinner.DOScale(1.1f, 0.5f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            
            // フェードイン
            CanvasGroup canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
            }
        }
        else
        {
            // アニメーションを停止
            _loadingSpinnerTween?.Kill();
            _loadingPulseTween?.Kill();
            
            // フェードアウト
            CanvasGroup canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
                    loadingPanel.SetActive(false);
                });
            }
            else
            {
                loadingPanel.SetActive(false);
            }
        }
    }

    private GameObject _categoryButtonTemplate;

    void OnEnable()
    {
        DebugLog("SearchUI: OnEnable");
        if (imageUploader != null)
        {
            // Subscribe safely
            imageUploader.OnObjectListUpdated -= RebuildCategoryButtons;
            imageUploader.OnObjectListUpdated += RebuildCategoryButtons;
            
            // Force refresh
            StartCoroutine(FetchRoutine());
            StartCoroutine(DirectFetchObjectList());
        }
    }



    
    void OnDisable()
    {
        if (imageUploader != null)
        {
            imageUploader.OnObjectListUpdated -= RebuildCategoryButtons;
        }
    }

    void Start()
    {
        DebugLog("[SearchUI] Start initializing...");
        
        if (imageUploader == null) Debug.LogError("[SearchUI] ImageUploader is NULL!");

        // Ensure LogForwarder exists for server-side debugging
        LogForwarder existingForwarder = GetComponent<LogForwarder>();
        if (existingForwarder == null)
        {
            DebugLog("SearchUI: Adding LogForwarder...");
            var lf = gameObject.AddComponent<LogForwarder>();
            if (imageUploader != null)
            {
                lf.serverUrlBase = imageUploader.serverUrlBase; // Sync IP
            }
        }

        SetupDropdown(); // Legacy support
        
        // Prepare dynamic button generation
        if (categoryButtonContainer != null && categoryButtonContainer.childCount > 0)
        {
            // Use the first button as a template
            Transform firstChild = categoryButtonContainer.GetChild(0);
            _categoryButtonTemplate = firstChild.gameObject;
            _categoryButtonTemplate.SetActive(false); // Hide template
            DebugLog($"SearchUI: Template captured from existing child: {_categoryButtonTemplate.name}");
            
            // Destroy other existing static buttons
            for (int i = 1; i < categoryButtonContainer.childCount; i++)
            {
                Destroy(categoryButtonContainer.GetChild(i).gameObject);
            }
        }
        else if (categoryButtonContainer != null)
        {
            // FALLBACK: Create a simple template button programmatically
            DebugLog("SearchUI: No children in container - creating fallback template");
            
            _categoryButtonTemplate = new GameObject("TemplateButton");
            _categoryButtonTemplate.transform.SetParent(categoryButtonContainer, false);
            
            // Add Image (button background)
            var img = _categoryButtonTemplate.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
            
            // Add Button
            var btn = _categoryButtonTemplate.AddComponent<Button>();
            btn.targetGraphic = img;
            
            // Add Text child
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(_categoryButtonTemplate.transform, false);
            var txt = textObj.AddComponent<Text>();
            txt.text = "Template";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            
            // Size the text to fill button
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // Add BoxCollider for NRSDK
            var col = _categoryButtonTemplate.AddComponent<BoxCollider>();
            col.size = new Vector3(160, 75, 1);
            
            _categoryButtonTemplate.SetActive(false);
            DebugLog("SearchUI: Fallback template created");
        }
        else
        {
            Debug.LogError("[SearchUI] categoryButtonContainer is NULL!");
            DebugLog("SearchUI: ERROR - categoryButtonContainer is NULL!");
        }
        
        // 初期表示としてデフォルトカテゴリでボタンを生成（サーバー通信待ちの間も表示する）
        if (_categoryButtonTemplate != null)
        {
            RebuildCategoryButtons(_gridCategories);
        }
        
        // Setup initial UI states
        UpdateStatus("Ready");
        
        SetupServerSettingsUI(); // 初期化呼び出し
        
        // ★ ランタイムで検索ボタンをバインド（エディター設定が効かない場合の保険）
        BindSearchExecuteButton();
        
        // ★ 設定メニューボタンをランタイムでバインド
        BindSettingsMenuButtons();
        
        // Direct fetch as fallback (in case event subscription doesn't work)
        StartCoroutine(DirectFetchObjectList());
        
        Debug.Log("[SearchUI] Initialized successfully");
    }
    
    /// <summary>
    /// 検索実行ボタンをランタイムでバインド
    /// </summary>
    void BindSearchExecuteButton()
    {
        // ★ SearchExecuteButtonはcategoryPanelの子に配置
        if (categoryPanel != null)
        {
            Transform searchBtn = categoryPanel.transform.Find("SearchExecuteButton");
            if (searchBtn != null)
            {
                Button btn = searchBtn.GetComponent<Button>();
                if (btn != null)
                {
                    // 既存のリスナーをクリアして新しくバインド
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        Debug.Log("[SearchUI] SearchExecuteButton clicked!");
                        ExecuteSearch();
                    });
                    Debug.Log("[SearchUI] SearchExecuteButton bound successfully");
                }
            }
            else
            {
                Debug.LogWarning("[SearchUI] SearchExecuteButton not found in categoryPanel");
            }
        }
        
        // CancelButtonはcategoryPanel内に残っている
        if (categoryPanel != null)
        {
            Transform cancelBtn = categoryPanel.transform.Find("CancelButton");
            if (cancelBtn != null)
            {
                Button btn = cancelBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        Debug.Log("[SearchUI] CancelButton clicked!");
                        HideCategoryPanel();
                    });
                    Debug.Log("[SearchUI] CancelButton bound successfully");
                }
            }
        }
    }
    
    /// <summary>
    /// 設定メニューボタンをランタイムでバインド
    /// </summary>
    void BindSettingsMenuButtons()
    {
        // 設定メニューを探す
        GameObject settingsMenu = GameObject.Find("SettingsMenuPanel")
            ?? GameObject.Find("SettingsHoverMenu")
            ?? GameObject.Find("SettingsPanel")
            ?? GameObject.Find("SettingsMenu");
        if (settingsMenu == null)
        {
            GameObject searchPanel = GameObject.Find("SearchPanel");
            if (searchPanel != null)
            {
                Transform found = FindChildRecursive(
                    searchPanel.transform,
                    "SettingsMenuPanel",
                    "SettingsHoverMenu",
                    "SettingsPanel",
                    "SettingsMenu"
                );
                if (found != null) settingsMenu = found.gameObject;
            }
        }
        if (settingsMenu == null)
        {
            Debug.LogWarning("[SearchUI] Settings menu not found");
            return;
        }
        
        Debug.Log("[SearchUI] Binding settings menu buttons...");
        
        // 入力モードボタン (インデックス2)
        Transform inputModeBtn = FindChildRecursive(
            settingsMenu.transform,
            "InputMode",
            "InputModeButton",
            "入力モード",
            "入力モードButton"
        );
        if (inputModeBtn != null)
        {
            Button btn = inputModeBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    Debug.Log("[SearchUI] InputMode button clicked!");
                    settingsMenu.SetActive(false);
                    
                    var newMode = currentInputMode == InputMode.Controller 
                        ? InputMode.HandTracking 
                        : InputMode.Controller;
                    SetInputMode(newMode);
                    
                    // ボタンラベル更新
                    Text btnText = inputModeBtn.GetComponentInChildren<Text>();
                    if (btnText != null)
                    {
                        btnText.text = newMode == InputMode.Controller 
                            ? "入力 コントローラー" 
                            : "入力 ハンド";
                    }
                });
                Debug.Log("[SearchUI] InputMode button bound");
            }
        }
        
        // +登録ボタン (インデックス3)
        Transform registrationBtn = FindChildRecursive(
            settingsMenu.transform,
            "Registration",
            "RegistrationButton",
            "+ 登録",
            "+ 登録Button"
        );
        if (registrationBtn != null)
        {
            Button btn = registrationBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    Debug.Log("[SearchUI] Registration button clicked!");
                    settingsMenu.SetActive(false);
                    
                    if (imageUploader != null)
                    {
                        imageUploader.ShowRegistrationSelectPanel();
                    }
                });
                Debug.Log("[SearchUI] Registration button bound");
            }
        }
        
        Debug.Log("[SearchUI] Settings menu buttons binding complete");
    }

    private Transform FindChildRecursive(Transform parent, params string[] names)
    {
        if (parent == null || names == null || names.Length == 0) return null;
        foreach (string name in names)
        {
            if (parent.name == name) return parent;
        }
        foreach (Transform child in parent)
        {
            Transform hit = FindChildRecursive(child, names);
            if (hit != null) return hit;
        }
        return null;
    }
    
    System.Collections.IEnumerator FetchRoutine()
    {
        DebugLog("SearchUI: FetchRoutine started");
        yield return new WaitForSeconds(0.5f);
        if (imageUploader != null)
        {
            DebugLog("SearchUI: Calling RefreshRegisteredList");
            imageUploader.RefreshRegisteredList();
        }
    }
    
    /// <summary>
    /// Direct fallback fetch from server (bypasses event subscription)
    /// </summary>
    System.Collections.IEnumerator DirectFetchObjectList()
    {
        yield return new WaitForSeconds(1.5f); // Wait for initial setup
        
        string serverUrl = imageUploader != null 
            ? imageUploader.serverUrlBase 
            : "http://localhost:5000";
        
        string url = $"{serverUrl}/objects/list";
        DebugLog($"SearchUI: DirectFetch from {url}");
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = 5;
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                DebugLog($"SearchUI: DirectFetch success, parsing JSON...");
                
                List<string> categories = ParseObjectListFromJson(json);
                if (categories.Count > 0)
                {
                    DebugLog($"SearchUI: DirectFetch got {categories.Count} categories");
                    RebuildCategoryButtons(categories);
                }
            }
            else
            {
                DebugLog($"SearchUI: DirectFetch failed: {www.error}");
            }
        }
    }
    
    /// <summary>
    /// Parse object list JSON manually (lightweight alternative to JsonUtility)
    /// </summary>
    List<string> ParseObjectListFromJson(string json)
    {
        List<string> result = new List<string>();
        result.Add("All"); // Always include "All" first
        
        try
        {
            // Simple regex-free parsing: find all "name":"xxx" patterns
            int searchStart = 0;
            string nameKey = "\"name\":";
            
            while (true)
            {
                int keyIndex = json.IndexOf(nameKey, searchStart);
                if (keyIndex < 0) break;
                
                int valueStart = keyIndex + nameKey.Length;
                // Skip whitespace and opening quote
                while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '"'))
                    valueStart++;
                
                // Find closing quote
                int valueEnd = json.IndexOf('"', valueStart);
                if (valueEnd < 0) break;
                
                string name = json.Substring(valueStart, valueEnd - valueStart);
                if (!string.IsNullOrEmpty(name) && !result.Contains(name))
                {
                    result.Add(name);
                }
                
                searchStart = valueEnd + 1;
            }
        }
        catch (Exception e)
        {
            DebugLog($"SearchUI: JSON parse error: {e.Message}");
        }
        
        return result;
    }

    // Callback from ImageUploader
    void RebuildCategoryButtons(List<string> categories)
    {
        DebugLog($"SearchUI: Rebuild called with {categories.Count} items");
        
        // Store full list for pagination
        _allCategories = new List<string>(categories);
        _currentPage = 0;
        
        // Create page navigation UI if not exists
        EnsurePageNavigationUI();
        
        // Display current page
        DisplayCurrentPage();
    }
    
    void EnsurePageNavigationUI()
    {
        if (categoryButtonContainer == null) return;
        
        Transform parent = categoryButtonContainer.parent;
        if (parent == null) return;
        
        // Create or find page indicator
        Transform existingIndicator = parent.Find("PageIndicator");
        if (existingIndicator == null)
        {
            GameObject indicatorObj = new GameObject("PageIndicator");
            indicatorObj.transform.SetParent(parent, false);
            
            RectTransform rt = indicatorObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -200);
            rt.sizeDelta = new Vector2(200, 40);
            
            _pageIndicatorText = indicatorObj.AddComponent<Text>();
            _pageIndicatorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _pageIndicatorText.fontSize = 20;
            _pageIndicatorText.color = Color.white;
            _pageIndicatorText.alignment = TextAnchor.MiddleCenter;
            _pageIndicatorText.text = "1/1";
        }
        else
        {
            _pageIndicatorText = existingIndicator.GetComponent<Text>();
        }
        
        // Create Prev button (左下端 - カテゴリと重ならないように)
        Transform existingPrev = parent.Find("PrevPageButton");
        if (existingPrev == null)
        {
            _prevPageButton = CreatePageButton(parent, "PrevPageButton", "◀ Prev", new Vector2(-230, -200));
            _prevPageButton.onClick.AddListener(PrevPage);
        }
        else
        {
            _prevPageButton = existingPrev.GetComponent<Button>();
        }
        
        // Create Next button (右下端)
        Transform existingNext = parent.Find("NextPageButton");
        if (existingNext == null)
        {
            _nextPageButton = CreatePageButton(parent, "NextPageButton", "Next ▶", new Vector2(230, -200));
            _nextPageButton.onClick.AddListener(NextPage);
        }
        else
        {
            _nextPageButton = existingNext.GetComponent<Button>();
        }
    }
    
    Button CreatePageButton(Transform parent, string name, string text, Vector2 position)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(90, 35);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 0.95f);
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        // Add collider for raycast
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(90, 35, 1);
        
        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        txt.raycastTarget = false;
        
        return btn;
    }
    
    void DisplayCurrentPage()
    {
        // Use Resources.FindObjectsOfTypeAll to find hidden templates if lost, or create new
        if (_categoryButtonTemplate == null)
        {
            GameObject tpl = new GameObject("CategoryBtnTemplate");
            // テンプレートは非表示のまま保持
            tpl.SetActive(false);
            // DontDestroyOnLoadはしないが、シーン内に保持
            if (this.transform != null) tpl.transform.SetParent(this.transform);
            
            Image img = tpl.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
            
            Button btn = tpl.AddComponent<Button>();
            btn.targetGraphic = img;
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(tpl.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = "Category";
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.raycastTarget = false;
            
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            _categoryButtonTemplate = tpl;
        }

        if (categoryButtonContainer == null) return;
        
        // Clean up existing buttons
        foreach (Transform child in categoryButtonContainer)
        {
            if (child.gameObject != _categoryButtonTemplate)
            {
                Destroy(child.gameObject);
            }
        }
        _categoryButtons.Clear();
        
        // Calculate page range
        int totalPages = Mathf.CeilToInt((float)_allCategories.Count / ITEMS_PER_PAGE);
        if (totalPages == 0) totalPages = 1;
        _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
        
        int startIndex = _currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, _allCategories.Count);
        
        // Create buttons for current page
        for (int i = startIndex; i < endIndex; i++)
        {
            string cat = _allCategories[i];
            GameObject btnObj = Instantiate(_categoryButtonTemplate, categoryButtonContainer);
            btnObj.name = cat;
            btnObj.SetActive(true);
            
            Text btnText = btnObj.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = cat;
                btnText.raycastTarget = false; // ★ テキストがレイキャストを受けないように
            }
            
            // ★ 子要素のコライダーを削除（重複クリック防止）
            foreach (Collider childCol in btnObj.GetComponentsInChildren<Collider>())
            {
                if (childCol.gameObject != btnObj)
                {
                    Destroy(childCol);
                }
            }
            
            // BoxColliderを確認・追加 (レイキャスト用)
            BoxCollider col = btnObj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = btnObj.AddComponent<BoxCollider>();
            }
            // RectTransformからサイズを取得
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                col.size = new Vector3(rt.rect.width > 0 ? rt.rect.width : 120, 
                                       rt.rect.height > 0 ? rt.rect.height : 50, 
                                       10); // 厚みを増やしてヒットしやすく
            }
            else
            {
                col.size = new Vector3(120, 50, 10); // デフォルトサイズ
            }
            
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string catCopy = cat; // Closure copy
                btn.onClick.AddListener(() => OnCategoryButtonClicked(catCopy));
                _categoryButtons[cat] = btn;
            }
        }
        
        // Update page indicator
        if (_pageIndicatorText != null)
        {
            _pageIndicatorText.text = $"{_currentPage + 1}/{totalPages}";
        }
        
        // Update nav button states
        if (_prevPageButton != null)
        {
            _prevPageButton.interactable = _currentPage > 0;
        }
        if (_nextPageButton != null)
        {
            _nextPageButton.interactable = _currentPage < totalPages - 1;
        }
        
        UpdateCategoryButtonVisuals();
        DebugLog($"SearchUI: Page {_currentPage + 1}/{totalPages} ({endIndex - startIndex} items)");
    }
    
    public void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)_allCategories.Count / ITEMS_PER_PAGE);
        if (_currentPage < totalPages - 1)
        {
            _currentPage++;
            DisplayCurrentPage();
        }
    }
    
    public void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            DisplayCurrentPage();
        }
    }
    
    void DebugLog(string msg)
    {
        Debug.Log(msg);
        if (imageUploader != null && imageUploader.debugText != null)
        {
            // Simple prepend log
            string current = imageUploader.debugText.text;
            if (current.Length > 1000) current = current.Substring(0, 1000);
            imageUploader.debugText.text = $"[UI] {msg}\n" + current;
        }
    }

    void Update()
    {
        // Manual Raycast logic removed to rely on standard EventSystem (NRCanvasRaycaster)
        // This resolves the issue where tapping buttons only worked via Drag-Out gestures.
        
        // If specific input handling is needed for non-UI elements, implement here.
    }
    
    // Legacy Raycast methods removed (TryRaycastClick, TryHandRaycastClick, HandlePointerClick)
    
    void ShowTutorial()
    {
        // TutorialManagerをServiceLocatorから取得
        TutorialManager tutorial = ServiceLocator.Instance?.tutorialManager;
        if (tutorial != null)
        {
            tutorial.ResetTutorial();
            tutorial.ShowTutorial();
            HideSettingsPanel();
        }
        else
        {
            Debug.LogWarning("[SearchUI] TutorialManager not found in ServiceLocator!");
        }
    }
    


    public void ToggleCategoryPanel()
    {
        var uiInit = FindObjectOfType<NRKernal.UIEventInitializer>();
        string msg = $"ToggleCat called. PnlNull: {categoryPanel == null}";
        if (uiInit != null) uiInit.LogToScreen(msg);
        Debug.Log($"[SearchUIManager] {msg}");

        if (categoryPanel == null)
        {
            // Fallback: Create CategoryPanel dynamically if missing
            if (uiInit != null) uiInit.LogToScreen("Creating CategoryPanel dynamically...");
            
            GameObject pnl = new GameObject("CategoryPanel");
            pnl.SetActive(false); 
            pnl.transform.SetParent(this.transform, false);
            pnl.transform.localScale = Vector3.one;
            pnl.transform.localPosition = Vector3.zero;
            pnl.transform.localRotation = Quaternion.identity;
            
            RectTransform rt = pnl.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // 背景
            Image bg = pnl.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            // コンテナ
            GameObject container = new GameObject("CategoryContainer");
            container.transform.SetParent(pnl.transform, false);
            RectTransform crt = container.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(20, 20);
            crt.offsetMax = new Vector2(-20, -20);
            
            // レイアウト
            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160, 60);
            grid.spacing = new Vector2(10, 10);
            
            categoryPanel = pnl;
            categoryButtonContainer = container.transform;
            
            // ボタン生成（テンプレートが必要だが、無ければデフォルト生成）
            EnsurePageNavigationUI();
            DisplayCurrentPage(); // これでボタンが作られるはず（テンプレートが無い場合の対応が必要）
        }

        if (categoryPanel != null)
        {
            bool newState = !categoryPanel.activeSelf;
            categoryPanel.SetActive(newState);
            
            // 表示時にユーザーの前方に配置
            if (newState)
            {
                PositionCategoryPanelInFront();
            }
            
            if (uiInit != null) uiInit.LogToScreen($"Set Pnl Active: {newState}");

        }
        else
        {
            if (uiInit != null) uiInit.LogToScreen("ERROR: CategoryPanel is NULL");
        }
    }
    
    /// <summary>
    /// カテゴリパネルをユーザーの手前に配置（1.5m）
    /// FlashbackCanvasは10m先、カテゴリパネルは手前に独立配置
    /// </summary>
    void PositionCategoryPanelInFront()
    {
        if (categoryPanel == null) return;
        
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // 親（FlashbackCanvas）から切り離し
        if (categoryPanel.transform.parent != null)
        {
            categoryPanel.transform.SetParent(null, true);
            
            // 手前配置用のスケール（1.5m用）
            categoryPanel.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            
            // Canvas追加
            Canvas panelCanvas = categoryPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
            {
                panelCanvas = categoryPanel.AddComponent<Canvas>();
                panelCanvas.renderMode = RenderMode.WorldSpace;
                panelCanvas.worldCamera = cam;
                
                var scaler = categoryPanel.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;
                
                categoryPanel.AddComponent<GraphicRaycaster>();
                categoryPanel.AddComponent<CanvasRaycastTarget>(); // NRSDK用
            }
        }
        
        // ★ 手前に配置（1.5m）
        float distance = 1.5f;
        
        Vector3 forward = cam.transform.forward;
        forward.y = 0;
        if (forward.magnitude < 0.1f) forward = Vector3.forward;
        forward.Normalize();
        
        Vector3 panelPos = cam.transform.position + forward * distance;
        panelPos.y = cam.transform.position.y - 0.1f;
        
        categoryPanel.transform.position = panelPos;
        categoryPanel.transform.rotation = Quaternion.LookRotation(forward);
        
        Debug.Log($"[SearchUIManager] CategoryPanel at {distance}m (front)");
    }

    void HideCategoryPanel()
    {
        if (categoryPanel != null)
        {
            categoryPanel.SetActive(false);
        }
    }

    // ============ Settings Panel Methods ============
    void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
    
    void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// サーバーURL設定の初期化（パネルが開くたびに呼んでも良いが、現状はStartでバインド）
    /// </summary>
    public void SetupServerSettingsUI()
    {
        if (serverUrlInput != null)
        {
            // 現在のURLを取得して表示
            string currentUrl = PlayerPrefs.GetString("ServerUrl", "http://10.218.149.69:5000");
            serverUrlInput.text = currentUrl;
        }
        
        if (saveServerUrlButton != null)
        {
            saveServerUrlButton.onClick.RemoveAllListeners();
            saveServerUrlButton.onClick.AddListener(OnServerUrlSaveClicked);
        }
    }
    
    void OnServerUrlSaveClicked()
    {
        if (serverUrlInput == null || imageUploader == null) return;
        
        string newUrl = serverUrlInput.text;
        if (string.IsNullOrEmpty(newUrl)) return;
        
        // URLの更新
        imageUploader.UpdateServerUrl(newUrl);
        
        // フィードバック（簡易）
        StartCoroutine(ShowSaveFeedback());
    }
    
    System.Collections.IEnumerator ShowSaveFeedback()
    {
        if (saveServerUrlButton != null)
        {
            Text btnText = saveServerUrlButton.GetComponentInChildren<Text>();
            string originalText = btnText != null ? btnText.text : "Save";
            
            if (btnText != null) btnText.text = "Saved!";
            yield return new WaitForSeconds(1.5f);
            if (btnText != null) btnText.text = originalText;
        }
    }
    
    /// <summary>
    /// 入力モードを設定
    /// </summary>
    public void SetInputMode(InputMode mode)
    {
        currentInputMode = mode;
        Debug.Log($"[SearchUI] Input mode changed to: {mode}");
        
        // ボタンのハイライト更新
        UpdateInputModeButtonVisuals();
        
        // NRSDKの入力ソースを切替
        // Note: SetInputSourceを呼ぶとNRSDKが自動的にハンドトラッキングを開始/停止する
        try
        {
            if (mode == InputMode.Controller)
            {
                NRInput.SetInputSource(InputSourceEnum.Controller);
                Debug.Log("[SearchUI] Switched to Controller mode");
            }
            else
            {
                NRInput.SetInputSource(InputSourceEnum.Hands);
                Debug.Log("[SearchUI] Switched to Hands mode");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SearchUI] Failed to set input source: {e.Message}");
        }
        
        UpdateStatus($"Input: {mode}");
    }
    
    void UpdateInputModeButtonVisuals()
    {
        if (controllerModeButton != null)
        {
            var img = controllerModeButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = currentInputMode == InputMode.Controller 
                    ? new Color(0.2f, 0.6f, 0.3f, 0.95f)  // 緑（選択中）
                    : new Color(0.25f, 0.25f, 0.25f, 0.95f); // グレー
            }
        }
        
        if (handModeButton != null)
        {
            var img = handModeButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = currentInputMode == InputMode.HandTracking 
                    ? new Color(0.2f, 0.6f, 0.3f, 0.95f)  // 緑（選択中）
                    : new Color(0.25f, 0.25f, 0.25f, 0.95f); // グレー
            }
        }
    }

    /// <summary>
    /// 検索結果の表示を終了
    /// </summary>
    public void CloseSearchResults()
    {
        Debug.Log("[SearchUI] Closing search results...");
        
        // カードをクリア
        if (arDisplay != null)
        {
            arDisplay.ClearResults();
        }
        
        // 結果コンテナをクリア
        if (resultContainer != null)
        {
            foreach (Transform child in resultContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        // ステータスをリセット
        UpdateStatus("Ready");
        
        // Closeボタンを非表示
        if (closeResultsButton != null)
            closeResultsButton.gameObject.SetActive(false);
        
        Debug.Log("[SearchUI] Search results closed.");
    }

    void SetupCategoryButtons()
    {
        // Buttons are set up in SceneSetupTool, this just registers click events
        if (categoryButtonContainer == null)
        {
            Debug.LogError("[SearchUI] categoryButtonContainer is NULL!");
            return;
        }

        Debug.Log($"[SearchUI] SetupCategoryButtons - Found {categoryButtonContainer.childCount} children");

        foreach (Transform child in categoryButtonContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                string category = child.name; // Use button name as category
                Debug.Log($"[SearchUI] Registering button: {category}");
                btn.onClick.RemoveAllListeners(); // ★ 重複防止
                btn.onClick.AddListener(() => OnCategoryButtonClicked(category));
            }
        }
    }

    public void OnCategoryButtonClicked(string category)
    {
        // ★ デバウンスチェック - 連続クリック防止
        if (Time.time - _lastCategoryClickTime < CLICK_DEBOUNCE_TIME)
        {
            Debug.Log($"[SearchUI] Category click ignored (debounce): {category}");
            return;
        }
        _lastCategoryClickTime = Time.time;
        
        Debug.Log($"[SearchUI] Category clicked: {category}");
        
        // "All" を選択した場合、他のカテゴリーをクリア
        if (category == "All")
        {
            _selectedCategories.Clear();
            _selectedCategories.Add("All");
            UpdateCategoryButtonVisuals();
            ExecuteSearch();
            return;
        }
        
        // "All" が選択されている場合、クリア
        _selectedCategories.Remove("All");
        
        // トグル式選択
        if (_selectedCategories.Contains(category))
        {
            _selectedCategories.Remove(category);
        }
        else
        {
            _selectedCategories.Add(category);
        }
        
        // 何も選択されていなければ "All" を選択
        if (_selectedCategories.Count == 0)
        {
            _selectedCategories.Add("All");
        }
        
        UpdateCategoryButtonVisuals();
        
        // カテゴリー選択後、少し待ってから検索実行
        Debug.Log($"[SearchUI] Selected categories: {string.Join(", ", _selectedCategories)}");
    }
    
    /// <summary>
    /// 複数カテゴリーで検索を実行
    /// </summary>
    public void ExecuteSearch()
    {
        if (_selectedCategories.Count == 0)
        {
            _selectedCategories.Add("All");
        }
        
        // 検索統計を記録
        SearchStatistics.Instance.RecordMultiSearch(_selectedCategories);
        
        // カテゴリーを結合
        _currentSearchTerm = string.Join(",", _selectedCategories);
        
        Debug.Log($"[SearchUI] Executing search with: {_currentSearchTerm}");
        UpdateStatus($"Searching: {_currentSearchTerm}...");
        ShowLoading(true);
        
        // Hide category panel
        if (categoryPanel != null)
        {
            categoryPanel.SetActive(false);
        }
        
        if (imageUploader != null)
        {
            // Clear previous results (if container exists)
            if (resultContainer != null)
            {
                foreach (Transform child in resultContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            imageUploader.SearchObjects(_currentSearchTerm, OnSearchCompleted);
        }
    }
    
    /// <summary>
    /// カテゴリーボタンの選択状態を視覚的に更新
    /// </summary>
    void UpdateCategoryButtonVisuals()
    {
        if (categoryButtonContainer == null) return;
        
        foreach (Transform child in categoryButtonContainer)
        {
            string category = child.name;
            Image btnImage = child.GetComponent<Image>();
            
            if (btnImage != null)
            {
                bool isSelected = _selectedCategories.Contains(category);
                
                if (isSelected)
                {
                    // 選択色（ネオングリーン + スケールアップ）
                    btnImage.color = new Color(0.2f, 1f, 0.4f, 1f); // Neon Green
                    child.localScale = Vector3.one * 1.15f;
                }
                else
                {
                    // 非選択色（暗いグレー）
                    btnImage.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
                    child.localScale = Vector3.one;
                }
            }
        }
    }
    
    /// <summary>
    /// カテゴリーボタンを登録
    /// </summary>
    public void RegisterCategoryButton(string category, Button button)
    {
        _categoryButtons[category] = button;
    }

    void SetupDropdown()
    {
        if (objectDropdown == null) return;

        objectDropdown.ClearOptions();
        objectDropdown.AddOptions(_gridCategories);
        objectDropdown.onValueChanged.AddListener(delegate {
            OnDropdownChanged(objectDropdown);
        });
    }

    void OnDropdownChanged(Dropdown change)
    {
        _currentSearchTerm = _gridCategories[change.value];
        UpdateStatus($"Selected: {_currentSearchTerm}");
    }

    void OnSearchButtonClicked()
    {
        if (imageUploader == null)
        {
            UpdateStatus("Error: ImageUploader not connected.");
            return;
        }

        UpdateStatus($"Searching server for '{_currentSearchTerm}'...");
        
        // Clear previous results
        if (resultContainer != null)
        {
            foreach (Transform child in resultContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Trigger server search
        imageUploader.SearchObjects(_currentSearchTerm, OnSearchCompleted);
    }

    // Callback when server response is received
    void OnSearchCompleted(List<SearchResultItem> results)
    {
        if (imageUploader != null) imageUploader.Log($"OnSearchCompleted: {results?.Count ?? 0} results");
        if (imageUploader != null) imageUploader.Log($"useARDisplay={useARDisplay}, arDisplay={(arDisplay != null ? "OK" : "NULL")}");
        
        if (results == null || results.Count == 0)
        {
            UpdateStatus($"No '{_currentSearchTerm}' found on server.");
            ShowLoading(false);
            // Hide scroll view when no results
            if (resultScrollView != null) resultScrollView.SetActive(false);
            if (arDisplay != null) arDisplay.ClearResults();
            return;
        }

        UpdateStatus($"Found {results.Count} images. Loading...");
        
        // Closeボタンを表示
        Debug.Log($"[SearchUI] closeResultsButton ref: {(closeResultsButton != null ? "OK" : "NULL")}");
        if (closeResultsButton == null)
        {
            // Try to find by name if not assigned
            GameObject closeBtn = GameObject.Find("CloseResultsButton");
            if (closeBtn != null)
            {
                closeResultsButton = closeBtn.GetComponent<Button>();
                Debug.Log("[SearchUI] Found CloseResultsButton by name");
            }
        }
        if (closeResultsButton != null)
        {
            closeResultsButton.gameObject.SetActive(true);
            Debug.Log("[SearchUI] CloseResultsButton shown");
        }
        else
        {
            Debug.LogWarning("[SearchUI] CloseResultsButton not found!");
        }

        // AR表示モードを使用する場合
        if (useARDisplay && arDisplay != null)
        {
            if (imageUploader != null) imageUploader.Log("CALLING arDisplay.ShowResults");
            // 2D ScrollViewを非表示
            if (resultScrollView != null) resultScrollView.SetActive(false);
            
            // InfiniteCarousel/TimelineCorridorモードでは内部で画像管理するためLoadingをすぐに非表示
            if (arDisplay.currentMode == ARSearchResultDisplay.DisplayMode.InfiniteCarousel ||
                arDisplay.currentMode == ARSearchResultDisplay.DisplayMode.TimelineCorridor)
            {
                UpdateStatus($"Found {results.Count} images");
                ShowLoading(false);
                arDisplay.ShowResults(results, null);
            }
            else
            {
                // FloatingCardモードでは従来通りコールバックで管理
                _pendingImageDownloads = results.Count;
                arDisplay.ShowResults(results, (tex, item) => {
                    // 画像ダウンロード処理
                    StartCoroutine(DownloadAndApplyImage(item, tex));
                });
            }
        }
        else
        {
            Debug.Log("[SearchUI] Using 2D Display mode (arDisplay is null or useARDisplay is false)");
            // 従来の2D表示
            // 従来の2D表示
            if (resultScrollView != null) resultScrollView.SetActive(true);
            _pendingImageDownloads = results.Count;

            foreach (var item in results)
            {
                CreateResultEntry(item);
            }
        }
    }

    // AR表示用の画像ダウンロード
    System.Collections.IEnumerator DownloadAndApplyImage(SearchResultItem item, Texture2D placeholder)
    {
        if (imageUploader != null) imageUploader.Log($"Downloading: {item.url}");
        
        yield return StartCoroutine(imageUploader.DownloadImage(item.url, (tex) => {
            if (tex != null)
            {
                if (imageUploader != null) imageUploader.Log($"Downloaded OK: {item.url}");
                // ResultCard3Dに画像を適用
                var cards = FindObjectsOfType<ResultCard3D>();
                foreach (var card in cards)
                {
                    if (card.itemData != null && card.itemData.url == item.url)
                    {
                        card.ApplyTexture(tex);
                        if (imageUploader != null) imageUploader.Log($"Applied texture to card");
                        break;
                    }
                }
            }
            else
            {
                if (imageUploader != null) imageUploader.Log($"Download FAILED: {item.url}");
            }
            
            _pendingImageDownloads--;
            if (_pendingImageDownloads <= 0)
            {
                ShowLoading(false);
                UpdateStatus($"AR表示完了: '{_currentSearchTerm}'");
            }
        }));
    }

    void CreateResultEntry(SearchResultItem item)
    {
        if (resultImagePrefab == null || resultContainer == null) return;

        GameObject entry = Instantiate(resultImagePrefab, resultContainer);
        entry.SetActive(true); // Ensure item is visible (prefab might be hidden)
        
        // Setup Text info
        Text infoText = entry.GetComponentInChildren<Text>();
        if (infoText != null)
        {
            string objStr = "";
            if (item.objects != null && item.objects.Count > 0)
            {
                objStr = "\n" + string.Join(", ", item.objects);
            }
            infoText.text = $"{item.timestamp}{objStr}";
        }

        // Setup Image (Download from server)
        RawImage rawImage = entry.GetComponentInChildren<RawImage>();
        if (rawImage != null)
        {
            // Set a placeholder color while loading
            rawImage.color = Color.gray;
            
            StartCoroutine(imageUploader.DownloadImage(item.url, (tex) => {
                if (tex != null && rawImage != null)
                {
                    rawImage.texture = tex;
                    rawImage.color = Color.white; // Restore color
                    
                    AspectRatioFitter fitter = rawImage.GetComponent<AspectRatioFitter>();
                    if (fitter != null)
                    {
                        fitter.aspectRatio = (float)tex.width / tex.height;
                    }
                    
                    // Force layout rebuild to fix any sizing issues
                    LayoutRebuilder.ForceRebuildLayoutImmediate(entry.GetComponent<RectTransform>());
                }
                
                // Track download completion
                _pendingImageDownloads--;
                if (_pendingImageDownloads <= 0)
                {
                    ShowLoading(false);
                    UpdateStatus($"Found {(resultContainer != null ? resultContainer.childCount : 0)} images of '{_currentSearchTerm}'.");
                }
            }));
        }
        else
        {
            // No image to download, decrement counter
            _pendingImageDownloads--;
            if (_pendingImageDownloads <= 0)
            {
                ShowLoading(false);
            }
        }
    }

    void UpdateStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
        Debug.Log($"[SearchUI] {msg}");
    }
}
