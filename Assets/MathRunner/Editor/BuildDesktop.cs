using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Linux and macOS players for Tools/installer/package-unix.sh (see RELEASING.md). They need the Unity Hub
// modules "Linux Build Support (Mono)" and "Mac Build Support (Mono)".
// Menu: Math Runner > Build Linux / Build macOS. Command line: -executeMethod BuildDesktop.Linux or .MacOS
public static class BuildDesktop
{
    [MenuItem("Math Runner/Build Linux")]
    public static void Linux()
    {
        Build("Builds/Linux", "MathRunner.x86_64", BuildTarget.StandaloneLinux64);
    }

    [MenuItem("Math Runner/Build macOS")]
    public static void MacOS()
    {
        // Intel and Apple silicon in one app.
        EditorUserBuildSettings.SetPlatformSettings("OSXUniversal", "Architecture", "x64arm64");
        Build("Builds/macOS", "MathRunner.app", BuildTarget.StandaloneOSX);
    }

    private static void Build(string folder, string output, BuildTarget target)
    {
        var full = Path.GetFullPath(folder);
        if (Directory.Exists(full)) Directory.Delete(full, true);
        Directory.CreateDirectory(full);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = Path.Combine(full, output),
            target = target,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(target + " build failed: " + report.summary.result);
        UnityEngine.Debug.Log(target + " build written to " + full + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
    }
}
