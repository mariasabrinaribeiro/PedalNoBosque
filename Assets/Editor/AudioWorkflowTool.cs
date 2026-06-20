using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AudioWorkflowTool : EditorWindow
{
    private AudioManager audioManager;
    private int selectedSoundIndex = 0;
    private AudioTrigger.TriggerEvent selectedTriggerType = AudioTrigger.TriggerEvent.OnStart;
    private string selectedTagFilter = "Carro";
    private Vector2 scrollPos;

    [MenuItem("Window/Diagnostics/Audio Workflow Tool")]
    public static void ShowWindow()
    {
        GetWindow<AudioWorkflowTool>("Audio Workflow Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gerenciador e Vinculador de Áudio (Audio Workflow Tool)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        audioManager = FindObjectOfType<AudioManager>();

        if (audioManager == null)
        {
            EditorGUILayout.HelpBox("Nenhum AudioManager encontrado na cena ativa.", MessageType.Warning);
            if (GUILayout.Button("Criar AudioManager na Cena", GUILayout.Height(30)))
            {
                GameObject go = new GameObject("AudioManager");
                audioManager = go.AddComponent<AudioManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
                Debug.Log("[Audio Workflow] AudioManager criado com sucesso na cena.");
            }
            return;
        }

        // 1. Sound Events Table
        GUILayout.Label("Tabela de Eventos de Som (Nome vs AudioClip):", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(250));
        
        Undo.RecordObject(audioManager, "Modify AudioManager Sound Events");

        int removeIndex = -1;
        for (int i = 0; i < audioManager.soundEvents.Count; i++)
        {
            var sEvent = audioManager.soundEvents[i];

            EditorGUILayout.BeginHorizontal();
            
            string newName = EditorGUILayout.TextField(sEvent.name, GUILayout.Width(150));
            AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(sEvent.clip, typeof(AudioClip), false);

            if (newName != sEvent.name || newClip != sEvent.clip)
            {
                sEvent.name = newName;
                sEvent.clip = newClip;
                audioManager.soundEvents[i] = sEvent;
                EditorUtility.SetDirty(audioManager);
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1)
        {
            audioManager.soundEvents.RemoveAt(removeIndex);
            EditorUtility.SetDirty(audioManager);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Adicionar Novo Evento de Som", GUILayout.Height(25)))
        {
            audioManager.soundEvents.Add(new AudioManager.SoundEvent { name = "NovoSom", clip = null });
            EditorUtility.SetDirty(audioManager);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // 2. Click & Bind System
        GUILayout.Label("Sistema 'Click & Bind' (Vincular Som à Hierarquia):", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (audioManager.soundEvents.Count == 0)
        {
            EditorGUILayout.HelpBox("Cadastre pelo menos um evento de som acima para poder vincular aos objetos.", MessageType.Info);
            return;
        }

        // Prepare sound names array for dropdown
        string[] soundNames = new string[audioManager.soundEvents.Count];
        for (int i = 0; i < audioManager.soundEvents.Count; i++)
        {
            soundNames[i] = string.IsNullOrEmpty(audioManager.soundEvents[i].name) ? $"(Sem Nome #{i})" : audioManager.soundEvents[i].name;
        }

        selectedSoundIndex = Mathf.Clamp(selectedSoundIndex, 0, soundNames.Length - 1);
        selectedSoundIndex = EditorGUILayout.Popup("Som Selecionado", selectedSoundIndex, soundNames);
        selectedTriggerType = (AudioTrigger.TriggerEvent)EditorGUILayout.EnumPopup("Tipo de Gatilho", selectedTriggerType);
        
        if (selectedTriggerType == AudioTrigger.TriggerEvent.OnTriggerEnter2D || selectedTriggerType == AudioTrigger.TriggerEvent.OnCollisionEnter2D)
        {
            selectedTagFilter = EditorGUILayout.TextField("Tag Filtro", selectedTagFilter);
        }

        EditorGUILayout.Space();

        GameObject activeGo = Selection.activeGameObject;
        if (activeGo == null)
        {
            EditorGUILayout.HelpBox("Selecione um GameObject na Hierarquia para vinculação.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Objeto Selecionado:", activeGo.name, EditorStyles.boldLabel);
        }

        EditorGUI.BeginDisabledGroup(activeGo == null);
        if (GUILayout.Button("Vincular Som ao Objeto Selecionado", GUILayout.Height(35)))
        {
            string soundToBind = soundNames[selectedSoundIndex];

            // Add AudioTrigger component using Undo system to support Ctrl+Z
            AudioTrigger trigger = Undo.AddComponent<AudioTrigger>(activeGo);
            trigger.soundName = soundToBind;
            trigger.triggerType = selectedTriggerType;
            trigger.tagFilter = selectedTagFilter;

            EditorUtility.SetDirty(activeGo);
            
            // Mark active scene as dirty if not playing
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }

            Debug.Log($"[Audio Workflow] Sucesso! Adicionado AudioTrigger ao objeto '{activeGo.name}' com o som '{soundToBind}' (Gatilho: {selectedTriggerType}).");
        }
        EditorGUI.EndDisabledGroup();
    }
}
