using UnityEngine;
using UnityEditor;

public class SceneCleanupTool : MonoBehaviour
{
    [MenuItem("Tools/Cleanup Scene (Reset UI & Managers)")]
    public static void CleanupScene()
    {
        // List of object names to destroy
        string[] objectsToDestroy = new string[]
        {
            "FlashbackCanvas",
            "Canvas", // Old canvas name
            "SearchPanel",
            "AppManager",
            "ImageUploaderSystem",
            "HeadLockedCanvas",
            "DebugLogPanel",
            "EventSystem",
            "DebugText3D", // Old 3D debug text
            "OnScreenDebugLog", // Old on-screen debug log
            "DebugLog" // Another possible name
        };

        int count = 0;

        foreach (string name in objectsToDestroy)
        {
            GameObject obj = GameObject.Find(name);
            while (obj != null)
            {
                DestroyImmediate(obj);
                count++;
                // Try finding again in case of duplicates
                obj = GameObject.Find(name);
            }
        }

        Debug.Log($"[Cleanup] Removed {count} objects. Scene is clean.");
    }
}
