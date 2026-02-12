using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NRKernal
{
    /// <summary>
    /// UI Toolkit版UIセットアップツール
    /// </summary>
    public class UIToolkitSetupTool : EditorWindow
    {
        [MenuItem("Tools/Setup UI Toolkit Menu")]
        public static void SetupUIToolkit()
        {
            // Find or create UI container
            GameObject uiContainer = GameObject.Find("UIToolkitContainer");
            if (uiContainer == null)
            {
                uiContainer = new GameObject("UIToolkitContainer");
            }
            
            // Load assets
            var mainMenuUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UIToolkit/MainMenu.uxml");
            var settingsPanelUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UIToolkit/SettingsPanel.uxml");
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/UIToolkit/PanelSettings.asset");
            
            // Create PanelSettings if not exists
            if (panelSettings == null)
            {
                panelSettings = CreatePanelSettings();
            }
            
            // Setup Main Menu
            SetupMainMenu(uiContainer, mainMenuUxml, panelSettings);
            
            // Setup Settings Panel
            SetupSettingsPanel(uiContainer, settingsPanelUxml, panelSettings);
            
            // Setup Weather
            var weatherUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/UIToolkit/Weather.uxml");
            SetupWeather(uiContainer, weatherUxml, panelSettings);
            
            // Disable old uGUI menus
            DisableOldUGUI();
            
            EditorUtility.DisplayDialog("UI Toolkit Setup", "UI Toolkit components have been set up successfully!\n\nOld uGUI menus have been disabled.", "OK");
            Debug.Log("[UIToolkitSetupTool] Setup complete!");
        }
        
        [MenuItem("Tools/Migrate to UI Toolkit (Full)")]
        public static void FullMigration()
        {
            SetupUIToolkit();
            DisableOldUGUI();
            
            EditorUtility.DisplayDialog("Full Migration Complete", 
                "UI Toolkit is now the primary UI system.\n\n" +
                "Disabled:\n" +
                "- BottomMenuPanel\n" +
                "- SettingsHoverPanel\n" +
                "- FunctionsHoverPanel\n" +
                "- IPSettingsPanel (uGUI version)\n\n" +
                "Active:\n" +
                "- MainMenuUI (UI Toolkit)\n" +
                "- SettingsPanelUI (UI Toolkit)", 
                "OK");
        }
        
        static void DisableOldUGUI()
        {
            // Find FlashbackCanvas
            var canvas = GameObject.Find("FlashbackCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[UIToolkitSetupTool] FlashbackCanvas not found");
                return;
            }
            
            // Disable old menu panels (keep other panels like Registration, etc.)
            string[] panelsToDisable = {
                "BottomMenuPanel",
                "SettingsHoverPanel", 
                "FunctionsHoverPanel",
                "IPSettingsPanel",
                "WeatherPanel",
                "RegionSettingsPanel",
                "TopBarWeatherWidget"
            };
            
            foreach (string panelName in panelsToDisable)
            {
                Transform panel = canvas.transform.Find(panelName);
                if (panel != null)
                {
                    panel.gameObject.SetActive(false);
                    Debug.Log($"[UIToolkitSetupTool] Disabled: {panelName}");
                }
            }
            
            Debug.Log("[UIToolkitSetupTool] Old uGUI menus disabled");
        }
        
        // RestoreOldUGUI は削除（使用されていないため）
        
        static PanelSettings CreatePanelSettings()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            
            // Save asset
            AssetDatabase.CreateAsset(panelSettings, "Assets/UI/UIToolkit/PanelSettings.asset");
            AssetDatabase.SaveAssets();
            
            Debug.Log("[UIToolkitSetupTool] Created PanelSettings.asset");
            return panelSettings;
        }
        
        static void SetupMainMenu(GameObject container, VisualTreeAsset uxml, PanelSettings panelSettings)
        {
            string objName = "MainMenuUI";
            
            // Find existing or create new
            Transform existing = container.transform.Find(objName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            
            GameObject menuObj = new GameObject(objName);
            menuObj.transform.SetParent(container.transform);
            
            // Add UIDocument
            UIDocument uiDoc = menuObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;
            
            // Add controller
            MainMenuUIController controller = menuObj.AddComponent<MainMenuUIController>();
            controller.uiDocument = uiDoc;
            
            // Find references
            controller.imageUploader = Object.FindObjectOfType<ImageUploader>();
            controller.weatherManager = Object.FindObjectOfType<WeatherManager>();
            controller.searchUIManager = Object.FindObjectOfType<SearchUIManager>();
            
            Debug.Log("[UIToolkitSetupTool] Main Menu setup complete");
        }
        
        static void SetupSettingsPanel(GameObject container, VisualTreeAsset uxml, PanelSettings panelSettings)
        {
            string objName = "SettingsPanelUI";
            
            // Find existing or create new
            Transform existing = container.transform.Find(objName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            
            GameObject panelObj = new GameObject(objName);
            panelObj.transform.SetParent(container.transform);
            
            // Add UIDocument
            UIDocument uiDoc = panelObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;
            uiDoc.sortingOrder = 10; // Above main menu
            
            // Add controller
            SettingsPanelUIController controller = panelObj.AddComponent<SettingsPanelUIController>();
            controller.uiDocument = uiDoc;
            
            // Find references
            controller.imageUploader = Object.FindObjectOfType<ImageUploader>();
            controller.weatherManager = Object.FindObjectOfType<WeatherManager>();
            
            Debug.Log("[UIToolkitSetupTool] Settings Panel setup complete");
        }
        
        static void SetupWeather(GameObject container, VisualTreeAsset uxml, PanelSettings panelSettings)
        {
            if (uxml == null)
            {
                Debug.LogWarning("[UIToolkitSetupTool] Weather.uxml not found, skipping...");
                return;
            }
            
            string objName = "WeatherUI";
            
            // Find existing or create new
            Transform existing = container.transform.Find(objName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            
            GameObject weatherObj = new GameObject(objName);
            weatherObj.transform.SetParent(container.transform);
            
            // Add UIDocument
            UIDocument uiDoc = weatherObj.AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.visualTreeAsset = uxml;
            uiDoc.sortingOrder = 5; // Between main menu and settings
            
            // Add controller
            WeatherUIController controller = weatherObj.AddComponent<WeatherUIController>();
            controller.uiDocument = uiDoc;
            
            Debug.Log("[UIToolkitSetupTool] Weather setup complete");
        }
    }
}
