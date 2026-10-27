using System.IO;
using UnityEditor;
using UnityEngine;

// Keep the directly importable OBJ version in Unity; GLBs are portable exports.
// Unity generates all asset GUIDs and .meta files itself.
public sealed class StarterModelImporter : AssetPostprocessor
{
    private const string Root = "Assets/MathRunner/Art/Models/";

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".obj")) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1;
        importer.useFileScale = false;
        importer.importNormals = ModelImporterNormals.Calculate;
        importer.normalSmoothingAngle = 0;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    private Material OnAssignMaterialModel(Material source, Renderer renderer)
    {
        if (!assetPath.StartsWith(Root)) return null;
        // Unity only accepts an existing material asset here, and assets cannot be created during import.
        // GameSceneBuilder creates one URP Lit asset per palette color before the models are imported.
        return AssetDatabase.LoadAssetAtPath<Material>($"Assets/MathRunner/Art/Materials/{source.name}.mat");
    }

    [MenuItem("Math Runner/Art/Create Starter Prefabs")]
    private static void CreatePrefabs()
    {
        const string destination = "Assets/MathRunner/Art/Prefabs";
        Directory.CreateDirectory(destination);
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Root, "*.obj"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            if (asset == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = Path.GetFileNameWithoutExtension(path);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            bool character = instance.name.Contains("runner_");
            bool pickup = instance.name.StartsWith("28_") || instance.name.StartsWith("29_") ||
                instance.name.StartsWith("30_") || instance.name.StartsWith("31_") || instance.name.StartsWith("32_");
            if (character)
            {
                var controller = instance.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.25f;
                controller.center = new Vector3(0, 0.9f, 0);
            }
            else if (instance.name.StartsWith("27_"))
            {
                // Separate posts and sign preserve the opening for sliding.
                foreach (var renderer in renderers)
                    renderer.gameObject.AddComponent<BoxCollider>();
            }
            else
            {
                var collider = instance.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                collider.isTrigger = pickup;
            }
            PrefabUtility.SaveAsPrefabAsset(instance, $"{destination}/{instance.name}.prefab");
            Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Starter prefabs created. Add game scripts and pickup handling before use.");
    }
}
