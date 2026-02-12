using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NRKernal
{
    /// <summary>
    /// 設定パネルのページ切り替えコントローラー
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Pages")]
        public List<GameObject> pages = new List<GameObject>();
        
        [Header("Navigation")]
        public Button prevButton;
        public Button nextButton;
        public Text pageIndicator;
        
        [Header("Debug")]
        public Text debugFeedbackText; // 画面上にデバッグ情報を表示
        
        private int _currentPage = 0;
        private int _buttonClickCount = 0; // ボタンクリック回数をカウント
        
        void Start()
        {
            Debug.Log("[SettingsPanelController] Start called");
            
            // ボタンイベント設定
            if (prevButton != null)
            {
                prevButton.onClick.AddListener(PreviousPage);
            }
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextPage);
            }
            
            // ページ内のボタンイベントを登録（遅延させる）
            // Editorスクリプトからのページ追加が完了するのを待つ
            Invoke("SetupPageButtonListeners", 1.0f); // 1秒待機
            
            // 初期表示
            ShowPage(0);
        }
        
        void SetupPageButtonListeners()
        {
            Debug.Log("[SettingsPanelController] SetupPageButtonListeners called");
            
            // Find ImageUploader via ServiceLocator
            // Find ImageUploader via ServiceLocator or fallback
            ImageUploader uploader = ServiceLocator.Instance?.imageUploader;
            if (uploader == null)
            {
                uploader = FindObjectOfType<ImageUploader>();
                if (uploader == null)
                {
                    Debug.LogWarning("[SettingsPanelController] ImageUploader not found!");
                    return;
                }
                // Register found uploader back to ServiceLocator if possible
                ServiceLocator.Instance?.Register(uploader);
            }
            Debug.Log("[SettingsPanelController] ImageUploader found");
            
            // Find debug panel via ImageUploader (more reliable than GameObject.Find)
            GameObject debugPanel = uploader.debugPanel;
            Debug.Log($"[SettingsPanelController] DebugPanel found via uploader: {debugPanel != null}");
            
            // 直接このGameObject（IPSettingsPanel）の子を検索
            Transform panelTransform = this.transform;
            Debug.Log($"[SettingsPanelController] Panel: {panelTransform.name}, Children: {panelTransform.childCount}");
            
            // PAGE 1: IP Settings & Console
            Transform page1 = panelTransform.Find("SettingsPage1");
            Debug.Log($"[SettingsPanelController] Page1 found: {page1 != null}");
            if (page1 != null)
            {
                Debug.Log("[SettingsPanelController] Setting up Page 1 buttons");
                
                // SET IP Button
                Button setIpBtn = page1.Find("IPRow/SetIPButton")?.GetComponent<Button>();
                InputField ipField = page1.Find("IPRow/IPInputField")?.GetComponent<InputField>();
                Debug.Log($"[SettingsPanelController] SET IP Button found: {setIpBtn != null}, IP Field found: {ipField != null}");
                
                if (setIpBtn != null && ipField != null)
                {
                    setIpBtn.onClick.RemoveAllListeners();
                    setIpBtn.onClick.AddListener(() => {
                        _buttonClickCount++;
                        UpdateDebugText($"SET IP clicked! Count: {_buttonClickCount}");
                        Debug.Log("[SettingsPanelController] SET IP Button clicked!");
                        uploader.SetServerUrl(ipField.text);
                    });
                    Debug.Log("[SettingsPanelController] SET IP Button listener registered");
                }
                
                // Console Log Toggle Button
                Button logToggleBtn = page1.Find("LogRow/LogToggleButton")?.GetComponent<Button>();
                Debug.Log($"[SettingsPanelController] Console Log Button found: {logToggleBtn != null}");
                
                if (logToggleBtn != null)
                {
                    Text logToggleText = logToggleBtn.GetComponentInChildren<Text>();
                    logToggleBtn.onClick.RemoveAllListeners();
                    logToggleBtn.onClick.AddListener(() => {
                        _buttonClickCount++;
                        UpdateDebugText($"Console clicked! Count: {_buttonClickCount}");
                        Debug.Log("[SettingsPanelController] Console Log Button clicked!");
                        // Use ImageUploader's ToggleLogPanel method
                        uploader.ToggleLogPanel();
                        // Update text based on panel state
                        if (logToggleText != null && debugPanel != null)
                        {
                            logToggleText.text = debugPanel.activeSelf ? "Console Log: ON" : "Console Log: OFF";
                        }
                    });
                    Debug.Log("[SettingsPanelController] Console Log Button listener registered");
                }
            }
            
            // PAGE 2: AR Mode, Input Mode, Beacon
            Transform page2 = panelTransform.Find("SettingsPage2");
            Debug.Log($"[SettingsPanelController] Page2 found: {page2 != null}");
            if (page2 != null)
            {
                Debug.Log("[SettingsPanelController] Setting up Page 2 buttons");
                
                // AR Mode Toggle (handled by ARModeSwitcher component, no manual listener needed)
                
                // Controller Mode Button
                Button controllerBtn = FindButtonRecursively(page2, "ControllerModeButton");
                // Hand Mode Button
                Button handBtn = FindButtonRecursively(page2, "HandModeButton");
                Debug.Log($"[SettingsPanelController] Controller Button found: {controllerBtn != null}, Hand Button found: {handBtn != null}");
                
                if (controllerBtn != null && handBtn != null)
                {
                    Image controllerBg = controllerBtn.GetComponent<Image>();
                    Image handBg = handBtn.GetComponent<Image>();
                    
                    // Get SearchUIManager reference
                    SearchUIManager searchUI = ServiceLocator.Instance?.searchUIManager;
                    
                    controllerBtn.onClick.RemoveAllListeners();
                    controllerBtn.onClick.AddListener(() => {
                        Debug.Log("[SettingsPanelController] Controller Button clicked!");
                        // Switch to controller mode
                        if (controllerBg != null) controllerBg.color = new Color(0.2f, 0.6f, 0.3f, 0.95f); // Green
                        if (handBg != null) handBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f); // Dark
                        
                        // ★ Actual mode switching
                        if (searchUI != null)
                        {
                            searchUI.SetInputMode(SearchUIManager.InputMode.Controller);
                        }
                        Debug.Log("Switched to Controller Mode");
                    });
                    
                    handBtn.onClick.RemoveAllListeners();
                    handBtn.onClick.AddListener(() => {
                        Debug.Log("[SettingsPanelController] Hand Button clicked!");
                        // Switch to hand tracking mode
                        if (handBg != null) handBg.color = new Color(0.2f, 0.6f, 0.3f, 0.95f); // Green
                        if (controllerBg != null) controllerBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f); // Dark
                        
                        // ★ Actual mode switching
                        if (searchUI != null)
                        {
                            searchUI.SetInputMode(SearchUIManager.InputMode.HandTracking);
                        }
                        Debug.Log("Switched to Hand Tracking Mode");
                    });
                    Debug.Log("[SettingsPanelController] Input Mode buttons listeners registered");
                }
                
                // Beacon Toggle Button
                Button beaconBtn = page2.Find("BeaconRow/BeaconToggleButton")?.GetComponent<Button>();
                Debug.Log($"[SettingsPanelController] Beacon Button found: {beaconBtn != null}");
                
                if (beaconBtn != null && uploader != null)
                {
                    Text beaconText = beaconBtn.GetComponentInChildren<Text>();
                    Image beaconBg = beaconBtn.GetComponent<Image>();
                    
                    beaconBtn.onClick.RemoveAllListeners();
                    beaconBtn.onClick.AddListener(() => {
                        Debug.Log("[SettingsPanelController] Beacon Button clicked!");
                        uploader.ToggleBeaconEnabled();
                        if (beaconText != null)
                        {
                            beaconText.text = uploader.beaconEnabled ? "📍 Beacon: ON" : "📍 Beacon: OFF";
                        }
                        if (beaconBg != null)
                        {
                            beaconBg.color = uploader.beaconEnabled 
                                ? new Color(0.1f, 0.5f, 0.3f, 0.95f) // Green
                                : new Color(0.4f, 0.2f, 0.2f, 0.95f); // Dark red
                        }
                    });
                    Debug.Log("[SettingsPanelController] Beacon Button listener registered");
                }
            }
            
            // Helper to find button recursively
            Button FindButtonRecursively(Transform parent, string btnName)
            {
                if (parent.name == btnName) return parent.GetComponent<Button>();
                foreach (Transform child in parent)
                {
                    var result = FindButtonRecursively(child, btnName);
                    if (result != null) return result;
                }
                return null;
            }

            // PAGE 3: Registered List Button
            Transform page3 = panelTransform.Find("SettingsPage3");
            Debug.Log($"[SettingsPanelController] Page3 found: {page3 != null}");
            if (page3 != null)
            {
                Debug.Log("[SettingsPanelController] Setting up Page 3 buttons");
                
                // Try recursive find
                Button listBtn = FindButtonRecursively(page3, "RegisteredListButton");
                Debug.Log($"[SettingsPanelController] Registered List Button found: {listBtn != null}");
                
                if (listBtn != null && uploader != null)
                {
                    listBtn.onClick.RemoveAllListeners();
                    listBtn.onClick.AddListener(() => {
                        Debug.Log("[SettingsPanelController] Registered List Button clicked!");
                        uploader.ShowRegisteredListPanel();
                    });
                    Debug.Log("[SettingsPanelController] Registered List Button listener registered");
                }
            }
            
            Debug.Log("[SettingsPanelController] SetupPageButtonListeners completed");
        }
        
        public void AddPage(GameObject page)
        {
            Debug.Log($"[SettingsPanelController] AddPage called: {page.name}");
            pages.Add(page);
            bool shouldActivate = (pages.Count == 1);
            page.SetActive(shouldActivate); // 最初のページのみ表示
            Debug.Log($"[SettingsPanelController] Page {page.name} added. Total pages: {pages.Count}, SetActive: {shouldActivate}");
            UpdateUI();
        }
        
        public void NextPage()
        {
            if (_currentPage < pages.Count - 1)
            {
                ShowPage(_currentPage + 1);
            }
        }
        
        public void PreviousPage()
        {
            if (_currentPage > 0)
            {
                ShowPage(_currentPage - 1);
            }
        }
        
        public void ShowPage(int index)
        {
            Debug.Log($"[SettingsPanelController] ShowPage called with index: {index}, Total pages: {pages.Count}");
            
            if (index < 0 || index >= pages.Count)
            {
                Debug.LogWarning($"[SettingsPanelController] Invalid page index: {index}");
                return;
            }
            
            // 全ページ非表示
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] != null)
                {
                    bool shouldBeActive = (i == index);
                    pages[i].SetActive(shouldBeActive);
                    Debug.Log($"[SettingsPanelController] Page {i} ({pages[i].name}): SetActive({shouldBeActive})");
                }
            }
            
            _currentPage = index;
            UpdateUI();
            
            Debug.Log($"[SettingsPanelController] Current page is now: {_currentPage}");
        }
        
        void UpdateUI()
        {
            // ページインジケータ更新
            if (pageIndicator != null)
            {
                pageIndicator.text = $"{_currentPage + 1} / {pages.Count}";
            }
            
            // ボタン状態更新
            if (prevButton != null)
            {
                prevButton.interactable = _currentPage > 0;
                // 色も変更
                Image img = prevButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _currentPage > 0 
                        ? new Color(0.3f, 0.6f, 0.9f, 0.95f) // Blue
                        : new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gray
                }
            }
            
            if (nextButton != null)
            {
                nextButton.interactable = _currentPage < pages.Count - 1;
                Image img = nextButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _currentPage < pages.Count - 1 
                        ? new Color(0.3f, 0.6f, 0.9f, 0.95f) // Blue
                        : new Color(0.3f, 0.3f, 0.3f, 0.5f); // Gray
                }
            }
        }
        
        public int CurrentPage => _currentPage;
        public int PageCount => pages.Count;
        
        void UpdateDebugText(string message)
        {
            if (debugFeedbackText != null)
            {
                debugFeedbackText.text = $"[DEBUG] {message}\nTime: {Time.time:F2}s";
            }
        }
    }
}
