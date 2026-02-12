using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace NRKernal
{
    /// <summary>
    /// ビーコン対象オブジェクトの選択管理
    /// 最大3つまで選択可能
    /// </summary>
    public class BeaconSelectionManager : MonoBehaviour
    {
        public static BeaconSelectionManager Instance { get; private set; }
        
        [Header("Settings")]
        public int maxSelections = 3;
        
        [Header("UI References")]
        public Transform listContainer; // 登録リスト表示用コンテナ
        public GameObject listItemPrefab; // リスト項目プレハブ
        public Text selectionCountText; // 選択数表示
        
        // 選択中のオブジェクト名
        private HashSet<string> _selectedObjects = new HashSet<string>();
        
        // リスト項目のUI参照
        private Dictionary<string, Toggle> _itemToggles = new Dictionary<string, Toggle>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Auto-find listContainer if missing
            if (listContainer == null)
            {
                var scrollRect = GetComponentInChildren<ScrollRect>();
                if (scrollRect != null) listContainer = scrollRect.content;
                
                if (listContainer == null)
                {
                    Transform t = transform.Find("Content");
                    if (t == null) t = transform.Find("Viewport/Content");
                    if (t == null) t = transform.Find("ListScrollView/Viewport/Content");
                    listContainer = t;
                }
            }
            
            if (listContainer == null) Debug.LogError("[BeaconMgr] ListContainer NOT FOUND!");
            else Debug.Log($"[BeaconMgr] ListContainer found: {listContainer.name}");
            
            // Debug: Add a test item
            // CreateListItem("TEST_ITEM", "object", "Debug Check");
            
            LoadSelections();
        }
        
        /// <summary>
        /// 選択されているオブジェクト名のリスト
        /// </summary>
        public List<string> SelectedObjects => _selectedObjects.ToList();
        
        /// <summary>
        /// 指定オブジェクトがビーコン対象かチェック
        /// </summary>
        public bool IsSelectedForBeacon(string objectName)
        {
            return _selectedObjects.Contains(objectName);
        }
        
        /// <summary>
        /// オブジェクトの選択状態をトグル
        /// </summary>
        public bool ToggleSelection(string objectName)
        {
            if (_selectedObjects.Contains(objectName))
            {
                // 選択解除
                _selectedObjects.Remove(objectName);
                UpdateToggleUI(objectName, false);
                SaveSelections();
                UpdateSelectionCountUI();
                Debug.Log($"[BeaconSelection] Deselected: {objectName}");
                return false;
            }
            else
            {
                // 選択追加（最大数チェック）
                if (_selectedObjects.Count >= maxSelections)
                {
                    Debug.LogWarning($"[BeaconSelection] Max {maxSelections} objects can be selected");
                    return false;
                }
                
                _selectedObjects.Add(objectName);
                UpdateToggleUI(objectName, true);
                SaveSelections();
                UpdateSelectionCountUI();
                Debug.Log($"[BeaconSelection] Selected: {objectName}");
                return true;
            }
        }
        
        /// <summary>
        /// 強制的に選択（最大数超えている場合は古いものを削除）
        /// </summary>
        public void ForceSelect(string objectName)
        {
            if (_selectedObjects.Contains(objectName)) return;
            
            // 最大数に達している場合、最初の要素を削除
            if (_selectedObjects.Count >= maxSelections)
            {
                string oldest = _selectedObjects.First();
                _selectedObjects.Remove(oldest);
                UpdateToggleUI(oldest, false);
            }
            
            _selectedObjects.Add(objectName);
            UpdateToggleUI(objectName, true);
            SaveSelections();
            UpdateSelectionCountUI();
        }
        
        /// <summary>
        /// 選択解除
        /// </summary>
        public void Deselect(string objectName)
        {
            if (_selectedObjects.Remove(objectName))
            {
                UpdateToggleUI(objectName, false);
                SaveSelections();
                UpdateSelectionCountUI();
            }
        }
        
        /// <summary>
        /// 全選択解除
        /// </summary>
        public void ClearAllSelections()
        {
            foreach (var name in _selectedObjects.ToList())
            {
                UpdateToggleUI(name, false);
            }
            _selectedObjects.Clear();
            SaveSelections();
            UpdateSelectionCountUI();
        }
        
        /// <summary>
        /// リスト項目UIを作成
        /// </summary>
        public void CreateListItem(string objectName, string objectType, string details)
        {
            // Auto-find listContainer if null (critical for panels that start disabled)
            if (listContainer == null)
            {
                var scrollRect = GetComponentInChildren<ScrollRect>();
                if (scrollRect != null) listContainer = scrollRect.content;
                
                if (listContainer == null)
                {
                    Transform t = transform.Find("ListScrollView/Viewport/Content");
                    if (t == null) t = transform.Find("Viewport/Content");
                    if (t == null) t = transform.Find("Content");
                    listContainer = t;
                }
                
                if (listContainer == null)
                {
                    Debug.LogError("[BeaconMgr] CRITICAL: listContainer not found! Cannot create items.");
                    return;
                }
                Debug.Log($"[BeaconMgr] Auto-found listContainer: {listContainer.name}");
            }
            
            GameObject itemObj = new GameObject(objectName + "_Item");
            itemObj.transform.SetParent(listContainer, false);
            
            // 背景
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            
            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);
            
            // Horizontal Layout
            HorizontalLayoutGroup hLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.spacing = 10;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            
            // Toggle (Checkbox)
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(itemObj.transform, false);
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            
            // Toggle Background
            GameObject toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            Image toggleBgImg = toggleBg.AddComponent<Image>();
            toggleBgImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            RectTransform toggleBgRect = toggleBg.GetComponent<RectTransform>();
            toggleBgRect.sizeDelta = new Vector2(30, 30);
            
            // Toggle Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            Image checkImg = checkmark.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.9f, 0.4f, 1f);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            
            RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(40, 40);
            
            toggle.targetGraphic = toggleBgImg;
            toggle.graphic = checkImg;
            toggle.isOn = _selectedObjects.Contains(objectName);
            
            // Toggle event
            string objName = objectName;
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if (_selectedObjects.Count >= maxSelections)
                    {
                        toggle.isOn = false;
                        Debug.LogWarning($"[BeaconSelection] Max {maxSelections} selections reached");
                        return;
                    }
                    _selectedObjects.Add(objName);
                }
                else
                {
                    _selectedObjects.Remove(objName);
                }
                SaveSelections();
                UpdateSelectionCountUI();
            });
            
            // コライダー追加
            BoxCollider col = toggleObj.AddComponent<BoxCollider>();
            col.size = new Vector3(40, 40, 1);
            
            _itemToggles[objectName] = toggle;
            
            // Type Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemObj.transform, false);
            Text iconText = iconObj.AddComponent<Text>();
            iconText.text = objectType == "face" ? "👤" : "📦";
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 24;
            iconText.color = objectType == "face" ? new Color(1f, 0.5f, 1f, 1f) : new Color(1f, 0.7f, 0.3f, 1f);
            iconText.alignment = TextAnchor.MiddleCenter;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(40, 40);
            
            // Name Text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(itemObj.transform, false);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = objectName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(200, 40);
            
            // Details Text
            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(itemObj.transform, false);
            Text detailsText = detailsObj.AddComponent<Text>();
            detailsText.text = details;
            detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailsText.fontSize = 14;
            detailsText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            detailsText.alignment = TextAnchor.MiddleLeft;
            RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
            detailsRect.sizeDelta = new Vector2(100, 40);
        }
        
        /// <summary>
        /// リストをクリア
        /// </summary>
        public void ClearList()
        {
            if (listContainer == null) return;
            
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }
            _itemToggles.Clear();
        }
        
        void UpdateToggleUI(string objectName, bool isSelected)
        {
            if (_itemToggles.TryGetValue(objectName, out Toggle toggle))
            {
                toggle.isOn = isSelected;
            }
        }
        
        void UpdateSelectionCountUI()
        {
            if (selectionCountText != null)
            {
                selectionCountText.text = $"Beacon Targets: {_selectedObjects.Count}/{maxSelections}";
            }
        }
        
        void SaveSelections()
        {
            string data = string.Join(",", _selectedObjects);
            PlayerPrefs.SetString("BeaconSelectedObjects", data);
            PlayerPrefs.Save();
        }
        
        void LoadSelections()
        {
            string data = PlayerPrefs.GetString("BeaconSelectedObjects", "");
            if (!string.IsNullOrEmpty(data))
            {
                string[] names = data.Split(',');
                _selectedObjects = new HashSet<string>(names.Where(n => !string.IsNullOrEmpty(n)));
            }
        }
    }
}
