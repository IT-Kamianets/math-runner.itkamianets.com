using System.IO;
using MathRunner.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Main.unity from the starter models: prefabs, camera, light, fog and the
// GameController with every prefab wired in. Safe to run again.
public static class GameSceneBuilder
{
    private const string ModelFolder = "Assets/MathRunner/Art/Models";
    private const string PrefabFolder = "Assets/MathRunner/Art/Prefabs";
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Math Runner/Build Game Scene")]
    public static void Build()
    {
        CreatePaletteMaterials();
        foreach (var path in Directory.GetFiles(ModelFolder, "*.obj"))
            AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();

        EditorApplication.ExecuteMenuItem("Math Runner/Art/Create Starter Prefabs");
        MeshyArt.CreatePrefabs(); // replaces the starter prefabs that have a Meshy model
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var old = Object.FindAnyObjectByType<GameController>();
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.fieldOfView = 62f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 400f;
        camera.transform.SetPositionAndRotation(new Vector3(0f, 3.3f, -6.4f), Quaternion.Euler(12f, 0f, 0f));

        if (Object.FindAnyObjectByType<Light>() == null)
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        var sun = Object.FindAnyObjectByType<Light>();
        sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.96f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 170f;

        var game = new GameObject("Game");
        var controller = game.AddComponent<GameController>();

        var keys = new System.Collections.Generic.List<string>();
        var prefabs = new System.Collections.Generic.List<GameObject>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            keys.Add(Path.GetFileNameWithoutExtension(path));
            prefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        var so = new SerializedObject(controller);
        var list = so.FindProperty("prefabs");
        list.arraySize = keys.Count;
        for (var i = 0; i < keys.Count; i++)
        {
            var element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("key").stringValue = keys[i];
            element.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
        }

        so.FindProperty("gameCamera").objectReferenceValue = camera;
        so.FindProperty("runnerYaw").floatValue = 0f; // the starter runner mesh already faces +Z, the way the track runs
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Math Runner: scene built with " + keys.Count + " prefabs.");
    }

    // One URP Lit material per `newmtl` entry in palette.mtl, named like the entry.
    private static void CreatePaletteMaterials()
    {
        const string folder = "Assets/MathRunner/Art/Materials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/MathRunner/Art", "Materials");
        var shader = Shader.Find("Universal Render Pipeline/Lit");

        string name = null;
        foreach (var line in File.ReadAllLines(ModelFolder + "/palette.mtl"))
        {
            var parts = line.Trim().Split(' ');
            if (parts[0] == "newmtl" && parts.Length > 1) name = parts[1];
            else if (parts[0] == "Kd" && name != null && parts.Length >= 4)
            {
                var color = new Color(
                    float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
                var path = $"{folder}/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(material);
                name = null;
            }
        }

        AssetDatabase.SaveAssets();
    }
}
