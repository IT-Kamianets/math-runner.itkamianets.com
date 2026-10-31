using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Turns the optimized Meshy FBX files (Assets/MathRunner/Art/Meshy) into prefabs that replace the
// starter prefabs with the same key, so the game (which looks prefabs up by key) needs no changes.
// Safe to run again.
public sealed class MeshyArt : AssetPostprocessor
{
    private const string Root = "Assets/MathRunner/Art/Meshy/";
    private const string PrefabFolder = "Assets/MathRunner/Art/Prefabs";
    private const string MaterialFolder = "Assets/MathRunner/Art/Materials/Meshy";

    // FBX file -> prefab keys it replaces. The one shop model stands in for all three shop colors for now.
    private static readonly (string fbx, string[] keys)[] Map =
    {
        ("runner_kid", new[] { "01_runner_kid" }),
        ("shop_market", new[] { "04_shop_red", "05_shop_teal", "06_shop_yellow" }),
        ("apartment_tall", new[] { "07_apartment_tall" }),
        ("building_wide", new[] { "08_apartment_wide" }),
        ("building_corner", new[] { "09_corner_building" }),
        ("street_lamp", new[] { "12_street_lamp" }),
        ("tree_round", new[] { "13_tree_round" }),
        ("bench", new[] { "15_bench" }),
        ("bus_shelter", new[] { "18_bus_shelter" }),
        ("barrier_striped", new[] { "23_striped_barrier" }),
        ("cone_cluster", new[] { "24_cone_cluster" }),
        ("crate_stack", new[] { "25_crate_stack" }),
        ("fence_low", new[] { "26_low_fence" }),
        ("sign_overhead", new[] { "27_overhead_sign" }),
        ("coin_star", new[] { "28_star_coin" }),
    };

    // Yaw that turns each model so its front faces +Z, the way the starter models face. Found by looking
    // at the preview sheet (Math Runner > Art > Render Meshy Preview), not guessed.
    // Every model already faces +Z except the lamp, whose arm has to point at the road once the side yaw is applied.
    private static readonly Dictionary<string, float> FrontYaw = new Dictionary<string, float>
    {
        { "street_lamp", 90f },
    };

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".fbx")) return;
        var importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        var runner = assetPath.EndsWith("runner_kid.fbx");
        importer.animationType = runner ? ModelImporterAnimationType.Human : ModelImporterAnimationType.None;
        importer.importAnimation = runner;
    }

    [MenuItem("Math Runner/Art/Create Meshy Prefabs")]
    public static void CreatePrefabs()
    {
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        LoopRunnerClips();

        foreach (var (fbx, keys) in Map)
        {
            var path = Root + fbx + ".fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning("Meshy model missing: " + path); continue; }
            var material = EnsureMaterial(fbx);
            foreach (var key in keys) SavePrefab(asset, key, fbx, material);
        }

        MeshyStreet.Create();
        AssetDatabase.SaveAssets();
        Debug.Log("Meshy prefabs created.");
    }

    // The imported clips are named like "Armature|Armature|Running"; rename them and loop them.
    private static void LoopRunnerClips()
    {
        var importer = AssetImporter.GetAtPath(Root + "runner_kid.fbx") as ModelImporter;
        if (importer == null) return;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            var name = clip.name;
            var bar = name.LastIndexOf('|');
            if (bar >= 0) name = name.Substring(bar + 1);
            clip.name = name;
            clip.loopTime = true;
            clip.loopPose = true;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static Material EnsureMaterial(string name)
    {
        var path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}{name}.fbm/{name}_albedo.jpg");
        if (texture == null) Debug.LogWarning("Meshy texture missing for " + name);
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.15f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SavePrefab(GameObject asset, string key, string fbx, Material material)
    {
        var root = new GameObject(key);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
        model.name = "Model";
        FrontYaw.TryGetValue(fbx, out var yaw);
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var materials = new Material[renderer.sharedMaterials.Length];
            for (var i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }

        if (fbx == "runner_kid")
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = EnsureRunnerController();
        }
        else
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var first = true;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }

            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = key.StartsWith("28_");
        }

        Directory.CreateDirectory(PrefabFolder);
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{key}.prefab");
        Object.DestroyImmediate(root);
    }

    private static AnimatorController EnsureRunnerController()
    {
        const string path = Root + "RunnerKid.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        var machine = controller.layers[0].stateMachine;
        foreach (var name in new[] { "Running", "Walking" })
        {
            AnimationClip clip = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Root + "runner_kid.fbx"))
                if (asset is AnimationClip c && c.name == name && !c.name.StartsWith("__preview")) clip = c;
            if (clip == null) { Debug.LogWarning("Runner clip missing: " + name); continue; }

            AnimatorState state = null;
            foreach (var existing in machine.states)
                if (existing.state.name == name) state = existing.state;
            if (state == null) state = machine.AddState(name);
            state.motion = clip;
            if (name == "Running") machine.defaultState = state;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // Renders every Meshy prefab from four sides into PNG sheets so facing and scale can be checked by eye.
    // Called from the command line: -executeMethod MeshyArt.RenderPreview -previewOut <folder>
    public static void RenderPreview()
    {
        var args = System.Environment.GetCommandLineArgs();
        var outFolder = "Temp/MeshyPreview";
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-previewOut") outFolder = args[i + 1];
        Directory.CreateDirectory(outFolder);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var lightObject = new GameObject("Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

        var cameraObject = new GameObject("Cam");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.3f, 0.32f, 0.36f);
        camera.fieldOfView = 30f;

        const int tile = 220;
        var lines = new List<string>();
        var keys = new List<string>();
        foreach (var (_, ks) in Map) keys.Add(ks[0]);

        var sheetIndex = 0;
        for (var start = 0; start < keys.Count; start += 8)
        {
            var count = Mathf.Min(8, keys.Count - start);
            var sheet = new Texture2D(tile * 4, tile * count, TextureFormat.RGB24, false);
            for (var row = 0; row < count; row++)
            {
                var key = keys[start + row];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{key}.prefab");
                var instance = (GameObject)Object.Instantiate(prefab);
                var bounds = new Bounds(instance.transform.position, Vector3.zero);
                var first = true;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                }

                lines.Add($"{key} size {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} min.y {bounds.min.y:F2} center.xz {bounds.center.x:F2},{bounds.center.z:F2}");
                var distance = bounds.extents.magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.95f;
                // View from +Z, +X, -Z, -X (so the first tile shows what a +Z front looks like).
                for (var view = 0; view < 4; view++)
                {
                    var angle = view * 90f;
                    var direction = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.25f, 1f).normalized;
                    camera.transform.position = bounds.center + direction * distance;
                    camera.transform.LookAt(bounds.center);
                    var target = RenderTexture.GetTemporary(tile, tile, 24);
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    sheet.ReadPixels(new Rect(0, 0, tile, tile), view * tile, (count - 1 - row) * tile);
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(target);
                }

                Object.DestroyImmediate(instance);
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(outFolder, $"sheet{sheetIndex++}.png"), sheet.EncodeToPNG());
        }

        File.WriteAllLines(Path.Combine(outFolder, "bounds.txt"), lines);
    }

    // Renders the street the way the game camera sees it: the environment, the runner and one of each obstacle.
    // Called from the command line: -executeMethod MeshyArt.RenderStreet -previewOut <folder>
    public static void RenderStreet()
    {
        var args = System.Environment.GetCommandLineArgs();
        var outFolder = "Temp/MeshyPreview";
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-previewOut") outFolder = args[i + 1];
        Directory.CreateDirectory(outFolder);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.96f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 170f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.62f);

        var light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
        light.intensity = 1.15f;

        var root = new GameObject("Root").transform;
        System.Func<string, GameObject> prefab = key => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{key}.prefab");
        var environment = new MathRunner.Game.TrackEnvironment(prefab, root);
        environment.Update(0f, 120f, 12f);

        void Put(string key, float x, float z)
        {
            var go = (GameObject)Object.Instantiate(prefab(key), root);
            go.transform.position = new Vector3(x, 0.1f, z);
        }

        Put("01_runner_kid", 0f, 0f);
        Put("23_striped_barrier", -2f, 14f);
        Put("24_cone_cluster", 0f, 22f);
        Put("25_crate_stack", 2f, 30f);
        Put("26_low_fence", -2f, 38f);
        Put("27_overhead_sign", 0f, 46f);
        Put("28_star_coin", 2f, 10f);

        var camera = new GameObject("Cam").AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.farClipPlane = 400f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.72f, 0.84f, 0.96f);
        camera.transform.SetPositionAndRotation(new Vector3(0f, 3.3f, -6.4f), Quaternion.Euler(12f, 0f, 0f));
        Shot(camera, Path.Combine(outFolder, "warmup.png"), 320, 180); // first render loads textures
        Shot(camera, Path.Combine(outFolder, "street_game.png"), 1280, 720);

        camera.transform.SetPositionAndRotation(new Vector3(-16f, 14f, 10f), Quaternion.Euler(35f, 58f, 0f));
        Shot(camera, Path.Combine(outFolder, "street_high.png"), 1280, 720);
    }

    private static void Shot(Camera camera, string path, int width, int height)
    {
        var target = RenderTexture.GetTemporary(width, height, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(target);
    }

    // Command-line entry: import, create prefabs, quit.
    public static void BatchCreate()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CreatePrefabs();
    }
}
