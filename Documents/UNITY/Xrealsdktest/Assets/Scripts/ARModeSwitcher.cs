using UnityEngine;
using UnityEngine.UI;
using NRKernal;

/// <summary>
/// AR Display Mode切り替え
/// ボタンクリックでモード選択パネルを表示
/// NRSDK CanvasRaycastTargetを使用してポインター操作に対応
/// </summary>
public class ARModeSwitcher : MonoBehaviour
{
    [Header("References")]
    public Button modeButton;
    public Text modeText;
    public ARSearchResultDisplay arSearchResultDisplay;
    
    private string[] modeNames = { "Card", "Carousel", "Corridor" };
    private GameObject _selectionPanel;
    private Button[] _modeButtons = new Button[3];
    private bool _initialized = false;

    void Start()
    {
        Debug.Log("[ARModeSwitcher] Start() - Initializing...");
        
        // シーンセットアップツールで事前生成されたパネルを探す
        if (_selectionPanel == null) {
            _selectionPanel = GameObject.Find("ARModeSelectionPanel");
            if (_selectionPanel != null) {
                Debug.Log("[ARModeSwitcher] Found pre-generated ARModeSelectionPanel");
                SetupPanelButtons();
            }
        }

        // 少し遅延して初期化（他のコンポーネントを待つ）
        Invoke("Initialize", 0.5f);
    }
    
    void SetupPanelButtons()
    {
        if (_selectionPanel == null) return;
        
        for(int i = 0; i < 3; i++) {
            Transform btnTr = _selectionPanel.transform.Find($"ModeBtn_{i}");
            if (btnTr != null) {
                _modeButtons[i] = btnTr.GetComponent<Button>();
                int idx = i;
                _modeButtons[i].onClick.RemoveAllListeners();
                _modeButtons[i].onClick.AddListener(() => SelectMode(idx));
            }
        }
        Debug.Log("[ARModeSwitcher] Panel buttons setup complete");
    }

    void Initialize()
    {
        if (_initialized) return;
        
        // デフォルトモードを適用
        int savedMode = PlayerPrefs.GetInt("ARDisplayMode", 2); // Gallery Corridor
        savedMode = Mathf.Clamp(savedMode, 0, 2);
        
        UpdateModeText(savedMode);
        
        // ARSearchResultDisplayを探す
        if (arSearchResultDisplay == null)
        {
            arSearchResultDisplay = FindObjectOfType<ARSearchResultDisplay>();
        }
        
        // 初期モードを適用
        if (arSearchResultDisplay != null)
        {
            arSearchResultDisplay.SetDisplayMode((ARSearchResultDisplay.DisplayMode)savedMode);
            Debug.Log($"[ARModeSwitcher] Initial mode set to: {modeNames[savedMode]}");
        }
        
        _initialized = true;
        Debug.Log("[ARModeSwitcher] Initialization complete");
    }

    void Update()
    {
        // 独自Raycastは廃止し、標準のuGUIイベントを使用する
    }

    void ToggleSelectionPanel()
    {
        Debug.Log("[ARModeSwitcher] ToggleSelectionPanel called");
        
        // まず事前生成されたパネルを探す
        if (_selectionPanel == null)
        {
            _selectionPanel = GameObject.Find("ARModeSelectionPanel");
            if (_selectionPanel != null) {
                SetupPanelButtons();
            }
        }
        
        // それでもなければ動的生成（フォールバック）
        if (_selectionPanel == null)
        {
            CreateSelectionPanel();
        }
        
        if (_selectionPanel != null)
        {
            bool newState = !_selectionPanel.activeSelf;
            _selectionPanel.SetActive(newState);
            
            if (newState)
            {
                AlignPanelToCamera();
            }
            
            Debug.Log($"[ARModeSwitcher] Panel visibility: {newState}");
        }
    }
    
    void AlignPanelToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // カメラ前方1.0m、少し下
        Vector3 panelPos = cam.transform.position + cam.transform.forward * 1.0f + cam.transform.up * -0.2f;
        _selectionPanel.transform.position = panelPos;
        _selectionPanel.transform.LookAt(cam.transform);
        _selectionPanel.transform.Rotate(0, 180, 0); // カメラに向ける
    }

    void CreateSelectionPanel()
    {
        Debug.Log("[ARModeSwitcher] Creating selection panel...");
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogError("[ARModeSwitcher] No camera found!");
            return;
        }

        // --- パネル生成 ---
        _selectionPanel = new GameObject("ARModeSelectionPanel");
        AlignPanelToCamera(); // 位置合わせ

        // --- Canvas設定 ---
        Canvas canvas = _selectionPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam; // 重要: Raycast用
        
        _selectionPanel.AddComponent<CanvasScaler>();
        _selectionPanel.AddComponent<GraphicRaycaster>();
        
        // ★★★ これが重要：NRSDKポインターに認識させる ★★★
        _selectionPanel.AddComponent<NRKernal.CanvasRaycastTarget>();

        RectTransform canvasRect = _selectionPanel.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 500);
        canvasRect.localScale = Vector3.one * 0.0008f; 

        // --- 背景 ---
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_selectionPanel.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
        bgImage.raycastTarget = true; // ヒットさせる
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // NRSDKポインターがあたるようにColliderも追加しておく（念のため）
        // BoxCollider bgCol = bgObj.AddComponent<BoxCollider>();
        // bgCol.size = new Vector3(800, 500, 10);

        // --- タイトル ---
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_selectionPanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "AR Display Mode";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.3f, 0.9f, 0.5f);
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.82f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // --- モードボタン ---
        string[] labels = { "Floating Card", "Infinite Carousel", "Gallery Corridor" };
        Color[] colors = {
            new Color(0.3f, 0.6f, 0.9f, 1f),
            new Color(0.9f, 0.5f, 0.2f, 1f),
            new Color(0.6f, 0.3f, 0.8f, 1f)
        };
        
        int currentMode = PlayerPrefs.GetInt("ARDisplayMode", 2);

        for (int i = 0; i < 3; i++)
        {
            int modeIndex = i;
            
            GameObject btnObj = new GameObject($"ModeBtn_{i}");
            btnObj.transform.SetParent(_selectionPanel.transform, false);
            
            // 背景 & Button
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = (i == currentMode) 
                ? new Color(0.2f, 0.8f, 0.4f, 1f) 
                : colors[i];
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(() => SelectMode(modeIndex));

            // レイアウト
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            float yMin = 0.55f - i * 0.25f;
            float yMax = 0.78f - i * 0.25f;
            btnRect.anchorMin = new Vector2(0.08f, yMin);
            btnRect.anchorMax = new Vector2(0.92f, yMax);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            // コライダー（念のため分厚く）
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(640, 115, 100); 

            // テキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = labels[i];
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 28;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.raycastTarget = false; // テキストはRaycastしない
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            _modeButtons[i] = btn;
        }

        // 初期状態で非表示
        _selectionPanel.SetActive(false);
        Debug.Log("[ARModeSwitcher] Selection panel created with CanvasRaycastTarget");
    }

    void SelectMode(int mode)
    {
        Debug.Log($"[ARModeSwitcher] SelectMode({mode}) - {modeNames[mode]}");
        
        PlayerPrefs.SetInt("ARDisplayMode", mode);
        PlayerPrefs.Save();

        UpdateModeText(mode);
        UpdateButtonHighlights(mode);

        if (arSearchResultDisplay == null) arSearchResultDisplay = FindObjectOfType<ARSearchResultDisplay>();
        if (arSearchResultDisplay != null)
        {
            arSearchResultDisplay.SetDisplayMode((ARSearchResultDisplay.DisplayMode)mode);
        }

        if (_selectionPanel != null) _selectionPanel.SetActive(false);
    }

    void UpdateModeText(int mode)
    {
        if (modeText != null) modeText.text = $"AR: {modeNames[mode]}";
    }

    void UpdateButtonHighlights(int selectedMode)
    {
        Color[] normalColors = {
            new Color(0.3f, 0.6f, 0.9f, 1f),
            new Color(0.9f, 0.5f, 0.2f, 1f),
            new Color(0.6f, 0.3f, 0.8f, 1f)
        };
        Color selectedColor = new Color(0.2f, 0.8f, 0.4f, 1f);

        for (int i = 0; i < _modeButtons.Length; i++)
        {
            if (_modeButtons[i] != null)
            {
                Image img = _modeButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == selectedMode) ? selectedColor : normalColors[i];
                }
            }
        }
    }
    
    // UIEventInitializerから呼び出される
    public void OnModeButtonClicked()
    {
        ToggleSelectionPanel();
    }
}
