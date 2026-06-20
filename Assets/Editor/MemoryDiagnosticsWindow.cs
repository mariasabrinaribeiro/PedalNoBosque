using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MemoryDiagnosticsWindow : EditorWindow
{
    private List<Texture2D> proceduralTextures = new List<Texture2D>();
    private int lastTextureCount = 0;
    private int growthStreak = 0;
    private float nextCheckTime = 0f;
    private Vector2 scrollPos;

    [MenuItem("Window/Diagnostics/Memory Diagnostics")]
    public static void ShowWindow()
    {
        GetWindow<MemoryDiagnosticsWindow>("Memory Diagnostics");
    }

    private void OnEnable()
    {
        EditorApplication.update += MonitorMemory;
    }

    private void OnDisable()
    {
        EditorApplication.update -= MonitorMemory;
    }

    private void MonitorMemory()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        // Check every 1 second during Playmode
        if (Time.realtimeSinceStartup < nextCheckTime)
        {
            return;
        }
        nextCheckTime = Time.realtimeSinceStartup + 1.0f;

        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        
        proceduralTextures.Clear();
        foreach (var tex in allTextures)
        {
            if (tex == null) continue;

            // Dynamically created textures have no asset path
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
            {
                proceduralTextures.Add(tex);
            }
        }

        int currentProceduralCount = proceduralTextures.Count;

        if (currentProceduralCount > lastTextureCount)
        {
            growthStreak++;
            if (growthStreak >= 10)
            {
                Debug.LogWarning($"[Memory Diagnostics] ALERTA DE VAZAMENTO: O número de texturas geradas dinamicamente (órfãs) cresceu consecutivamente {growthStreak} vezes! Total atual: {currentProceduralCount} texturas. Possível vazamento detectado no jogo!");
            }
        }
        else if (currentProceduralCount < lastTextureCount)
        {
            growthStreak = 0; // reset if count decreases
        }

        lastTextureCount = currentProceduralCount;
        
        // Repaint the window to show live updates
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("Monitor de Memória (Texturas Dinâmicas)", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("O monitoramento em tempo real é ativado durante o Playmode.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Monitorando ativamente no Playmode...", MessageType.Info);
        }

        Texture2D[] totalTex = Resources.FindObjectsOfTypeAll<Texture2D>();
        EditorGUILayout.LabelField("Total de Texturas na Memória (Carregadas + Dinâmicas):", totalTex.Length.ToString());
        EditorGUILayout.LabelField("Texturas Geradas Dinamicamente (Sem Arquivo):", lastTextureCount.ToString());
        EditorGUILayout.LabelField("Sequência de Crescimento Consecutivo:", growthStreak.ToString());

        EditorGUILayout.Space();

        if (GUILayout.Button("Limpar Memória (Unload Unused Assets)", GUILayout.Height(30)))
        {
            Resources.UnloadUnusedAssets();
            Debug.Log("[Memory Diagnostics] Resources.UnloadUnusedAssets() executado manualmente para limpar texturas órfãs.");
        }

        EditorGUILayout.Space();
        GUILayout.Label("Últimas Texturas Dinâmicas Encontradas:", EditorStyles.boldLabel);

        if (proceduralTextures.Count == 0)
        {
            GUILayout.Label("Nenhuma textura dinâmica encontrada na memória no momento.");
        }
        else
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(200));
            
            // Group by name for cleaner display
            Dictionary<string, int> grouped = new Dictionary<string, int>();
            foreach (var tex in proceduralTextures)
            {
                string name = string.IsNullOrEmpty(tex.name) ? "(Sem Nome)" : tex.name;
                if (!grouped.ContainsKey(name)) grouped[name] = 0;
                grouped[name]++;
            }

            foreach (var pair in grouped)
            {
                EditorGUILayout.LabelField($"- {pair.Key} (x{pair.Value} instâncias)");
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
