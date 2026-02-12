using UnityEngine;
using UnityEngine.UI;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4
{
    private static void SetupSettingsSystem(GameObject panelObj, ImageUploader uploader, SearchUIManager manager, GameObject debugPanelGO, Text debugText, BottomMenuController bottomMenuCtrl)
    {
        if (uploader == null) return;

        uploader.debugText = debugText;

        // --- Create IP Settings Panel (Main Settings Panel) ---
        GameObject ipSettingsPanelObj = new GameObject("IPSettingsPanel");
        ipSettingsPanelObj.transform.SetParent(panelObj.transform, false);
        Image ipSettingsPanelBg = ipSettingsPanelObj.AddComponent<Image>();
        ipSettingsPanelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform ipSettingsRect = ipSettingsPanelObj.GetComponent<RectTransform>();
        ipSettingsRect.anchorMin = new Vector2(0.1f, 0.22f);
        ipSettingsRect.anchorMax = new Vector2(0.9f, 0.78f);
        ipSettingsRect.offsetMin = Vector2.zero;
        ipSettingsRect.offsetMax = Vector2.zero;

        // Title with Icon
        GameObject ipSettingsTitleObj = new GameObject("IPSettingsTitle");
        ipSettingsTitleObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        
        // Icon
        GameObject ipSettingsTitleIcon = new GameObject("Icon");
        ipSettingsTitleIcon.transform.SetParent(ipSettingsTitleObj.transform, false);
        Image stIcon = ipSettingsTitleIcon.AddComponent<Image>();
        stIcon.sprite = LoadIcon("icon_settings");
        stIcon.preserveAspect = true;
        RectTransform stIconRect = ipSettingsTitleIcon.GetComponent<RectTransform>();
        stIconRect.anchorMin = new Vector2(0.3f, 0.1f);
        stIconRect.anchorMax = new Vector2(0.45f, 0.9f);
        stIconRect.offsetMin = Vector2.zero;
        stIconRect.offsetMax = Vector2.zero;

        // Text
        GameObject ipSettingsTitleText = new GameObject("Text");
        ipSettingsTitleText.transform.SetParent(ipSettingsTitleObj.transform, false);
        Text ipSettingsTitle = ipSettingsTitleText.AddComponent<Text>();
        ipSettingsTitle.text = "Settings";
        ipSettingsTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ipSettingsTitle.fontSize = 22;
        ipSettingsTitle.color = Color.white;
        ipSettingsTitle.alignment = TextAnchor.MiddleLeft;
        RectTransform stTextRect = ipSettingsTitleText.GetComponent<RectTransform>();
        stTextRect.anchorMin = new Vector2(0.35f, 0);
        stTextRect.anchorMax = new Vector2(0.65f, 1);
        stTextRect.offsetMin = Vector2.zero;
        stTextRect.offsetMax = Vector2.zero;

        RectTransform ipSettingsTitleRect = ipSettingsTitleObj.AddComponent<RectTransform>();
        ipSettingsTitleRect.anchorMin = new Vector2(0, 0.88f);
        ipSettingsTitleRect.anchorMax = new Vector2(1, 1);
        ipSettingsTitleRect.offsetMin = Vector2.zero;
        ipSettingsTitleRect.offsetMax = Vector2.zero;

        // Navigation Resources
        DefaultControls.Resources navUiResources = new DefaultControls.Resources();

        // --- Page Navigation ---
        // Previous Button (<)
        GameObject prevPageBtnObj = DefaultControls.CreateButton(navUiResources);
        prevPageBtnObj.name = "PrevPageButton";
        prevPageBtnObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform prevPageBtnRect = prevPageBtnObj.GetComponent<RectTransform>();
        prevPageBtnRect.anchorMin = new Vector2(0.02f, 0.88f);
        prevPageBtnRect.anchorMax = new Vector2(0.12f, 0.98f);
        prevPageBtnRect.offsetMin = Vector2.zero;
        prevPageBtnRect.offsetMax = Vector2.zero;
        Text prevPageBtnText = prevPageBtnObj.GetComponentInChildren<Text>();
        prevPageBtnText.text = "＜";
        prevPageBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        prevPageBtnText.fontSize = 20;
        Image prevPageBtnBg = prevPageBtnObj.GetComponent<Image>();
        prevPageBtnBg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        BoxCollider prevPageCol = prevPageBtnObj.AddComponent<BoxCollider>();
        prevPageCol.size = new Vector3(50, 40, 1);

        // Next Button (>)
        GameObject nextPageBtnObj = DefaultControls.CreateButton(navUiResources);
        nextPageBtnObj.name = "NextPageButton";
        nextPageBtnObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform nextPageBtnRect = nextPageBtnObj.GetComponent<RectTransform>();
        nextPageBtnRect.anchorMin = new Vector2(0.88f, 0.88f);
        nextPageBtnRect.anchorMax = new Vector2(0.98f, 0.98f);
        nextPageBtnRect.offsetMin = Vector2.zero;
        nextPageBtnRect.offsetMax = Vector2.zero;
        Text nextPageBtnText = nextPageBtnObj.GetComponentInChildren<Text>();
        nextPageBtnText.text = "＞";
        nextPageBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nextPageBtnText.fontSize = 20;
        Image nextPageBtnBg = nextPageBtnObj.GetComponent<Image>();
        nextPageBtnBg.color = new Color(0.3f, 0.6f, 0.9f, 0.95f);
        BoxCollider nextPageCol = nextPageBtnObj.AddComponent<BoxCollider>();
        nextPageCol.size = new Vector3(50, 40, 1);

        // Page Indicator (1/3)
        GameObject settingsPageIndicatorObj = new GameObject("PageIndicator");
        settingsPageIndicatorObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        Text settingsPageIndicator = settingsPageIndicatorObj.AddComponent<Text>();
        settingsPageIndicator.text = "1 / 3";
        settingsPageIndicator.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        settingsPageIndicator.fontSize = 14;
        settingsPageIndicator.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        settingsPageIndicator.alignment = TextAnchor.MiddleCenter;
        RectTransform settingsPageIndRect = settingsPageIndicatorObj.GetComponent<RectTransform>();
        settingsPageIndRect.anchorMin = new Vector2(0.35f, 0.88f);
        settingsPageIndRect.anchorMax = new Vector2(0.65f, 0.98f);
        settingsPageIndRect.offsetMin = Vector2.zero;
        settingsPageIndRect.offsetMax = Vector2.zero;

        // SettingsPanelController
        SettingsPanelController settingsController = ipSettingsPanelObj.AddComponent<SettingsPanelController>();
        settingsController.prevButton = prevPageBtnObj.GetComponent<Button>();
        settingsController.nextButton = nextPageBtnObj.GetComponent<Button>();
        settingsController.pageIndicator = settingsPageIndicator;
        
        // Debug Feedback Text
        GameObject debugFeedbackObj = new GameObject("DebugFeedbackText");
        debugFeedbackObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        Text debugFeedbackText = debugFeedbackObj.AddComponent<Text>();
        debugFeedbackText.text = "[Waiting for button click...]";
        debugFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugFeedbackText.fontSize = 18;
        debugFeedbackText.color = Color.yellow;
        debugFeedbackText.fontStyle = FontStyle.Bold;
        debugFeedbackText.alignment = TextAnchor.MiddleCenter;
        RectTransform debugFeedbackRect = debugFeedbackObj.GetComponent<RectTransform>();
        debugFeedbackRect.anchorMin = new Vector2(0, 0.78f);
        debugFeedbackRect.anchorMax = new Vector2(1, 0.88f);
        debugFeedbackRect.offsetMin = Vector2.zero;
        debugFeedbackRect.offsetMax = Vector2.zero;
        debugFeedbackText.raycastTarget = false;
        debugFeedbackObj.transform.SetAsLastSibling();
        
        settingsController.debugFeedbackText = debugFeedbackText;

        // ============ PAGE 1: Basic Settings ============
        GameObject page1 = new GameObject("SettingsPage1");
        page1.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform page1Rect = page1.AddComponent<RectTransform>();
        page1Rect.anchorMin = new Vector2(0, 0);
        page1Rect.anchorMax = new Vector2(1, 0.88f);
        page1Rect.offsetMin = Vector2.zero;
        page1Rect.offsetMax = Vector2.zero;

        // --- Preset Buttons Row ---
        GameObject presetRowObj = new GameObject("PresetRow");
        presetRowObj.transform.SetParent(page1.transform, false);
        RectTransform presetRowRect = presetRowObj.AddComponent<RectTransform>();
        presetRowRect.anchorMin = new Vector2(0.05f, 0.75f);
        presetRowRect.anchorMax = new Vector2(0.95f, 0.95f);
        presetRowRect.offsetMin = Vector2.zero;
        presetRowRect.offsetMax = Vector2.zero;

        // Tethering WiFi Button
        GameObject tetheringBtnObj = CreateRoundedSettingsButton(
            "TetheringButton", 
            "📶 テザリング WiFi", 
            presetRowObj.transform,
            new Color(0.2f, 0.5f, 0.3f, 0.95f) // Green
        );
        RectTransform tetheringRect = tetheringBtnObj.GetComponent<RectTransform>();
        tetheringRect.anchorMin = new Vector2(0, 0);
        tetheringRect.anchorMax = new Vector2(0.48f, 1);
        tetheringRect.offsetMin = Vector2.zero;
        tetheringRect.offsetMax = Vector2.zero;
        BoxCollider tetheringCol = tetheringBtnObj.AddComponent<BoxCollider>();
        tetheringCol.size = new Vector3(200, 50, 1f);

        // Home Server Button
        GameObject homeServerBtnObj = CreateRoundedSettingsButton(
            "HomeServerButton", 
            "🏠 自宅サーバー", 
            presetRowObj.transform,
            new Color(0.5f, 0.3f, 0.6f, 0.95f) // Purple
        );
        RectTransform homeServerRect = homeServerBtnObj.GetComponent<RectTransform>();
        homeServerRect.anchorMin = new Vector2(0.52f, 0);
        homeServerRect.anchorMax = new Vector2(1, 1);
        homeServerRect.offsetMin = Vector2.zero;
        homeServerRect.offsetMax = Vector2.zero;
        BoxCollider homeServerCol = homeServerBtnObj.AddComponent<BoxCollider>();
        homeServerCol.size = new Vector3(200, 50, 1f);

        // --- IP Input Row ---
        GameObject ipRowObj = new GameObject("IPRow");
        ipRowObj.transform.SetParent(page1.transform, false);
        RectTransform ipRowRect = ipRowObj.AddComponent<RectTransform>();
        ipRowRect.anchorMin = new Vector2(0.05f, 0.50f);
        ipRowRect.anchorMax = new Vector2(0.95f, 0.70f);
        ipRowRect.offsetMin = Vector2.zero;
        ipRowRect.offsetMax = Vector2.zero;

        // IP Label
        GameObject ipLabelObj = new GameObject("IPLabel");
        ipLabelObj.transform.SetParent(ipRowObj.transform, false);
        Text ipLabel = ipLabelObj.AddComponent<Text>();
        ipLabel.text = "Server IP:";
        ipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ipLabel.fontSize = 16;
        ipLabel.color = Color.white;
        RectTransform ipLabelRect = ipLabelObj.GetComponent<RectTransform>();
        ipLabelRect.anchorMin = new Vector2(0, 0);
        ipLabelRect.anchorMax = new Vector2(0.2f, 1);
        ipLabelRect.offsetMin = Vector2.zero;
        ipLabelRect.offsetMax = Vector2.zero;

        // IP Input Field
        DefaultControls.Resources ipUiResources = new DefaultControls.Resources();
        GameObject ipInputObj = DefaultControls.CreateInputField(ipUiResources);
        ipInputObj.name = "IPInputField";
        ipInputObj.transform.SetParent(ipRowObj.transform, false);
        RectTransform ipInputRect = ipInputObj.GetComponent<RectTransform>();
        ipInputRect.anchorMin = new Vector2(0.22f, 0);
        ipInputRect.anchorMax = new Vector2(0.78f, 1);
        ipInputRect.offsetMin = Vector2.zero;
        ipInputRect.offsetMax = Vector2.zero;
        BoxCollider ipCollider = ipInputObj.AddComponent<BoxCollider>();
        ipCollider.size = new Vector3(300, 40, 1f);
        
        InputField ipField = ipInputObj.GetComponent<InputField>();
        ipField.text = uploader.serverUrlBase;
        Text ipText = ipInputObj.GetComponentInChildren<Text>();
        if (ipText != null) ipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // SET IP Button
        GameObject setIpBtnObj = CreateIconButton("SetIPButton", "icon_check", "SET", ipRowObj.transform);
        RectTransform setIpBtnRect = setIpBtnObj.GetComponent<RectTransform>();
        setIpBtnRect.anchorMin = new Vector2(0.8f, 0);
        setIpBtnRect.anchorMax = new Vector2(1, 1);
        setIpBtnRect.offsetMin = Vector2.zero;
        setIpBtnRect.offsetMax = Vector2.zero;
        BoxCollider setIpCol = setIpBtnObj.AddComponent<BoxCollider>();
        setIpCol.size = new Vector3(80, 40, 1f);
        
        Button setIpBtn = setIpBtnObj.GetComponent<Button>();
        setIpBtn.onClick.AddListener(() => {
            uploader.SetServerUrl(ipField.text);
        });

        // Preset Button Events
        Button tetheringBtn = tetheringBtnObj.GetComponent<Button>();
        tetheringBtn.onClick.AddListener(() => {
            ipField.text = "http://192.168.43.1:5000"; // Android hotspot default
            uploader.SetServerUrl(ipField.text);
            Debug.Log("[Settings] Tethering IP set: " + ipField.text);
        });

        Button homeServerBtn = homeServerBtnObj.GetComponent<Button>();
        homeServerBtn.onClick.AddListener(() => {
            ipField.text = "http://192.168.0.19:5000"; // Home server
            uploader.SetServerUrl(ipField.text);
            Debug.Log("[Settings] Home Server IP set: " + ipField.text);
        });

        // --- Log Toggle Row ---
        GameObject logRowObj = new GameObject("LogRow");
        logRowObj.transform.SetParent(page1.transform, false);
        RectTransform logRowRect = logRowObj.AddComponent<RectTransform>();
        logRowRect.anchorMin = new Vector2(0.1f, 0.25f);
        logRowRect.anchorMax = new Vector2(0.9f, 0.45f);
        logRowRect.offsetMin = Vector2.zero;
        logRowRect.offsetMax = Vector2.zero;

        // Log Toggle Button
        GameObject logToggleBtnObj = CreateRoundedSettingsButton(
            "LogToggleButton", 
            "搭 Console Log: ON", 
            logRowObj.transform,
            new Color(0.2f, 0.4f, 0.7f, 0.95f) // Blue
        );
        RectTransform logToggleRect = logToggleBtnObj.GetComponent<RectTransform>();
        logToggleRect.anchorMin = Vector2.zero;
        logToggleRect.anchorMax = Vector2.one;
        logToggleRect.offsetMin = Vector2.zero;
        logToggleRect.offsetMax = Vector2.zero;
        
        BoxCollider logToggleCol = logToggleBtnObj.AddComponent<BoxCollider>();
        logToggleCol.size = new Vector3(350, 50, 1f);
        
        Text logToggleText = logToggleBtnObj.GetComponentInChildren<Text>();
        Button logToggleBtn = logToggleBtnObj.GetComponent<Button>();
        logToggleBtn.onClick.AddListener(() => {
            bool isActive = debugPanelGO.activeSelf;
            debugPanelGO.SetActive(!isActive);
            if (logToggleText != null)
            {
                logToggleText.text = debugPanelGO.activeSelf ? "搭 Console Log: ON" : "搭 Console Log: OFF";
            }
        });

        // Common Close Button (Parented to Panel, not Page) - Standardized (No Icon)
        DefaultControls.Resources closeSettingsBtnRes = new DefaultControls.Resources();
        GameObject commonCloseBtnObj = DefaultControls.CreateButton(closeSettingsBtnRes);
        commonCloseBtnObj.name = "CommonCloseButton";
        commonCloseBtnObj.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform commonCloseRect = commonCloseBtnObj.GetComponent<RectTransform>();
        commonCloseRect.anchorMin = new Vector2(0.35f, 0.02f);
        commonCloseRect.anchorMax = new Vector2(0.65f, 0.10f);
        commonCloseRect.offsetMin = Vector2.zero;
        commonCloseRect.offsetMax = Vector2.zero;
        
        // Style: Dark Red background, white text
        Image commonCloseBg = commonCloseBtnObj.GetComponent<Image>();
        if (commonCloseBg != null) commonCloseBg.color = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        Text commonCloseText = commonCloseBtnObj.GetComponentInChildren<Text>();
        if(commonCloseText != null) {
            commonCloseText.text = "閉じる";
            commonCloseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            commonCloseText.fontSize = 18;
            commonCloseText.color = Color.white;
        }
        
        BoxCollider commonCloseCol = commonCloseBtnObj.AddComponent<BoxCollider>();
        commonCloseCol.size = new Vector3(300, 80, 10);
        
        // Z-Offset
        Vector3 ccPos = commonCloseRect.anchoredPosition3D;
        commonCloseRect.anchoredPosition3D = new Vector3(ccPos.x, ccPos.y, -10f);

        Button commonCloseBtn = commonCloseBtnObj.GetComponent<Button>();
        commonCloseBtn.onClick.AddListener(() => {
            Debug.Log("[SettingUI] CommonCloseButton Clicked!"); 
            if (ipSettingsPanelObj != null) ipSettingsPanelObj.SetActive(false);
            else Debug.LogError("[SettingUI] ipSettingsPanelObj is NULL");
        });
        commonCloseBtnObj.transform.SetAsLastSibling();
        uploader.closeSettingsButton = commonCloseBtnObj.GetComponent<Button>();

        settingsController.AddPage(page1);

        // ============ PAGE 2: Mode Settings ============
        GameObject page2 = new GameObject("SettingsPage2");
        page2.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform page2Rect = page2.AddComponent<RectTransform>();
        page2Rect.anchorMin = new Vector2(0, 0);
        page2Rect.anchorMax = new Vector2(1, 0.88f);
        page2Rect.offsetMin = Vector2.zero;
        page2Rect.offsetMax = Vector2.zero;
        page2.SetActive(false);

        // --- AR Display Mode Toggle Row ---
        GameObject arModeRowObj = new GameObject("ARModeRow");
        arModeRowObj.transform.SetParent(page2.transform, false);
        RectTransform arModeRowRect = arModeRowObj.AddComponent<RectTransform>();
        arModeRowRect.anchorMin = new Vector2(0.05f, 0.65f);
        arModeRowRect.anchorMax = new Vector2(0.95f, 0.85f);
        arModeRowRect.offsetMin = Vector2.zero;
        arModeRowRect.offsetMax = Vector2.zero;

        GameObject arModeBtnObj = DefaultControls.CreateButton(ipUiResources);
        arModeBtnObj.name = "ARModeToggleButton";
        arModeBtnObj.transform.SetParent(arModeRowObj.transform, false);
        RectTransform arModeRect = arModeBtnObj.GetComponent<RectTransform>();
        arModeRect.anchorMin = new Vector2(0, 0);
        arModeRect.anchorMax = new Vector2(1, 1);
        arModeRect.offsetMin = Vector2.zero;
        arModeRect.offsetMax = Vector2.zero;
        Text arModeText = arModeBtnObj.GetComponentInChildren<Text>();
        
        int savedMode = PlayerPrefs.GetInt("ARDisplayMode", 1); // Default: InfiniteCarousel
        savedMode = Mathf.Clamp(savedMode, 0, 2);
        string[] modeNamesInit = { "Floating Card", "Infinite Carousel", "Gallery Corridor" };
        arModeText.text = $"AR Mode: {modeNamesInit[savedMode]}";
        arModeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        arModeText.fontSize = 14;
        
        BoxCollider arModeCol = arModeBtnObj.AddComponent<BoxCollider>();
        arModeCol.size = new Vector3(400, 50, 10f);
        
        Button arModeBtn = arModeBtnObj.GetComponent<Button>();
        ARModeSwitcher modeSwitcher = arModeBtnObj.AddComponent<ARModeSwitcher>();
        modeSwitcher.modeButton = arModeBtn;
        modeSwitcher.modeText = arModeText;

        // --- Input Mode Toggle Row ---
        GameObject inputModeRowObj = new GameObject("InputModeRow");
        inputModeRowObj.transform.SetParent(page2.transform, false);
        RectTransform inputModeRowRect = inputModeRowObj.AddComponent<RectTransform>();
        inputModeRowRect.anchorMin = new Vector2(0.05f, 0.35f);
        inputModeRowRect.anchorMax = new Vector2(0.95f, 0.55f);
        inputModeRowRect.offsetMin = Vector2.zero;
        inputModeRowRect.offsetMax = Vector2.zero;

        // Input Mode Label
        GameObject inputModeLabelObj = new GameObject("InputModeLabel");
        inputModeLabelObj.transform.SetParent(inputModeRowObj.transform, false);
        Text inputModeLabel = inputModeLabelObj.AddComponent<Text>();
        inputModeLabel.text = "Input:";
        inputModeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputModeLabel.fontSize = 14;
        inputModeLabel.color = Color.white;
        inputModeLabel.alignment = TextAnchor.MiddleLeft;
        RectTransform inputLabelRect = inputModeLabelObj.GetComponent<RectTransform>();
        inputLabelRect.anchorMin = new Vector2(0, 0);
        inputLabelRect.anchorMax = new Vector2(0.15f, 1);
        inputLabelRect.offsetMin = Vector2.zero;
        inputLabelRect.offsetMax = Vector2.zero;

        // Controller Mode Button
        GameObject controllerBtnObj = CreateIconButton("ControllerModeButton", "icon_settings", "Controller", inputModeRowObj.transform);
        RectTransform controllerBtnRect = controllerBtnObj.GetComponent<RectTransform>();
        controllerBtnRect.anchorMin = new Vector2(0.18f, 0.1f);
        controllerBtnRect.anchorMax = new Vector2(0.55f, 0.9f);
        controllerBtnRect.offsetMin = Vector2.zero;
        controllerBtnRect.offsetMax = Vector2.zero;
        Image controllerBtnBg = controllerBtnObj.GetComponent<Image>();
        if (controllerBtnBg != null) controllerBtnBg.color = new Color(0.2f, 0.6f, 0.3f, 0.95f);
        BoxCollider controllerCol = controllerBtnObj.AddComponent<BoxCollider>();
        controllerCol.size = new Vector3(150, 50, 1);

        // Hand Mode Button  
        GameObject handBtnObj = CreateIconButton("HandModeButton", "icon_face_id", "Hands", inputModeRowObj.transform);
        RectTransform handBtnRect = handBtnObj.GetComponent<RectTransform>();
        handBtnRect.anchorMin = new Vector2(0.58f, 0.1f);
        handBtnRect.anchorMax = new Vector2(0.95f, 0.9f);
        handBtnRect.offsetMin = Vector2.zero;
        handBtnRect.offsetMax = Vector2.zero;
        BoxCollider handCol = handBtnObj.AddComponent<BoxCollider>();
        handCol.size = new Vector3(150, 50, 1);

        // --- Beacon Toggle Row ---
        GameObject beaconRowObj = new GameObject("BeaconRow");
        beaconRowObj.transform.SetParent(page2.transform, false);
        RectTransform beaconRowRect = beaconRowObj.AddComponent<RectTransform>();
        beaconRowRect.anchorMin = new Vector2(0.1f, 0.08f);
        beaconRowRect.anchorMax = new Vector2(0.9f, 0.28f);
        beaconRowRect.offsetMin = Vector2.zero;
        beaconRowRect.offsetMax = Vector2.zero;

        GameObject beaconBtnObj = CreateRoundedSettingsButton(
            "BeaconToggleButton",
            "桃 Beacon: ON",
            beaconRowObj.transform,
            new Color(0.1f, 0.5f, 0.3f, 0.95f) // Green
        );
        RectTransform beaconBtnRect = beaconBtnObj.GetComponent<RectTransform>();
        beaconBtnRect.anchorMin = Vector2.zero;
        beaconBtnRect.anchorMax = Vector2.one;
        beaconBtnRect.offsetMin = Vector2.zero;
        beaconBtnRect.offsetMax = Vector2.zero;
        BoxCollider beaconCol = beaconBtnObj.AddComponent<BoxCollider>();
        beaconCol.size = new Vector3(350, 50, 1f);
        
        Text beaconBtnText = beaconBtnObj.GetComponentInChildren<Text>();
        Image beaconBtnBg = beaconBtnObj.GetComponent<Image>();
        Button beaconBtnComponent = beaconBtnObj.GetComponent<Button>();
        beaconBtnComponent.onClick.AddListener(() => {
            uploader.ToggleBeaconEnabled();
            if (beaconBtnText != null)
            {
                beaconBtnText.text = uploader.beaconEnabled ? "桃 Beacon: ON" : "桃 Beacon: OFF";
            }
            if (beaconBtnBg != null)
            {
                beaconBtnBg.color = uploader.beaconEnabled 
                    ? new Color(0.1f, 0.5f, 0.3f, 0.95f) // Green
                    : new Color(0.4f, 0.2f, 0.2f, 0.95f); // Dark red
            }
        });

        settingsController.AddPage(page2);

        // ============ PAGE 3: Extra Settings ============
        GameObject page3 = new GameObject("SettingsPage3");
        page3.transform.SetParent(ipSettingsPanelObj.transform, false);
        RectTransform page3Rect = page3.AddComponent<RectTransform>();
        page3Rect.anchorMin = new Vector2(0, 0);
        page3Rect.anchorMax = new Vector2(1, 0.88f);
        page3Rect.offsetMin = Vector2.zero;
        page3Rect.offsetMax = Vector2.zero;
        page3.SetActive(false);

        // Registered List Button
        GameObject listBtnObj = DefaultControls.CreateButton(ipUiResources);
        listBtnObj.name = "RegisteredListButton";
        listBtnObj.transform.SetParent(page3.transform, false);
        RectTransform listBtnRect = listBtnObj.GetComponent<RectTransform>();
        listBtnRect.anchorMin = new Vector2(0.05f, 0.7f);
        listBtnRect.anchorMax = new Vector2(0.5f, 0.9f);
        listBtnRect.offsetMin = Vector2.zero;
        listBtnRect.offsetMax = Vector2.zero;
        Text listBtnText = listBtnObj.GetComponentInChildren<Text>();
        listBtnText.text = "搭 List";
        listBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        listBtnText.fontSize = 14;
        Image listBtnBg = listBtnObj.GetComponent<Image>();
        listBtnBg.color = new Color(0.2f, 0.3f, 0.5f, 0.95f); // Blue
        BoxCollider listCol = listBtnObj.AddComponent<BoxCollider>();
        listCol.size = new Vector3(180, 40, 1f);
        Button listBtn = listBtnObj.GetComponent<Button>();
        listBtn.onClick.AddListener(() => {
            uploader.ShowRegisteredListPanel();
        });

        // Clear Beacons Button
        GameObject clearBeaconsBtnObj = DefaultControls.CreateButton(ipUiResources);
        clearBeaconsBtnObj.name = "ClearBeaconsButton";
        clearBeaconsBtnObj.transform.SetParent(page3.transform, false);
        RectTransform clearBeaconsRect = clearBeaconsBtnObj.GetComponent<RectTransform>();
        clearBeaconsRect.anchorMin = new Vector2(0.52f, 0.7f);
        clearBeaconsRect.anchorMax = new Vector2(0.95f, 0.9f);
        clearBeaconsRect.offsetMin = Vector2.zero;
        clearBeaconsRect.offsetMax = Vector2.zero;
        Text clearBeaconsText = clearBeaconsBtnObj.GetComponentInChildren<Text>();
        clearBeaconsText.text = "卵 Beacons";
        clearBeaconsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        clearBeaconsText.fontSize = 14;
        Image clearBeaconsBg = clearBeaconsBtnObj.GetComponent<Image>();
        clearBeaconsBg.color = new Color(0.5f, 0.2f, 0.2f, 0.95f); // Red
        BoxCollider clearBeaconsCol = clearBeaconsBtnObj.AddComponent<BoxCollider>();
        clearBeaconsCol.size = new Vector3(180, 40, 1f);
        Button clearBeaconsBtn = clearBeaconsBtnObj.GetComponent<Button>();
        clearBeaconsBtn.onClick.AddListener(() => {
            uploader.ClearAllBeacons();
        });

        // Weather Region Button
        GameObject regionBtnObj = DefaultControls.CreateButton(ipUiResources);
        regionBtnObj.name = "WeatherRegionButton";
        regionBtnObj.transform.SetParent(page3.transform, false);
        RectTransform regionBtnRect = regionBtnObj.GetComponent<RectTransform>();
        regionBtnRect.anchorMin = new Vector2(0.05f, 0.45f);
        regionBtnRect.anchorMax = new Vector2(0.95f, 0.65f);
        regionBtnRect.offsetMin = Vector2.zero;
        regionBtnRect.offsetMax = Vector2.zero;
        Text regionBtnText = regionBtnObj.GetComponentInChildren<Text>();
        regionBtnText.text = "研 地域: 大阪";
        regionBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        regionBtnText.fontSize = 14;
        Image regionIconBg = regionBtnObj.GetComponent<Image>();
        regionIconBg.color = new Color(0.3f, 0.5f, 0.6f, 0.95f); // Teal
        BoxCollider regionCol = regionBtnObj.AddComponent<BoxCollider>();
        regionCol.size = new Vector3(300, 50, 1f);

        // Show Tutorial Button
        GameObject showTutorialBtnObj = CreateIconButton("ShowTutorialButton", "icon_info", "チュートリアル", page3.transform);
        RectTransform showTutorialRect = showTutorialBtnObj.GetComponent<RectTransform>();
        showTutorialRect.anchorMin = new Vector2(0.05f, 0.2f);
        showTutorialRect.anchorMax = new Vector2(0.95f, 0.4f);
        showTutorialRect.offsetMin = Vector2.zero;
        showTutorialRect.offsetMax = Vector2.zero;
        BoxCollider showTutorialCol = showTutorialBtnObj.AddComponent<BoxCollider>();
        showTutorialCol.size = new Vector3(300, 50, 1);

        settingsController.AddPage(page3);

        ipSettingsPanelObj.SetActive(false);

        // Link all references to uploader
        uploader.settingsPanel = ipSettingsPanelObj;
        uploader.debugPanel = debugPanelGO;
        uploader.logToggleButtonText = logToggleText;
        uploader.settingsButton = null; 
        uploader.logToggleButton = logToggleBtn;
        uploader.closeSettingsButton = commonCloseBtn;
        uploader.setIpButton = setIpBtn;
        uploader.ipInputField = ipField;

        // Link to BottomMenuController
        if (bottomMenuCtrl != null)
        {
            bottomMenuCtrl.ipSettingsPanel = ipSettingsPanelObj;
            bottomMenuCtrl.consoleLogPanel = debugPanelGO;
            bottomMenuCtrl.commonCloseButton = commonCloseBtn.gameObject;
            bottomMenuCtrl.imageUploader = uploader;
        }

        // Link to SearchUIManager
        manager.settingsPanel = ipSettingsPanelObj;
        manager.controllerModeButton = controllerBtnObj;
        manager.handModeButton = handBtnObj;

        // Force Close Button to be On Top of everything (Layout & Rendering)
        if (commonCloseBtnObj != null) commonCloseBtnObj.transform.SetAsLastSibling();
    }
    
    // Helper to Create Rounded Settings Button (Re-implemented)
    private static GameObject CreateRoundedSettingsButton(string name, string text, Transform parent, Color color)
    {
        GameObject btnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        btnObj.name = name;
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.GetComponent<Image>();
        if (img != null) img.color = color;
        
        Text txt = btnObj.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
        }
        
        ApplyRoundedCorners(btnObj, 12f);
        
        return btnObj;
    }
}
