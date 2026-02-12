using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using NRKernal;

public partial class SceneSetupTool_v4
{
    private static void SetupClockSystem(GameObject panelObj)
    {
        // ============ Clock Display (画面左上) ============
        GameObject clockPanelObj = new GameObject("ClockPanel");
        clockPanelObj.transform.SetParent(panelObj.transform, false);
        Image clockPanelBg = clockPanelObj.AddComponent<Image>();
        clockPanelBg.color = Color.clear; // 透明
        RectTransform clockPanelRect = clockPanelObj.GetComponent<RectTransform>();
        clockPanelRect.anchorMin = new Vector2(0.02f, 0.88f); // 左上（小さめ）
        clockPanelRect.anchorMax = new Vector2(0.16f, 0.97f);
        clockPanelRect.offsetMin = Vector2.zero;
        clockPanelRect.offsetMax = Vector2.zero;
        
        // 時間表示コンテナ
        GameObject clockContainer = new GameObject("ClockContainer");
        clockContainer.transform.SetParent(clockPanelObj.transform, false);
        RectTransform clockContainerRect = clockContainer.AddComponent<RectTransform>();
        clockContainerRect.anchorMin = Vector2.zero;
        clockContainerRect.anchorMax = Vector2.one;
        clockContainerRect.offsetMin = new Vector2(10, 5);
        clockContainerRect.offsetMax = new Vector2(-10, -5);
        
        HorizontalLayoutGroup clockLayout = clockContainer.AddComponent<HorizontalLayoutGroup>();
        clockLayout.spacing = 3;
        clockLayout.childAlignment = TextAnchor.MiddleCenter;
        clockLayout.childControlWidth = false;
        clockLayout.childControlHeight = true;
        clockLayout.childForceExpandWidth = false;
        clockLayout.childForceExpandHeight = true;
        
        // 数字スプライトをロード（共通）
        Sprite[] clockDigits = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            string digitPath = $"Assets/UI/Number/white/digit-{i}.png";
            clockDigits[i] = AssetDatabase.LoadAssetAtPath<Sprite>(digitPath);
            
            TextureImporter digitImporter = AssetImporter.GetAtPath(digitPath) as TextureImporter;
            if (digitImporter != null && digitImporter.textureType != TextureImporterType.Sprite)
            {
                digitImporter.textureType = TextureImporterType.Sprite;
                digitImporter.SaveAndReimport();
                clockDigits[i] = AssetDatabase.LoadAssetAtPath<Sprite>(digitPath);
            }
        }
        
        // 時間表示（H）
        GameObject hourDisplayObj = new GameObject("HourDisplay");
        hourDisplayObj.transform.SetParent(clockContainer.transform, false);
        RectTransform hourRect = hourDisplayObj.AddComponent<RectTransform>();
        hourRect.sizeDelta = new Vector2(36, 30);
        SpriteNumberDisplay hourDisplay = hourDisplayObj.AddComponent<SpriteNumberDisplay>();
        hourDisplay.digitWidth = 16f;
        hourDisplay.digitSpacing = 1f;
        hourDisplay.centerAlign = true;
        hourDisplay.digitSprites = clockDigits;
        
        // コロン (:) - スプライト画像を使用
        GameObject colonObj = new GameObject("Colon");
        colonObj.transform.SetParent(clockContainer.transform, false);
        Image colonImage = colonObj.AddComponent<Image>();
        string colonPath = "Assets/UI/Number/white/digit-_.png";
        Sprite colonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(colonPath);
        TextureImporter colonImporter = AssetImporter.GetAtPath(colonPath) as TextureImporter;
        if (colonImporter != null && colonImporter.textureType != TextureImporterType.Sprite)
        {
            colonImporter.textureType = TextureImporterType.Sprite;
            colonImporter.SaveAndReimport();
            colonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(colonPath);
        }
        colonImage.sprite = colonSprite;
        colonImage.preserveAspect = true;
        RectTransform colonRect = colonObj.GetComponent<RectTransform>();
        colonRect.sizeDelta = new Vector2(10, 26);
        
        // 分表示（M）
        GameObject minuteDisplayObj = new GameObject("MinuteDisplay");
        minuteDisplayObj.transform.SetParent(clockContainer.transform, false);
        RectTransform minuteRect = minuteDisplayObj.AddComponent<RectTransform>();
        minuteRect.sizeDelta = new Vector2(36, 30);
        SpriteNumberDisplay minuteDisplay = minuteDisplayObj.AddComponent<SpriteNumberDisplay>();
        minuteDisplay.digitWidth = 16f;
        minuteDisplay.digitSpacing = 1f;
        minuteDisplay.centerAlign = true;
        minuteDisplay.digitSprites = clockDigits;
        
        // SpriteClockDisplayを追加（点滅なし）
        SpriteClockDisplay clockDisplay = clockPanelObj.AddComponent<SpriteClockDisplay>();
        clockDisplay.hourDisplay = hourDisplay;
        clockDisplay.minuteDisplay = minuteDisplay;
        clockDisplay.colonText = null; // スプライト使用のためnull
        clockDisplay.colonObject = colonObj;
        clockDisplay.blinkColon = false; // 点滅無効
        
        Debug.Log("[SceneSetupTool] Clock Display created!");
    }
}
