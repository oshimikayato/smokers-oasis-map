using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq; // Added for Linq usage

public class AutoBuilder
{
    [MenuItem("Build/Build Android APK (Auto Name)")]
    public static void BuildAndroid()
    {
        // 1. Generate Filename with Timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folderPath = "Builds";
        string fileName = $"XRealApp_{timestamp}.apk";
        string fullPath = Path.Combine(folderPath, fileName);

        // 2. Ensure folder exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 3. Get Configured Scenes
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Settings!");
            return;
        }

        Debug.Log($"Starting Build: {fullPath}...");

        // 4. Configure Build Options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = fullPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // 5. Execute Build
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        // 6. Report Result
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"<color=green>Build Succeeded!</color> Output: {fullPath}");
            // Open the folder
            EditorUtility.RevealInFinder(fullPath);
        }
        else
        {
            Debug.LogError($"Build Failed! Errors: {report.summary.totalErrors}");
        }
    }
}
