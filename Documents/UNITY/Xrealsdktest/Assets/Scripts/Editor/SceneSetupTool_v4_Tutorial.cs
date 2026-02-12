using UnityEngine;
using UnityEngine.UI;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4
{
    private static void SetupTutorialSystem(GameObject panelObj, BottomMenuController bottomMenuCtrl, ImageUploader uploader)
    {
        // ============ TutorialManager Setup ============
        TutorialManager tutorialMgr = Object.FindObjectOfType<TutorialManager>();
        if (tutorialMgr == null)
        {
            // Create TutorialManager on the same panel
            tutorialMgr = panelObj.AddComponent<TutorialManager>();
            Debug.Log("[SceneSetupTool] TutorialManager created.");
        }
        
        if (bottomMenuCtrl != null)
        {
            bottomMenuCtrl.tutorialManager = tutorialMgr;
            bottomMenuCtrl.imageUploader = uploader;
        }
        
        // ============ TutorialPanel - CREATE NEW (delete existing first) ============
        {
            // Cleanup: Remove all existing TutorialPanels
            GameObject[] allGOs = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allGOs)
            {
                if (go.name == "TutorialPanel")
                {
                    DestroyImmediate(go);
                    Debug.Log("[SceneSetupTool] Removed existing TutorialPanel");
                }
            }
            
            // Create new TutorialPanel
            GameObject tutorialPanel = new GameObject("TutorialPanel");
            tutorialPanel.transform.SetParent(panelObj.transform, false);
            
            Image tutorialBg2 = tutorialPanel.AddComponent<Image>();
            tutorialBg2.color = new Color(0.05f, 0.1f, 0.2f, 0.95f); // Dark blue
            
            RectTransform tutorialRect2 = tutorialPanel.GetComponent<RectTransform>();
            tutorialRect2.anchorMin = new Vector2(0.1f, 0.1f);
            tutorialRect2.anchorMax = new Vector2(0.9f, 0.9f);
            tutorialRect2.offsetMin = Vector2.zero;
            tutorialRect2.offsetMax = Vector2.zero;
            
            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(tutorialPanel.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "Tutorial";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // Image for Logo
            GameObject imageObj = new GameObject("TutorialImage");
            imageObj.transform.SetParent(tutorialPanel.transform, false);
            Image tutorialImage = imageObj.AddComponent<Image>();
            tutorialImage.color = Color.white;
            tutorialImage.preserveAspect = true;
            RectTransform imageRect = imageObj.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.3f, 0.45f);
            imageRect.anchorMax = new Vector2(0.7f, 0.80f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            
            // Content Text
            GameObject contentObj2 = new GameObject("ContentText");
            contentObj2.transform.SetParent(tutorialPanel.transform, false);
            Text contentText = contentObj2.AddComponent<Text>();
            contentText.text = "Content here";
            contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contentText.fontSize = 18;
            contentText.color = Color.white;
            contentText.alignment = TextAnchor.MiddleCenter;
            RectTransform contentRect = contentObj2.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.15f);
            contentRect.anchorMax = new Vector2(0.95f, 0.45f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            // Page Indicator
            GameObject pageObj = new GameObject("PageIndicator");
            pageObj.transform.SetParent(tutorialPanel.transform, false);
            Text pageIndicator2 = pageObj.AddComponent<Text>();
            pageIndicator2.text = "1 / 5";
            pageIndicator2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pageIndicator2.fontSize = 14;
            pageIndicator2.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            pageIndicator2.alignment = TextAnchor.MiddleCenter;
            RectTransform pageRect2 = pageObj.GetComponent<RectTransform>();
            pageRect2.anchorMin = new Vector2(0.4f, 0.02f);
            pageRect2.anchorMax = new Vector2(0.6f, 0.08f);
            pageRect2.offsetMin = Vector2.zero;
            pageRect2.offsetMax = Vector2.zero;
            
            // Next Button
            GameObject nextBtnObj2 = DefaultControls.CreateButton(new DefaultControls.Resources());
            nextBtnObj2.name = "NextButton";
            nextBtnObj2.transform.SetParent(tutorialPanel.transform, false);
            RectTransform nextRect2 = nextBtnObj2.GetComponent<RectTransform>();
            nextRect2.anchorMin = new Vector2(0.6f, 0.02f);
            nextRect2.anchorMax = new Vector2(0.85f, 0.10f);
            nextRect2.offsetMin = Vector2.zero;
            nextRect2.offsetMax = Vector2.zero;
            Text nextText = nextBtnObj2.GetComponentInChildren<Text>();
            if (nextText != null) nextText.text = "次へ";
            Image nextBg = nextBtnObj2.GetComponent<Image>();
            if (nextBg != null) nextBg.color = new Color(0.2f, 0.5f, 0.8f, 0.95f);
            Button nextButton = nextBtnObj2.GetComponent<Button>();
            BoxCollider nextCol2 = nextBtnObj2.AddComponent<BoxCollider>();
            nextCol2.size = new Vector3(150, 50, 10);
            // Z Offset for touch detection
            RectTransform nextRt2 = nextBtnObj2.GetComponent<RectTransform>();
            Vector3 nextP2 = nextRt2.anchoredPosition3D;
            nextRt2.anchoredPosition3D = new Vector3(nextP2.x, nextP2.y, -10f);
            
            // Skip Button
            GameObject skipBtnObj2 = DefaultControls.CreateButton(new DefaultControls.Resources());
            skipBtnObj2.name = "SkipButton";
            skipBtnObj2.transform.SetParent(tutorialPanel.transform, false);
            RectTransform skipRect2 = skipBtnObj2.GetComponent<RectTransform>();
            skipRect2.anchorMin = new Vector2(0.15f, 0.02f);
            skipRect2.anchorMax = new Vector2(0.4f, 0.10f);
            skipRect2.offsetMin = Vector2.zero;
            skipRect2.offsetMax = Vector2.zero;
            Text skipText = skipBtnObj2.GetComponentInChildren<Text>();
            if (skipText != null) skipText.text = "スキップ";
            Image skipBg = skipBtnObj2.GetComponent<Image>();
            if (skipBg != null) skipBg.color = new Color(0.4f, 0.4f, 0.4f, 0.95f);
            Button skipButton = skipBtnObj2.GetComponent<Button>();
            BoxCollider skipCol2 = skipBtnObj2.AddComponent<BoxCollider>();
            skipCol2.size = new Vector3(150, 50, 10);
            // Z Offset for touch detection
            RectTransform skipRt2 = skipBtnObj2.GetComponent<RectTransform>();
            Vector3 skipP2 = skipRt2.anchoredPosition3D;
            skipRt2.anchoredPosition3D = new Vector3(skipP2.x, skipP2.y, -10f);
            
            // Assign references to TutorialManager
            if (tutorialMgr != null)
            {
                tutorialMgr.tutorialPanel = tutorialPanel;
                tutorialMgr.titleText = titleText;
                tutorialMgr.contentText = contentText;
                tutorialMgr.tutorialImage = tutorialImage;
                tutorialMgr.pageIndicator = pageIndicator2;
                tutorialMgr.nextButton = nextButton;
                tutorialMgr.skipButton = skipButton;
                Debug.Log("[SceneSetupTool] TutorialPanel created and references assigned!");
            }
            
            tutorialPanel.SetActive(false); // Hidden by default
        }
    }
}
