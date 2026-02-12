using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace NRKernal
{
    /// <summary>
    /// UI Toolkit版設定パネルコントローラー
    /// ページ切り替えアニメーションを含む
    /// </summary>
    public class SettingsPanelUIController : MonoBehaviour
    {
        [Header("UI Document")]
        public UIDocument uiDocument;
        
        [Header("References")]
        public ImageUploader imageUploader;
        public WeatherManager weatherManager;
        
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _panel;
        private VisualElement _content;
        
        private Button _prevBtn;
        private Button _nextBtn;
        private Button _closeBtn;
        private Label _pageIndicator;
        
        private VisualElement[] _pages = new VisualElement[3];
        private int _currentPage = 0;
        
        private bool _consoleOn = true;
        private bool _beaconOn = true;
        private int _arMode = 1; // 0: Float, 1: Carousel, 2: Corridor
        private int _inputMode = 0; // 0: Controller, 1: Hands
        
        void Start()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            
            _root = uiDocument.rootVisualElement;
            
            // Find elements
            _overlay = _root.Q<VisualElement>("settings-overlay");
            _panel = _root.Q<VisualElement>("settings-panel");
            _content = _root.Q<VisualElement>("settings-content");
            
            _prevBtn = _root.Q<Button>("prev-page-btn");
            _nextBtn = _root.Q<Button>("next-page-btn");
            _closeBtn = _root.Q<Button>("close-btn");
            _pageIndicator = _root.Q<Label>("page-indicator");
            
            // Find pages
            _pages[0] = _root.Q<VisualElement>("page-1");
            _pages[1] = _root.Q<VisualElement>("page-2");
            _pages[2] = _root.Q<VisualElement>("page-3");
            
            // Setup events
            SetupEvents();
            
            // Initial state
            UpdatePageIndicator();
            
            Debug.Log("[SettingsPanelUIController] Initialized");
        }
        
        void SetupEvents()
        {
            // Navigation
            _prevBtn?.RegisterCallback<ClickEvent>(evt => PreviousPage());
            _nextBtn?.RegisterCallback<ClickEvent>(evt => NextPage());
            _closeBtn?.RegisterCallback<ClickEvent>(evt => HidePanel());
            
            // Overlay click to close
            _overlay?.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == _overlay)
                {
                    HidePanel();
                }
            });
            
            // Page 1 buttons
            _root.Q<Button>("set-ip-btn")?.RegisterCallback<ClickEvent>(evt => SetServerIP());
            _root.Q<Button>("console-toggle-btn")?.RegisterCallback<ClickEvent>(evt => ToggleConsole());
            
            // Page 2 buttons
            _root.Q<Button>("mode-float-btn")?.RegisterCallback<ClickEvent>(evt => SetARMode(0));
            _root.Q<Button>("mode-carousel-btn")?.RegisterCallback<ClickEvent>(evt => SetARMode(1));
            _root.Q<Button>("mode-corridor-btn")?.RegisterCallback<ClickEvent>(evt => SetARMode(2));
            _root.Q<Button>("controller-btn")?.RegisterCallback<ClickEvent>(evt => SetInputMode(0));
            _root.Q<Button>("hand-btn")?.RegisterCallback<ClickEvent>(evt => SetInputMode(1));
            _root.Q<Button>("beacon-toggle-btn")?.RegisterCallback<ClickEvent>(evt => ToggleBeacon());
            
            // Page 3 buttons
            _root.Q<Button>("reg-list-open-btn")?.RegisterCallback<ClickEvent>(evt => OpenRegisteredList());
            _root.Q<Button>("region-open-btn")?.RegisterCallback<ClickEvent>(evt => OpenRegionSettings());
        }
        
        #region Panel Show/Hide
        
        public void ShowPanel()
        {
            _overlay.style.display = DisplayStyle.Flex;
            
            // Trigger animation
            _root.schedule.Execute(() => {
                _panel.AddToClassList("open");
            }).StartingIn(10);
            
            // Show first page
            ShowPage(0, false);
            
            Debug.Log("[SettingsPanelUI] Panel opened");
        }
        
        public void HidePanel()
        {
            _panel.RemoveFromClassList("open");
            
            // Hide after animation
            _root.schedule.Execute(() => {
                _overlay.style.display = DisplayStyle.None;
            }).StartingIn(400);
            
            Debug.Log("[SettingsPanelUI] Panel closed");
        }
        
        #endregion
        
        #region Page Navigation
        
        void PreviousPage()
        {
            if (_currentPage > 0)
            {
                ShowPage(_currentPage - 1, true, false);
            }
        }
        
        void NextPage()
        {
            if (_currentPage < _pages.Length - 1)
            {
                ShowPage(_currentPage + 1, true, true);
            }
        }
        
        void ShowPage(int index, bool animate, bool forward = true)
        {
            if (index < 0 || index >= _pages.Length) return;
            
            // Hide current page
            if (_pages[_currentPage] != null)
            {
                _pages[_currentPage].RemoveFromClassList("active");
                if (animate)
                {
                    _pages[_currentPage].AddToClassList(forward ? "exit-left" : "");
                }
            }
            
            _currentPage = index;
            
            // Show new page
            if (_pages[_currentPage] != null)
            {
                _pages[_currentPage].RemoveFromClassList("exit-left");
                
                // Delay for animation
                _root.schedule.Execute(() => {
                    _pages[_currentPage].AddToClassList("active");
                }).StartingIn(animate ? 50 : 0);
            }
            
            UpdatePageIndicator();
            UpdateNavigationButtons();
        }
        
        void UpdatePageIndicator()
        {
            if (_pageIndicator != null)
            {
                _pageIndicator.text = $"{_currentPage + 1} / {_pages.Length}";
            }
        }
        
        void UpdateNavigationButtons()
        {
            if (_prevBtn != null)
            {
                _prevBtn.SetEnabled(_currentPage > 0);
            }
            if (_nextBtn != null)
            {
                _nextBtn.SetEnabled(_currentPage < _pages.Length - 1);
            }
        }
        
        #endregion
        
        #region Settings Actions
        
        void SetServerIP()
        {
            var ipInput = _root.Q<TextField>("ip-input");
            if (ipInput != null && imageUploader != null)
            {
                imageUploader.SetServerUrl(ipInput.value);
                Debug.Log($"[SettingsPanelUI] Server IP set to: {ipInput.value}");
            }
        }
        
        void ToggleConsole()
        {
            _consoleOn = !_consoleOn;
            
            var btn = _root.Q<Button>("console-toggle-btn");
            var label = btn?.Q<Label>();
            
            if (btn != null)
            {
                btn.style.backgroundColor = new StyleColor(_consoleOn 
                    ? new Color(0.2f, 0.4f, 0.73f) 
                    : new Color(0.3f, 0.3f, 0.4f));
            }
            
            if (label != null)
            {
                label.text = _consoleOn ? "📋 Console Log: ON" : "📋 Console Log: OFF";
            }
            
            var debugPanel = GameObject.Find("DebugPanel");
            debugPanel?.SetActive(_consoleOn);
            
            Debug.Log($"[SettingsPanelUI] Console: {(_consoleOn ? "ON" : "OFF")}");
        }
        
        void SetARMode(int mode)
        {
            _arMode = mode;
            
            var floatBtn = _root.Q<Button>("mode-float-btn");
            var carouselBtn = _root.Q<Button>("mode-carousel-btn");
            var corridorBtn = _root.Q<Button>("mode-corridor-btn");
            
            if (floatBtn != null)
                floatBtn.style.backgroundColor = new StyleColor(mode == 0 
                    ? new Color(0.6f, 0.2f, 0.7f) : new Color(0.2f, 0.2f, 0.25f));
            
            if (carouselBtn != null)
                carouselBtn.style.backgroundColor = new StyleColor(mode == 1 
                    ? new Color(0.6f, 0.2f, 0.7f) : new Color(0.2f, 0.2f, 0.25f));
            
            if (corridorBtn != null)
                corridorBtn.style.backgroundColor = new StyleColor(mode == 2 
                    ? new Color(0.6f, 0.2f, 0.7f) : new Color(0.2f, 0.2f, 0.25f));
            
            // Apply to AR display
            var arDisplay = FindObjectOfType<ARSearchResultDisplay>();
            if (arDisplay != null)
            {
                arDisplay.SetDisplayMode((ARSearchResultDisplay.DisplayMode)mode);
            }
            
            PlayerPrefs.SetInt("ARDisplayMode", mode);
            Debug.Log($"[SettingsPanelUI] AR Mode: {mode}");
        }
        
        void SetInputMode(int mode)
        {
            _inputMode = mode;
            
            var controllerBtn = _root.Q<Button>("controller-btn");
            var handBtn = _root.Q<Button>("hand-btn");
            
            if (controllerBtn != null)
                controllerBtn.style.backgroundColor = new StyleColor(mode == 0 
                    ? new Color(0.1f, 0.5f, 0.3f) : new Color(0.2f, 0.2f, 0.25f));
            
            if (handBtn != null)
                handBtn.style.backgroundColor = new StyleColor(mode == 1 
                    ? new Color(0.1f, 0.5f, 0.3f) : new Color(0.2f, 0.2f, 0.25f));
            
            Debug.Log($"[SettingsPanelUI] Input Mode: {(mode == 0 ? "Controller" : "Hands")}");
        }
        
        void ToggleBeacon()
        {
            _beaconOn = !_beaconOn;
            
            var btn = _root.Q<Button>("beacon-toggle-btn");
            var label = _root.Q<Label>("beacon-label");
            
            if (btn != null)
            {
                btn.RemoveFromClassList("toggle-on");
                btn.RemoveFromClassList("toggle-off");
                btn.AddToClassList(_beaconOn ? "toggle-on" : "toggle-off");
            }
            
            if (label != null)
            {
                label.text = _beaconOn ? "📍 Beacon: ON" : "📍 Beacon: OFF";
            }
            
            imageUploader?.ToggleBeaconEnabled();
            Debug.Log($"[SettingsPanelUI] Beacon: {(_beaconOn ? "ON" : "OFF")}");
        }
        
        void OpenRegisteredList()
        {
            HidePanel();
            imageUploader?.ShowRegisteredListPanel();
        }
        
        void OpenRegionSettings()
        {
            HidePanel();
            // Show region panel directly
            if (weatherManager != null && weatherManager.regionSettingsPanel != null)
            {
                weatherManager.regionSettingsPanel.SetActive(true);
            }
        }
        
        #endregion
    }
}
