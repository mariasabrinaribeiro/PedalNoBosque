using UnityEngine;

/// <summary>
/// Shared character orientation & anti-jitter facing utility for Orla das Fadas:
/// 1. Auto-detects the true local forward Z sign (+1 or -1) of any character model by inspecting
///    its Eyes/Nose relative to its Head/Torso in Unity local space (eliminating 180° backwards-facing bugs!).
/// 2. Enforces applyRootMotion = false on any Animator so Root Motion never fights the movement controller.
/// 3. Orients characters strictly by their REAL planar displacement with deadzone tolerance, preserving
///    the last valid heading whenever stopped or pressed against a wall (zero jitter / zero flipping!).
/// </summary>
public static class GirlPowerOrla3DCharacterFacing
{
    private static readonly string[] EyeCandidates =
    {
        "Protag_EyeL", "Protag_EyeR", "EyeL", "EyeR", "P_EyeL", "DogEyeL", "CatEyeL", "GullEyeL"
    };

    private static readonly string[] HeadCandidates =
    {
        "Protag_Head", "Head", "Pipoca_Head", "Cat_Head", "Protag_Torso", "Torso"
    };

    /// <summary>
    /// Measures whether the character's face points along local +Z (+1f, Unity standard) or local -Z (-1f).
    /// All Orla das Fadas models are standardized to +Z forward on export; runtime verification preserves safety.
    /// </summary>
    public static float DetectFaceForwardSign(Transform visualRoot, float fallbackSign = 1f)
    {
        if (visualRoot == null) return fallbackSign;

        Transform eye = FindAnyChild(visualRoot, EyeCandidates);
        Transform head = FindAnyChild(visualRoot, HeadCandidates);

        if (eye != null)
        {
            Vector3 eyeLocal = visualRoot.InverseTransformPoint(GetVisualCenter(eye));
            Vector3 refLocal = head != null
                ? visualRoot.InverseTransformPoint(GetVisualCenter(head))
                : Vector3.zero;
            float dz = eyeLocal.z - refLocal.z;
            if (Mathf.Abs(dz) > 0.015f)
                return dz > 0f ? 1f : -1f;
        }

        // Fallback: inspect combined mesh bounds of any child containing "Eye" or "Nose" or "Snout"
        MeshRenderer[] renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
        Vector3 headCenter = Vector3.zero;
        bool hasHead = false;
        Vector3 eyeCenter = Vector3.zero;
        bool hasEye = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            string n = renderers[i].name;
            if (!hasEye && (n.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Nose", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Snout", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                eyeCenter = visualRoot.InverseTransformPoint(renderers[i].bounds.center);
                hasEye = true;
            }
            else if (!hasHead && (n.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Skull", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Torso", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                headCenter = visualRoot.InverseTransformPoint(renderers[i].bounds.center);
                hasHead = true;
            }
        }
        if (hasEye)
        {
            float dz = eyeCenter.z - (hasHead ? headCenter.z : 0f);
            if (Mathf.Abs(dz) > 0.015f)
                return dz > 0f ? 1f : -1f;
        }

        return fallbackSign;
    }

    /// <summary>
    /// Ensures Root Motion is disabled on all Animator components under root so the controller owns movement.
    /// </summary>
    public static void DisableRootMotion(Transform root)
    {
        if (root == null) return;
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
            if (animators[i] != null)
                animators[i].applyRootMotion = false;
    }

    /// <summary>
    /// Computes the target world yaw rotation so the character's FACE aligns with worldMoveDir.
    /// Returns false (preserving last valid orientation) when planar movement is below deadzone tolerance.
    /// </summary>
    public static bool TryGetFaceRotation(Vector3 worldMoveDir, float faceForwardSign, float minSqrMagnitude, out Quaternion targetRotation)
    {
        Vector3 flat = new Vector3(worldMoveDir.x, 0f, worldMoveDir.z);
        if (flat.sqrMagnitude < minSqrMagnitude)
        {
            targetRotation = Quaternion.identity;
            return false;
        }
        Vector3 lookVec = flat.normalized * (faceForwardSign >= 0f ? 1f : -1f);
        targetRotation = Quaternion.LookRotation(lookVec, Vector3.up);
        return true;
    }

    private static Vector3 GetVisualCenter(Transform tr)
    {
        Renderer r = tr.GetComponent<Renderer>();
        return r != null ? r.bounds.center : tr.position;
    }

    private static Transform FindAnyChild(Transform root, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform hit = FindRecursive(root, names[i]);
            if (hit != null) return hit;
        }
        return null;
    }

    private static Transform FindRecursive(Transform root, string targetName)
    {
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindRecursive(root.GetChild(i), targetName);
            if (hit != null) return hit;
        }
        return null;
    }
}
