using UnityEngine;

/// <summary>
/// Eliminates camera-to-player building clipping/occlusion by driving screen-space
/// Bayer dither fade in GirlPower/OrlaACNHCozy whenever the player walks behind houses,
/// kiosk awnings, market stalls, or the Pavilion roof!
/// </summary>
public sealed class GirlPowerOrla3DOcclusionFader : MonoBehaviour
{
    public Transform player;
    public Camera targetCamera;
    public Transform[] occluderRoots;
    public float cutoutRadius = 1.45f;

    private static readonly int PropPlayerPos = Shader.PropertyToID("_BaiaPlayerPos");
    private static readonly int PropCamPos = Shader.PropertyToID("_BaiaCamPos");
    private static readonly int PropActive = Shader.PropertyToID("_BaiaOccluderActive");
    private static readonly int PropEnabled = Shader.PropertyToID("_OccluderEnabled");
    private static readonly int PropRadius = Shader.PropertyToID("_OccluderRadius");

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        EnableOccluderPropertyBlocks();
        Shader.SetGlobalFloat(PropActive, 1f);
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(PropActive, 0f);
    }

    private void EnableOccluderPropertyBlocks()
    {
        if (occluderRoots == null) return;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        for (int i = 0; i < occluderRoots.Length; i++)
        {
            Transform root = occluderRoots[i];
            if (root == null) continue;
            foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.GetPropertyBlock(mpb);
                mpb.SetFloat(PropEnabled, 1f);
                mpb.SetFloat(PropRadius, cutoutRadius);
                mr.SetPropertyBlock(mpb);
            }
        }
    }

    private void LateUpdate()
    {
        if (player == null || targetCamera == null)
        {
            Shader.SetGlobalFloat(PropActive, 0f);
            return;
        }

        Vector3 focus = player.position + Vector3.up * 0.92f;
        Shader.SetGlobalVector(PropPlayerPos, new Vector4(focus.x, focus.y, focus.z, 1f));
        Vector3 cp = targetCamera.transform.position;
        Shader.SetGlobalVector(PropCamPos, new Vector4(cp.x, cp.y, cp.z, 1f));
        Shader.SetGlobalFloat(PropActive, 1f);
    }
}
