using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Builds the Windows player the installer packs: Builds/Windows/MathRunner.exe (see RELEASING.md).
// Menu: Math Runner > Build Windows. Command line: -executeMethod BuildWindows.Run
public static class BuildWindows
{
    [MenuItem("Math Runner/Build Windows")]
    public static void Run()
    {
        var folder = Path.GetFullPath("Builds/Windows");
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = Path.Combine(folder, "MathRunner.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
        UnityEngine.Debug.Log("Windows build written to " + folder + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
    }
}
