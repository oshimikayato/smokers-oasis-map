using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public class PreBuildCleanup : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string resPath = Path.Combine(Application.dataPath, "Plugins", "Android", "res");
        
        if (Directory.Exists(resPath))
        {
            Debug.Log($"[PreBuildCleanup] Removing obsolete res folder: {resPath}");
            try
            {
                Directory.Delete(resPath, true);
                
                // Also delete .meta file
                string metaPath = resPath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
                
                AssetDatabase.Refresh();
                Debug.Log("[PreBuildCleanup] res folder removed successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PreBuildCleanup] Failed to remove res folder: {e.Message}");
            }
        }
        else
        {
            Debug.Log("[PreBuildCleanup] No res folder found, proceeding with build");
        }
    }
}
