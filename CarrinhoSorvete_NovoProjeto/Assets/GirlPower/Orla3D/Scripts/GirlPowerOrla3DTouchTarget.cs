using UnityEngine;

/// <summary>
/// Mobile Golden Rule Touch Target (+35% to +40% oversized trigger hitbox)
/// so fingers never miss interactive objects (Fairies, Water Bowls, Tables, Cart, Counters, Orchard).
/// Always attached to an isTrigger child collider so it never blocks character physics.
/// </summary>
public sealed class GirlPowerOrla3DTouchTarget : MonoBehaviour
{
    public enum TargetRole
    {
        FairyWaitress = 0,
        WaterBowl = 1,
        PetTable = 2,
        CartOrKitchen = 3,
        GardenOrchard = 4,
        ShopStall = 5,
        StoryNpc = 6
    }

    public TargetRole role = TargetRole.StoryNpc;
    public int index = -1;
    public string displayHint = "";
    public Transform anchorTransform;

    public Transform Anchor => anchorTransform != null ? anchorTransform : (transform.parent != null ? transform.parent : transform);

    /// <summary>
    /// Creates an oversized (+40%) mobile-friendly trigger sphere on a dedicated child object.
    /// </summary>
    public static GirlPowerOrla3DTouchTarget AttachOversizedHitbox(
        GameObject owner,
        TargetRole role,
        int index,
        Vector3 localCenter,
        float baseVisualRadius,
        string hint = "")
    {
        if (owner == null) return null;

        Transform existing = owner.transform.Find("TouchHitbox_Mobile");
        GameObject hitGo = existing != null ? existing.gameObject : new GameObject("TouchHitbox_Mobile");
        hitGo.transform.SetParent(owner.transform, false);
        hitGo.transform.localPosition = localCenter;
        hitGo.transform.localRotation = Quaternion.identity;
        hitGo.transform.localScale = Vector3.one;

        SphereCollider sc = hitGo.GetComponent<SphereCollider>();
        if (sc == null) sc = hitGo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.center = Vector3.zero;
        // Mobile Golden Rule: +40% larger than visual radius so fingertips never miss!
        sc.radius = Mathf.Max(0.75f, baseVisualRadius * 1.40f);

        GirlPowerOrla3DTouchTarget target = hitGo.GetComponent<GirlPowerOrla3DTouchTarget>();
        if (target == null) target = hitGo.AddComponent<GirlPowerOrla3DTouchTarget>();
        target.role = role;
        target.index = index;
        target.displayHint = hint;
        target.anchorTransform = owner.transform;
        return target;
    }
}
