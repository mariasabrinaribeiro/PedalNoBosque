using UnityEngine;
using UnityEditor;

public class PixelArtTextureOptimizer : EditorWindow
{
    [MenuItem("Window/Diagnostics/Optimize Pixel Art Textures")]
    public static void OptimizeTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Pula pacotes externos e arquivos do TextMesh Pro
            if (path.StartsWith("Packages/") || path.Contains("TextMesh Pro"))
                continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool modified = false;

                // 1. Define Filter Mode como Point (no filter) para Pixel Art nítido
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    modified = true;
                }

                // 2. Garante que texturas na pasta de arte sejam do tipo Sprite
                string lowerPath = path.ToLower();
                if (lowerPath.Contains("art") || lowerPath.Contains("sprite") || lowerPath.Contains("vehicle") || lowerPath.Contains("tiles"))
                {
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }
                }

                // 3. Define formato ideal de compressão para Android (ASTC 4x4)
                TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                if (!androidSettings.overridden || 
                    androidSettings.format != TextureImporterFormat.ASTC_4x4 || 
                    androidSettings.compressionQuality != (int)TextureCompressionQuality.Best)
                {
                    androidSettings.overridden = true;
                    androidSettings.format = TextureImporterFormat.ASTC_4x4;
                    androidSettings.compressionQuality = (int)TextureCompressionQuality.Best;
                    importer.SetPlatformTextureSettings(androidSettings);
                    modified = true;
                }

                if (modified)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        EditorUtility.DisplayDialog("Otimizador de Pixel Art", 
            $"Otimização concluída!\n\n{count} texturas foram reconfiguradas com Point Filter (Pixel Art) e formato de alta qualidade ASTC para Android.", 
            "OK");
    }
}
