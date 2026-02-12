using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// AR検索結果の表示モードを管理する基底クラス
/// 設定でFloatingCard (A) / InfiniteCarousel (新) / TimelineCorridor (C) を切り替え可能
/// </summary>
public class ARSearchResultDisplay : MonoBehaviour
{
    public enum DisplayMode
    {
        FloatingCard,       // Option A: 弧状に浮かぶカード
        InfiniteCarousel,   // Option A2: 無限スクロールカルーセル（新）
        TimelineCorridor    // Option C: 時間軸の回廊
    }

    [Header("Display Settings")]
    public DisplayMode currentMode = DisplayMode.InfiniteCarousel; // デフォルトはカルーセル
    public Transform arResultContainer; // AR空間での結果表示親オブジェクト
    public Camera arCamera; // ARカメラ参照

    [Header("Mode Implementations")]
    public FloatingCardDisplay floatingCardDisplay;
    public InfiniteCardCarousel infiniteCarousel;
    public TimelineCorridorDisplay timelineCorridorDisplay;

    [Header("Common Settings")]
    public float cardDistance = 2.0f; // カメラからの距離
    public float animationDuration = 0.5f;

    [Header("Debug")]
    public ImageUploader imageUploader; // For logging to app panel

    private List<GameObject> _activeCards = new List<GameObject>();

    void Awake()
    {
        // 保存された設定を読み込み (デフォルト: Gallery Corridor)
        int savedMode = PlayerPrefs.GetInt("ARDisplayMode", (int)DisplayMode.TimelineCorridor);
        currentMode = (DisplayMode)savedMode;
        Debug.Log($"[ARSearchResultDisplay] Awake - Mode loaded: {currentMode}");
    }

    void Log(string msg)
    {
        if (imageUploader != null) imageUploader.Log($"[ARDisplay] {msg}");
        else Debug.Log($"[ARSearchResultDisplay] {msg}");
    }

    /// <summary>
    /// 検索結果を表示（SearchUIManagerから呼ばれる）
    /// </summary>
    public void ShowResults(List<SearchResultItem> results, System.Action<Texture2D, SearchResultItem> onImageLoaded)
    {
        Log($"ShowResults: {results?.Count ?? 0} results, mode={currentMode}");
        Log($"floatingCardDisplay={(floatingCardDisplay != null ? "OK" : "NULL")}");
        
        ClearResults();

        if (results == null || results.Count == 0) return;
        
        // Limit to most recent 50 results
        const int MAX_RESULTS = 50;
        if (results.Count > MAX_RESULTS)
        {
            Log($"Limiting results from {results.Count} to {MAX_RESULTS}");
            results = results.GetRange(results.Count - MAX_RESULTS, MAX_RESULTS);
        }

        switch (currentMode)
        {
            case DisplayMode.FloatingCard:
                if (floatingCardDisplay != null)
                {
                    Log("Calling FloatingCardDisplay.DisplayResults");
                    floatingCardDisplay.DisplayResults(results, onImageLoaded);
                    Log("FloatingCardDisplay.DisplayResults called");
                }
                else
                {
                    Log("ERROR: floatingCardDisplay is NULL!");
                }
                break;

            case DisplayMode.InfiniteCarousel:
                if (infiniteCarousel != null)
                {
                    Log("Calling InfiniteCardCarousel.DisplayResults");
                    infiniteCarousel.DisplayResults(results, onImageLoaded);
                    Log("InfiniteCardCarousel.DisplayResults called");
                }
                else
                {
                    Log("ERROR: infiniteCarousel is NULL!");
                }
                break;

            case DisplayMode.TimelineCorridor:
                if (timelineCorridorDisplay != null)
                {
                    Log("Calling TimelineCorridorDisplay.DisplayResults");
                    timelineCorridorDisplay.DisplayResults(results, onImageLoaded);
                }
                else
                {
                    Log("ERROR: timelineCorridorDisplay is NULL!");
                }
                break;
        }
        
        // 閉じるボタンを表示
        ShowCloseButton();
        
        // 再生ボタンを表示（ギャラリーモードのみ）
        ShowPlayButton();
    }

    /// <summary>
    /// 表示中の結果をクリア
    /// </summary>
    public void ClearResults()
    {
        foreach (var card in _activeCards)
        {
            if (card != null) Destroy(card);
        }
        _activeCards.Clear();

        if (floatingCardDisplay != null) floatingCardDisplay.ClearCards();
        if (infiniteCarousel != null) infiniteCarousel.ClearCards();
        if (timelineCorridorDisplay != null) timelineCorridorDisplay.ClearCards();
        
        // 閉じるボタンを非表示
        HideCloseButton();
        
        // 再生ボタンを非表示
        HidePlayButton();
    }

    /// <summary>
    /// 表示モードを切り替え
    /// </summary>
    public void SetDisplayMode(DisplayMode mode)
    {
        Debug.Log($"[ARSearchResultDisplay] SetDisplayMode called: {currentMode} -> {mode}");
        
        // 既存の結果をクリア
        ClearResults();
        
        currentMode = mode;
        PlayerPrefs.SetInt("ARDisplayMode", (int)mode);
        PlayerPrefs.Save();
        Debug.Log($"[ARSearchResultDisplay] Mode changed to: {mode}");
    }

    /// <summary>
    /// カメラ前方の位置を取得
    /// </summary>
    public Vector3 GetPositionInFrontOfCamera(float distance, float xOffset = 0, float yOffset = 0)
    {
        if (arCamera == null) arCamera = Camera.main;
        
        Vector3 forward = arCamera.transform.forward;
        Vector3 right = arCamera.transform.right;
        Vector3 up = arCamera.transform.up;
        
        return arCamera.transform.position + forward * distance + right * xOffset + up * yOffset;
    }

    /// <summary>
    /// カメラに向くRotationを取得
    /// </summary>
    public Quaternion GetLookAtCameraRotation(Vector3 position)
    {
        if (arCamera == null) arCamera = Camera.main;
        
        Vector3 directionToCamera = arCamera.transform.position - position;
        directionToCamera.y = 0; // Y軸回転のみ
        
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            return Quaternion.LookRotation(-directionToCamera);
        }
        return Quaternion.identity;
    }

    // ============ Close Button ============
    private GameObject _closeButton;

    void ShowCloseButton()
    {
        if (_closeButton == null)
        {
            CreateCloseButton();
        }
        
        if (_closeButton != null)
        {
            // カメラ前方の右下に配置
            Camera cam = arCamera != null ? arCamera : Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position 
                    + cam.transform.forward * 1.0f 
                    + cam.transform.right * 0.4f 
                    + cam.transform.up * -0.3f;
                _closeButton.transform.position = pos;
                _closeButton.transform.LookAt(cam.transform);
                _closeButton.transform.Rotate(0, 180, 0);
            }
            _closeButton.SetActive(true);
            Debug.Log("[ARSearchResultDisplay] Close button shown");
        }
    }

    void HideCloseButton()
    {
        if (_closeButton != null)
        {
            _closeButton.SetActive(false);
            Debug.Log("[ARSearchResultDisplay] Close button hidden");
        }
    }

    void CreateCloseButton()
    {
        Debug.Log("[ARSearchResultDisplay] Creating close button...");
        
        _closeButton = new GameObject("CloseResultsButton");

        // World Space Canvas
        Canvas canvas = _closeButton.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        _closeButton.AddComponent<CanvasScaler>();
        _closeButton.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = _closeButton.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 80);
        canvasRect.localScale = Vector3.one * 0.001f;

        // ボタン背景
        GameObject btnObj = new GameObject("Button");
        btnObj.transform.SetParent(_closeButton.transform, false);
        
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.9f, 0.2f, 0.2f, 0.95f); // 赤
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(() => {
            Debug.Log("[ARSearchResultDisplay] Close button clicked!");
            ClearResults();
        });

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.sizeDelta = Vector2.zero;

        // コライダー
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(200, 80, 20);

        // テキスト
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = "✕ CLOSE";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        _closeButton.SetActive(false);
        Debug.Log("[ARSearchResultDisplay] Close button created");
    }

    // ============ Play Button (Gallery Slideshow) ============
    private GameObject _playButton;

    void ShowPlayButton()
    {
        // ギャラリーモードでのみ表示
        if (currentMode != DisplayMode.TimelineCorridor) return;
        
        if (_playButton == null)
        {
            CreatePlayButton();
        }
        
        if (_playButton != null)
        {
            // Closeボタンの下に配置
            Camera cam = arCamera != null ? arCamera : Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position 
                    + cam.transform.forward * 1.0f 
                    + cam.transform.right * 0.4f 
                    + cam.transform.up * -0.42f; // Closeボタンより下
                _playButton.transform.position = pos;
                _playButton.transform.LookAt(cam.transform);
                _playButton.transform.Rotate(0, 180, 0);
            }
            _playButton.SetActive(true);
            Debug.Log("[ARSearchResultDisplay] Play button shown");
        }
    }

    void HidePlayButton()
    {
        if (_playButton != null)
        {
            _playButton.SetActive(false);
        }
    }

    void CreatePlayButton()
    {
        Debug.Log("[ARSearchResultDisplay] Creating play button...");
        
        _playButton = new GameObject("PlayButton");

        // World Space Canvas
        Canvas canvas = _playButton.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        _playButton.AddComponent<CanvasScaler>();
        _playButton.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = _playButton.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 80);
        canvasRect.localScale = Vector3.one * 0.001f;

        // ボタン背景
        GameObject btnObj = new GameObject("Button");
        btnObj.transform.SetParent(_playButton.transform, false);
        
        _playButtonImage = btnObj.AddComponent<Image>();
        _playButtonImage.color = new Color(0.2f, 0.7f, 0.3f, 0.95f); // 緑（再生）
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = _playButtonImage;
        btn.onClick.AddListener(() => {
            Debug.Log("[ARSearchResultDisplay] Play button clicked!");
            if (timelineCorridorDisplay != null)
            {
                timelineCorridorDisplay.StartSlideshow();
                UpdatePlayButtonState();
            }
        });

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.sizeDelta = Vector2.zero;

        // コライダー
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(200, 80, 20);

        // テキスト
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        _playButtonText = textObj.AddComponent<Text>();
        _playButtonText.text = "▶ PLAY";
        _playButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _playButtonText.fontSize = 28;
        _playButtonText.fontStyle = FontStyle.Bold;
        _playButtonText.alignment = TextAnchor.MiddleCenter;
        _playButtonText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        _playButton.SetActive(false);
        Debug.Log("[ARSearchResultDisplay] Play button created");
    }

    private Text _playButtonText;
    private Image _playButtonImage;

    void UpdatePlayButtonState()
    {
        if (_playButtonText == null || _playButtonImage == null) return;
        
        bool isPlaying = timelineCorridorDisplay != null && timelineCorridorDisplay.IsSlideshowPlaying;
        
        if (isPlaying)
        {
            _playButtonText.text = "■ STOP";
            _playButtonImage.color = new Color(0.9f, 0.5f, 0.2f, 0.95f); // オレンジ（停止）
        }
        else
        {
            _playButtonText.text = "▶ PLAY";
            _playButtonImage.color = new Color(0.2f, 0.7f, 0.3f, 0.95f); // 緑（再生）
        }
    }


    // ============ Mode Selection Panel ============
    private GameObject _modeSelectionPanel;

    /// <summary>
    /// モード選択パネルを表示/非表示切り替え
    /// </summary>
    public void ToggleModeSelectionPanel()
    {
        Debug.Log("[ARSearchResultDisplay] ToggleModeSelectionPanel called");
        
        if (_modeSelectionPanel == null)
        {
            CreateModeSelectionPanel();
        }

        if (_modeSelectionPanel != null)
        {
            bool newState = !_modeSelectionPanel.activeSelf;
            _modeSelectionPanel.SetActive(newState);
            Debug.Log($"[ARSearchResultDisplay] Panel visibility: {newState}");
        }
    }

    void CreateModeSelectionPanel()
    {
        Debug.Log("[ARSearchResultDisplay] Creating mode selection panel...");
        
        Camera cam = arCamera != null ? arCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError("[ARSearchResultDisplay] No camera found!");
            return;
        }

        // パネル位置（カメラ前方1.2m）
        Vector3 panelPos = cam.transform.position + cam.transform.forward * 1.2f;
        
        _modeSelectionPanel = new GameObject("ARModeSelectionPanel");
        _modeSelectionPanel.transform.position = panelPos;
        _modeSelectionPanel.transform.LookAt(cam.transform);
        _modeSelectionPanel.transform.Rotate(0, 180, 0);

        // World Space Canvas
        Canvas canvas = _modeSelectionPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        _modeSelectionPanel.AddComponent<CanvasScaler>();
        _modeSelectionPanel.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = _modeSelectionPanel.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(500, 350);
        canvasRect.localScale = Vector3.one * 0.001f;

        // 背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_modeSelectionPanel.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // タイトル
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_modeSelectionPanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "AR Display Mode";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.3f, 0.9f, 0.5f);
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.82f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.sizeDelta = Vector2.zero;

        // 3つのモードボタン
        string[] labels = { "Floating Card", "Infinite Carousel", "Gallery Corridor" };
        Color[] colors = {
            new Color(0.3f, 0.6f, 0.9f, 1f),
            new Color(0.9f, 0.5f, 0.2f, 1f),
            new Color(0.6f, 0.3f, 0.8f, 1f)
        };

        for (int i = 0; i < 3; i++)
        {
            int modeIndex = i;
            
            GameObject btnObj = new GameObject($"ModeBtn_{i}");
            btnObj.transform.SetParent(_modeSelectionPanel.transform, false);
            
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = ((int)currentMode == i) 
                ? new Color(0.2f, 0.8f, 0.4f, 1f) 
                : colors[i];
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(() => OnModeSelected(modeIndex));

            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            float yMin = 0.55f - i * 0.25f;
            float yMax = 0.78f - i * 0.25f;
            btnRect.anchorMin = new Vector2(0.08f, yMin);
            btnRect.anchorMax = new Vector2(0.92f, yMax);
            btnRect.sizeDelta = Vector2.zero;

            // コライダー
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(420, 70, 20);

            // テキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = labels[i];
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 26;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        _modeSelectionPanel.SetActive(false);
        Debug.Log("[ARSearchResultDisplay] Mode selection panel created");
    }

    void OnModeSelected(int modeIndex)
    {
        Debug.Log($"[ARSearchResultDisplay] Mode selected: {modeIndex}");
        SetDisplayMode((DisplayMode)modeIndex);
        
        if (_modeSelectionPanel != null)
        {
            _modeSelectionPanel.SetActive(false);
        }
    }
}
