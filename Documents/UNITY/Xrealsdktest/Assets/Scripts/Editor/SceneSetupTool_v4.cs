using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using NRKernal;
using System.Collections.Generic;

/// <summary>
/// Scene Setup Tool v4 - With Custom Icons
/// Uses custom PNG icons instead of emoji text for a more professional look.
/// </summary>
public partial class SceneSetupTool_v4 : MonoBehaviour
{
    // Icon paths relative to Assets folder
    private const string ICON_PATH = "Assets/UI/Icons/";
    
    // Icon sprite cache
    private static Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();

    [MenuItem("Tools/Setup Flashback Search UI (v4 with Icons)")]
    public static void SetupSearchUI()
    {
        Debug.Log("[SceneSetupTool] Starting Setup...");
        
        // Clear icon cache
        _iconCache.Clear();

        // 1. Find or Create Specific Canvas
        string canvasName = "FlashbackCanvas";
        Debug.Log($"[SceneSetupTool] Looking for canvas: {canvasName}");
        GameObject canvasObj = GameObject.Find(canvasName);
        Canvas canvas;

        // Find CenterCamera to attach UI
        Transform centerCam = null;
        GameObject rig = GameObject.Find("NRCameraRig");
        if (rig != null)
        {
            Transform t = rig.transform.Find("TrackingSpace/CenterCamera");
            if (t != null) centerCam = t;
        }

        // Link ImageUploader - Find EARLY to avoid scope issues
        ImageUploader uploader = FindObjectOfType<ImageUploader>();
        if (uploader == null)
        {
            GameObject uploaderObj = new GameObject("ImageUploaderSystem");
            uploader = uploaderObj.AddComponent<ImageUploader>();
            Debug.Log("Created new ImageUploaderSystem.");
        }

        // Setup LogForwarder for remote debugging
        LogForwarder logForwarder = FindObjectOfType<LogForwarder>();
        if (logForwarder == null)
        {
            GameObject logForwarderObj = new GameObject("LogForwarder");
            logForwarder = logForwarderObj.AddComponent<LogForwarder>();
            Debug.Log("[SceneSetupTool] Created LogForwarder for remote debugging.");
        }

        if (canvasObj != null)
        {
            Debug.Log($"[SceneSetupTool] Found existing {canvasName}. Destroying for clean rebuild...");
            GameObject.DestroyImmediate(canvasObj);
            canvasObj = null;
        }

        if (canvasObj == null)
        {
            canvasObj = new GameObject(canvasName);
            canvas = canvasObj.AddComponent<Canvas>();
            
            // IMPORTANT: Use WorldSpace for AR Glasses
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Detach from camera (Parent = null) and use Script for Head-Lock
            canvasObj.transform.SetParent(null); 
            canvasObj.transform.position = new Vector3(0, 0, 10f); // 遠くに配置
            canvasObj.transform.localScale = Vector3.one * 0.007f; // 比例拡大

            // Add HeadLock script
            SimpleHeadLock headLock = canvasObj.AddComponent<SimpleHeadLock>();
            headLock.distance = 10f; // ★ 10m先に配置（3Dオブジェクトをブロックしない）
            headLock.targetScale = 0.006f; // ★ 10m用に拡大（0.0008 * ~7.5）
            if (centerCam != null) headLock.targetCamera = centerCam;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<CanvasRaycastTarget>(); // REQUIRED for NRSDK UI Interaction!
            
            // ランタイムでイベントを設定するコンポーネント
            canvasObj.AddComponent<UIEventInitializer>();
            
            // Set Sorting Order to ensure visibility
            canvas.sortingOrder = 100;
            
            // Set Canvas Size for WorldSpace
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 600);
            
            // Fix Layer: Ensure Canvas and all children are on "UI" layer (5) for correct AR rendering
            SetLayerRecursive(canvasObj, 5); 
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.sortingOrder = 100; // Force sorting order update
            SetLayerRecursive(canvasObj, 5); // Ensure existing canvas is also on UI layer
        }

        // 2. Create Search Panel
        // CLEANUP: Destroy potential orphans or detached panels to prevent duplicates
        var orphans = new string[] { "SearchPanel", "ObjectRegPanel", "RegistrationListPanel", "ObjectRegistration3DProgress" };
        foreach (string orphanName in orphans)
        {
            GameObject orphan = GameObject.Find(orphanName);
            if (orphan != null)
            {
                DestroyImmediate(orphan);
                Debug.Log($"[SceneSetupTool] Cleaned up orphan: {orphanName}");
            }
        }
        
        Transform existingPanel = canvas.transform.Find("SearchPanel");
        if (existingPanel != null)
        {
            DestroyImmediate(existingPanel.gameObject);
        }

        GameObject panelObj = new GameObject("SearchPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Transparent background
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0f);

        // === CRITICAL FIX: Add Missing Managers ===
        if (panelObj.GetComponent<SearchUIManager>() == null) panelObj.AddComponent<SearchUIManager>();
        if (panelObj.GetComponent<ImageUploader>() == null) panelObj.AddComponent<ImageUploader>();
        if (panelObj.GetComponent<WeatherManager>() == null) panelObj.AddComponent<WeatherManager>();
        if (panelObj.GetComponent<ARSearchResultDisplay>() == null) panelObj.AddComponent<ARSearchResultDisplay>();
        if (panelObj.GetComponent<TutorialManager>() == null) 
        {
             // TutorialManager is usually on TutorialPanel, but adding here as fallback/finder target isn't bad
             // But let's stick to core managers
        }
        Debug.Log("[SceneSetupTool] Attached Managers (SearchUI, ImageUploader, Weather, ARDisplay)");
        // ==========================================

        // --- Helper: CreateMenuButton (Local Function) ---
        System.Func<string, Transform, float, GameObject> CreateMenuButton = (text, parent, yPosition) => {
            GameObject btnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
            btnObj.name = text + "Button";
            btnObj.transform.SetParent(parent, false);
            
            // Position
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, yPosition - 0.04f); // Reduced height for more spacing (was -0.08)
            rect.anchorMax = new Vector2(0.95f, yPosition + 0.04f); // Reduced height for more spacing (was +0.04)
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Style
            Image bg = btnObj.GetComponent<Image>();
            if (bg != null) 
            {
                bg.color = new Color(0.12f, 0.23f, 0.54f, 0.6f); // Figma glassmorphic blue
                bg.raycastTarget = true; // クリック判定に必須
            }
            
            // Hover Color Effect
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = btn.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 1f);
                colors.highlightedColor = new Color(0.6f, 0.8f, 1f, 1f); // 水色のハイライト
                colors.pressedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.fadeDuration = 0.1f;
                btn.colors = colors;
                btn.interactable = true; // 確実にクリック可能に
            }
            
            // Text
            Text btnText = btnObj.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = text;
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                btnText.fontSize = 16;
                btnText.color = Color.white;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.raycastTarget = false; // テキストがクリックをブロックしないように
            }
            
            // Collider - サイズを大きく
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(300, 80, 10); // より大きなCollider
            
            // Z Offset (Global)
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            Vector3 p = rt.anchoredPosition3D;
            rt.anchoredPosition3D = new Vector3(p.x, p.y, -10f);
            
            return btnObj;
        };



        // ============ NEW: 2ボタンレイアウト（左下：設定、右下：機能） ============
        
        // === 左下：設定メインボタン ===
        GameObject settingsMainBtn = new GameObject("SettingsMainButton");
        settingsMainBtn.transform.SetParent(panelObj.transform, false);
        Image settingsMainBg = settingsMainBtn.AddComponent<Image>();
        settingsMainBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // ダーク背景
        
        // アイコンを子オブジェクトとして追加
        string settingsIconPath = "Assets/UI/Icons/icon_settings.png";
        Sprite settingsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(settingsIconPath);
        TextureImporter settingsImporter = AssetImporter.GetAtPath(settingsIconPath) as TextureImporter;
        if (settingsImporter != null && settingsImporter.textureType != TextureImporterType.Sprite)
        {
            settingsImporter.textureType = TextureImporterType.Sprite;
            settingsImporter.alphaIsTransparency = true;
            settingsImporter.SaveAndReimport();
            settingsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(settingsIconPath);
        }
        
        if (settingsSprite != null)
        {
            GameObject iconChild = new GameObject("Icon");
            iconChild.transform.SetParent(settingsMainBtn.transform, false);
            Image iconImg = iconChild.AddComponent<Image>();
            iconImg.sprite = settingsSprite;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            RectTransform iconRect = iconChild.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            // ホバー拡大・クリック回転アニメーター追加
            ButtonScaleRotateAnimator settingsAnimator = settingsMainBtn.AddComponent<ButtonScaleRotateAnimator>();
            settingsAnimator.hoverScale = 1.15f;
            settingsAnimator.hoverDuration = 0.2f;
            settingsAnimator.rotateAngle = 360f;
            settingsAnimator.rotateDuration = 0.4f;
            settingsAnimator.rotateClockwise = true;
        }
        else
        {
            settingsMainBg.color = new Color(0.12f, 0.23f, 0.54f, 0.6f); // フォールバック
        }
        
        RectTransform settingsMainRect = settingsMainBtn.GetComponent<RectTransform>();
        settingsMainRect.anchorMin = new Vector2(0.05f, 0.15f); // Moved to far left
        settingsMainRect.anchorMax = new Vector2(0.20f, 0.33f); // Moved to far left
        settingsMainRect.offsetMin = Vector2.zero;
        settingsMainRect.offsetMax = Vector2.zero;
        BoxCollider settingsMainCol = settingsMainBtn.AddComponent<BoxCollider>();
        settingsMainCol.size = new Vector3(160, 110, 1);
        Button settingsMainButton = settingsMainBtn.AddComponent<Button>();
        settingsMainButton.transition = Selectable.Transition.None; // アニメーターで制御
        
        // Z軸でSearchPanelから離す（ボタンを手前に出す）
        settingsMainBtn.transform.localPosition = new Vector3(
            settingsMainBtn.transform.localPosition.x,
            settingsMainBtn.transform.localPosition.y,
            -10f // 手前に10単位移動
        );
        
        // 設定のバーメニューパネル
        GameObject settingsHoverMenu = new GameObject("SettingsHoverMenu");
        settingsHoverMenu.transform.SetParent(panelObj.transform, false);
        Image settingsHoverBg = settingsHoverMenu.AddComponent<Image>();
        settingsHoverBg.color = new Color(0.12f, 0.11f, 0.42f, 0.85f); // Figma dark purple for hover menu
        RectTransform settingsHoverRect = settingsHoverMenu.GetComponent<RectTransform>();
        settingsHoverRect.anchorMin = new Vector2(0.02f, 0.25f); // FunctionsHoverMenuと同じY
        settingsHoverRect.anchorMax = new Vector2(0.25f, 0.90f); // FunctionsHoverMenuと同じY
        settingsHoverRect.offsetMin = Vector2.zero;
        settingsHoverRect.offsetMax = Vector2.zero;
        
        // Z軸でボタンと離す（手前に出す）
        settingsHoverMenu.transform.localPosition = new Vector3(
            settingsHoverMenu.transform.localPosition.x,
            settingsHoverMenu.transform.localPosition.y,
            -50f // 手前に50単位移動
        );
        // BoxCollider removed - was blocking button touches
        
        // 設定メニュー内ボタン（上から下へ）
        string[] settingsMenuItems = {"詳細設定", "登録リスト", "入力モード", "+ 登録", "Tutorial"};
        // internal names for robust finding (avoid Japanese encoding issues)
        string[] settingsMenuNames = {"IPSettings", "RegisteredList", "InputMode", "Registration", "Tutorial"};
        
        GameObject[] settingsMenuBtns = new GameObject[settingsMenuItems.Length];
        for (int i = 0; i < settingsMenuItems.Length; i++)
        {
            float yPos = 1f - (i + 1) * (1f / (settingsMenuItems.Length + 1));
            settingsMenuBtns[i] = CreateMenuButton(settingsMenuItems[i], settingsHoverMenu.transform, yPos);
            // Rename GameObject to English key + "Button"
            settingsMenuBtns[i].name = settingsMenuNames[i] + "Button";
        }
        
        settingsHoverMenu.SetActive(false); // 初期非表示
        
        // クリックでメニュー表示/非表示
        GameObject settingsMenuRef = settingsHoverMenu;
        settingsMainButton.onClick.AddListener(() => {
            settingsMenuRef.SetActive(!settingsMenuRef.activeSelf);
        });
        
        // 設定メニュー内ボタンのクリックイベント
        // 新インデックス: 0:詳細設定, 1:登録リスト, 2:入力モード, 3:+登録, 4:Tutorial
        
        // 0: IP設定
        Button ipSettingsBtn = settingsMenuBtns[0].GetComponent<Button>();
        if (ipSettingsBtn != null)
        {
            ipSettingsBtn.onClick.AddListener(() => {
                settingsMenuRef.SetActive(false);
                // IP設定パネルを探して表示
                Transform searchPanel = settingsMenuRef.transform.parent;
                Transform ipPanel = searchPanel?.Find("IPSettingsPanel");
                if (ipPanel != null)
                {
                    ipPanel.gameObject.SetActive(true);
                }
                Debug.Log("[Settings] IP設定 clicked!");
            });
        }
        
        // Console Log ボタンは削除
        // Beacon ボタンは削除 - 登録リストから操作するように変更
        
        // 1: 登録リスト
        Button registeredListBtn = settingsMenuBtns[1].GetComponent<Button>();
        if (registeredListBtn != null)
        {
            registeredListBtn.onClick.AddListener(() => {
                settingsMenuRef.SetActive(false);
                // 登録リストパネルを探して表示
                Transform searchPanel = settingsMenuRef.transform.parent;
                Transform listPanel = searchPanel?.Find("RegisteredListPanel");
                if (listPanel != null)
                {
                    listPanel.gameObject.SetActive(true);
                }
                Debug.Log("[Settings] 登録リスト clicked!");
            });
        }
        
        // 2: 入力モード
        GameObject inputModeBtnGO = settingsMenuBtns[2];
        Button inputModeBtn = settingsMenuBtns[2].GetComponent<Button>();
        if (inputModeBtn != null)
        {
            inputModeBtn.onClick.AddListener(() => {
                settingsMenuRef.SetActive(false);
                SearchUIManager sm = GameObject.FindObjectOfType<SearchUIManager>();
                if (sm != null) {
                    var newMode = sm.currentInputMode == SearchUIManager.InputMode.Controller 
                        ? SearchUIManager.InputMode.HandTracking 
                        : SearchUIManager.InputMode.Controller;
                    sm.SetInputMode(newMode);
                    Debug.Log($"[InputMode] Changed to: {newMode}");
                    
                    // ボタンラベルを現在のモードに更新
                    if (inputModeBtnGO != null) {
                        Text btnText = inputModeBtnGO.GetComponentInChildren<Text>();
                        if (btnText != null) {
                            btnText.text = newMode == SearchUIManager.InputMode.Controller 
                                ? "入力 コントローラー" 
                                : "入力 ハンド";
                        }
                    }
                } else {
                    Debug.LogWarning("[Settings] SearchUIManager not found!");
                }
            });
        }
        
        // 3: + 登録
        Button registrationBtn = settingsMenuBtns[3].GetComponent<Button>();
        if (registrationBtn != null)
        {
            registrationBtn.onClick.AddListener(() => {
                settingsMenuRef.SetActive(false);
                ImageUploader up = GameObject.FindObjectOfType<ImageUploader>();
                if (up != null) up.ShowRegistrationSelectPanel();
            });
        }
        
        // 4: Tutorial
        Button tutorialBtn = settingsMenuBtns[4].GetComponent<Button>();
        if (tutorialBtn != null)
        {
            tutorialBtn.onClick.AddListener(() => {
                settingsMenuRef.SetActive(false);
                TutorialManager tm = FindObjectOfType<TutorialManager>();
                if (tm != null)
                {
                    tm.ShowTutorial();
                }
                else
                {
                    Debug.LogWarning("[Settings] TutorialManager not found!");
                }
            });
        }

        // === 右下：機能メインボタン ===
        GameObject functionsMainBtn = new GameObject("FunctionsMainButton");
        functionsMainBtn.transform.SetParent(panelObj.transform, false);
        Image functionsMainBg = functionsMainBtn.AddComponent<Image>();
        functionsMainBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // ダーク背景
        
        // アイコンを子オブジェクトとして追加
        string functionsIconPath = "Assets/UI/Icons/icon_search.png";
        Sprite functionsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(functionsIconPath);
        TextureImporter functionsImporter = AssetImporter.GetAtPath(functionsIconPath) as TextureImporter;
        if (functionsImporter != null && functionsImporter.textureType != TextureImporterType.Sprite)
        {
            functionsImporter.textureType = TextureImporterType.Sprite;
            functionsImporter.alphaIsTransparency = true;
            functionsImporter.SaveAndReimport();
            functionsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(functionsIconPath);
        }
        
        if (functionsSprite != null)
        {
            // アイコン用の子オブジェクト
            GameObject iconChild = new GameObject("Icon");
            iconChild.transform.SetParent(functionsMainBtn.transform, false);
            Image iconImg = iconChild.AddComponent<Image>();
            iconImg.sprite = functionsSprite;
            iconImg.color = Color.white; // 白アイコンをそのまま表示
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            RectTransform iconRect = iconChild.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            // ホバー拡大・クリック回転アニメーター追加
            ButtonScaleRotateAnimator functionsAnimator = functionsMainBtn.AddComponent<ButtonScaleRotateAnimator>();
            functionsAnimator.hoverScale = 1.15f;
            functionsAnimator.hoverDuration = 0.2f;
            functionsAnimator.rotateAngle = 360f;
            functionsAnimator.rotateDuration = 0.4f;
            functionsAnimator.rotateClockwise = true;
        }
        else
        {
            functionsMainBg.color = new Color(0.34f, 0.11f, 0.53f, 0.6f); // フォールバック
        }
        
        RectTransform functionsMainRect = functionsMainBtn.GetComponent<RectTransform>();
        functionsMainRect.anchorMin = new Vector2(0.80f, 0.15f); // Moved to far right
        functionsMainRect.anchorMax = new Vector2(0.95f, 0.33f); // Moved to far right
        functionsMainRect.offsetMin = Vector2.zero;
        functionsMainRect.offsetMax = Vector2.zero;
        BoxCollider functionsMainCol = functionsMainBtn.AddComponent<BoxCollider>();
        functionsMainCol.size = new Vector3(160, 110, 1);
        
        // Z Offset
        RectTransform fRt = functionsMainBtn.GetComponent<RectTransform>();
        Vector3 fP = fRt.anchoredPosition3D;
        fRt.anchoredPosition3D = new Vector3(fP.x, fP.y, -10f);

        Button functionsMainButton = functionsMainBtn.AddComponent<Button>();
        functionsMainButton.transition = Selectable.Transition.None; // アニメーターで制御
        
        // 機能ホバーメニューパネル
        GameObject functionsHoverMenu = new GameObject("FunctionsHoverMenu");
        functionsHoverMenu.transform.SetParent(panelObj.transform, false);
        Image functionsHoverBg = functionsHoverMenu.AddComponent<Image>();
        functionsHoverBg.color = new Color(0.1f, 0.2f, 0.3f, 0.95f);
        RectTransform functionsHoverRect = functionsHoverMenu.GetComponent<RectTransform>();
        functionsHoverRect.anchorMin = new Vector2(0.75f, 0.25f); // 上に移動
        functionsHoverRect.anchorMax = new Vector2(0.98f, 0.90f); // 上に移動
        functionsHoverRect.offsetMin = Vector2.zero;
        functionsHoverRect.offsetMax = Vector2.zero;
        
        // Z軸で手前に出す
        functionsHoverMenu.transform.localPosition = new Vector3(
            functionsHoverMenu.transform.localPosition.x,
            functionsHoverMenu.transform.localPosition.y,
            -50f
        );
        
        // 機能メニュー内ボタン（上から下へ） より大きなボタン
        // AR Mode, 入力モード, + 登録は設定メニューに移動
        string[] functionsMenuItems = {"SEARCH"};
        // 機能メニューは削除するため1項目のみ
        GameObject[] functionsMenuBtns = new GameObject[functionsMenuItems.Length];
        for (int i = 0; i < functionsMenuItems.Length; i++)
        {
            float yPos = 1f - (i + 1) * (1f / (functionsMenuItems.Length + 1));
            functionsMenuBtns[i] = CreateMenuButton(functionsMenuItems[i], functionsHoverMenu.transform, yPos);
            
            // ボタンをZ軸で手前に出す
            functionsMenuBtns[i].transform.localPosition = new Vector3(
                functionsMenuBtns[i].transform.localPosition.x,
                functionsMenuBtns[i].transform.localPosition.y,
                -10f
            );
            
            // BoxColliderを大きくする
            BoxCollider funcBtnCol = functionsMenuBtns[i].GetComponent<BoxCollider>();
            if (funcBtnCol != null)
            {
                funcBtnCol.size = new Vector3(300, 80, 10);
            }
        }
        
        functionsHoverMenu.SetActive(false); // 使用しないが互換性のため残す
        
        // 機能ボタンはSEARCH専用 - 直接カテゴリウィンドウを表示
        functionsMainButton.onClick.AddListener(() => {
            SearchUIManager sm = GameObject.FindObjectOfType<SearchUIManager>();
            if (sm != null) sm.ToggleCategoryPanel();
            else Debug.LogWarning("SearchUIManager not found!");
        });
        
        // ============ BottomMenuController Setup ============
        BottomMenuController bottomMenuCtrl = panelObj.AddComponent<BottomMenuController>();
        bottomMenuCtrl.settingsMainButton = settingsMainButton;
        bottomMenuCtrl.functionsMainButton = functionsMainButton;
        bottomMenuCtrl.settingsMenuPanel = settingsHoverMenu;
        bottomMenuCtrl.functionsMenuPanel = functionsHoverMenu;
        
        bottomMenuCtrl.settingsMenuButtons = new Button[settingsMenuBtns.Length];
        for(int i=0; i<settingsMenuBtns.Length; i++) {
            bottomMenuCtrl.settingsMenuButtons[i] = settingsMenuBtns[i].GetComponent<Button>();
        }
        
        bottomMenuCtrl.functionsMenuButtons = new Button[functionsMenuBtns.Length];
        for(int i=0; i<functionsMenuBtns.Length; i++) {
            bottomMenuCtrl.functionsMenuButtons[i] = functionsMenuBtns[i].GetComponent<Button>();
        }
                SetupTutorialSystem(panelObj, bottomMenuCtrl, uploader);


        // ============ 機能メニューボタンのイベント登録 (Runtime calls) ============
        // 0: SEARCH
        bottomMenuCtrl.functionsMenuButtons[0].onClick.AddListener(() => {
             bottomMenuCtrl.CloseAllMenus(); // メニューを閉じる
             SearchUIManager sm = GameObject.FindObjectOfType<SearchUIManager>();
             if (sm != null) sm.ToggleCategoryPanel();
             else Debug.LogWarning("SearchUIManager not found!");
        });

        // AR Mode, 入力モード, + 登録 は設定メニューに移動したため削除
        
        // ★★★ ARModeSelectionPanel を事前生成（詳細設定内でも使用可能） ★★★
        GameObject arModePanel = CreateARModeSelectionPanel(panelObj);
        
        // BackgroundOverlay削除 - 3D空間のボタンをブロックしていたため



        SetupSearchSystem(panelObj);
        SearchUIManager manager = panelObj.GetComponent<SearchUIManager>();
        if (manager == null) manager = panelObj.AddComponent<SearchUIManager>();

        // 6.5 Create Debug Log Panel
        GameObject debugPanelGO = new GameObject("DebugPanel");
        debugPanelGO.transform.SetParent(panelObj.transform, false);
        Image debugBg = debugPanelGO.AddComponent<Image>();
        debugBg.color = new Color(0, 0, 0, 0.5f);
        debugBg.raycastTarget = false; // 3Dボタンをブロックしないように
        RectTransform debugRect = debugPanelGO.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0.1f, 0.05f);
        debugRect.anchorMax = new Vector2(0.9f, 0.2f);
        debugRect.offsetMin = Vector2.zero;
        debugRect.offsetMax = Vector2.zero;

        GameObject debugTextGO = new GameObject("DebugText");
        debugTextGO.transform.SetParent(debugPanelGO.transform, false);
        Text debugText = debugTextGO.AddComponent<Text>();
        debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugText.color = Color.white;
        debugText.fontSize = 14;
        debugText.alignment = TextAnchor.UpperLeft;
        RectTransform debugTextRect = debugTextGO.GetComponent<RectTransform>();
        debugTextRect.anchorMin = Vector2.zero;
        debugTextRect.anchorMax = Vector2.one;
        debugTextRect.offsetMin = new Vector2(5, 5);
        debugTextRect.offsetMax = new Vector2(-5, -5);

        // Link to Uploader
        if (uploader != null)
        {
            uploader.debugText = debugText;

            SetupSettingsSystem(panelObj, uploader, manager, debugPanelGO, debugText, bottomMenuCtrl);
        }

        SetupRegistrationSystem(panelObj, uploader, bottomMenuCtrl);      // Link to uploader
        // ============ Weather Panel Setup (System) ============
        WeatherManager weatherManager;
        SetupWeatherSystem(panelObj, out weatherManager);

        SetupClockSystem(panelObj);

        // --- 強力なクリーンアップ: シーン内の古い Label などを強制削除 ---
        // 複数のキャンバスがある場合や、更新漏れを防ぐため
        GameObject[] allButtons = GameObject.FindObjectsOfType<GameObject>(); // 全オブジェクト検索は重いがエディタ拡張ならOK
        foreach (var obj in allButtons)
        {
            if (obj.name == "WeatherButton")
            {
                Transform t = obj.transform.Find("Label");
                if (t != null) 
                {
                    Debug.Log($"[SceneSetupTool] Removing stray Label from {obj.name}");
                    DestroyImmediate(t.gameObject);
                }
                // 旧名 "WeatherText" も念のため削除
                Transform t2 = obj.transform.Find("WeatherText");
                if (t2 != null)
                {
                    Debug.Log($"[SceneSetupTool] Removing stray WeatherText from {obj.name}");
                    DestroyImmediate(t2.gameObject);
                }
            }
            // 不要な旧WeatherWidgetも削除
            if (obj.name == "WeatherWidget")
            {
                Debug.Log($"[SceneSetupTool] Removing legacy WeatherWidget: {obj.name}");
                DestroyImmediate(obj);
            }
        }
        
        // Fix Layers recursively for EVERYTHING created
        SetLayerRecursive(canvasObj, 5); // Layer 5 = UI
        
        Debug.Log("Search UI v4 (with Icons) Setup Complete!");
        
        // 4. Setup Startup Video (Flashback Intro) - DISABLED due to Android StreamingAssets limitation
        // CreateStartupVideo(canvas, panelObj);


        SetupHandTracking();


        // ============ BottomMenuController References Setup ============
        // 全てのUI生成後に参照を設定
        BottomMenuController finalBottomCtrl = panelObj.GetComponent<BottomMenuController>();
        if (finalBottomCtrl != null)
        {
            // Managers attached to panelObj or sub-panels
            finalBottomCtrl.imageUploader = panelObj.GetComponent<ImageUploader>();
            
            // WeatherPanel (Find because variable might be out of scope)
            Transform weatherPanelTr = panelObj.transform.Find("WeatherPanel");
            // WeatherManager is attached to panelObj, NOT weatherPanelTr
            finalBottomCtrl.weatherManager = panelObj.GetComponent<WeatherManager>();

            // TutorialPanel (Find to be safe)
            Transform tutorialPanelTr = panelObj.transform.Find("TutorialPanel");
            if (tutorialPanelTr != null)
            {
                finalBottomCtrl.tutorialManager = tutorialPanelTr.GetComponent<TutorialManager>();
            }
            
            // Panels
            finalBottomCtrl.ipSettingsPanel = panelObj.transform.Find("IPSettingsPanel")?.gameObject;
            finalBottomCtrl.consoleLogPanel = panelObj.transform.Find("DebugPanel")?.gameObject;
            finalBottomCtrl.registrationListPanel = panelObj.transform.Find("RegistrationListPanel")?.gameObject;
            finalBottomCtrl.weatherRegionPanel = panelObj.transform.Find("WeatherRegionPanel")?.gameObject;
            
            // Common Close Button (TopBarのCloseResultsButtonではなく、各パネル共通の戻るボタンがあれば)
            // ここでは未設定のままにするか、必要なら作成
        }

        Debug.Log("[SceneSetupTool] Tutorial Panel setup complete!");
        
        // FIX: Assign remaining BottomMenuController references
        if (bottomMenuCtrl != null)
        {
            bottomMenuCtrl.tutorialManager = FindObjectOfType<TutorialManager>();
            bottomMenuCtrl.weatherManager = FindObjectOfType<WeatherManager>();
            // Try to find Region Panel if possible, otherwise it might be handled by WeatherManager
            WeatherManager wm = FindObjectOfType<WeatherManager>();
            if (wm != null) bottomMenuCtrl.weatherRegionPanel = wm.regionSettingsPanel;
        }

        // 7. Ensure EventSystem exists with NRInputModule
        // NRSDK's NRInputModule.Initialize() will auto-create EventSystem if needed
        // We should NOT add StandaloneInputModule as it conflicts with NRInputModule
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<NRInputModule>(); // NRSDK's input module for controller/hand input
            Debug.Log("Created EventSystem with NRInputModule.");
        }
        else
        {
            // Ensure existing EventSystem has NRInputModule
            var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (es.GetComponent<NRInputModule>() == null)
            {
                es.gameObject.AddComponent<NRInputModule>();
                Debug.Log("Added NRInputModule to existing EventSystem.");
            }
        }
        
        // 8. Setup UI Toolkit (DISABLED - not used)
        // SetupUIToolkitOverlay();
        
        // 9. Apply rounded corners to all UI elements
        ApplyRoundedCornersToUI(panelObj);
        
        // Helper to ensure everything is on the correct layer at the end
        if (canvasObj != null) SetLayerRecursive(canvasObj, 5); // Ensure UI layer (5) for Raycasting
        
        Debug.Log("[SceneSetupTool] UI Setup Complete & Layers Fixed!");
    } // End of SetupSearchUI

    
    /// <summary>
    /// UI Toolkit オーバーレイのセットアップ
    /// </summary>
    private static void SetupUIToolkitOverlay()
    {
        Debug.Log("[SceneSetupTool] Setting up UI Toolkit overlay...");
        
        // UI Toolkit コンテナを作成または取得
        GameObject uiContainer = GameObject.Find("UIToolkitContainer");
        if (uiContainer == null)
        {
            uiContainer = new GameObject("UIToolkitContainer");
        }
        
        // PanelSettings をロードまたは作成
        var panelSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/UI/UIToolkit/PanelSettings.asset");
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            panelSettings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            
            // ディレクトリ作成
            if (!System.IO.Directory.Exists("Assets/UI/UIToolkit"))
            {
                System.IO.Directory.CreateDirectory("Assets/UI/UIToolkit");
            }
            
            AssetDatabase.CreateAsset(panelSettings, "Assets/UI/UIToolkit/PanelSettings.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneSetupTool] Created PanelSettings.asset");
        }
        
        // 各UIドキュメントをセットアップ
        SetupUIDocument(uiContainer, "MainMenuUI", "Assets/UI/UIToolkit/MainMenu.uxml", panelSettings, 0);
        SetupUIDocument(uiContainer, "SettingsPanelUI", "Assets/UI/UIToolkit/SettingsPanel.uxml", panelSettings, 10);
        SetupUIDocument(uiContainer, "WeatherUI", "Assets/UI/UIToolkit/Weather.uxml", panelSettings, 5);
        
        Debug.Log("[SceneSetupTool] UI Toolkit overlay setup complete!");
    }
    
    /// <summary>
    /// 個別のUIDocumentをセットアップ
    /// </summary>
    private static void SetupUIDocument(GameObject container, string objName, string uxmlPath, UnityEngine.UIElements.PanelSettings panelSettings, int sortOrder)
    {
        var uxml = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(uxmlPath);
        if (uxml == null)
        {
            Debug.LogWarning($"[SceneSetupTool] UXML not found: {uxmlPath}");
            return;
        }
        
        // 既存を削除
        Transform existing = container.transform.Find(objName);
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }
        
        // 新規作成
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(container.transform);
        
        // UIDocument追加
        var uiDoc = obj.AddComponent<UnityEngine.UIElements.UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.visualTreeAsset = uxml;
        uiDoc.sortingOrder = sortOrder;
        
        // コントローラー追加（名前と応じて）
        if (objName == "MainMenuUI")
        {
            var controller = obj.AddComponent<MainMenuUIController>();
            controller.uiDocument = uiDoc;
            controller.imageUploader = FindObjectOfType<ImageUploader>();
            controller.weatherManager = FindObjectOfType<WeatherManager>();
            controller.searchUIManager = FindObjectOfType<SearchUIManager>();
        }
        else if (objName == "SettingsPanelUI")
        {
            var controller = obj.AddComponent<SettingsPanelUIController>();
            controller.uiDocument = uiDoc;
            controller.imageUploader = FindObjectOfType<ImageUploader>();
            controller.weatherManager = FindObjectOfType<WeatherManager>();
        }
        else if (objName == "WeatherUI")
        {
            var controller = obj.AddComponent<WeatherUIController>();
            controller.uiDocument = uiDoc;
        }
        
        Debug.Log($"[SceneSetupTool] {objName} setup complete");
    }
    
    /// <summary>
    /// ARモード選択パネルを事前生成（CanvasRaycastTargetがシーン開始時に登録されるようにする）
    /// </summary>
    static GameObject CreateARModeSelectionPanel(GameObject parentPanel)
    {
        Debug.Log("[SceneSetupTool] Creating ARModeSelectionPanel...");
        
        // パネル本体（独立したCanvas）
        GameObject panelObj = new GameObject("ARModeSelectionPanel");
        panelObj.transform.SetParent(parentPanel.transform.parent, false); // シーンルートに配置
        panelObj.transform.position = new Vector3(0, 0, 1.5f); // 初期位置
        
        // Canvas設定
        Canvas canvas = panelObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 200; // メインUIより前
        
        panelObj.AddComponent<CanvasScaler>();
        panelObj.AddComponent<GraphicRaycaster>();
        panelObj.AddComponent<CanvasRaycastTarget>(); // ★ NRSDK必須
        
        RectTransform canvasRect = panelObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 500);
        canvasRect.localScale = Vector3.one * 0.0008f;
        
        // 背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(panelObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);
        bgImage.raycastTarget = true;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // BoxCollider削除 - 3Dボタンをブロックしていたため
        
        // タイトル
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "AR Display Mode";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.3f, 0.9f, 0.5f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.82f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // 3つのモードボタン
        string[] labels = { "Floating Card", "Infinite Carousel", "Gallery Corridor" };
        Color[] colors = {
            new Color(0.3f, 0.6f, 0.9f, 1f),
            new Color(0.9f, 0.5f, 0.2f, 1f),
            new Color(0.6f, 0.3f, 0.8f, 1f)
        };
        
        for (int i = 0; i < 3; i++)
        {
            GameObject btnObj = new GameObject($"ModeBtn_{i}");
            btnObj.transform.SetParent(panelObj.transform, false);
            
            Image btnBg = btnObj.AddComponent<Image>();
            // Gallery Corridor (index 2) を既定として緑色でハイライト
            btnBg.color = (i == 2) ? new Color(0.2f, 0.8f, 0.4f, 1f) : colors[i];
            btnBg.raycastTarget = true;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            // onClick はランタイムでARModeSwitcherが設定する
            
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            float yMin = 0.55f - i * 0.25f;
            float yMax = 0.78f - i * 0.25f;
            btnRect.anchorMin = new Vector2(0.08f, yMin);
            btnRect.anchorMax = new Vector2(0.92f, yMax);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            // BoxCollider (厚め)
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(640, 115, 50);
            
            // テキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text btnText = textObj.AddComponent<Text>();
            btnText.text = labels[i];
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 28;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.raycastTarget = false;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        // 初期状態で非表示
        panelObj.SetActive(false);
        
        Debug.Log("[SceneSetupTool] ARModeSelectionPanel created");
        return panelObj;
    }
}
