using System.IO;
using UnityEditor;
using UnityEngine;

// Restyles what is around the Meshy buildings so it matches them: a cobblestone road, paved sidewalks,
// grass instead of flat green, and hazy distant buildings in place of the starter skyline blocks.
// The textures are generated here (tileable), so there are no source images to keep.
public static class MeshyStreet
{
    private const string Folder = "Assets/MathRunner/Art/Street/";
    private const string PrefabFolder = "Assets/MathRunner/Art/Prefabs/";
    private const string MeshyFolder = "Assets/MathRunner/Art/Meshy/";

    public static void Create()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();

        // Sizes match the starter pieces so the track code keeps working: width, height, length in meters.
        Surface("road_segment", Cobble(), new Vector3(6f, 0.1f, 12f), 3f);
        Surface("sidewalk_segment", Paving(), new Vector3(2f, 0.36f, 12f), 2f);
        Surface("ground_fill", Grass(), new Vector3(12f, 0.05f, 12f), 4f);

        Skyline("10_skyline_a", "apartment_tall", 1.6f);
        Skyline("11_skyline_b", "building_corner", 1.7f);
    }

    // ---- Surfaces ----------------------------------------------------------------------------

    private static void Surface(string key, Texture2D texture, Vector3 size, float tileMeters)
    {
        var material = Material(key, texture);
        material.SetTextureScale("_BaseMap", new Vector2(size.x / tileMeters, size.z / tileMeters));
        EditorUtility.SetDirty(material);

        var root = new GameObject(key);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        cube.name = "Model";
        cube.transform.SetParent(root.transform, false);
        cube.transform.localScale = size;
        cube.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
        cube.GetComponent<Renderer>().sharedMaterial = material;
        Save(root, key);
    }

    private static void Skyline(string key, string fbx, float scale)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyFolder + fbx + ".fbx");
        if (asset == null) return;

        // Same texture as the near buildings, pulled toward the sky color so they read as far away.
        var material = Material(key, AssetDatabase.LoadAssetAtPath<Texture2D>($"{MeshyFolder}{fbx}.fbm/{fbx}_albedo.jpg"));
        material.SetColor("_BaseColor", new Color(0.74f, 0.8f, 0.92f));
        EditorUtility.SetDirty(material);

        var root = new GameObject(key);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
        model.name = "Model";
        model.transform.localScale = Vector3.one * scale;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Save(root, key);
    }

    private static Material Material(string name, Texture2D texture)
    {
        var path = $"{Folder}{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.05f);
        material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static void Save(GameObject root, string key)
    {
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}{key}.prefab");
        Object.DestroyImmediate(root);
    }

    // ---- Generated textures ------------------------------------------------------------------

    private static Texture2D Cobble()
    {
        const int size = 512, cells = 12;
        var pixels = new Color[size * size];
        var cell = size / (float)cells;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var row = Mathf.FloorToInt(y / cell);
            var shifted = x + (row % 2 == 0 ? 0f : cell * 0.5f);
            var column = Mathf.FloorToInt(shifted / cell) % cells;
            var local = new Vector2(shifted % cell / cell - 0.5f, y % cell / cell - 0.5f);
            var rounded = Mathf.Pow(Mathf.Abs(local.x) * 2f, 3f) + Mathf.Pow(Mathf.Abs(local.y) * 2f, 3f);
            var tint = 0.78f + 0.3f * Hash(column, row);
            var stone = new Color(0.46f, 0.47f, 0.52f) * tint;
            var shade = 1f - 0.18f * Mathf.Clamp01((rounded - 0.45f) * 2f);
            var color = rounded < 0.86f ? stone * shade : new Color(0.2f, 0.21f, 0.25f);
            color.a = 1f;
            pixels[y * size + x] = color;
        }

        return Bake("road_cobble", pixels, size);
    }

    private static Texture2D Paving()
    {
        const int size = 256, slabs = 2;
        var pixels = new Color[size * size];
        var slab = size / (float)slabs;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var sx = Mathf.FloorToInt(x / slab);
            var sy = Mathf.FloorToInt(y / slab);
            var edge = Mathf.Min(Mathf.Min(x % slab, slab - x % slab), Mathf.Min(y % slab, slab - y % slab));
            var tint = 0.9f + 0.14f * Hash(sx, sy);
            var color = edge < 3f ? new Color(0.48f, 0.5f, 0.55f) : new Color(0.72f, 0.73f, 0.77f) * tint;
            color.a = 1f;
            pixels[y * size + x] = color;
        }

        return Bake("sidewalk_paving", pixels, size);
    }

    private static Texture2D Grass()
    {
        const int size = 256;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            // Sums of whole-period waves repeat exactly, so neighbouring tiles join without a seam.
            var u = x / (float)size * Mathf.PI * 2f;
            var v = y / (float)size * Mathf.PI * 2f;
            var n = Mathf.Sin(u * 2f + Mathf.Sin(v * 3f)) * 0.5f + Mathf.Sin(v * 2f + u * 3f) * 0.3f + Mathf.Sin((u + v) * 5f) * 0.2f;
            var k = n * 0.5f + 0.5f;
            var color = Color.Lerp(new Color(0.36f, 0.56f, 0.27f), new Color(0.5f, 0.68f, 0.32f), k);
            color.a = 1f;
            pixels[y * size + x] = color;
        }

        return Bake("ground_grass", pixels, size);
    }

    private static float Hash(int a, int b)
    {
        unchecked
        {
            var h = a * 73856093 ^ b * 19349663;
            return ((h & 0x7fffffff) % 1000) / 1000f;
        }
    }

    private static Texture2D Bake(string name, Color[] pixels, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        texture.SetPixels(pixels);
        texture.Apply();
        var path = $"{Folder}{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
