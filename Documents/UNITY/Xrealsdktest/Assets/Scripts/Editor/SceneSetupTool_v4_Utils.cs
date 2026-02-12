using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4 : MonoBehaviour
{
    /// <summary>
    /// 標準ボタン作成メソッド - RegistrationListPanel CloseButtonパターンに基づく
    /// 全ボタンはこのメソッドを使用して統一された動作を保証
    /// </summary>
    /// <param name="name">ボタン名</param>
    /// <param name="parent">親Transform</param>
    /// <param name="labelText">ボタンテキスト</param>
    /// <param name="bgColor">背景色</param>
    /// <param name="anchorMin">RectTransform anchorMin</param>
    /// <param name="anchorMax">RectTransform anchorMax</param>
    /// <returns>作成されたボタンGameObject</returns>
    private static GameObject CreateStandardButton(
        string name, 
        Transform parent, 
        string labelText, 
        Color bgColor,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        // 1. Create base GameObject
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        // 2. Add Image component (background)
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;
        btnBg.raycastTarget = true; // Essential for NRSDK CanvasRaycastTarget
        
        // 3. Setup RectTransform with anchors
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        // 4. Z Offset (standard: -10)
        Vector3 pos = btnRect.anchoredPosition3D;
        btnRect.anchoredPosition3D = new Vector3(pos.x, pos.y, -10f);
        
        // 5. Add Button component
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        
        // 6. Add BoxCollider for NRSDK Physics Raycast (standard: size z=50, center z=0)
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        // Size will be set based on RectTransform after layout
        col.size = new Vector3(100, 40, 50f);
        col.center = new Vector3(0, 0, 0);
        
        // 7. Add Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text labelText_ = labelObj.AddComponent<Text>();
        labelText_.text = labelText;
        labelText_.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText_.fontSize = 16;
        labelText_.fontStyle = FontStyle.Bold;
        labelText_.color = Color.white;
        labelText_.alignment = TextAnchor.MiddleCenter;
        labelText_.raycastTarget = false; // Don't block parent raycast
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        return btnObj;
    }
    
    /// <summary>
    /// 標準ボタン作成メソッド（固定サイズ版）
    /// GridLayoutGroup等で使用する場合
    /// </summary>
    private static GameObject CreateStandardButtonFixedSize(
        string name, 
        Transform parent, 
        string labelText, 
        Color bgColor,
        Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;
        btnBg.raycastTarget = true;
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = size;
        
        // Z Offset
        Vector3 pos = btnRect.anchoredPosition3D;
        btnRect.anchoredPosition3D = new Vector3(pos.x, pos.y, -10f);
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 50f);
        col.center = new Vector3(0, 0, 0);
        
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text labelText_ = labelObj.AddComponent<Text>();
        labelText_.text = labelText;
        labelText_.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText_.fontSize = 14;
        labelText_.fontStyle = FontStyle.Bold;
        labelText_.color = Color.white;
        labelText_.alignment = TextAnchor.MiddleCenter;
        labelText_.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        return btnObj;
    }
    /// <summary>
    /// Creates a button with icon + text label
    /// </summary>
    private static GameObject CreateIconButton(string name, string iconName, string labelText, Transform parent)
    {
        GameObject btnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        btnObj.name = name;
        btnObj.transform.SetParent(parent, false);

        // Set dark background for white icons
        Image btnImage = btnObj.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = new Color(0.18f, 0.18f, 0.18f, 0.95f); // Dark gray background
        }

        // Update button colors
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.pressedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            btn.colors = colors;
        }

        // Remove default text
        Text defaultText = btnObj.GetComponentInChildren<Text>();
        if (defaultText != null)
        {
            DestroyImmediate(defaultText.gameObject);
        }

        // Create horizontal layout
        HorizontalLayoutGroup layout = btnObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 5;
        layout.padding = new RectOffset(8, 8, 5, 5);

        // Icon (小さめに設定)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = LoadIcon(iconName);
        iconImage.preserveAspect = true;
        LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 20;  // 小さく調整
        iconLayout.preferredHeight = 20; // 小さく調整

        // Label (white text for dark background)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.text = labelText;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.color = Color.white; // White text for dark background
        label.alignment = TextAnchor.MiddleCenter;
        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 60;
        labelLayout.preferredHeight = 30;

        // Z Offset
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        Vector3 p = rt.anchoredPosition3D;
        rt.anchoredPosition3D = new Vector3(p.x, p.y, -10f);
        
        // Add BoxCollider for NRSDK Raycast
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(100, 40, 1);
        col.center = Vector3.zero;

        return btnObj;
    }

    /// <summary>
    /// Creates a button with only an icon (no text)
    /// </summary>
    private static GameObject CreateIconOnlyButton(string name, string iconName, Transform parent)
    {
        GameObject btnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
        btnObj.name = name;
        btnObj.transform.SetParent(parent, false);

        // Set dark background for white icons
        Image btnImage = btnObj.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = new Color(0.18f, 0.18f, 0.18f, 0.95f); // Dark gray background
        }

        // Update button colors
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            colors.pressedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            btn.colors = colors;
        }

        // Remove default text
        Text defaultText = btnObj.GetComponentInChildren<Text>();
        if (defaultText != null)
        {
            DestroyImmediate(defaultText.gameObject);
        }

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = LoadIcon(iconName);
        iconImage.preserveAspect = true;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.15f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Add collider
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(60, 60, 1);

        // Z Offset
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        Vector3 p = rt.anchoredPosition3D;
        rt.anchoredPosition3D = new Vector3(p.x, p.y, -10f);

        return btnObj;
    }

    /// <summary>
    /// Loads an icon sprite from the Icons folder (PNG only)
    /// Supports both new Figma naming (xxx-icon-2x.png) and old naming (icon_xxx.png)
    /// </summary>
    private static Sprite LoadIcon(string iconName)
    {
        if (_iconCache.ContainsKey(iconName) && _iconCache[iconName] != null)
        {
            return _iconCache[iconName];
        }

        Sprite sprite = null;
        string path = null;
        
        // Try new Figma naming convention first (xxx-icon-2x.png)
        string figmaName = iconName.Replace("icon_", "").Replace("_", "-") + "-icon-2x";
        string figmaPath = ICON_PATH + figmaName + ".png";
        
        if (System.IO.File.Exists(figmaPath.Replace("Assets/", Application.dataPath + "/")))
        {
            path = figmaPath;
        }
        else
        {
            // Fallback to old naming (icon_xxx.png)
            path = ICON_PATH + iconName + ".png";
        }
        
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool needsReimport = false;
            
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                needsReimport = true;
            }
            
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                needsReimport = true;
            }
            
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }
            
            if (importer.spritePixelsPerUnit != 100)
            {
                importer.spritePixelsPerUnit = 100;
                needsReimport = true;
            }
            
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }
            
            if (needsReimport)
            {
                importer.SaveAndReimport();
            }
        }
        
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite != null)
        {
            _iconCache[iconName] = sprite;
        }
        else
        {
            Debug.LogWarning($"Could not load icon: {path}");
        }

        return sprite;
    }

    /// <summary>
    /// Loads animation frames from the Icons folder
    /// </summary>
    private static Sprite[] LoadAnimationFrames(string baseName, int frameCount)
    {
        Sprite[] frames = new Sprite[frameCount];
        bool allLoaded = true;
        
        for (int i = 0; i < frameCount; i++)
        {
            int frameNum = i + 1;
            string framePath = ICON_PATH + baseName + "-" + frameNum + "-2x.png";
            
            // スプライト設定を適用
            TextureImporter importer = AssetImporter.GetAtPath(framePath) as TextureImporter;
            if (importer != null)
            {
                bool needsReimport = false;
                
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    needsReimport = true;
                }
                
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    needsReimport = true;
                }
                
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    needsReimport = true;
                }
                
                if (needsReimport)
                {
                    importer.SaveAndReimport();
                }
            }
            
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
            if (sprite != null)
            {
                frames[i] = sprite;
            }
            else
            {
                Debug.LogWarning($"Could not load animation frame: {framePath}");
                allLoaded = false;
            }
        }
        
        if (!allLoaded)
        {
            Debug.LogWarning($"Some animation frames could not be loaded for: {baseName}");
        }
        else
        {
            Debug.Log($"[SceneSetupTool] Loaded {frameCount} animation frames for: {baseName}");
        }
        
        return allLoaded ? frames : null;
    }

    /// <summary>
    /// Creates the Startup Video Controller
    /// </summary>
    private static void CreateStartupVideo(Canvas canvas, GameObject mainUiRoot)
    {
        string videoObjName = "StartupVideoPlayer";
        GameObject videoObj = GameObject.Find(videoObjName);
        if (videoObj != null) DestroyImmediate(videoObj);

        videoObj = new GameObject(videoObjName);
        
        // Attach to main canvas for simplicity (WorldSpace)
        videoObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = videoObj.AddComponent<RectTransform>();
        // Fill parent
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Ensure it is last (on top)
        rt.SetAsLastSibling(); 

        // RawImage for video display
        RawImage img = videoObj.AddComponent<RawImage>();
        img.color = Color.black; // Black background initially

        // Controller logic
        StartupVideoController controller = videoObj.AddComponent<StartupVideoController>();
        controller.displayImage = img;
        controller.mainUiRoot = mainUiRoot;
        // VideoPlayer, CanvasGroup, AudioSource are added automatically by the script logic

        Debug.Log("[SceneSetupTool] Startup Video Player created.");
    }
    
    /// <summary>
    /// Apply rounded corners to all panels and buttons in the UI
    /// </summary>
    private static void ApplyRoundedCornersToUI(GameObject rootPanel)
    {
        if (rootPanel == null) return;
        
        // パネル名のリスト（角丸を適用する対象）
        string[] panelNames = {
            "SettingsHoverMenu",
            "FunctionsHoverMenu", 
            "CategoryPanel",
            "IPSettingsPanel",
            "RegistrationSelectPanel",
            "ObjectRegPanel",
            "RegisteredListPanel",
            "WeatherPanel",
            "RegionPanel",
            "TutorialPanel",
            "DebugPanel",
            "LoadingPanel",
            "WeatherToast",
            "ClockPanel"
        };
        
        foreach (string panelName in panelNames)
        {
            Transform panelTransform = rootPanel.transform.Find(panelName);
            if (panelTransform != null)
            {
                ApplyRoundedCorners(panelTransform.gameObject, 12f);
            }
        }
        
        // ボタンにも角丸を適用
        ApplyRoundedCornersToButtons(rootPanel.transform);
        
        Debug.Log("[SceneSetupTool] Rounded corners applied to UI elements.");
    }
    
    /// <summary>
    /// Apply rounded corners to a specific GameObject
    /// </summary>
    private static void ApplyRoundedCorners(GameObject obj, float radius)
    {
        if (obj == null) return;
        
        // RoundedCornersコンポーネントを追加
        RoundedCorners rc = obj.GetComponent<RoundedCorners>();
        if (rc == null)
        {
            rc = obj.AddComponent<RoundedCorners>();
        }
        rc.radius = radius;
        rc.ApplyRoundedCorners();
    }
    
    /// <summary>
    /// Recursively apply rounded corners to all buttons
    /// </summary>
    private static void ApplyRoundedCornersToButtons(Transform parent)
    {
        if (parent == null) return;
        
        foreach (Transform child in parent)
        {
            // ボタンの検出
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                ApplyRoundedCorners(child.gameObject, 8f);
            }
            
            // 再帰的に子要素も処理
            ApplyRoundedCornersToButtons(child);
        }
    }

    /// <summary>
    /// Recursively set the layer of an object and all its children.
    /// </summary>
    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
