using UnityEngine;
using UnityEngine.UI;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4
{
    private static void SetupWeatherSystem(GameObject panelObj, out WeatherManager weatherManager)
    {
        // 1. Weather Panel Setup (Main Panel)
        GameObject weatherPanelObj = new GameObject("WeatherPanel");
        weatherPanelObj.transform.SetParent(panelObj.transform, false);
        Image weatherPanelBg = weatherPanelObj.AddComponent<Image>();
        weatherPanelBg.color = new Color(0.05f, 0.1f, 0.2f, 0.95f); // Dark blue
        RectTransform weatherPanelRect = weatherPanelObj.GetComponent<RectTransform>();
        weatherPanelRect.anchorMin = new Vector2(0.1f, 0.2f);
        weatherPanelRect.anchorMax = new Vector2(0.9f, 0.8f);
        weatherPanelRect.offsetMin = Vector2.zero;
        weatherPanelRect.offsetMax = Vector2.zero;

        // Title
        GameObject weatherTitleObj = new GameObject("WeatherTitle");
        weatherTitleObj.transform.SetParent(weatherPanelObj.transform, false);
        Text weatherTitle = weatherTitleObj.AddComponent<Text>();
        weatherTitle.text = "天気予報";
        weatherTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weatherTitle.fontSize = 24;
        weatherTitle.color = new Color(0.6f, 0.9f, 1f, 1f);
        weatherTitle.fontStyle = FontStyle.Bold;
        weatherTitle.alignment = TextAnchor.MiddleCenter;
        RectTransform weatherTitleRect = weatherTitleObj.GetComponent<RectTransform>();
        weatherTitleRect.anchorMin = new Vector2(0, 0.85f);
        weatherTitleRect.anchorMax = new Vector2(1, 0.98f);
        weatherTitleRect.offsetMin = Vector2.zero;
        weatherTitleRect.offsetMax = Vector2.zero;

        // Date
        GameObject weatherDateObj = new GameObject("WeatherDate");
        weatherDateObj.transform.SetParent(weatherPanelObj.transform, false);
        Text weatherDate = weatherDateObj.AddComponent<Text>();
        weatherDate.text = "";
        weatherDate.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weatherDate.fontSize = 14;
        weatherDate.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);
        weatherDate.alignment = TextAnchor.MiddleCenter;
        RectTransform weatherDateRect = weatherDateObj.GetComponent<RectTransform>();
        weatherDateRect.anchorMin = new Vector2(0, 0.78f);
        weatherDateRect.anchorMax = new Vector2(1, 0.85f);
        weatherDateRect.offsetMin = Vector2.zero;
        weatherDateRect.offsetMax = Vector2.zero;

        // Content
        GameObject weatherContentObj = new GameObject("WeatherContent");
        weatherContentObj.transform.SetParent(weatherPanelObj.transform, false);
        Text weatherContent = weatherContentObj.AddComponent<Text>();
        weatherContent.text = "読み込み中...";
        weatherContent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weatherContent.fontSize = 18;
        weatherContent.color = Color.white;
        weatherContent.alignment = TextAnchor.UpperLeft;
        weatherContent.supportRichText = true;
        RectTransform weatherContentRect = weatherContentObj.GetComponent<RectTransform>();
        weatherContentRect.anchorMin = new Vector2(0.05f, 0.15f);
        weatherContentRect.anchorMax = new Vector2(0.95f, 0.75f);
        weatherContentRect.offsetMin = Vector2.zero;
        weatherContentRect.offsetMax = Vector2.zero;

        // Refresh Button
        GameObject refreshWeatherBtnObj = CreateIconButton("RefreshWeatherButton", "icon_search", "更新", weatherPanelObj.transform);
        RectTransform refreshWeatherRect = refreshWeatherBtnObj.GetComponent<RectTransform>();
        refreshWeatherRect.anchorMin = new Vector2(0.1f, 0.03f);
        refreshWeatherRect.anchorMax = new Vector2(0.45f, 0.12f);
        refreshWeatherRect.offsetMin = Vector2.zero;
        refreshWeatherRect.offsetMax = Vector2.zero;
        BoxCollider refreshWeatherCol = refreshWeatherBtnObj.AddComponent<BoxCollider>();
        refreshWeatherCol.size = new Vector3(150, 40, 1);

        // Close Button (No Icon)
        DefaultControls.Resources closeWeatherBtnRes = new DefaultControls.Resources();
        GameObject closeWeatherBtnObj = DefaultControls.CreateButton(closeWeatherBtnRes);
        closeWeatherBtnObj.name = "CloseWeatherButton";
        closeWeatherBtnObj.transform.SetParent(weatherPanelObj.transform, false);
        RectTransform closeWeatherRect = closeWeatherBtnObj.GetComponent<RectTransform>();
        closeWeatherRect.anchorMin = new Vector2(0.55f, 0.03f);
        closeWeatherRect.anchorMax = new Vector2(0.9f, 0.12f);
        closeWeatherRect.offsetMin = Vector2.zero;
        closeWeatherRect.offsetMax = Vector2.zero;
        
        // Style: Dark Red background, white text
        Image closeWeatherBg = closeWeatherBtnObj.GetComponent<Image>();
        if (closeWeatherBg != null) closeWeatherBg.color = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        Text closeWeatherText = closeWeatherBtnObj.GetComponentInChildren<Text>();
        if(closeWeatherText != null) {
            closeWeatherText.text = "閉じる";
            closeWeatherText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeWeatherText.fontSize = 16;
            closeWeatherText.color = Color.white;
        }
        
        BoxCollider closeWeatherCol = closeWeatherBtnObj.AddComponent<BoxCollider>();
        closeWeatherCol.size = new Vector3(150, 40, 10);
        // Z-Offset
        Vector3 cwPos = closeWeatherRect.anchoredPosition3D;
        closeWeatherRect.anchoredPosition3D = new Vector3(cwPos.x, cwPos.y, -10f);

        // Region Settings Button (on Weather Panel)
        GameObject regionBtnObj = new GameObject("RegionButton");
        regionBtnObj.transform.SetParent(weatherPanelObj.transform, false);
        Image regionBtnBg = regionBtnObj.AddComponent<Image>();
        regionBtnBg.color = new Color(0.2f, 0.4f, 0.6f, 0.9f);
        
        Text regionBtnText = new GameObject("Text").AddComponent<Text>();
        regionBtnText.transform.SetParent(regionBtnObj.transform, false);
        regionBtnText.text = "地域変更";
        regionBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        regionBtnText.fontSize = 16;
        regionBtnText.color = Color.white;
        regionBtnText.alignment = TextAnchor.MiddleCenter;
        RectTransform regionBtnTextRect = regionBtnText.GetComponent<RectTransform>();
        regionBtnTextRect.anchorMin = Vector2.zero;
        regionBtnTextRect.anchorMax = Vector2.one;
        regionBtnTextRect.offsetMin = Vector2.zero;
        regionBtnTextRect.offsetMax = Vector2.zero;
        
        Button regionBtn = regionBtnObj.AddComponent<Button>();
        regionBtn.transition = Selectable.Transition.ColorTint;
        regionBtn.targetGraphic = regionBtnBg;
        
        RectTransform regionBtnRect = regionBtnObj.GetComponent<RectTransform>();
        regionBtnRect.anchorMin = new Vector2(0.3f, 0.75f);
        regionBtnRect.anchorMax = new Vector2(0.7f, 0.82f);
        regionBtnRect.offsetMin = Vector2.zero;
        regionBtnRect.offsetMax = Vector2.zero;
        BoxCollider regionBtnCol = regionBtnObj.AddComponent<BoxCollider>();
        regionBtnCol.size = new Vector3(150, 30, 1);

        weatherPanelObj.SetActive(false);


        // 2. Region Settings Panel (地域選択パネル)
        GameObject regionPanelObj = new GameObject("RegionSettingsPanel");
        regionPanelObj.transform.SetParent(panelObj.transform, false);
        Image regionPanelBg = regionPanelObj.AddComponent<Image>();
        regionPanelBg.color = new Color(0.08f, 0.15f, 0.25f, 0.95f);
        
        RectTransform regionPanelRect = regionPanelObj.GetComponent<RectTransform>();
        regionPanelRect.anchorMin = new Vector2(0.1f, 0.1f);
        regionPanelRect.anchorMax = new Vector2(0.9f, 0.9f);
        regionPanelRect.offsetMin = Vector2.zero;
        regionPanelRect.offsetMax = Vector2.zero;
        
        // Title
        GameObject regionTitleObj = new GameObject("RegionTitle");
        regionTitleObj.transform.SetParent(regionPanelObj.transform, false);
        Text regionTitle = regionTitleObj.AddComponent<Text>();
        regionTitle.text = "地域を選択";
        regionTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        regionTitle.fontSize = 22;
        regionTitle.color = Color.white;
        regionTitle.alignment = TextAnchor.MiddleCenter;
        RectTransform regionTitleRect = regionTitleObj.GetComponent<RectTransform>();
        regionTitleRect.anchorMin = new Vector2(0, 0.88f);
        regionTitleRect.anchorMax = new Vector2(1, 0.98f);
        regionTitleRect.offsetMin = Vector2.zero;
        regionTitleRect.offsetMax = Vector2.zero;
        
        // ScrollView
        GameObject regionScrollObj = new GameObject("RegionScrollView");
        regionScrollObj.transform.SetParent(regionPanelObj.transform, false);
        RectTransform regionScrollRect = regionScrollObj.AddComponent<RectTransform>();
        regionScrollRect.anchorMin = new Vector2(0.02f, 0.12f);
        regionScrollRect.anchorMax = new Vector2(0.98f, 0.86f);
        regionScrollRect.offsetMin = Vector2.zero;
        regionScrollRect.offsetMax = Vector2.zero;
        
        // Content/Grid
        GameObject regionContentObj = new GameObject("RegionContent");
        regionContentObj.transform.SetParent(regionScrollObj.transform, false);
        RectTransform regionContentRect = regionContentObj.AddComponent<RectTransform>();
        regionContentRect.anchorMin = Vector2.zero;
        regionContentRect.anchorMax = Vector2.one;
        regionContentRect.offsetMin = Vector2.zero;
        regionContentRect.offsetMax = Vector2.zero;
        
        GridLayoutGroup regionGrid = regionContentObj.AddComponent<GridLayoutGroup>();
        regionGrid.cellSize = new Vector2(80, 35);
        regionGrid.spacing = new Vector2(8, 8);
        regionGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        regionGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        regionGrid.childAlignment = TextAnchor.UpperCenter;
        regionGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        regionGrid.constraintCount = 7;
        
        // Main Regions
        string[] mainRegions = {"北海道", "東京", "神奈川", "千葉", "埼玉", "愛知", "大阪", 
                                "京都", "兵庫", "広島", "福岡", "沖縄", "宮城", "新潟"};
        
        // Note: Logic to link buttons will be added after WeatherManager creation below
        
        // Close Button
        GameObject closeRegionBtnObj = new GameObject("CloseRegionButton");
        closeRegionBtnObj.transform.SetParent(regionPanelObj.transform, false);
        Image closeRegionBtnBg = closeRegionBtnObj.AddComponent<Image>();
        closeRegionBtnBg.color = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        
        Text closeRegionBtnText = new GameObject("Text").AddComponent<Text>();
        closeRegionBtnText.transform.SetParent(closeRegionBtnObj.transform, false);
        closeRegionBtnText.text = "閉じる";
        closeRegionBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeRegionBtnText.fontSize = 16;
        closeRegionBtnText.color = Color.white;
        closeRegionBtnText.alignment = TextAnchor.MiddleCenter;
        RectTransform closeRegionBtnTextRect = closeRegionBtnText.GetComponent<RectTransform>();
        closeRegionBtnTextRect.anchorMin = Vector2.zero;
        closeRegionBtnTextRect.anchorMax = Vector2.one;
        closeRegionBtnTextRect.offsetMin = Vector2.zero;
        closeRegionBtnTextRect.offsetMax = Vector2.zero;
        
        Button closeRegionBtn = closeRegionBtnObj.AddComponent<Button>();
        closeRegionBtn.transition = Selectable.Transition.ColorTint;
        closeRegionBtn.targetGraphic = closeRegionBtnBg;
        
        RectTransform closeRegionBtnRect = closeRegionBtnObj.GetComponent<RectTransform>();
        closeRegionBtnRect.anchorMin = new Vector2(0.35f, 0.02f);
        closeRegionBtnRect.anchorMax = new Vector2(0.65f, 0.10f);
        closeRegionBtnRect.offsetMin = Vector2.zero;
        closeRegionBtnRect.offsetMax = Vector2.zero;
        BoxCollider closeRegionBtnCol = closeRegionBtnObj.AddComponent<BoxCollider>();
        closeRegionBtnCol.size = new Vector3(300, 80, 10);
        
        // Z-Offset for interaction
        closeRegionBtnObj.transform.localPosition = new Vector3(
            closeRegionBtnObj.transform.localPosition.x,
            closeRegionBtnObj.transform.localPosition.y,
            -10f
        );
        
        closeRegionBtn.onClick.AddListener(() => {
            regionPanelObj.SetActive(false);
            Debug.Log("[RegionSettings] Panel closed!");
        });
        
        regionPanelObj.SetActive(false);
        
        // Restore Region Button Logic
        regionBtn.onClick.AddListener(() => {
            Debug.Log("[WeatherUI] RegionButton Clicked!");
            regionPanelObj.SetActive(true);
        });
        
        Button closeWeatherBtn = closeWeatherBtnObj.GetComponent<Button>();
        closeWeatherBtn.onClick.AddListener(() => { 
            Debug.Log("[WeatherUI] CloseWeatherButton Clicked!");
            weatherPanelObj.SetActive(false); 
        });


        // 3. Weather Toast
        GameObject weatherToastObj = new GameObject("WeatherToast");
        weatherToastObj.transform.SetParent(panelObj.transform, false);
        Image weatherToastBg = weatherToastObj.AddComponent<Image>();
        weatherToastBg.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);
        RectTransform weatherToastRect = weatherToastObj.GetComponent<RectTransform>();
        weatherToastRect.anchorMin = new Vector2(0.05f, 0.02f);
        weatherToastRect.anchorMax = new Vector2(0.95f, 0.10f);
        weatherToastRect.offsetMin = Vector2.zero;
        weatherToastRect.offsetMax = Vector2.zero;
        
        GameObject weatherToastTextObj = new GameObject("ToastText");
        weatherToastTextObj.transform.SetParent(weatherToastObj.transform, false);
        Text weatherToastText = weatherToastTextObj.AddComponent<Text>();
        weatherToastText.text = "天気読み込み中...";
        weatherToastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weatherToastText.fontSize = 18;
        weatherToastText.color = Color.white;
        weatherToastText.alignment = TextAnchor.MiddleCenter;
        RectTransform weatherToastTextRect = weatherToastTextObj.GetComponent<RectTransform>();
        weatherToastTextRect.anchorMin = Vector2.zero;
        weatherToastTextRect.anchorMax = Vector2.one;
        weatherToastTextRect.offsetMin = new Vector2(10, 0);
        weatherToastTextRect.offsetMax = new Vector2(-10, 0);
        
        weatherToastObj.SetActive(false);


        // 4. Top Bar Weather Button
        GameObject topBarWeatherBtn = new GameObject("WeatherButton");
        topBarWeatherBtn.transform.SetParent(panelObj.transform, false);
        Image topBarWeatherBg = topBarWeatherBtn.AddComponent<Image>();
        topBarWeatherBg.color = Color.clear;
        topBarWeatherBg.sprite = null;

        RectTransform topBarWeatherBtnRect = topBarWeatherBtn.GetComponent<RectTransform>();
        topBarWeatherBtnRect.anchorMin = new Vector2(0.90f, 0.85f);
        topBarWeatherBtnRect.anchorMax = new Vector2(0.99f, 0.98f);
        topBarWeatherBtnRect.offsetMin = Vector2.zero;
        topBarWeatherBtnRect.offsetMax = Vector2.zero;
        
        // Icon
        GameObject topBarWeatherIconObj = new GameObject("WeatherIcon");
        topBarWeatherIconObj.transform.SetParent(topBarWeatherBtn.transform, false);
        Image topBarWeatherIconImage = topBarWeatherIconObj.AddComponent<Image>();
        
        // Load default "Sunny" icon
        string weatherIconPath = "Assets/UI/tenki/digit-晴れ.png";
        // Note: Using private AssetDatabase call if possible, or Resources? Original used AssetDatabase.
        // AssetDatabase is Editor only.
#if UNITY_EDITOR
        Sprite weatherIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(weatherIconPath);
        // ... Importer logic omitted for brevity/safety, assuming assets exist or it will be null ...
        if (weatherIconSprite != null)
        {
            topBarWeatherIconImage.sprite = weatherIconSprite;
            topBarWeatherIconImage.preserveAspect = true;
        }
#endif
        topBarWeatherIconImage.color = Color.white;
        
        RectTransform topBarWeatherIconRect = topBarWeatherIconObj.GetComponent<RectTransform>();
        topBarWeatherIconRect.anchorMin = Vector2.zero;
        topBarWeatherIconRect.anchorMax = Vector2.one;
        topBarWeatherIconRect.offsetMin = Vector2.zero;
        topBarWeatherIconRect.offsetMax = Vector2.zero;
        
        Text topBarWeatherIcon = null; // Placeholder variable
        
        Button topBarWeatherButton = topBarWeatherBtn.AddComponent<Button>();
        topBarWeatherButton.transition = Selectable.Transition.None;
        BoxCollider topBarWeatherCol = topBarWeatherBtn.AddComponent<BoxCollider>();
        topBarWeatherCol.size = new Vector3(80, 80, 1);


        // 5. WeatherManager Setup & Linking
        weatherManager = panelObj.AddComponent<WeatherManager>();
        weatherManager.weatherPanel = weatherPanelObj;
        weatherManager.weatherTitleText = weatherTitle;
        weatherManager.weatherContentText = weatherContent;
        weatherManager.weatherDateText = weatherDate;
        weatherManager.closeWeatherButton = closeWeatherBtnObj.GetComponent<Button>();
        weatherManager.refreshWeatherButton = refreshWeatherBtnObj.GetComponent<Button>();
        weatherManager.weatherToast = weatherToastObj;
        weatherManager.weatherToastText = weatherToastText;
        weatherManager.toastDisplayDuration = 5f;
        weatherManager.topBarWeatherText = topBarWeatherIcon;
        weatherManager.topBarWeatherIcon = topBarWeatherIconImage;
        weatherManager.regionSettingsPanel = regionPanelObj;

        // Listeners
        // Capture out variable for lambda
        WeatherManager wmListener = weatherManager;
        weatherManager.closeWeatherButton.onClick.AddListener(() => {
            wmListener.HideWeatherPanel();
        });
        weatherManager.refreshWeatherButton.onClick.AddListener(() => {
            wmListener.RefreshWeather();
        });
        
        // TopBar Button Action (NRSDK)
        WeatherButton wb = topBarWeatherBtn.AddComponent<WeatherButton>();
        wb.weatherMgr = weatherManager;
        
        Debug.Log("[SceneSetupTool] WeatherManager Setup Complete.");

        // Region Buttons Logic (requires weatherManager)
        foreach (string region in mainRegions)
        {
            GameObject regionItemBtn = new GameObject(region + "Button");
            regionItemBtn.transform.SetParent(regionContentObj.transform, false);
            Image regionItemBg = regionItemBtn.AddComponent<Image>();
            regionItemBg.color = new Color(0.15f, 0.3f, 0.5f, 0.9f);
            
            Text regionItemText = new GameObject("Text").AddComponent<Text>();
            regionItemText.transform.SetParent(regionItemBtn.transform, false);
            regionItemText.text = region;
            regionItemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            regionItemText.fontSize = 14;
            regionItemText.color = Color.white;
            regionItemText.alignment = TextAnchor.MiddleCenter;
            RectTransform regionItemTextRect = regionItemText.GetComponent<RectTransform>();
            regionItemTextRect.anchorMin = Vector2.zero;
            regionItemTextRect.anchorMax = Vector2.one;
            regionItemTextRect.offsetMin = Vector2.zero;
            regionItemTextRect.offsetMax = Vector2.zero;
            
            Button regionItemButton = regionItemBtn.AddComponent<Button>();
            regionItemButton.transition = Selectable.Transition.ColorTint;
            regionItemButton.targetGraphic = regionItemBg;
            
            BoxCollider regionItemCol = regionItemBtn.AddComponent<BoxCollider>();
            regionItemCol.size = new Vector3(100, 50, 10);
            
            // Z Offset
            regionItemBtn.transform.localPosition = new Vector3(
                regionItemBtn.transform.localPosition.x,
                regionItemBtn.transform.localPosition.y,
                -10f
            );
            
            string selectedRegion = region;
            WeatherManager wm = weatherManager; // Local copy for lambda
            regionItemButton.onClick.AddListener(() => {
                if (wm != null)
                {
                    wm.SetRegion(selectedRegion);
                    regionPanelObj.SetActive(false);
                    wm.RefreshWeather();
                    Debug.Log($"[RegionSettings] Region changed to: {selectedRegion}");
                }
            });
        }
    }
}
