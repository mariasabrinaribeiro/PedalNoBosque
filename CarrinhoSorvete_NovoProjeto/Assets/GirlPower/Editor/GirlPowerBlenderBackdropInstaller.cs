using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GirlPowerBlenderBackdropInstaller
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Primeira Entrega.unity";
    private const string ArtPath = "Assets/GirlPower/Blender/BlenderOrlaBackdrop.png";
    private const string BackdropName = "Arte de fundo - orla de Matinhos ao entardecer";

    [MenuItem("Girl Power/Apply Blender Coastal Backdrop to First Delivery")]
    public static void Apply()
    {
        AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Blender backdrop texture could not be imported: " + ArtPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();

        Sprite backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (backdropSprite == null)
        {
            Debug.LogError("Blender backdrop sprite could not be loaded: " + ArtPath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject backdrop = GameObject.Find(BackdropName);
        if (backdrop == null)
        {
            Debug.LogError("Could not find existing coastal backdrop object in " + ScenePath);
            return;
        }

        SpriteRenderer renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogError("Existing coastal backdrop has no SpriteRenderer.");
            return;
        }

        renderer.sprite = backdropSprite;
        renderer.color = Color.white;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied Blender-rendered pastel coastal panorama to Girl Power - Primeira Entrega.");
    }
}

