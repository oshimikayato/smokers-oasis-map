using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace NRKernal
{
    public class BottomMenuController : MonoBehaviour
    {
        [Header("Main Buttons")]
        public Button settingsMainButton;
        public Button functionsMainButton;

        [Header("Menu Panels")]
        public GameObject settingsMenuPanel;
        public GameObject functionsMenuPanel;

        [Header("Settings Menu Buttons")]
        public Button[] settingsMenuButtons; 
        // 0: IP設定, 1: Console Log, 2: Beacon, 3: 登録リスト, 4: 地域設定, 5: Tutorial

        [Header("Functions Menu Buttons")]
        public Button[] functionsMenuButtons;
        // 0: SEARCH, 1: AR Mode, 2: 入力モード, 3: + 登録

        [Header("References")]
        public GameObject ipSettingsPanel;
        public GameObject consoleLogPanel;
        public GameObject registrationListPanel;
        public GameObject weatherRegionPanel;
        public GameObject commonCloseButton; // パネル共通の閉じるボタン
        
        public ImageUploader imageUploader;
        public WeatherManager weatherManager;
        public TutorialManager tutorialManager;
        
        [Header("Background Click to Close")]
        public GameObject backgroundOverlay; // 背景オーバーレイ（クリックでメニュー閉じる）
        
        // Animation State
        private Coroutine _settingsMenuCoroutine;
        private Coroutine _functionsMenuCoroutine;
        private Dictionary<Button, Vector2> _initialPositions = new Dictionary<Button, Vector2>();

        void Start()
        {
            // Prepare buttons for animation
            PrepareButtons(settingsMenuButtons);
            PrepareButtons(functionsMenuButtons);

            SetupMainButtons();
            SetupSettingsMenuButtons();
            SetupFunctionsMenuButtons();
        }

        void PrepareButtons(Button[] buttons)
        {
            if (buttons == null) return;
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    // Add CanvasGroup if missing
                    if (btn.GetComponent<CanvasGroup>() == null)
                    {
                        btn.gameObject.AddComponent<CanvasGroup>();
                    }
                    
                    // Store initial position for animation
                    RectTransform rt = btn.GetComponent<RectTransform>();
                    if (rt != null && !_initialPositions.ContainsKey(btn))
                    {
                        _initialPositions[btn] = rt.anchoredPosition;
                    }
                }
            }
        }

        void SetupMainButtons()
        {
            if (settingsMainButton != null && settingsMenuPanel != null)
            {
                settingsMainButton.onClick.AddListener(() =>
                {
                    // チュートリアル表示中は無視
                    if (tutorialManager != null && tutorialManager.IsShowing) return;
                    
                    bool isOpening = !settingsMenuPanel.activeSelf;
                    // Toggle Logic
                    ToggleMenu(settingsMenuPanel, settingsMenuButtons, isOpening, ref _settingsMenuCoroutine, _functionsMenuCoroutine, functionsMenuPanel);
                });
            }

            // 機能ボタンはSEARCH専用 - 直接カテゴリウィンドウを表示
            if (functionsMainButton != null)
            {
                functionsMainButton.onClick.AddListener(() =>
                {
                    // チュートリアル表示中は無視
                    if (tutorialManager != null && tutorialManager.IsShowing) return;
                    
                    // 直接カテゴリウィンドウを開く（メニューを経由しない）
                    SearchUIManager sm = FindObjectOfType<SearchUIManager>();
                    if (sm != null) sm.ToggleCategoryPanel();
                    else Debug.LogWarning("[BottomMenu] SearchUIManager not found!");
                });
            }
        }

        void ToggleMenu(GameObject targetPanel, Button[] buttons, bool isOpening, ref Coroutine targetCoroutine, Coroutine otherCoroutine, GameObject otherPanel)
        {
            // Stop running animations
            if (targetCoroutine != null) StopCoroutine(targetCoroutine);
            
            // Close other menu if open
            if (otherPanel.activeSelf)
            {
                if (otherCoroutine != null) StopCoroutine(otherCoroutine);
                otherPanel.SetActive(false);
            }

            if (isOpening)
            {
                targetCoroutine = StartCoroutine(AnimateMenuOpen(targetPanel, buttons));
                // 背景オーバーレイを表示
                if (backgroundOverlay != null)
                {
                    backgroundOverlay.SetActive(true);
                }
            }
            else
            {
                targetPanel.SetActive(false);
                // 両方のメニューが閉じたらオーバーレイも非表示
                if (backgroundOverlay != null && !settingsMenuPanel.activeSelf && !functionsMenuPanel.activeSelf)
                {
                    backgroundOverlay.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 全てのメニューを閉じる（背景クリック用）
        /// </summary>
        public void CloseAllMenus()
        {
            if (_settingsMenuCoroutine != null) StopCoroutine(_settingsMenuCoroutine);
            if (_functionsMenuCoroutine != null) StopCoroutine(_functionsMenuCoroutine);
            
            if (settingsMenuPanel != null && settingsMenuPanel.activeSelf)
            {
                settingsMenuPanel.SetActive(false);
            }
            if (functionsMenuPanel != null && functionsMenuPanel.activeSelf)
            {
                functionsMenuPanel.SetActive(false);
            }
            if (backgroundOverlay != null)
            {
                backgroundOverlay.SetActive(false);
            }
        }

        IEnumerator AnimateMenuOpen(GameObject panel, Button[] buttons)
        {
            panel.SetActive(true);
            
            // Wait for end of frame to ensure layout is calculated (if needed)
            // or just force update immediately if possible.
            // For Safety, we update collider size here.
            
            // 1. Initialize all buttons to hidden state & Update Colliders
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                
                // Update BoxCollider size to match RectTransform
                RectTransform rt = btn.GetComponent<RectTransform>();
                BoxCollider col = btn.GetComponent<BoxCollider>();
                if (rt != null && col != null)
                {
                    // rect.size is valid even if object was just activated usually
                    // If size is zero, fallback to default or log
                    if (rt.rect.width > 0 && rt.rect.height > 0)
                    {
                        col.size = new Vector3(rt.rect.width, rt.rect.height, 1f);
                        // Center logic if needed (offset is usually used by pivot)
                        // col.center = new Vector3((0.5f - rt.pivot.x) * rt.rect.width, (0.5f - rt.pivot.y) * rt.rect.height, 0);
                        // Default BoxCollider is centered, pivot usually (0.5, 0.5) so it matches.
                    }
                }

                CanvasGroup cg = btn.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
                
                if (rt != null && _initialPositions.ContainsKey(btn))
                {
                    // Start 20px lower
                    rt.anchoredPosition = _initialPositions[btn] - new Vector2(0, 20);
                }
                
                btn.gameObject.SetActive(true); // Active but invisible due to alpha
            }

            // 2. Animate one by one
            foreach (var btn in buttons)
            {
                if (btn == null) continue;

                // Start individual animation
                StartCoroutine(AnimateButtonAppearance(btn));
                
                // Stagger delay
                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator AnimateButtonAppearance(Button btn)
        {
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            RectTransform rt = btn.GetComponent<RectTransform>();
            Vector2 targetPos = _initialPositions.ContainsKey(btn) ? _initialPositions[btn] : rt.anchoredPosition;
            Vector2 startPos = rt.anchoredPosition;

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Ease Out Back or Cubic
                float ease = 1f - Mathf.Pow(1f - t, 3);

                if (cg != null) cg.alpha = t;
                if (rt != null) rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);

                yield return null;
            }

            // Ensure final state
            if (cg != null) cg.alpha = 1f;
            if (rt != null) rt.anchoredPosition = targetPos;
        }

        void SetupSettingsMenuButtons()
    {
        if (settingsMenuButtons == null || settingsMenuButtons.Length < 6) return;

        // 0: IP設定
        settingsMenuButtons[0].onClick.AddListener(() => {
            CloseAllMenus();
            if (ipSettingsPanel != null) ipSettingsPanel.SetActive(true);
            ShowCommonCloseButton();
        });

        // 1: Console Log (Toggle) - 3D空間にコンソールを表示
        settingsMenuButtons[1].onClick.AddListener(() => {
            // 3Dコンソールを優先使用
            DebugConsole3D console3D = DebugConsole3D.Instance;
            if (console3D == null)
            {
                console3D = DebugConsole3D.CreateOrGet();
            }
            
            if (console3D != null)
            {
                console3D.Toggle();
                Debug.Log($"[BottomMenu] 3D Console toggled: {console3D.gameObject.activeSelf}");
            }
            else if (consoleLogPanel != null)
            {
                // フォールバック: UIパネル
                bool newState = !consoleLogPanel.activeSelf;
                consoleLogPanel.SetActive(newState);
                Debug.Log($"[BottomMenu] Console Panel toggled: {newState}");
            }
            else
            {
                Debug.LogWarning("[BottomMenu] No console available!");
            }
            CloseAllMenus();
        });

        // 2: Beacon
        settingsMenuButtons[2].onClick.AddListener(() => {
            CloseAllMenus();
            if (imageUploader != null) imageUploader.ToggleBeaconEnabled();
            Debug.Log("[BottomMenu] Beacon toggled");
        });

        // 3: 登録リスト
        settingsMenuButtons[3].onClick.AddListener(() => {
            CloseAllMenus();
            if (registrationListPanel != null) registrationListPanel.SetActive(true);
            ShowCommonCloseButton();
        });

        // 4: 地域設定
        settingsMenuButtons[4].onClick.AddListener(() => {
            CloseAllMenus();
            if (weatherRegionPanel != null) weatherRegionPanel.SetActive(true);
            ShowCommonCloseButton();
        });
        
        // 5: Tutorial
        settingsMenuButtons[5].onClick.AddListener(() => {
            CloseAllMenus();
            if (tutorialManager != null) tutorialManager.ShowTutorial();
        });
    }

        void SetupFunctionsMenuButtons()
        {
            if (functionsMenuButtons == null || functionsMenuButtons.Length < 4) return;

            // 0: SEARCH
            functionsMenuButtons[0].onClick.AddListener(() => {
                CloseAllMenus();
                // 検索はカメラ撮影+アップロードで実行される（ImageUploaderのUIから）
                Debug.Log("[BottomMenu] Search Button Clicked - capture image to search");
            });

            // 1: AR Mode (Display Mode Cycle)
            functionsMenuButtons[1].onClick.AddListener(() => {
                ARSearchResultDisplay arDisplay = ServiceLocator.Instance?.arSearchResultDisplay;
                if (arDisplay != null)
                {
                    // Cycle through modes: FloatingCard -> InfiniteCarousel -> TimelineCorridor -> FloatingCard
                    switch (arDisplay.currentMode)
                    {
                        case ARSearchResultDisplay.DisplayMode.FloatingCard:
                            arDisplay.SetDisplayMode(ARSearchResultDisplay.DisplayMode.InfiniteCarousel);
                            Debug.Log("[BottomMenu] AR Mode: InfiniteCarousel (カルーセル)");
                            break;
                        case ARSearchResultDisplay.DisplayMode.InfiniteCarousel:
                            arDisplay.SetDisplayMode(ARSearchResultDisplay.DisplayMode.TimelineCorridor);
                            Debug.Log("[BottomMenu] AR Mode: TimelineCorridor (記憶の回廊)");
                            break;
                        case ARSearchResultDisplay.DisplayMode.TimelineCorridor:
                            arDisplay.SetDisplayMode(ARSearchResultDisplay.DisplayMode.FloatingCard);
                            Debug.Log("[BottomMenu] AR Mode: FloatingCard (浮遊カード)");
                            break;
                    }
                    
                    // ボタンラベルを更新
                    Text modeText = functionsMenuButtons[1].GetComponentInChildren<Text>();
                    if (modeText != null)
                    {
                        modeText.text = $"AR: {arDisplay.currentMode}";
                    }
                }
                CloseAllMenus();
            });

            // 2: 入力モード (Controller <-> Hand Tracking)
            functionsMenuButtons[2].onClick.AddListener(() => {
                SearchUIManager searchUI = ServiceLocator.Instance?.searchUIManager;
                if (searchUI != null)
                {
                    SearchUIManager.InputMode newMode;
                    if (searchUI.currentInputMode == SearchUIManager.InputMode.Controller)
                    {
                        newMode = SearchUIManager.InputMode.HandTracking;
                    }
                    else
                    {
                        newMode = SearchUIManager.InputMode.Controller;
                    }
                    searchUI.SetInputMode(newMode);
                    Debug.Log($"[BottomMenu] Input Mode changed to: {newMode}");
                    
                    // ボタンラベルを更新
                    Text inputText = functionsMenuButtons[2].GetComponentInChildren<Text>();
                    if (inputText != null)
                    {
                        inputText.text = newMode == SearchUIManager.InputMode.Controller 
                            ? "入力: コントローラー" 
                            : "入力: ハンド";
                    }
                }
                else
                {
                    Debug.LogWarning("[BottomMenu] SearchUIManager not found!");
                }
                CloseAllMenus();
            });

            // 3: + 登録 (選択パネル)
            functionsMenuButtons[3].onClick.AddListener(() => {
                CloseAllMenus();
                if (imageUploader != null) imageUploader.ShowRegistrationSelectPanel();
                Debug.Log("[BottomMenu] Registration panel opened");
            });
        }
        
        void ShowCommonCloseButton()
        {
            if (commonCloseButton != null)
            {
                commonCloseButton.SetActive(true);
                commonCloseButton.transform.SetAsLastSibling(); // 最前面へ
            }
        }
    }
}
