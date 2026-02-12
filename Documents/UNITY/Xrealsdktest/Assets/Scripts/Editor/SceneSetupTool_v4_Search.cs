using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4
{
    private static void SetupSearchSystem(GameObject panelObj)
    {
        Debug.Log("[SceneSetupTool] Setting up Search System...");

        Canvas canvas = panelObj.GetComponentInParent<Canvas>();
        GameObject canvasObj = canvas.gameObject;
        
        // Find CenterCamera
        Transform centerCam = null;
        GameObject rig = GameObject.Find("NRCameraRig");
        if (rig != null)
        {
            Transform t = rig.transform.Find("TrackingSpace/CenterCamera");
            if (t != null) centerCam = t;
        }

        ImageUploader uploader = Object.FindObjectOfType<ImageUploader>();

        // ResultScrollView removed - not used in this project

        // 5.5 Create Loading Panel (DOTween対応)
        GameObject loadingPanelObj = new GameObject("LoadingPanel");
        loadingPanelObj.transform.SetParent(panelObj.transform, false);
        Image loadingBg = loadingPanelObj.AddComponent<Image>();
        loadingBg.color = new Color(0, 0, 0, 0.7f);
        RectTransform loadingRect = loadingPanelObj.GetComponent<RectTransform>();
        loadingRect.anchorMin = new Vector2(0.3f, 0.4f);
        loadingRect.anchorMax = new Vector2(0.7f, 0.6f);
        loadingRect.offsetMin = Vector2.zero;
        loadingRect.offsetMax = Vector2.zero;
        
        // CanvasGroupを追加（フェードアニメーション用）
        CanvasGroup loadingCanvasGroup = loadingPanelObj.AddComponent<CanvasGroup>();
        loadingCanvasGroup.alpha = 1f;

        // Spinner（DOTween回転アニメーション用）
        GameObject spinnerObj = new GameObject("Spinner");
        spinnerObj.transform.SetParent(loadingPanelObj.transform, false);
        Image spinnerIcon = spinnerObj.AddComponent<Image>();
        spinnerIcon.sprite = LoadIcon("icon_loading");
        spinnerIcon.preserveAspect = true;
        RectTransform spinnerRect = spinnerObj.GetComponent<RectTransform>();
        spinnerRect.anchorMin = new Vector2(0.1f, 0.2f);
        spinnerRect.anchorMax = new Vector2(0.3f, 0.8f);
        spinnerRect.offsetMin = Vector2.zero;
        spinnerRect.offsetMax = Vector2.zero;
        spinnerRect.pivot = new Vector2(0.5f, 0.5f); // 回転の中心

        // Loading icon (旧オブジェクトの削除、Spinnerに統合)

        GameObject loadingTextObj = new GameObject("LoadingText");
        loadingTextObj.transform.SetParent(loadingPanelObj.transform, false);
        Text loadingText = loadingTextObj.AddComponent<Text>();
        loadingText.text = "Loading...";
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingText.fontSize = 28;
        loadingText.color = Color.white;
        loadingText.alignment = TextAnchor.MiddleLeft;
        RectTransform loadingTextRect = loadingTextObj.GetComponent<RectTransform>();
        loadingTextRect.anchorMin = new Vector2(0.35f, 0);
        loadingTextRect.anchorMax = new Vector2(1, 1);
        loadingTextRect.offsetMin = Vector2.zero;
        loadingTextRect.offsetMax = Vector2.zero;

        loadingPanelObj.SetActive(false);

        // ============================================
        // RECONSTRUCTED: Category Panel
        // ============================================
        GameObject categoryPanel = new GameObject("CategoryPanel");
        categoryPanel.transform.SetParent(panelObj.transform, false);
        Image cpBg = categoryPanel.AddComponent<Image>();
        cpBg.color = new Color(0.1f, 0.15f, 0.3f, 0.95f);
        RectTransform cpRect = categoryPanel.GetComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(0.1f, 0.1f);
        cpRect.anchorMax = new Vector2(0.9f, 0.9f);
        cpRect.offsetMin = Vector2.zero;
        cpRect.offsetMax = Vector2.zero;
        categoryPanel.SetActive(false); // Initially hidden

        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(categoryPanel.transform, false);
        RectTransform bcRect = buttonContainer.AddComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0.05f, 0.15f); // Leave space for title/close
        bcRect.anchorMax = new Vector2(0.95f, 0.85f);
        bcRect.offsetMin = Vector2.zero;
        bcRect.offsetMax = Vector2.zero;
        GridLayoutGroup categoryGrid = buttonContainer.AddComponent<GridLayoutGroup>();
        categoryGrid.cellSize = new Vector2(140, 60);
        categoryGrid.spacing = new Vector2(10, 10);
        categoryGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        categoryGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        categoryGrid.childAlignment = TextAnchor.MiddleCenter;
        categoryGrid.constraint = GridLayoutGroup.Constraint.Flexible;

        // Search Execute Button - カテゴリパネルの子オブジェクト（下部に大きく配置）
        GameObject searchExecBtnObj = new GameObject("SearchExecuteButton");
        searchExecBtnObj.transform.SetParent(categoryPanel.transform, false); // ★ カテゴリパネルの子
        Image searchExecBg = searchExecBtnObj.AddComponent<Image>();
        searchExecBg.color = new Color(0.1f, 0.8f, 0.3f); // 明るい緑
        RectTransform searchExecRect = searchExecBtnObj.GetComponent<RectTransform>();
        // カテゴリパネルの下部に大きく配置
        searchExecRect.anchorMin = new Vector2(0.15f, -0.25f); // パネルの下に出る
        searchExecRect.anchorMax = new Vector2(0.85f, -0.05f);
        searchExecRect.offsetMin = Vector2.zero;
        searchExecRect.offsetMax = Vector2.zero;
        // X軸で手前に傾ける（奥に倒れる感じ）
        searchExecRect.localRotation = Quaternion.Euler(20f, 0, 0);
        
        Button searchExecBtn = searchExecBtnObj.AddComponent<Button>();
        BoxCollider searchExecCol = searchExecBtnObj.AddComponent<BoxCollider>();
        searchExecCol.size = new Vector3(500, 100, 80f); // 大きめのコライダー
        searchExecCol.center = new Vector3(0, 0, -40f);
        searchExecBtnObj.AddComponent<CanvasRaycastTarget>(); // NRSDK用
        // Horizontal Layout for Icon + Text
        HorizontalLayoutGroup btnLayout = searchExecBtnObj.AddComponent<HorizontalLayoutGroup>();
        btnLayout.childAlignment = TextAnchor.MiddleCenter;
        btnLayout.spacing = 20;
        btnLayout.childControlWidth = false;
        btnLayout.childControlHeight = false;
        btnLayout.childForceExpandWidth = false;
        btnLayout.childForceExpandHeight = false;

        // Icon Object
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(searchExecBtnObj.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = LoadIcon("icon_search"); // Load icon_search.png
        iconImg.preserveAspect = true;
        LayoutElement iconLe = iconObj.AddComponent<LayoutElement>();
        iconLe.preferredWidth = 60; // Size 60x60
        iconLe.preferredHeight = 60;
        
        // Text Object
        GameObject searchExecTextObj = new GameObject("Text");
        searchExecTextObj.transform.SetParent(searchExecBtnObj.transform, false);
        Text searchExecText = searchExecTextObj.AddComponent<Text>();
        searchExecText.text = "検索開始"; // Removed unicode icon
        searchExecText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        searchExecText.alignment = TextAnchor.MiddleLeft;
        searchExecText.color = Color.white;
        searchExecText.fontSize = 28;
        searchExecText.raycastTarget = false;
        
        // Text Layout Element
        LayoutElement textLe = searchExecTextObj.AddComponent<LayoutElement>();
        textLe.preferredWidth = 150;
        textLe.preferredHeight = 40;

        // Cancel Button (Top-Right Corner - to avoid overlap with page navigation)
        GameObject cancelBtnObj = new GameObject("CancelButton");
        cancelBtnObj.transform.SetParent(categoryPanel.transform, false);
        Image cancelBg = cancelBtnObj.AddComponent<Image>();
        cancelBg.color = new Color(0.8f, 0.2f, 0.2f); // Red
        RectTransform cancelRect = cancelBtnObj.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.85f, 0.88f);  // Top-right
        cancelRect.anchorMax = new Vector2(0.98f, 0.98f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
        Button cancelBtn = cancelBtnObj.AddComponent<Button>();
        BoxCollider cancelCol = cancelBtnObj.AddComponent<BoxCollider>();
        cancelCol.size = new Vector3(150, 60, 60f);
        cancelCol.center = new Vector3(0, 0, -30f);
        cancelBtnObj.AddComponent<CanvasRaycastTarget>(); // NRSDK用
        
        GameObject cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelBtnObj.transform, false);
        Text cancelText = cancelTextObj.AddComponent<Text>();
        cancelText.text = "❌ 閉じる";
        cancelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cancelText.alignment = TextAnchor.MiddleCenter;
        cancelText.color = Color.white;
        cancelText.fontSize = 18;
        RectTransform cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        cancelTextRect.offsetMin = Vector2.zero;
        cancelTextRect.offsetMax = Vector2.zero;

        // 6. Attach Manager to Panel (Standard UI location)
        SearchUIManager manager = panelObj.GetComponent<SearchUIManager>();
        if (manager == null) manager = panelObj.AddComponent<SearchUIManager>();
        
        // Ensure uploader does NOT have SearchUIManager (cleanup legacy)
        if (uploader != null)
        {
            SearchUIManager legacyManager = uploader.GetComponent<SearchUIManager>();
            if (legacyManager != null && legacyManager != manager) Object.DestroyImmediate(legacyManager);
        }

        manager.searchButton = searchExecBtn; // Search Execute button
        
        // Search Execute button event
        searchExecBtn.onClick.AddListener(() => {
            manager.ExecuteSearch();
            categoryPanel.SetActive(false); // Close panel after search
        });
        
        // RESTORED LINKAGE
        manager.categoryPanel = categoryPanel;
        manager.categoryButtonContainer = buttonContainer.transform;
        
        manager.resultContainer = null; // ResultScrollView removed
        manager.resultImagePrefab = null; // ResultScrollView removed
        manager.loadingPanel = loadingPanelObj;
        manager.cancelButton = cancelBtn; 
        manager.closeResultsButton = null; 
        manager.resultScrollView = null; // ResultScrollView removed
        
        // Settings Panel References: Linked via SetupSettingsSystem or find later
        manager.settingsPanel = null; 
        // Controller/Hand buttons are found from Functions menu (handled in Main for now, but reference should be set here if possible)
        // Main file has manual assignment for these using finding by name?
        // manager.controllerModeButton = controllerBtnObj; // These objects are in Main, local vars. 
        // We will need to Find them or pass them. For now, let's Find them.
        
        Transform functionsMenu = panelObj.transform.Find("FunctionsHoverMenu");
        if (functionsMenu != null)
        {
             Transform inputBtnTr = functionsMenu.Find("Input MethodButton"); // Internal name "Input MethodButton" ?
             // In Main: functionsMenuBtns[2].name = "入力モードButton";
             // Actually, Main sets name when creating...
             // Look at Main: settingsMenuBtns[i].name = settingsMenuNames[i] + "Button";
             // But Functions menu names? 
             // Main: functionsMenuBtns[i] = CreateMenuButton...
             // It doesn't rename them explicitly to English keys like settings.
             // functionsMenuItems = {"SEARCH", "AR Mode", "入力モード", "+ 登録"};
             // So names are "SEARCHButton", "AR ModeButton", "入力モードButton", "+ 登録Button"
             
             Transform inputBtn = functionsMenu.Find("入力モードButton");
             if (inputBtn != null) 
             {
                 manager.controllerModeButton = inputBtn.gameObject;
                 manager.handModeButton = inputBtn.gameObject; // Same button toggles
             }
        }

        manager.imageUploader = uploader;
        if (uploader != null) 
        {
             uploader.serverUrlBase = "http://192.168.0.19:5000";
             Debug.Log("[SceneSetupTool] Set ImageUploader URL to: " + uploader.serverUrlBase);
        }

        // ============ Setup AR Display System ============
        GameObject arDisplayContainer = new GameObject("ARResultDisplay");
        arDisplayContainer.transform.SetParent(canvasObj.transform.parent, false);
        
        ARSearchResultDisplay arDisplay = arDisplayContainer.AddComponent<ARSearchResultDisplay>();
        FloatingCardDisplay floatingCard = arDisplayContainer.AddComponent<FloatingCardDisplay>();
        InfiniteCardCarousel infiniteCarousel = arDisplayContainer.AddComponent<InfiniteCardCarousel>();
        TimelineCorridorDisplay timelineCorridor = arDisplayContainer.AddComponent<TimelineCorridorDisplay>();
        
        // Create card container for 3D results
        GameObject cardContainer = new GameObject("CardContainer");
        cardContainer.transform.SetParent(arDisplayContainer.transform, false);
        
        // Link references
        arDisplay.floatingCardDisplay = floatingCard;
        arDisplay.infiniteCarousel = infiniteCarousel;
        arDisplay.timelineCorridorDisplay = timelineCorridor;
        arDisplay.arCamera = centerCam != null ? centerCam.GetComponent<Camera>() : Camera.main;
        arDisplay.currentMode = ARSearchResultDisplay.DisplayMode.InfiniteCarousel; 
        
        floatingCard.baseDisplay = arDisplay;
        floatingCard.cardContainer = cardContainer.transform;
        floatingCard.imageUploader = uploader;
        
        infiniteCarousel.baseDisplay = arDisplay;
        infiniteCarousel.cardContainer = cardContainer.transform;
        infiniteCarousel.imageUploader = uploader;
        
        timelineCorridor.baseDisplay = arDisplay;
        timelineCorridor.corridorContainer = cardContainer.transform;
        timelineCorridor.imageUploader = uploader;
        
        // Link to SearchUIManager
        manager.arDisplay = arDisplay;
        manager.useARDisplay = true;
        arDisplay.imageUploader = uploader;
        
        Debug.Log("[SceneSetupTool] AR Display System setup complete (InfiniteCarousel mode)!");

        manager.statusText = null;
    }
}
