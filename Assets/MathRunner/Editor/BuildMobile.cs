using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Android and iOS players (see RELEASING.md). They need the Unity Hub modules "Android Build Support"
// (with OpenJDK and Android SDK & NDK Tools) and "iOS Build Support".
// Menu: Math Runner > Build Android APK / Build Android AAB / Export iOS Xcode project.
// Command line: -executeMethod BuildMobile.AndroidApk, .AndroidBundle or .IOS
// iOS output is an Xcode project: open it on a Mac to sign and archive.
public static class BuildMobile
{
    [MenuItem("Math Runner/Build Android APK")]
    public static void AndroidApk() => Android("Builds/Android", "MathRunner.apk", false);

    [MenuItem("Math Runner/Build Android AAB")]
    public static void AndroidBundle() => Android("Builds/Android", "MathRunner.aab", true);

    [MenuItem("Math Runner/Export iOS Xcode project")]
    public static void IOS()
    {
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        Build("Builds/iOS", string.Empty, BuildTarget.iOS);
    }

    private static void Android(string folder, string file, bool bundle)
    {
        EditorUserBuildSettings.buildAppBundle = bundle;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        Build(folder, file, BuildTarget.Android);
    }

    private static void Build(string folder, string output, BuildTarget target)
    {
        var full = Path.GetFullPath(folder);
        Directory.CreateDirectory(full);
        var path = output.Length == 0 ? full : Path.Combine(full, output);
        if (output.Length == 0 && Directory.Exists(full)) { Directory.Delete(full, true); Directory.CreateDirectory(full); }
        if (output.Length > 0 && File.Exists(path)) File.Delete(path);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception(target + " build failed: " + report.summary.result);
        UnityEngine.Debug.Log(target + " build written to " + path + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
    }
}
