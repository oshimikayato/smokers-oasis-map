using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace NRKernal
{
    /// <summary>
    /// UI Toolkit版メインメニューコントローラー
    /// ホバーメニューのStaggeredアニメーションを含む
    /// </summary>
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("UI Document")]
        public UIDocument uiDocument;
        
        [Header("References")]
        public ImageUploader imageUploader;
        public WeatherManager weatherManager;
        public SearchUIManager searchUIManager;
        
        private VisualElement _root;
        private Button _settingsBtn;
        private Button _functionsBtn;
        private VisualElement _settingsHoverMenu;
        private VisualElement _functionsHoverMenu;
        
        private bool _settingsMenuOpen = false;
        private bool _functionsMenuOpen = false;
        
        private List<Button> _settingsMenuItems = new List<Button>();
        private List<Button> _functionsMenuItems = new List<Button>();
        
        void Start()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            
            _root = uiDocument.rootVisualElement;
            
            // Find elements
            _settingsBtn = _root.Q<Button>("settings-btn");
            _functionsBtn = _root.Q<Button>("functions-btn");
            _settingsHoverMenu = _root.Q<VisualElement>("settings-hover-menu");
            _functionsHoverMenu = _root.Q<VisualElement>("functions-hover-menu");
            
            // Collect menu items
            CollectMenuItems();
            
            // Setup button events
            SetupButtonEvents();
            
            // Initial state - hide menus
            HideSettingsMenu(false);
            HideFunctionsMenu(false);
            
            Debug.Log("[MainMenuUIController] Initialized");
        }
        
        void CollectMenuItems()
        {
            // Settings menu items
            _settingsMenuItems.Add(_root.Q<Button>("ip-settings-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("console-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("ar-mode-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("input-mode-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("beacon-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("reg-list-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("region-btn"));
            _settingsMenuItems.Add(_root.Q<Button>("tutorial-btn"));
            
            // Functions menu items
            _functionsMenuItems.Add(_root.Q<Button>("search-btn"));
            _functionsMenuItems.Add(_root.Q<Button>("face-btn"));
            _functionsMenuItems.Add(_root.Q<Button>("object-btn"));
            _functionsMenuItems.Add(_root.Q<Button>("register-btn"));
            _functionsMenuItems.Add(_root.Q<Button>("weather-btn"));
        }
        
        void SetupButtonEvents()
        {
            // Main menu buttons
            _settingsBtn?.RegisterCallback<ClickEvent>(evt => ToggleSettingsMenu());
            _functionsBtn?.RegisterCallback<ClickEvent>(evt => ToggleFunctionsMenu());
            
            // Settings menu items
            _root.Q<Button>("ip-settings-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                OpenSettingsPanel();
            });
            
            _root.Q<Button>("console-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                ToggleConsoleLog();
            });
            
            _root.Q<Button>("ar-mode-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                CycleARMode();
            });
            
            _root.Q<Button>("beacon-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                ToggleBeacon();
            });
            
            _root.Q<Button>("reg-list-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                OpenRegisteredList();
            });
            
            _root.Q<Button>("region-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                OpenRegionSettings();
            });
            
            _root.Q<Button>("tutorial-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideSettingsMenu(true);
                OpenTutorial();
            });
            
            // Functions menu items
            _root.Q<Button>("search-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideFunctionsMenu(true);
                StartSearch();
            });
            
            _root.Q<Button>("face-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideFunctionsMenu(true);
                ToggleFaceRecognition();
            });
            
            _root.Q<Button>("object-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideFunctionsMenu(true);
                ToggleObjectRecognition();
            });
            
            _root.Q<Button>("register-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideFunctionsMenu(true);
                OpenRegistrationPanel();
            });
            
            _root.Q<Button>("weather-btn")?.RegisterCallback<ClickEvent>(evt => {
                HideFunctionsMenu(true);
                OpenWeatherPanel();
            });
        }
        
        #region Menu Toggle
        
        void ToggleSettingsMenu()
        {
            if (_settingsMenuOpen)
            {
                HideSettingsMenu(true);
            }
            else
            {
                ShowSettingsMenu();
                HideFunctionsMenu(true);
            }
        }
        
        void ToggleFunctionsMenu()
        {
            if (_functionsMenuOpen)
            {
                HideFunctionsMenu(true);
            }
            else
            {
                ShowFunctionsMenu();
                HideSettingsMenu(true);
            }
        }
        
        void ShowSettingsMenu()
        {
            _settingsMenuOpen = true;
            _settingsHoverMenu.AddToClassList("open");
            
            // Staggered animation
            StartCoroutine(ShowMenuItemsStaggered(_settingsMenuItems));
        }
        
        void HideSettingsMenu(bool animate)
        {
            _settingsMenuOpen = false;
            _settingsHoverMenu.RemoveFromClassList("open");
            
            foreach (var item in _settingsMenuItems)
            {
                item?.RemoveFromClassList("visible");
            }
        }
        
        void ShowFunctionsMenu()
        {
            _functionsMenuOpen = true;
            _functionsHoverMenu.AddToClassList("open");
            
            // Staggered animation
            StartCoroutine(ShowMenuItemsStaggered(_functionsMenuItems));
        }
        
        void HideFunctionsMenu(bool animate)
        {
            _functionsMenuOpen = false;
            _functionsHoverMenu.RemoveFromClassList("open");
            
            foreach (var item in _functionsMenuItems)
            {
                item?.RemoveFromClassList("visible");
            }
        }
        
        IEnumerator ShowMenuItemsStaggered(List<Button> items)
        {
            yield return null; // Wait one frame for CSS transitions to reset
            
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.AddToClassList("visible");
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
        
        #endregion
        
        #region Actions
        
        void OpenSettingsPanel()
        {
            var settingsController = FindObjectOfType<SettingsPanelUIController>();
            settingsController?.ShowPanel();
            Debug.Log("[MainMenuUI] Open Settings Panel");
        }
        
        void ToggleConsoleLog()
        {
            var debugPanel = GameObject.Find("DebugPanel");
            if (debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
            }
            Debug.Log("[MainMenuUI] Toggle Console Log");
        }
        
        void CycleARMode()
        {
            var arDisplay = FindObjectOfType<ARSearchResultDisplay>();
            if (arDisplay != null)
            {
                // Cycle through modes: 0 -> 1 -> 2 -> 0
                int nextMode = ((int)arDisplay.currentMode + 1) % 3;
                arDisplay.SetDisplayMode((ARSearchResultDisplay.DisplayMode)nextMode);
            }
            Debug.Log("[MainMenuUI] Cycle AR Mode");
        }
        
        void ToggleBeacon()
        {
            imageUploader?.ToggleBeaconEnabled();
            Debug.Log("[MainMenuUI] Toggle Beacon");
        }
        
        void OpenRegisteredList()
        {
            imageUploader?.ShowRegisteredListPanel();
            Debug.Log("[MainMenuUI] Open Registered List");
        }
        
        void OpenRegionSettings()
        {
            // Region panel visibility toggling
            if (weatherManager != null && weatherManager.regionSettingsPanel != null)
            {
                bool isActive = weatherManager.regionSettingsPanel.activeSelf;
                weatherManager.regionSettingsPanel.SetActive(!isActive);
            }
            Debug.Log("[MainMenuUI] Open Region Settings");
        }
        
        void OpenTutorial()
        {
            var tutorialPanel = GameObject.Find("TutorialPanel");
            tutorialPanel?.SetActive(true);
            Debug.Log("[MainMenuUI] Open Tutorial");
        }
        
        void StartSearch()
        {
            // Use SearchUIManager to trigger search
            if (searchUIManager != null)
            {
                searchUIManager.ExecuteSearch();
            }
            Debug.Log("[MainMenuUI] Start Search");
        }
        
        void ToggleFaceRecognition()
        {
            // Face recognition feature removed
            Debug.Log("[MainMenuUI] Face Recognition feature removed");
        }
        
        void ToggleObjectRecognition()
        {
            imageUploader?.ShowObjectIdPanel();
            Debug.Log("[MainMenuUI] Toggle Object Recognition");
        }
        
        void OpenRegistrationPanel()
        {
            imageUploader?.ShowRegistrationSelectPanel();
            Debug.Log("[MainMenuUI] Open Registration Panel");
        }
        
        void OpenWeatherPanel()
        {
            weatherManager?.ShowWeatherPanel();
            Debug.Log("[MainMenuUI] Open Weather Panel");
        }
        
        #endregion
    }
}
