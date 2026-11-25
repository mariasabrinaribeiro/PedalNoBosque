using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grounds every dynamic character, pet, cart, bench, stall, umbrella, and prop in Baía do Avesso
/// with soft, Animal Crossing-style contact/blob shadows so nothing ever appears to float!
/// </summary>
public sealed class GirlPowerOrla3DContactShadows : MonoBehaviour
{
    [Serializable]
    public struct ShadowEntry
    {
        public Transform target;
        public Vector2 sizeXZ;
        public float yOffset;
        public bool isDynamic;
    }

    public ShadowEntry[] entries;
    public Material serializedBlobMat;
    public Color shadowColor = new Color(0.14f, 0.10f, 0.24f, 0.42f);

    private sealed class ShadowInstance
    {
        public Transform target;
        public Transform quad;
        public Vector2 baseSize;
        public float yOffset;
        public bool isDynamic;
    }

    private readonly List<ShadowInstance> instances = new List<ShadowInstance>();
    private Material blobMat;
    private Mesh quadMesh;

    private void Start()
    {
        if (serializedBlobMat != null)
        {
            blobMat = serializedBlobMat;
        }
        else
        {
            Shader sh = Shader.Find("GirlPower/OrlaBlobShadow");
            if (sh == null) return; // Never fall back to flat opaque white quad!
            blobMat = new Material(sh);
        }
        if (blobMat.HasProperty("_Color")) blobMat.SetColor("_Color", shadowColor);
        if (blobMat.HasProperty("_CurveStrength")) blobMat.SetFloat("_CurveStrength", 0.0008f);

        quadMesh = CreateGroundQuadMesh();

        GameObject container = new GameObject("Sombras_Contato_Container");
        container.transform.SetParent(transform, false);

        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].target == null) continue;
                SpawnShadow(container.transform, entries[i]);
            }
        }
    }

    private void SpawnShadow(Transform parent, ShadowEntry e)
    {
        GameObject go = new GameObject("BlobShadow_" + e.target.name);
        go.transform.SetParent(parent, false);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = quadMesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = blobMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Vector2 sz = e.sizeXZ.sqrMagnitude > 0.01f ? e.sizeXZ : new Vector2(1.1f, 1.1f);
        go.transform.localScale = new Vector3(sz.x, 1f, sz.y);

        ShadowInstance inst = new ShadowInstance
        {
            target = e.target,
            quad = go.transform,
            baseSize = sz,
            yOffset = e.yOffset,
            isDynamic = e.isDynamic
        };
        PositionShadow(inst);
        instances.Add(inst);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < instances.Count; i++)
        {
            ShadowInstance inst = instances[i];
            if (inst.target == null || inst.quad == null) continue;
            bool visible = inst.target.gameObject.activeInHierarchy;
            if (inst.quad.gameObject.activeSelf != visible)
                inst.quad.gameObject.SetActive(visible);
            if (!visible) continue;
            if (inst.isDynamic)
                PositionShadow(inst);
        }
    }

    private static void PositionShadow(ShadowInstance inst)
    {
        Vector3 p = inst.target.position;
        float groundY = SampleGroundY(p.x, p.z);
        float heightAbove = Mathf.Max(0f, p.y - groundY);
        float shrink = Mathf.Clamp(1f - heightAbove * 0.25f, 0.52f, 1.05f);

        // Hide ground shadow slightly when surfing deep water (replace with subtle water silhouette)
        bool inDeepWater = p.z >= 10.2f;
        float scaleBoost = inDeepWater ? 0.85f : 1f;

        inst.quad.position = new Vector3(p.x, groundY + 0.032f + inst.yOffset, p.z);
        inst.quad.localScale = new Vector3(inst.baseSize.x * shrink * scaleBoost, 1f, inst.baseSize.y * shrink * scaleBoost);
    }

    private static float SampleGroundY(float x, float z)
    {
        if (z >= 9.8f) return -0.26f;
        if (x >= 12.6f && x <= 18.6f && z >= 2.6f && z <= 9.8f)
        {
            if (z < 3.4f) return Mathf.Lerp(0f, 0.36f, (z - 2.6f) / 0.8f);
            if (z <= 7.4f) return 0.36f;
            return Mathf.Lerp(0.36f, -0.24f, Mathf.Clamp01((z - 7.4f) / 2.2f));
        }
        if (z < -3.75f) return -0.08f;
        if (z < -3.45f) return Mathf.Lerp(-0.08f, 0f, (z + 3.75f) / 0.30f);
        if (z <= 3.35f) return 0f;
        if (z < 4.10f) return Mathf.Sin(((z - 3.35f) / 0.75f) * Mathf.PI) * 0.48f;
        float beachT = Mathf.Clamp01((z - 4.10f) / 5.70f);
        return Mathf.Lerp(-0.04f, -0.32f, beachT);
    }

    private static Mesh CreateGroundQuadMesh()
    {
        Mesh m = new Mesh { name = "BlobShadowQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f)
        };
        m.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        m.RecalculateNormals();
        return m;
    }
}
