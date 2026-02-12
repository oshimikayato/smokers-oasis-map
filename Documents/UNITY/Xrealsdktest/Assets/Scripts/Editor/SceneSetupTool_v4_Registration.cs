using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using NRKernal;
using System.Collections.Generic;

public partial class SceneSetupTool_v4
{
    private static void SetupRegistrationSystem(GameObject panelObj, ImageUploader uploader, BottomMenuController bottomMenuCtrl)
    {
        // ============ Registration Selection Panel ============
        GameObject regSelectPanelObj = new GameObject("RegistrationSelectPanel");
        regSelectPanelObj.transform.SetParent(panelObj.transform, false);
        Image regSelectBg = regSelectPanelObj.AddComponent<Image>();
        regSelectBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        RectTransform regSelectRect = regSelectPanelObj.GetComponent<RectTransform>();
        regSelectRect.anchorMin = new Vector2(0.2f, 0.35f);
        regSelectRect.anchorMax = new Vector2(0.8f, 0.65f);
        regSelectRect.offsetMin = Vector2.zero;
        regSelectRect.offsetMax = Vector2.zero;

        // Title
        GameObject regSelectTitleObj = new GameObject("Title");
        regSelectTitleObj.transform.SetParent(regSelectPanelObj.transform, false);
        Text regSelectTitle = regSelectTitleObj.AddComponent<Text>();
        regSelectTitle.text = "実体物体登録"; // Japanese text restored
        regSelectTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        regSelectTitle.fontSize = 20;
        regSelectTitle.color = Color.white;
        regSelectTitle.alignment = TextAnchor.MiddleCenter;
        RectTransform regSelectTitleRect = regSelectTitleObj.GetComponent<RectTransform>();
        regSelectTitleRect.anchorMin = new Vector2(0, 0.7f);
        regSelectTitleRect.anchorMax = new Vector2(1, 0.95f);
        regSelectTitleRect.offsetMin = Vector2.zero;
        regSelectTitleRect.offsetMax = Vector2.zero;

        // Object Button
        DefaultControls.Resources regSelectUiRes = new DefaultControls.Resources();
        GameObject selectObjectBtnObj = DefaultControls.CreateButton(regSelectUiRes);
        selectObjectBtnObj.name = "SelectObjectButton";
        selectObjectBtnObj.transform.SetParent(regSelectPanelObj.transform, false);
        RectTransform selectObjectBtnRect = selectObjectBtnObj.GetComponent<RectTransform>();
        selectObjectBtnRect.anchorMin = new Vector2(0.2f, 0.25f);
        selectObjectBtnRect.anchorMax = new Vector2(0.8f, 0.65f);
        selectObjectBtnRect.offsetMin = Vector2.zero;
        selectObjectBtnRect.offsetMax = Vector2.zero;
        Text selectObjectBtnText = selectObjectBtnObj.GetComponentInChildren<Text>();
        selectObjectBtnText.text = "📦 物体を登録 (Object)";
        selectObjectBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selectObjectBtnText.fontSize = 18;
        Image selectObjectBtnBg = selectObjectBtnObj.GetComponent<Image>();
        selectObjectBtnBg.color = new Color(1f, 0.5f, 0.1f, 0.9f); // Orange
        BoxCollider selectObjectCol = selectObjectBtnObj.AddComponent<BoxCollider>();
        selectObjectCol.size = new Vector3(200, 60, 1);
        // Z Offset
        Vector3 objPos = selectObjectBtnRect.anchoredPosition3D;
        selectObjectBtnRect.anchoredPosition3D = new Vector3(objPos.x, objPos.y, -10f);
        selectObjectCol.size = new Vector3(200, 60, 10); // Thicker collider

        // Cancel Button
        GameObject selectCancelBtnObj = DefaultControls.CreateButton(regSelectUiRes);
        selectCancelBtnObj.name = "SelectCancelButton";
        selectCancelBtnObj.transform.SetParent(regSelectPanelObj.transform, false);
        RectTransform selectCancelBtnRect = selectCancelBtnObj.GetComponent<RectTransform>();
        selectCancelBtnRect.anchorMin = new Vector2(0.3f, 0.05f);
        selectCancelBtnRect.anchorMax = new Vector2(0.7f, 0.2f);
        selectCancelBtnRect.offsetMin = Vector2.zero;
        selectCancelBtnRect.offsetMax = Vector2.zero;
        Text selectCancelBtnText = selectCancelBtnObj.GetComponentInChildren<Text>();
        selectCancelBtnText.text = "✖ キャンセル";
        selectCancelBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selectCancelBtnText.fontSize = 14;
        BoxCollider selectCancelCol = selectCancelBtnObj.AddComponent<BoxCollider>();
        selectCancelCol.size = new Vector3(150, 40, 1);
        // Z Offset
        Vector3 cancelPos = selectCancelBtnRect.anchoredPosition3D;
        selectCancelBtnRect.anchoredPosition3D = new Vector3(cancelPos.x, cancelPos.y, -10f);
        selectCancelCol.size = new Vector3(150, 40, 10); // Thicker collider

        // Events
        Button selectObjectBtn = selectObjectBtnObj.GetComponent<Button>();
        selectObjectBtn.onClick.AddListener(() => {
            regSelectPanelObj.SetActive(false);
            if(uploader != null) uploader.ShowObjectIdPanel();
        });

        Button selectCancelBtn = selectCancelBtnObj.GetComponent<Button>();
        selectCancelBtn.onClick.AddListener(() => {
            regSelectPanelObj.SetActive(false);
        });

        regSelectPanelObj.SetActive(false);

        // Link
        if(uploader != null) {
            uploader.registrationSelectPanel = regSelectPanelObj;
            uploader.closeRegistrationSelectButton = selectCancelBtnObj.GetComponent<Button>();
        }

        // ============ Object Registration Panel Setup ============
        if (uploader != null)
        {
            GameObject objRegPanelObj = new GameObject("ObjectRegPanel");
            objRegPanelObj.transform.SetParent(panelObj.transform, false);
            Image objRegPanelBg = objRegPanelObj.AddComponent<Image>();
            objRegPanelBg.color = new Color(0.1f, 0.08f, 0.05f, 0.95f); // Dark orange tint
            RectTransform objRegPanelRect = objRegPanelObj.GetComponent<RectTransform>();
            objRegPanelRect.anchorMin = new Vector2(0.1f, 0.25f);
            objRegPanelRect.anchorMax = new Vector2(0.9f, 0.75f);
            objRegPanelRect.offsetMin = Vector2.zero;
            objRegPanelRect.offsetMax = Vector2.zero;

            // Title
            GameObject objRegTitleObj = new GameObject("Title");
            objRegTitleObj.transform.SetParent(objRegPanelObj.transform, false);
            Text objRegTitleText = objRegTitleObj.AddComponent<Text>();
            objRegTitleText.text = "OBJECT REGISTRATION";
            objRegTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            objRegTitleText.fontSize = 24;
            objRegTitleText.color = new Color(1f, 0.6f, 0.2f, 1f); // Orange
            objRegTitleText.fontStyle = FontStyle.Bold;
            objRegTitleText.alignment = TextAnchor.MiddleCenter;
            RectTransform objRegTextRect = objRegTitleObj.GetComponent<RectTransform>();
            objRegTextRect.anchorMin = new Vector2(0, 0.75f);
            objRegTextRect.anchorMax = new Vector2(1, 0.95f);
            objRegTextRect.offsetMin = Vector2.zero;
            objRegTextRect.offsetMax = Vector2.zero;

            // Instruction
            GameObject objInstrObj = new GameObject("Instruction");
            objInstrObj.transform.SetParent(objRegPanelObj.transform, false);
            Text objInstrText = objInstrObj.AddComponent<Text>();
            objInstrText.text = "Point camera at object and enter name:";
            objInstrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            objInstrText.fontSize = 16;
            objInstrText.color = Color.white;
            objInstrText.alignment = TextAnchor.MiddleCenter;
            RectTransform objInstrRect = objInstrObj.GetComponent<RectTransform>();
            objInstrRect.anchorMin = new Vector2(0, 0.55f);
            objInstrRect.anchorMax = new Vector2(1, 0.75f);
            objInstrRect.offsetMin = Vector2.zero;
            objInstrRect.offsetMax = Vector2.zero;

            // Object Name Input
            DefaultControls.Resources objUiResources = new DefaultControls.Resources();
            GameObject objNameInputObj = DefaultControls.CreateInputField(objUiResources);
            objNameInputObj.name = "ObjectNameInputField";
            objNameInputObj.transform.SetParent(objRegPanelObj.transform, false);
            RectTransform objNameInputRect = objNameInputObj.GetComponent<RectTransform>();
            objNameInputRect.anchorMin = new Vector2(0.1f, 0.35f);
            objNameInputRect.anchorMax = new Vector2(0.9f, 0.55f);
            objNameInputRect.offsetMin = Vector2.zero;
            objNameInputRect.offsetMax = Vector2.zero;
            InputField objNameInput = objNameInputObj.GetComponent<InputField>();
            objNameInput.placeholder.GetComponent<Text>().text = "Enter object name...";
            objNameInput.placeholder.GetComponent<Text>().font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            objNameInputObj.GetComponentInChildren<Text>().font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BoxCollider objNameInputCol = objNameInputObj.AddComponent<BoxCollider>();
            objNameInputCol.size = new Vector3(300, 50, 1);

            // Register Object Button
            GameObject regObjBtnObj = CreateIconButton("RegisterObjectButton", "icon_check", "Register", objRegPanelObj.transform);
            RectTransform regObjBtnRect = regObjBtnObj.GetComponent<RectTransform>();
            regObjBtnRect.anchorMin = new Vector2(0.1f, 0.1f);
            regObjBtnRect.anchorMax = new Vector2(0.45f, 0.3f);
            regObjBtnRect.offsetMin = Vector2.zero;
            regObjBtnRect.offsetMax = Vector2.zero;
            Image regObjBtnBg = regObjBtnObj.GetComponent<Image>();
            if (regObjBtnBg != null) regObjBtnBg.color = new Color(1f, 0.5f, 0.1f, 0.9f); // Orange
            BoxCollider regObjBtnCol = regObjBtnObj.AddComponent<BoxCollider>();
            regObjBtnCol.size = new Vector3(150, 50, 1);

            // Cancel Button
            GameObject cancelObjRegBtnObj = CreateIconButton("CancelObjectRegButton", "icon_close", "Cancel", objRegPanelObj.transform);
            RectTransform cancelObjRegRect = cancelObjRegBtnObj.GetComponent<RectTransform>();
            cancelObjRegRect.anchorMin = new Vector2(0.55f, 0.1f);
            cancelObjRegRect.anchorMax = new Vector2(0.9f, 0.3f);
            cancelObjRegRect.offsetMin = Vector2.zero;
            cancelObjRegRect.offsetMax = Vector2.zero;
            BoxCollider cancelObjRegCol = cancelObjRegBtnObj.AddComponent<BoxCollider>();
            cancelObjRegCol.size = new Vector3(150, 50, 10); // Thicker collider
            // Ensure Z-Offset
            RectTransform corRect = cancelObjRegBtnObj.GetComponent<RectTransform>();
            Vector3 corPos = corRect.anchoredPosition3D;
            corRect.anchoredPosition3D = new Vector3(corPos.x, corPos.y, -10f);

            // Progress Bar
            GameObject progressBarBgObj = new GameObject("ProgressBarBg");
            progressBarBgObj.transform.SetParent(objRegPanelObj.transform, false);
            Image progressBarBg = progressBarBgObj.AddComponent<Image>();
            progressBarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            RectTransform progressBarBgRect = progressBarBgObj.GetComponent<RectTransform>();
            progressBarBgRect.anchorMin = new Vector2(0.1f, 0.8f);
            progressBarBgRect.anchorMax = new Vector2(0.9f, 0.88f);
            progressBarBgRect.offsetMin = Vector2.zero;
            progressBarBgRect.offsetMax = Vector2.zero;
            
            GameObject progressBarFillObj = new GameObject("ProgressBarFill");
            progressBarFillObj.transform.SetParent(progressBarBgObj.transform, false);
            Image progressBarFill = progressBarFillObj.AddComponent<Image>();
            progressBarFill.color = new Color(1f, 0.5f, 0.1f, 1f); // Orange
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillAmount = 0f;
            RectTransform progressBarFillRect = progressBarFillObj.GetComponent<RectTransform>();
            progressBarFillRect.anchorMin = Vector2.zero;
            progressBarFillRect.anchorMax = Vector2.one;
            progressBarFillRect.offsetMin = Vector2.zero;
            progressBarFillRect.offsetMax = Vector2.zero;
            progressBarBgObj.SetActive(false);
            
            GameObject progressTextObj = new GameObject("ProgressText");
            progressTextObj.transform.SetParent(objRegPanelObj.transform, false);
            Text progressText = progressTextObj.AddComponent<Text>();
            progressText.text = "0/30";
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 28;
            progressText.color = new Color(1f, 0.9f, 0.6f, 1f); // Warm yellow
            progressText.fontStyle = FontStyle.Bold;
            progressText.alignment = TextAnchor.MiddleCenter;
            RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.1f, 0.88f);
            progressTextRect.anchorMax = new Vector2(0.9f, 0.98f);
            progressTextRect.offsetMin = Vector2.zero;
            progressTextRect.offsetMax = Vector2.zero;
            progressTextObj.SetActive(false);

            objRegPanelObj.SetActive(false);

            // Link
            uploader.objectIdPanel = objRegPanelObj;
            uploader.objectNameInput = objNameInput;
            uploader.registerObjectButton = regObjBtnObj.GetComponent<Button>();
            uploader.closeObjectIdPanelButton = cancelObjRegBtnObj.GetComponent<Button>();
            uploader.objectProgressText = progressText;
            uploader.objectProgressBar = progressBarFill;
            
            Debug.Log("[SceneSetupTool] Object Registration Panel with progress UI created!");
        }

        // ============ Registration List Panel Setup ============
        if (uploader != null)
        {
            GameObject listPanelObj = new GameObject("RegistrationListPanel");
            listPanelObj.transform.SetParent(panelObj.transform, false);
            Image listPanelBg = listPanelObj.AddComponent<Image>();
            listPanelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);
            RectTransform listPanelRect = listPanelObj.GetComponent<RectTransform>();
            listPanelRect.anchorMin = new Vector2(0.02f, 0.05f);
            listPanelRect.anchorMax = new Vector2(0.98f, 0.95f);
            listPanelRect.offsetMin = Vector2.zero;
            listPanelRect.offsetMax = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(listPanelObj.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = "検出対象リスト";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.4f, 0.8f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.90f);
            titleRect.anchorMax = new Vector2(1, 0.99f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            if (bottomMenuCtrl != null)
            {
                bottomMenuCtrl.registrationListPanel = listPanelObj;
            }

            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(listPanelObj.transform, false);
            Text subtitleText = subtitleObj.AddComponent<Text>();
            subtitleText.text = "追跡する物体を3つまで選択してください";
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.fontSize = 14;
            subtitleText.color = new Color(0.6f, 0.6f, 0.7f);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 0.85f);
            subtitleRect.anchorMax = new Vector2(1, 0.90f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;

            // Grid
            GameObject gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(listPanelObj.transform, false);
            RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.02f, 0.15f);
            gridRect.anchorMax = new Vector2(0.98f, 0.84f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            
            GridLayoutGroup regListGrid = gridContainer.AddComponent<GridLayoutGroup>();
            regListGrid.cellSize = new Vector2(120, 60);
            regListGrid.spacing = new Vector2(8, 8);
            regListGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            regListGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            regListGrid.childAlignment = TextAnchor.UpperCenter;
            regListGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            regListGrid.constraintCount = 5;

            // Object names remain in English for YOLO detection, but display names can be mapped
            string[] commonObjects = {
                "person", "bicycle", "car", "dog", "cat", 
                "backpack", "umbrella", "bottle", "cup", "fork", 
                "spoon", "bowl", "chair", "laptop", "cell phone"
            };
            // Japanese display names for UI (matches YOLO class names)
            Dictionary<string, string> objectNameJp = new Dictionary<string, string>() {
                {"person", "人"}, {"bicycle", "自転車"}, {"car", "車"}, {"dog", "犬"}, {"cat", "猫"},
                {"backpack", "リュック"}, {"umbrella", "傘"}, {"bottle", "ボトル"}, {"cup", "カップ"}, {"fork", "フォーク"},
                {"spoon", "スプーン"}, {"bowl", "ボウル"}, {"chair", "椅子"}, {"laptop", "PC"}, {"cell phone", "携帯"}
            };
            
            GameObject selCountObj = new GameObject("SelectionCount");
            selCountObj.transform.SetParent(listPanelObj.transform, false);
            Text selCountTxt = selCountObj.AddComponent<Text>();
            selCountTxt.text = "選択中: 0/3";
            selCountTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            selCountTxt.fontSize = 16;
            selCountTxt.fontStyle = FontStyle.Bold;
            selCountTxt.color = new Color(0.4f, 1f, 0.6f);
            selCountTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform selCountRect = selCountObj.GetComponent<RectTransform>();
            selCountRect.anchorMin = new Vector2(0, 0.12f);
            selCountRect.anchorMax = new Vector2(0.3f, 0.18f);
            selCountRect.offsetMin = Vector2.zero;
            selCountRect.offsetMax = Vector2.zero;
            
            uploader.selectionCountText = selCountTxt;
            
            foreach (string objName in commonObjects)
            {
                // 1. Container for Grid Layout (Keeps Grid happy)
                GameObject containerObj = new GameObject(objName + "_Container");
                containerObj.transform.SetParent(gridContainer.transform, false);
                containerObj.AddComponent<RectTransform>(); 
                
                // 2. Actual Interactable Button (Floats forward)
                // Use DefaultControls logic to ensure standard components exist
                GameObject btnObj = DefaultControls.CreateButton(new DefaultControls.Resources());
                btnObj.name = objName + "_Btn";
                btnObj.transform.SetParent(containerObj.transform, false);
                
                // Remove Default Text
                DestroyImmediate(btnObj.GetComponentInChildren<Text>()); 
                
                // RectTransform Setup
                RectTransform btnRect = btnObj.GetComponent<RectTransform>();
                btnRect.anchorMin = Vector2.zero;
                btnRect.anchorMax = Vector2.one;
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                
                // Z-Offset (Critical)
                btnRect.anchoredPosition3D = new Vector3(0, 0, -20f);
                
                // Image Setup
                Image btnBg = btnObj.GetComponent<Image>();
                btnBg.color = new Color(0.12f, 0.15f, 0.22f, 1f);
                
                // Button Component Setup
                Button btn = btnObj.GetComponent<Button>();
                btn.transition = Selectable.Transition.None; 
                
                // Collider Setup (Matches CreateIconButton logic)
                BoxCollider col = btnObj.AddComponent<BoxCollider>();
                col.size = new Vector3(120, 60, 50f); // Wide Z for easy hit
                col.center = new Vector3(0, 0, 0); // Center relative to Rect (already offset)

                // 3. Child Elements (Checkbox, Label)
                
                // Checkbox
                GameObject checkboxObj = new GameObject("Checkbox");
                checkboxObj.transform.SetParent(btnObj.transform, false);
                Image checkboxImg = checkboxObj.AddComponent<Image>();
                
                string uncheckedPath = "Assets/UI/Box/digit-チェックなし.png";
                string checkedPath = "Assets/UI/Box/digit-チェックあり.png";
                
                // ... (Texture Loading Logic remains same) ...
                Sprite uncheckedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(uncheckedPath);
                Sprite checkedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(checkedPath);
                
                if (uncheckedSprite == null) uncheckedSprite = Resources.Load<Sprite>("UISprites/Checkmark"); // Fallback
                if (checkedSprite == null) checkedSprite = Resources.Load<Sprite>("UISprites/Checkmark"); // Fallback
                
                checkboxImg.sprite = uncheckedSprite;
                checkboxImg.preserveAspect = true;
                checkboxImg.color = Color.white;
                checkboxImg.raycastTarget = false; // Important: Let parent button catch ray
                
                RectTransform checkboxRect = checkboxObj.GetComponent<RectTransform>();
                checkboxRect.anchorMin = new Vector2(0.05f, 0.15f);
                checkboxRect.anchorMax = new Vector2(0.25f, 0.85f);
                checkboxRect.offsetMin = Vector2.zero;
                checkboxRect.offsetMax = Vector2.zero;
                
                // Label
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                Text labelText = labelObj.AddComponent<Text>();
                labelText.text = objectNameJp.ContainsKey(objName) ? objectNameJp[objName] : objName;
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.fontSize = 14;
                labelText.color = Color.white;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.raycastTarget = false; // Important
                
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.3f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                
                // Script Setup
                RegistrationListButton regBtn = btnObj.AddComponent<RegistrationListButton>();
                regBtn.ObjectName = objName;
                regBtn.CheckboxImage = checkboxImg;
                regBtn.UncheckedSprite = uncheckedSprite;
                regBtn.CheckedSprite = checkedSprite;
                regBtn.BackgroundImage = btnBg;
            }

            // Beacon Toggle
            GameObject beaconToggleObj = new GameObject("BeaconToggleBtn");
            beaconToggleObj.transform.SetParent(listPanelObj.transform, false);
            Image beaconToggleBg = beaconToggleObj.AddComponent<Image>();
            beaconToggleBg.color = new Color(0.2f, 0.6f, 0.3f);
            RectTransform beaconToggleRect = beaconToggleObj.GetComponent<RectTransform>();
            beaconToggleRect.anchorMin = new Vector2(0.1f, 0.02f);
            beaconToggleRect.anchorMax = new Vector2(0.48f, 0.12f);
            beaconToggleRect.offsetMin = Vector2.zero;
            beaconToggleRect.offsetMax = Vector2.zero;
            // Z Offset
            Vector3 btPos = beaconToggleRect.anchoredPosition3D;
            beaconToggleRect.anchoredPosition3D = new Vector3(btPos.x, btPos.y, -10f); // Float forward

            Button beaconToggleBtn = beaconToggleObj.AddComponent<Button>();
            beaconToggleBtn.targetGraphic = beaconToggleBg;
            BoxCollider beaconToggleCol = beaconToggleObj.AddComponent<BoxCollider>();
            beaconToggleCol.size = new Vector3(200, 50, 50f);
            beaconToggleCol.center = new Vector3(0, 0, 0); // Already offset by RectTransform

            GameObject beaconLabelObj = new GameObject("Label");
            beaconLabelObj.transform.SetParent(beaconToggleObj.transform, false);
            Text beaconLabelText = beaconLabelObj.AddComponent<Text>();
            beaconLabelText.text = "ビーコン: オン";
            beaconLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            beaconLabelText.fontSize = 18;
            beaconLabelText.fontStyle = FontStyle.Bold;
            beaconLabelText.color = Color.white;
            beaconLabelText.alignment = TextAnchor.MiddleCenter;
            RectTransform beaconLabelRect = beaconLabelObj.GetComponent<RectTransform>();
            beaconLabelRect.anchorMin = Vector2.zero;
            beaconLabelRect.anchorMax = Vector2.one;
            beaconLabelRect.offsetMin = Vector2.zero;
            beaconLabelRect.offsetMax = Vector2.zero;

            BeaconVisibilityButton visBtn = beaconToggleObj.AddComponent<BeaconVisibilityButton>();
            visBtn.uploader = uploader;

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(listPanelObj.transform, false);
            Image closeBtnBg = closeBtnObj.AddComponent<Image>();
            closeBtnBg.color = new Color(0.5f, 0.15f, 0.15f, 1f);
            RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.52f, 0.02f);
            closeBtnRect.anchorMax = new Vector2(0.9f, 0.12f);
            closeBtnRect.offsetMin = Vector2.zero;
            closeBtnRect.offsetMax = Vector2.zero;
            // Z Offset
            Vector3 clPos = closeBtnRect.anchoredPosition3D;
            closeBtnRect.anchoredPosition3D = new Vector3(clPos.x, clPos.y, -10f); // Float forward
            
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnBg;
            BoxCollider closeBtnCol = closeBtnObj.AddComponent<BoxCollider>();
            closeBtnCol.size = new Vector3(200, 50, 50f); // Increased thickness
            closeBtnCol.center = new Vector3(0, 0, 0); // Already offset by RectTransform
            
            GameObject closeLabelObj = new GameObject("Label");
            closeLabelObj.transform.SetParent(closeBtnObj.transform, false);
            Text closeLabelText = closeLabelObj.AddComponent<Text>();
            closeLabelText.text = "閉じる";
            closeLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeLabelText.fontSize = 18;
            closeLabelText.fontStyle = FontStyle.Bold;
            closeLabelText.color = Color.white;
            closeLabelText.alignment = TextAnchor.MiddleCenter;
            RectTransform closeLabelRect = closeLabelObj.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;
            
            closeBtn.onClick.AddListener(() => {
                listPanelObj.SetActive(false);
            });

            listPanelObj.SetActive(false);
        }
    }
}
