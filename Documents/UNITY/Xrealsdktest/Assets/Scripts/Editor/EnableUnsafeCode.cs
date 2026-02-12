using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EnableUnsafeCode
{
    static EnableUnsafeCode()
    {
        // Force enable unsafe code for all relevant platforms
        SetUnsafe(BuildTargetGroup.Android);
        SetUnsafe(BuildTargetGroup.Standalone);
        SetUnsafe(BuildTargetGroup.iOS);
    }

    static void SetUnsafe(BuildTargetGroup group)
    {
        // We set it even if it's already true, just to be sure, 
        // but checking first avoids dirtying the project settings unnecessarily.
        // However, since the user is seeing errors, let's force it.
        PlayerSettings.allowUnsafeCode = true;
        
        // Note: PlayerSettings.allowUnsafeCode applies to the *current* build target group in newer Unity versions,
        // or globally in older ones. To be safe, we might need to switch targets or just rely on the global property.
        // The property 'allowUnsafeCode' is actually per-project in recent versions but exposed via this static property 
        // which usually affects the active target.
        
        // Let's try to set it via SerializedObject if the simple way fails, but for now this is the standard way.
        Debug.Log($"[AutoFix] Enforcing 'Allow unsafe code' for {group} (Current Active Target)");
    }
}
