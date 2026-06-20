using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UIHierarchySanitizer : EditorWindow
{
    private enum OperationMode { Selection, AllCanvas }
    private OperationMode mode = OperationMode.Selection;
    private bool roundCoordinates = true;
    private float maxDistanceThreshold = 5000f;
    private Vector2 scrollPos;

    [MenuItem("Window/Diagnostics/UI Hierarchy Sanitizer")]
    public static void ShowWindow()
    {
        GetWindow<UIHierarchySanitizer>("UI Sanitizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sanitizador de Hierarquia e Escala de UI", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        mode = (OperationMode)EditorGUILayout.EnumPopup("Modo de Operação", mode);
        roundCoordinates = EditorGUILayout.Toggle("Arredondar Posições X/Y", roundCoordinates);
        maxDistanceThreshold = EditorGUILayout.FloatField("Limite Máximo de Posição", maxDistanceThreshold);

        EditorGUILayout.Space();

        if (GUILayout.Button("Executar Sanitização", GUILayout.Height(35)))
        {
            SanitizeUI();
        }
    }

    private void SanitizeUI()
    {
        List<RectTransform> targets = new List<RectTransform>();

        if (mode == OperationMode.Selection)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (!targets.Contains(rt)) targets.Add(rt);
                    
                    // Add all child RectTransforms recursively as well
                    RectTransform[] children = go.GetComponentsInChildren<RectTransform>(true);
                    foreach (var child in children)
                    {
                        if (!targets.Contains(child))
                        {
                            targets.Add(child);
                        }
                    }
                }
            }
            
            if (targets.Count == 0)
            {
                Debug.LogWarning("[UI Sanitizer] Nenhum objeto com RectTransform selecionado na Hierarquia.");
                return;
            }
        }
        else
        {
            // All Canvas mode
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                RectTransform[] children = canvas.GetComponentsInChildren<RectTransform>(true);
                foreach (RectTransform rt in children)
                {
                    if (!targets.Contains(rt))
                    {
                        targets.Add(rt);
                    }
                }
            }

            if (targets.Count == 0)
            {
                Debug.LogWarning("[UI Sanitizer] Nenhum Canvas ou RectTransform encontrado na cena ativa.");
                return;
            }
        }

        int modifiedCount = 0;
        foreach (RectTransform rt in targets)
        {
            bool modified = false;

            // Sanity validations
            // 1. Negative scale check
            if (rt.localScale.x < 0f || rt.localScale.y < 0f || rt.localScale.z < 0f)
            {
                Debug.LogWarning($"[UI Sanitizer] AVISO: Elemento '{rt.gameObject.name}' tem escala negativa: {rt.localScale}. Verifique manualmente!", rt.gameObject);
            }

            // 2. Position out of bounds check
            if (Mathf.Abs(rt.anchoredPosition.x) > maxDistanceThreshold || Mathf.Abs(rt.anchoredPosition.y) > maxDistanceThreshold)
            {
                Debug.LogWarning($"[UI Sanitizer] AVISO: Elemento '{rt.gameObject.name}' está posicionado muito fora do Canvas (X: {rt.anchoredPosition.x}, Y: {rt.anchoredPosition.y}). Verifique se está correto!", rt.gameObject);
            }

            // Register undo for the RectTransform to support Cmd+Z
            Undo.RecordObject(rt, "Sanitize UI Transform");

            // Apply corrections
            // A. Scale correction to (1, 1, 1)
            if (rt.localScale != Vector3.one)
            {
                rt.localScale = Vector3.one;
                modified = true;
            }

            // B. Z axis correction to 0
            if (rt.anchoredPosition3D.z != 0f)
            {
                Vector3 pos3D = rt.anchoredPosition3D;
                pos3D.z = 0f;
                rt.anchoredPosition3D = pos3D;
                modified = true;
            }

            // C. Coordinate rounding (X, Y)
            if (roundCoordinates)
            {
                Vector2 pos2D = rt.anchoredPosition;
                float rx = Mathf.Round(pos2D.x);
                float ry = Mathf.Round(pos2D.y);

                if (pos2D.x != rx || pos2D.y != ry)
                {
                    rt.anchoredPosition = new Vector2(rx, ry);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(rt);
                modifiedCount++;
            }
        }

        Debug.Log($"[UI Sanitizer] Concluído! Varridos {targets.Count} elementos. {modifiedCount} elementos foram corrigidos com sucesso.");
        
        // Mark active scene as dirty if any changes were made
        if (modifiedCount > 0 && !Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
