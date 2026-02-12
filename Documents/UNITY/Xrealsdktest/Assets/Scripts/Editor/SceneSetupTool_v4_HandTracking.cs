using UnityEngine;
using UnityEditor;

public partial class SceneSetupTool_v4
{
    private static void SetupHandTracking()
    {
        // ============ Hand Tracking Prefab Setup ============
        // Instantiate HandTrackingExample.prefab for hand tracking support
        string handTrackingPrefabPath = "Assets/HandTrackingExample.prefab";
        GameObject handTrackingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(handTrackingPrefabPath);
        if (handTrackingPrefab != null)
        {
            // Check if already exists in scene
            GameObject existingHandTracking = GameObject.Find("HandTrackingExample");
            if (existingHandTracking == null)
            {
                GameObject handTrackingInstance = (GameObject)PrefabUtility.InstantiatePrefab(handTrackingPrefab);
                handTrackingInstance.name = "HandTrackingExample";
                
                // 不要なデモオブジェクトを削除（再帰的に検索）
                string[] objectsToDelete = { "Cube", "Panel", "ControllerPanel", "GrabbableItems" };
                foreach (string objName in objectsToDelete)
                {
                    // 直接の子から検索
                    Transform child = handTrackingInstance.transform.Find(objName);
                    if (child != null) DestroyImmediate(child.gameObject);
                    
                    // シーン全体からも検索（ネストされている場合）
                    GameObject sceneObj = GameObject.Find(objName);
                    if (sceneObj != null) DestroyImmediate(sceneObj);
                }
                
                Debug.Log("[SceneSetupTool] HandTrackingExample.prefab instantiated (demo objects removed)!");
            }
            else
            {
                // 既存のインスタンスからも不要オブジェクトを削除
                string[] objectsToDelete = { "Cube", "Panel", "ControllerPanel", "GrabbableItems" };
                foreach (string objName in objectsToDelete)
                {
                    Transform child = existingHandTracking.transform.Find(objName);
                    if (child != null) DestroyImmediate(child.gameObject);
                    
                    // シーン全体からも検索
                    GameObject sceneObj = GameObject.Find(objName);
                    if (sceneObj != null) DestroyImmediate(sceneObj);
                }
                
                Debug.Log("[SceneSetupTool] HandTrackingExample already exists, demo objects removed.");
            }
        }
        else
        {
            Debug.LogWarning($"[SceneSetupTool] HandTrackingExample.prefab not found at {handTrackingPrefabPath}. Hand tracking may not work correctly.");
        }
    }
}
