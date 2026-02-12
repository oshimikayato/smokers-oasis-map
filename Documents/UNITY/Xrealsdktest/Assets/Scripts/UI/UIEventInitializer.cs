using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace NRKernal
{
    /// <summary>
    /// ランタイムでUIイベントを設定するヘルパー
    /// </summary>
    public class UIEventInitializer : MonoBehaviour
    {
        // 画面表示用テキスト
        private Text diagnosticsText;
        private System.Text.StringBuilder logBuffer = new System.Text.StringBuilder();
        
        // [FIX] Global Double-click prevention
        private float _globalLastClickTime = 0f;
        private const float CLICK_DEBOUNCE_TIME = 0.4f; // Slightly reduced for better responsiveness

        void Start()
        {
            // [CRITICAL FIX] ドラッグ判定を緩くする
            // デフォルトだと微細なブレでドラッグと判定され、クリックがキャンセルされる
            NRInput.DragThreshold = 400.0f; // 大幅に緩和（デフォルトはもっと小さいはず）
            
            StartCoroutine(InitializeEventsDelayed());
            
            // WorldSpace mode is set in SceneSetupTool - do not override here
        }

        public void ClearLog()
        {
            logBuffer.Length = 0;
        }

        public void LogToScreen(string msg)
        {
            Debug.Log($"[UIEventInitializer] {msg}");
            logBuffer.Insert(0, $"> {msg}\n"); // 新しいものを上に
            if (logBuffer.Length > 500) logBuffer.Length = 500;
        }

        /// <summary>
        /// 安全なクリックイベント登録（重複削除 + デバウンス）
        /// </summary>
        void AddClickListener(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                if (Time.time - _globalLastClickTime < CLICK_DEBOUNCE_TIME) return;
                _globalLastClickTime = Time.time;
                action.Invoke();
            });
        }


        
        System.Collections.IEnumerator InitializeEventsDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            
            // 診断UIのセットアップ
            Transform canvasTr = transform.Find("SearchUI/Canvas");
            if (canvasTr == null) canvasTr = FindObjectOfType<Canvas>()?.transform;
            
            if (canvasTr != null)
            {
                // 左上に診断表示
                // ... (既存のコード)
                // DISABLED for button visibility test
                // CreateDiagnosticOverlay(canvasTr, "Initializing...");
            }
            
            // === NRSDK Input System Diagnostics ===
            System.Text.StringBuilder diagLog = new System.Text.StringBuilder();
            diagLog.AppendLine("=== NRSDK DIAG ===");
            
            diagLog.AppendLine($"NRInputModule: {NRInputModule.Active}");
            
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                diagLog.AppendLine($"ES: {eventSystem.name}");
                diagLog.AppendLine($"IM: {eventSystem.currentInputModule?.GetType().Name ?? "NULL"}");
                
                var nrInputModule = eventSystem.GetComponent<NRInputModule>();
                diagLog.AppendLine($"NRIM: {(nrInputModule != null ? "OK" : "NG")}");
            }
            else
            {
                diagLog.AppendLine("ES: NG!");
            }
            
            var raycasters = FindObjectsOfType<NRPointerRaycaster>();
            diagLog.AppendLine($"Raycaster: {raycasters.Length}");
            
            var canvasTargets = FindObjectsOfType<CanvasRaycastTarget>();
            diagLog.AppendLine($"CanvasRT: {canvasTargets.Length}");

            Debug.Log(diagLog.ToString());
            
            // === 画面最前面に大きく表示 ===
            Transform searchPanel = transform.Find("SearchPanel");
            if (searchPanel == null)
            {
                searchPanel = GameObject.Find("SearchPanel")?.transform;
            }
            if (searchPanel == null)
            {
                SearchUIManager searchUI = FindObjectOfType<SearchUIManager>();
                if (searchUI != null) searchPanel = searchUI.transform;
            }
            if (searchPanel != null)
            {
                // 診断用のUIを作成（オプション）
                // CreateDiagnosticOverlay(searchPanel, diagLog.ToString());
                
                SetupSettingsMenuEvents(searchPanel);
                SetupFunctionsMenuEvents(searchPanel);
                
                // [FIX] Double-click issue: Explicitly setup main buttons with debounce
                SetupMainButtonsEvents(searchPanel);
                
                SetupCloseButtonEvents(searchPanel);
                SetupRegistrationListCloseButton();
                SetupRegistrationSelectCancelButton();
                Debug.Log("[UIEventInitializer] All events initialized!");
            }
            else
            {
                Debug.LogError("[UIEventInitializer] SearchPanel not found!");
            }
            
            // Monitor children survival
            StartCoroutine(MonitorChildren());
        }

        void SetupMainButtonsEvents(Transform searchPanel)
        {
            Transform settingsMenu = searchPanel.Find("SettingsHoverMenu");
            Transform functionsMenu = searchPanel.Find("FunctionsHoverMenu");

            // 1. Settings Main Button
            Transform settingsBtnTr = searchPanel.Find("SettingsMainButton");
            if (settingsBtnTr != null)
            {
                Button btn = settingsBtnTr.GetComponent<Button>();
                if (btn != null && settingsMenu != null)
                {
                    AddClickListener(btn, () => {
                        // Close counterpart
                        if (functionsMenu != null) functionsMenu.gameObject.SetActive(false);
                        
                        // Force Open
                        settingsMenu.gameObject.SetActive(true);
                        Debug.Log("[UIEventInitializer] Settings Menu OPENED");
                    });
                    Debug.Log("[UIEventInitializer] SettingsMainButton configured (Open Only)");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] SettingsMainButton not found");
            
            // 2. Functions Main Button - SEARCH専用（直接カテゴリウィンドウを開く）
            Transform funcBtnTr = searchPanel.Find("FunctionsMainButton");
            if (funcBtnTr != null)
            {
                Button btn = funcBtnTr.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        // 直接カテゴリウィンドウを開く（メニューを経由しない）
                        SearchUIManager sm = FindObjectOfType<SearchUIManager>();
                        if (sm != null) sm.ToggleCategoryPanel();
                        else Debug.LogWarning("[UIEventInitializer] SearchUIManager not found!");
                    });
                    Debug.Log("[UIEventInitializer] FunctionsMainButton configured (Direct Search)");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] FunctionsMainButton not found");
        }

        System.Collections.IEnumerator MonitorChildren()
        {
            while (true)
            {
                yield return new WaitForSeconds(2.0f);
                int count = transform.childCount;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append($"[UI Monitor] Canvas children ({count}): ");
                
                Transform searchPanel = transform.Find("SearchPanel");
                if (searchPanel != null)
                {
                     sb.Append($"[SearchPanel] Pos: {searchPanel.position}, Scale: {searchPanel.localScale} ");
                     
                     // Check Main Buttons
                     Transform btnCont = searchPanel.Find("SettingsMainButton");
                     if (btnCont != null) sb.Append($"[SetBtn]: {btnCont.position} ");
                     
                     // Camera Distance
                     Camera mainCam = Camera.main;
                     if (mainCam != null)
                     {
                         float dist = Vector3.Distance(mainCam.transform.position, searchPanel.position);
                         sb.Append($"[Dist from MainCam]: {dist:F2}m ");
                     }
                }
                else
                {
                    sb.Append("SearchPanel MISSING! ");
                }

                for(int i=0; i<count; i++) sb.Append(transform.GetChild(i).name + ", ");
                Debug.Log(sb.ToString());
            }
        }
        
        void CreateDiagnosticOverlay(Transform parent, string message)
        {
            // シーン内の全DiagnosticOverlayを探して削除（親に関係なく、絶対的な重複排除）
            var allOverlays = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allOverlays)
            {
                // アセット内のプレハブ等は除外、シーン内のオブジェクトのみ
                if (obj.name == "DiagnosticOverlay" && obj.scene.isLoaded) 
                {
                    Destroy(obj);
                }
            }
            
            // オーバーレイ作成
            GameObject overlay = new GameObject("DiagnosticOverlay");
            overlay.transform.SetParent(parent, false);
            
            // 背景 (完全不透明で見やすく)
            UnityEngine.UI.Image bg = overlay.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0, 0, 0, 0.1f); // Semi-Transparent
            bg.raycastTarget = false;
            
            // RectTransform - 画面中央に大きく
            RectTransform rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.2f);
            rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition3D = new Vector3(0, 0, -100f); // 最前面確保
            
            // テキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(overlay.transform, false);
            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 10);
            textRt.offsetMax = new Vector2(-10, -10);
            
            // フィールドに保存
            diagnosticsText = text;
            LogToScreen(message); // 初期メッセージ
            
            // リアルタイムヒット表示を開始
            StartCoroutine(RealtimeHitMonitor());
        }
        
        System.Collections.IEnumerator RealtimeHitMonitor()
        {
            float startTime = Time.time;
            float duration = 99999f; // 実質無限
            
            while (Time.time - startTime < duration)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                
                // 1. ログバッファ表示
                sb.AppendLine("=== LOG ===");
                sb.AppendLine(logBuffer.ToString());
                sb.AppendLine("=== MONITOR ===");

                var raycasters = FindObjectsOfType<NRPointerRaycaster>();
                if (raycasters.Length > 0)
                {
                    var raycaster = raycasters[0];
                    var hoverData = raycaster.HoverEventData;
                    
                    if (hoverData != null && hoverData.pointerCurrentRaycast.isValid)
                    {
                        var hitGO = hoverData.pointerCurrentRaycast.gameObject;
                        sb.AppendLine($"HIT: {hitGO?.name ?? "null"}");
                        
                        // Button検索
                        if (hitGO != null)
                        {
                            var btn = hitGO.GetComponentInParent<UnityEngine.UI.Button>();
                            sb.AppendLine($"Btn: {(btn != null ? btn.name : "NONE")}");
                        }
                        
                        sb.AppendLine($"Press: {hoverData.pressPrecessed}");
                    }
                    else
                    {
                        sb.AppendLine("HIT: -");
                    }
                }
                
                sb.AppendLine($"Trig: {NRInput.GetButton(ControllerButton.TRIGGER)}");
                sb.Append($"{(int)(Time.time - startTime)}s");
                
                if (diagnosticsText != null) diagnosticsText.text = sb.ToString(); 
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // 終了後にオーバーレイを削除
            if (diagnosticsText != null && diagnosticsText.transform.parent != null)
            {
                Destroy(diagnosticsText.transform.parent.gameObject);
            }
        }
        
        System.Collections.IEnumerator HideAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) Destroy(obj);
        }
        
        string GetHierarchyPath(GameObject obj)
        {
            if (obj == null) return "null";
            string path = obj.name;
            Transform parent = obj.transform.parent;
            int depth = 0;
            while (parent != null && depth < 3)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
                depth++;
            }
            return path;
        }
        
        void SetupSettingsMenuEvents(Transform searchPanel)
        {
            Transform settingsMenu = searchPanel.Find("SettingsHoverMenu");
            if (settingsMenu == null)
            {
                Debug.LogWarning("[UIEventInitializer] SettingsHoverMenu not found");
                return;
            }
            
            // メニューが非アクティブの場合、一時的にアクティブにして子要素を取得
            bool wasActive = settingsMenu.gameObject.activeSelf;
            settingsMenu.gameObject.SetActive(true);
            
            Debug.Log("[UIEventInitializer] Configuring SettingsMenu buttons...");

            // 子要素を走査して名前でマッチング（より確実な方法）
            foreach (Transform child in settingsMenu)
            {
                string name = child.name;
                Button btn = child.GetComponent<Button>();
                
                if (btn == null) continue;
                
                // 既存のイベントをクリアして重複防止
                btn.onClick.RemoveAllListeners();
                bool configured = false;

                // 0: IP設定
                if (name.Contains("IPSettings") || name.Contains("IP設定") || name.Contains("IP Settings"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        Transform ipPanel = searchPanel.Find("IPSettingsPanel");
                        if (ipPanel != null) ipPanel.gameObject.SetActive(true);
                        Debug.Log("[UIEventInitializer] IP設定 clicked!");
                    });
                    configured = true;
                }
                // 1: Beacon
                else if (name.Contains("Beacon"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        ImageUploader uploader = FindObjectOfType<ImageUploader>();
                        if (uploader != null) {
                            uploader.ToggleBeaconEnabled();
                            Debug.Log("[UIEventInitializer] Beacon toggled!");
                        } else Debug.LogError("[UIEventInitializer] ImageUploader not found!");
                    });
                    configured = true;
                }
                // 2: 登録リスト
                else if (name.Contains("RegisteredList") || name.Contains("登録リスト") || name.Contains("Registered List"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        // 名前が微妙に異なる可能性を考慮して複数パターンで探す
                        Transform listPanel = searchPanel.Find("RegisteredListPanel");
                        if (listPanel == null) listPanel = searchPanel.Find("RegisteredList");
                        if (listPanel == null) listPanel = searchPanel.Find("RegistrationListPanel");
                        
                        if (listPanel != null) {
                            listPanel.gameObject.SetActive(true);
                            Debug.Log("[UIEventInitializer] 登録リスト opened!");
                        } else Debug.LogError("[UIEventInitializer] RegisteredListPanel not found!");
                    });
                    configured = true;
                }
                // 3: 地域設定
                else if (name.Contains("RegionSettings") || name.Contains("地域設定") || name.Contains("Region"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        WeatherManager wm = FindObjectOfType<WeatherManager>();
                        if (wm != null) wm.ShowRegionPanel();
                        else Debug.LogError("[UIEventInitializer] WeatherManager not found!");
                    });
                    configured = true;
                }
                // 4: Tutorial
                else if (name.Contains("Tutorial"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        TutorialManager tm = FindObjectOfType<TutorialManager>();
                        if (tm != null) tm.ShowTutorial();
                        else Debug.LogError("[UIEventInitializer] TutorialManager not found!");
                    });
                    configured = true;
                }
                // 入力モード
                else if (name.Contains("InputMode") || name.Contains("入力モード"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        SearchUIManager searchMgr = FindObjectOfType<SearchUIManager>();
                        if (searchMgr != null)
                        {
                            var newMode = searchMgr.currentInputMode == SearchUIManager.InputMode.Controller
                                ? SearchUIManager.InputMode.HandTracking
                                : SearchUIManager.InputMode.Controller;
                            searchMgr.SetInputMode(newMode);
                            Debug.Log($"[UIEventInitializer] InputMode changed to: {newMode}");
                        }
                        else
                        {
                            // Fallback: toggle NRInput source
                            NRInput.SetInputSource(NRInput.CurrentInputSourceType == InputSourceEnum.Hands
                                ? InputSourceEnum.Controller
                                : InputSourceEnum.Hands);
                            Debug.Log($"[UIEventInitializer] InputMode switched to: {NRInput.CurrentInputSourceType}");
                        }
                    });
                    configured = true;
                }
                // + 登録
                else if (name.Contains("Registration") || name.Contains("+ 登録"))
                {
                    AddClickListener(btn, () => {
                        settingsMenu.gameObject.SetActive(false);
                        ImageUploader uploader = FindObjectOfType<ImageUploader>();
                        if (uploader != null)
                        {
                            uploader.ShowRegistrationSelectPanel();
                            Debug.Log("[UIEventInitializer] + 登録 opened!");
                        }
                        else
                        {
                            Debug.LogError("[UIEventInitializer] ImageUploader not found!");
                        }
                    });
                    configured = true;
                }
                
                if (configured) Debug.Log($"[UIEventInitializer] Button configured successfully: {name}");
            }
            
            // メニューを元の状態に戻す
            settingsMenu.gameObject.SetActive(wasActive);
            
            // 地域設定パネルのイベント設定
            SetupRegionPanelEvents(searchPanel);
            
            // コンソールログパネルのイベント設定
            SetupConsoleLogPanelEvents(searchPanel);
        }

        void SetupConsoleLogPanelEvents(Transform searchPanel)
        {
            Transform consolePanel = searchPanel.Find("ConsoleLogPanel");
            if (consolePanel == null)
            {
                Debug.LogWarning("[UIEventInitializer] ConsoleLogPanel not found");
                return;
            }
            
            // 閉じるボタン
            Transform closeBtn = consolePanel.Find("CloseButton");
            if (closeBtn != null)
            {
                Button btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        consolePanel.gameObject.SetActive(false);
                        Debug.Log("[UIEventInitializer] ConsoleLogPanel closed!");
                    });
                    Debug.Log("[UIEventInitializer] ConsoleLogPanel CloseButton event set!");
                }
            }
            else
            {
                // 閉じるボタンが見つからない場合、他の名前を試す
                Transform altCloseBtn = consolePanel.Find("CloseConsoleButton");
                if (altCloseBtn != null)
                {
                    Button btn = altCloseBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            consolePanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] ConsoleLogPanel CloseConsoleButton event set!");
                    }
                }
            }
        }
        
        void SetupRegionPanelEvents(Transform searchPanel)
        {
            Transform regionPanel = FindDeepChild(transform, "RegionSettingsPanel");
            if (regionPanel == null)
            {
                Debug.LogWarning("[UIEventInitializer] RegionSettingsPanel not found");
                return;
            }
            
            // 閉じるボタン
            Transform closeBtn = regionPanel.Find("CloseRegionButton");
            if (closeBtn != null)
            {
                Button btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        regionPanel.gameObject.SetActive(false);
                        Debug.Log("[UIEventInitializer] RegionPanel closed!");
                    });
                    Debug.Log("[UIEventInitializer] CloseRegionButton event set!");
                }
            }
            
            // 地域ボタンのイベント設定
            Transform regionContent = regionPanel.Find("RegionScrollView/RegionContent");
            if (regionContent != null)
            {
                WeatherManager wm = FindObjectOfType<WeatherManager>();
                foreach (Transform child in regionContent)
                {
                    Button btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        string regionName = child.name.Replace("Button", "");
                        AddClickListener(btn, () => {
                            if (wm != null)
                            {
                                wm.SetRegion(regionName);
                                wm.RefreshWeather();
                            }
                            regionPanel.gameObject.SetActive(false);
                            Debug.Log($"[UIEventInitializer] Region set to: {regionName}");
                        });
                    }
                }
                Debug.Log("[UIEventInitializer] Region buttons events set!");
            }
        }

        void SetupFunctionsMenuEvents(Transform searchPanel)
        {
            Transform FindChildRecursive(Transform parent, params string[] names)
            {
                if (parent == null || names == null || names.Length == 0) return null;
                foreach (string name in names)
                {
                    if (parent.name == name) return parent;
                }
                foreach (Transform child in parent)
                {
                    var hit = FindChildRecursive(child, names);
                    if (hit != null) return hit;
                }
                return null;
            }

            Transform functionsMenu = FindChildRecursive(searchPanel, "FunctionsHoverMenu");
            if (functionsMenu == null)
            {
                Debug.LogWarning("[UIEventInitializer] FunctionsHoverMenu not found");
                return;
            }
            
            // メニューが非アクティブの場合、一時的にアクティブにして子要素を取得
            bool wasActive = functionsMenu.gameObject.activeSelf;
            functionsMenu.gameObject.SetActive(true);

            Transform searchBtn = functionsMenu.Find("SEARCHButton");
            if (searchBtn != null)
            {
                Button btn = searchBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        ClearLog();
                        LogToScreen("SEARCH BUTTON CLICKED!");
                        Debug.Log("[UIEventInitializer] SEARCH BUTTON CLICKED!");
                        try {
                            SearchUIManager searchMgr = FindObjectOfType<SearchUIManager>();
                            if (searchMgr != null) 
                            {
                                LogToScreen($"Mgr Found: {searchMgr.name}");
                                
                                // Force Open Logic
                                if (searchMgr.categoryPanel != null)
                                {
                                    searchMgr.categoryPanel.SetActive(true);
                                    LogToScreen("Category Panel SetActive(true)");
                                }
                                else
                                {
                                    // If null, ToggleCategoryPanel handles creation
                                    searchMgr.ToggleCategoryPanel(); // Create & Open
                                    // Ensure it stays open if toggle logic closed it (though unlikely on first creation)
                                    if (searchMgr.categoryPanel != null) searchMgr.categoryPanel.SetActive(true);
                                    LogToScreen("Category Panel Created & Opened");
                                }
                            }
                            else 
                            {
                                LogToScreen("SearchUIManager NOT FOUND!");
                            }
                        } catch (System.Exception e) {
                            LogToScreen($"ERR: {e.Message}");
                        }
                    });
                    Debug.Log("[UIEventInitializer] SEARCH button event set!");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] SEARCHButton not found!");
            
            // 1: AR Mode
            Transform arModeBtn = functionsMenu.Find("AR ModeButton");
            if (arModeBtn != null)
            {
                Button btn = arModeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        LogToScreen("AR MODE BUTTON CLICKED!");
                        functionsMenu.gameObject.SetActive(false);
                        // ARSearchResultDisplayのモード選択パネルを直接呼び出す
                        ARSearchResultDisplay arDisplay = FindObjectOfType<ARSearchResultDisplay>();
                        if (arDisplay != null) 
                        {
                            arDisplay.ToggleModeSelectionPanel();
                            Debug.Log("[UIEventInitializer] AR Mode panel toggled!");
                        }
                        else
                        {
                            Debug.LogWarning("[UIEventInitializer] ARSearchResultDisplay not found!");
                        }
                    });
                    Debug.Log("[UIEventInitializer] AR Mode button event set!");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] AR ModeButton not found!");
            
            // 2: Input Mode
            Transform inputModeBtn = FindChildRecursive(
                functionsMenu,
                "入力モードButton",
                "InputModeButton",
                "InputMode",
                "入力モード"
            );
            if (inputModeBtn != null)
            {
                Button btn = inputModeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        functionsMenu.gameObject.SetActive(false);
                        NRInput.SetInputSource(NRInput.CurrentInputSourceType == InputSourceEnum.Hands 
                            ? InputSourceEnum.Controller 
                            : InputSourceEnum.Hands);
                        Debug.Log($"[UIEventInitializer] Input Mode switched to: {NRInput.CurrentInputSourceType}");
                    });
                    Debug.Log("[UIEventInitializer] Input Mode button event set!");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] Input ModeButton not found!");
            
            // 3: + 登録
            Transform registerBtn = FindChildRecursive(
                functionsMenu,
                "+ 登録Button",
                "RegistrationButton",
                "+ 登録",
                "Registration"
            );
            Transform regSelectPanel = searchPanel.Find("RegistrationSelectPanel");
            if (registerBtn != null)
            {
                Button btn = registerBtn.GetComponent<Button>();
                if (btn != null)
                {
                    AddClickListener(btn, () => {
                        functionsMenu.gameObject.SetActive(false);
                        if (regSelectPanel != null) regSelectPanel.gameObject.SetActive(true);
                        Debug.Log("[UIEventInitializer] + 登録 clicked!");
                    });
                    Debug.Log("[UIEventInitializer] + 登録 button event set!");
                }
            }
            else Debug.LogWarning("[UIEventInitializer] + 登録Button not found!");
            
            // メニューを元の状態に戻す
            functionsMenu.gameObject.SetActive(wasActive);
            
            // 登録関連パネルのイベント設定
            SetupRegistrationPanelEvents(searchPanel);
        }
        
        void SetupRegistrationPanelEvents(Transform searchPanel)
        {
            Transform regSelectPanel = searchPanel.Find("RegistrationSelectPanel");
            Transform faceIdPanel = searchPanel.Find("FaceRegistrationPanel"); // 正しい名前
            Transform objectIdPanel = searchPanel.Find("ObjectRegPanel"); // 正しい名前
            
            // ===== RegistrationSelectPanel =====
            if (regSelectPanel != null)
            {
                // 閉じるボタン
                Transform closeBtn = regSelectPanel.Find("CloseRegSelectButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            regSelectPanel.gameObject.SetActive(false);
                        });
                    }
                }
                
                // 顔を選択ボタン
                Transform selectFaceBtn = regSelectPanel.Find("SelectFaceButton");
                if (selectFaceBtn != null)
                {
                    Button btn = selectFaceBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            regSelectPanel.gameObject.SetActive(false);
                            if (faceIdPanel != null) faceIdPanel.gameObject.SetActive(true);
                        });
                    }
                }
                
                // オブジェクトを選択ボタン
                Transform selectObjectBtn = regSelectPanel.Find("SelectObjectButton");
                if (selectObjectBtn != null)
                {
                    Button btn = selectObjectBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            regSelectPanel.gameObject.SetActive(false);
                            if (objectIdPanel != null) objectIdPanel.gameObject.SetActive(true);
                        });
                    }
                }
                Debug.Log("[UIEventInitializer] RegistrationSelectPanel events set!");
            }
            
            // ===== FaceIdPanel (Removed) =====
            // Face recognition feature has been removed from the application
            
            // ===== ObjectIdPanel (ObjectRegPanel) =====
            if (objectIdPanel != null)
            {
                Debug.Log($"[UIEventInitializer] ObjectIdPanel found: {objectIdPanel.name}");
                
                // 閉じるボタン - 正しい名前: CancelObjectRegButton
                Transform closeBtn = objectIdPanel.Find("CancelObjectRegButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            objectIdPanel.gameObject.SetActive(false);
                            Debug.Log("[UIEventInitializer] ObjectRegPanel closed!");
                        });
                        Debug.Log("[UIEventInitializer] CancelObjectRegButton event set!");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIEventInitializer] CancelObjectRegButton not found!");
                }
                
                // 登録ボタン（ImageUploaderのRegisterCurrentObjectを呼び出す）
                Transform regBtn = objectIdPanel.Find("RegisterObjectButton");
                if (regBtn != null)
                {
                    Button btn = regBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] RegisterObjectButton clicked!");
                            ImageUploader uploader = FindObjectOfType<ImageUploader>();
                            if (uploader != null) 
                            {
                                uploader.RegisterCurrentObject();
                                objectIdPanel.gameObject.SetActive(false);
                            }
                            else
                            {
                                Debug.LogError("[UIEventInitializer] ImageUploader not found!");
                            }
                        });
                        Debug.Log("[UIEventInitializer] RegisterObjectButton event set!");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIEventInitializer] RegisterObjectButton not found!");
                }
                Debug.Log("[UIEventInitializer] ObjectIdPanel events set!");
            }
            else
            {
                Debug.LogWarning("[UIEventInitializer] ObjectIdPanel (ObjectRegPanel) not found!");
            }
        }
        
        /// <summary>
        /// Close buttons - Register listeners at RUNTIME (not Editor time)
        /// This ensures lambdas are functional since Unity doesn't serialize them.
        /// </summary>
        void SetupCloseButtonEvents(Transform searchPanel)
        {
            Debug.Log("[UIEventInitializer] SetupCloseButtonEvents started...");
            
            // 1. IP Settings Panel (CommonCloseButton)
            Transform ipSettingsPanel = searchPanel.Find("IPSettingsPanel");
            if (ipSettingsPanel != null)
            {
                Transform closeBtn = ipSettingsPanel.Find("CommonCloseButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] IPSettings CommonCloseButton clicked!");
                            ipSettingsPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] IPSettings CommonCloseButton event set!");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIEventInitializer] CommonCloseButton not found in IPSettingsPanel!");
                }
            }
            else
            {
                Debug.LogWarning("[UIEventInitializer] IPSettingsPanel not found!");
            }
            
            // 2. Category Panel (CloseSearchButton - actual name in SceneSetupTool)
            Transform categoryPanel = searchPanel.Find("CategoryPanel");
            if (categoryPanel != null)
            {
                Transform closeBtn = categoryPanel.Find("CancelButton"); // Fixed: was CloseSearchButton
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] CategoryPanel CloseButton clicked!");
                            categoryPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] CategoryPanel CloseButton event set!");
                    }
                }
            }
            
            // 3. Result Scroll View (CloseResultsButton)
            Transform resultScrollView = searchPanel.Find("ResultScrollView");
            if (resultScrollView != null)
            {
                Transform closeBtn = resultScrollView.Find("CloseResultsButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] ResultScrollView CloseButton clicked!");
                            resultScrollView.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] ResultScrollView CloseButton event set!");
                    }
                }
            }
            
            // 4. Weather Panel (CloseWeatherButton)
            Transform weatherPanel = searchPanel.Find("WeatherPanel");
            if (weatherPanel == null) weatherPanel = transform.Find("SearchPanel/WeatherPanel");
            if (weatherPanel == null) weatherPanel = FindDeepChild(transform, "WeatherPanel");
            
            if (weatherPanel != null)
            {
                Transform closeBtn = weatherPanel.Find("CloseWeatherButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] WeatherPanel CloseButton clicked!");
                            weatherPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] WeatherPanel CloseButton event set!");
                    }
                }
            }
            
            // 5. Region Settings Panel (CloseRegionButton)
            Transform regionPanel = FindDeepChild(transform, "RegionSettingsPanel");
            if (regionPanel != null)
            {
                Transform closeBtn = regionPanel.Find("CloseRegionButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] RegionPanel CloseButton clicked!");
                            regionPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] RegionPanel CloseButton event set!");
                    }
                }
            }
            
            Debug.Log("[UIEventInitializer] SetupCloseButtonEvents completed!");
        }
        
        /// <summary>
        /// Registration List Panel close button (separate because different parent)
        /// </summary>
        void SetupRegistrationListCloseButton()
        {
            Transform regListPanel = FindDeepChild(transform, "RegistrationListPanel");
            if (regListPanel != null)
            {
                Transform closeBtn = regListPanel.Find("CloseButton");
                if (closeBtn != null)
                {
                    Button btn = closeBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] RegistrationListPanel CloseButton clicked!");
                            regListPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] RegistrationListPanel CloseButton event set!");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIEventInitializer] CloseButton not found in RegistrationListPanel!");
                }
            }
            else
            {
                Debug.LogWarning("[UIEventInitializer] RegistrationListPanel not found!");
            }
        }
        
        /// <summary>
        /// Registration Select Panel cancel button (SelectCancelButton)
        /// </summary>
        void SetupRegistrationSelectCancelButton()
        {
            Transform regSelectPanel = FindDeepChild(transform, "RegistrationSelectPanel");
            if (regSelectPanel != null)
            {
                Transform cancelBtn = regSelectPanel.Find("SelectCancelButton");
                if (cancelBtn != null)
                {
                    Button btn = cancelBtn.GetComponent<Button>();
                    if (btn != null)
                    {
                        AddClickListener(btn, () => {
                            Debug.Log("[UIEventInitializer] RegistrationSelectPanel CancelButton clicked!");
                            regSelectPanel.gameObject.SetActive(false);
                        });
                        Debug.Log("[UIEventInitializer] RegistrationSelectPanel CancelButton event set!");
                    }
                }
                else
                {
                    Debug.LogWarning("[UIEventInitializer] SelectCancelButton not found in RegistrationSelectPanel!");
                }
            }
            else
            {
                Debug.LogWarning("[UIEventInitializer] RegistrationSelectPanel not found!");
            }
        }
        
        /// <summary>
        /// Deep find child by name (recursive)
        /// </summary>
        Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                Transform found = FindDeepChild(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        System.Collections.IEnumerator DelayedDump()
        {
            yield return new WaitForSeconds(1.0f);
            DumpHierarchy();
        }

        void DumpHierarchy()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== HIERARCHY DUMP ===");
            
            // SearchPanel (Root of UI usually)
            Transform searchPanel = transform.Find("SearchPanel");
            if (searchPanel == null) sb.AppendLine("CRITICAL: SearchPanel NOT FOUND via transform.Find!");
            else sb.AppendLine($"SearchPanel: Act={searchPanel.gameObject.activeInHierarchy} Pos={searchPanel.position} Loc={searchPanel.localPosition} Scale={searchPanel.localScale}");

            // FunctionsMenu
            if (searchPanel != null)
            {
                var fMenu = searchPanel.Find("FunctionsHoverMenu");
                if (fMenu != null) sb.AppendLine($"FunctionsHoverMenu: Act={fMenu.gameObject.activeInHierarchy}");
                else sb.AppendLine("FunctionsHoverMenu: NOT FOUND");
                
                var diag = transform.Find("DiagnosticOverlay"); // DiagnosticOverlay is usually child of UIEventInitializer.transform (Canvas)
                if (diag != null) sb.AppendLine($"DiagnosticOverlay: Act={diag.gameObject.activeInHierarchy} Pos={diag.localPosition}");
            }
            
            // SearchUIManager Check
            var mgr = FindObjectOfType<SearchUIManager>();
            if (mgr != null) 
            {
                sb.AppendLine($"SearchUIManager Found on: {mgr.gameObject.name}");
            }
            else sb.AppendLine("SearchUIManager: NOT FOUND in Scene");

            // Recursive Dump
            DumpRecursive(transform, "", sb);
            
            Debug.Log(sb.ToString());
            LogToScreen("Hierarchy Dumped to Console.");
            if (searchPanel != null) LogToScreen($"SP Active: {searchPanel.gameObject.activeInHierarchy}");
            else LogToScreen("SP MISSING");
        }

        void DumpRecursive(Transform t, string indent, StringBuilder sb)
        {
            sb.AppendLine($"{indent}{t.name} [Act:{t.gameObject.activeSelf}] Pos:{t.localPosition}");
            foreach (Transform child in t)
            {
                DumpRecursive(child, indent + "  ", sb);
            }
        }
    }
}
