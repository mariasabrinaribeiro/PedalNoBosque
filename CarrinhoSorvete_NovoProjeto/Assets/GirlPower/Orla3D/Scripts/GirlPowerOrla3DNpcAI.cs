using UnityEngine;

/// <summary>
/// Brings the coastal village and ecology of Baía do Avesso to life:
/// - Strolling NPCs walking along the promenade, pausing to browse stalls & waving happily when the player passes nearby!
/// - Perched beach seagulls that startle and take flight into the sky when you sprint near them!
/// - Swimming tropical 3D fish (FishProp.fbx) darting gently through the shallow turquoise bay!
/// </summary>
public sealed class GirlPowerOrla3DNpcAI : MonoBehaviour
{
    public Transform player;
    public Transform[] strollingNpcs;
    public Transform[] beachSeagulls;
    public Transform[] shallowFish;

    private Vector3[] npcOrigin;
    private float[] npcPhase;
    private float[] npcWaveTimer;

    private Vector3[] gullBase;
    private float[] gullFlyTimer;

    private Vector3[] fishBase;

    private void Start()
    {
        if (strollingNpcs != null)
        {
            npcOrigin = new Vector3[strollingNpcs.Length];
            npcPhase = new float[strollingNpcs.Length];
            npcWaveTimer = new float[strollingNpcs.Length];
            for (int i = 0; i < strollingNpcs.Length; i++)
            {
                if (strollingNpcs[i] != null) npcOrigin[i] = strollingNpcs[i].position;
                npcPhase[i] = i * 1.9f;
            }
        }

        if (beachSeagulls != null)
        {
            gullBase = new Vector3[beachSeagulls.Length];
            gullFlyTimer = new float[beachSeagulls.Length];
            for (int i = 0; i < beachSeagulls.Length; i++)
                if (beachSeagulls[i] != null) gullBase[i] = beachSeagulls[i].position;
        }

        if (shallowFish != null)
        {
            fishBase = new Vector3[shallowFish.Length];
            for (int i = 0; i < shallowFish.Length; i++)
                if (shallowFish[i] != null) fishBase[i] = shallowFish[i].position;
        }
    }

    private void Update()
    {
        float t = Time.time;
        float dt = Time.deltaTime;

        UpdateStrollingNpcs(t, dt);
        UpdateStartledSeagulls(t, dt);
        UpdateShallowFish(t);
    }

    private void UpdateStrollingNpcs(float t, float dt)
    {
        if (strollingNpcs == null) return;
        for (int i = 0; i < strollingNpcs.Length; i++)
        {
            Transform npc = strollingNpcs[i];
            if (npc == null || !npc.gameObject.activeInHierarchy) continue;

            // Friendly wave/hop when player strolls near!
            if (player != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(npc.position.x, npc.position.z));
                if (d < 3.0f)
                {
                    Vector3 look = new Vector3(player.position.x - npc.position.x, 0f, player.position.z - npc.position.z);
                    if (look.sqrMagnitude > 0.01f)
                        npc.rotation = Quaternion.Slerp(npc.rotation, Quaternion.LookRotation(-look.normalized, Vector3.up), dt * 6f);
                    float waveBob = Mathf.Abs(Mathf.Sin(t * 7.5f + i)) * 0.14f;
                    npc.position = new Vector3(npc.position.x, npcOrigin[i].y + waveBob, npc.position.z);
                    continue;
                }
            }

            // Gentle patrol with natural window-shopping pauses!
            float cycle = Mathf.Sin(t * 0.34f + npcPhase[i]);
            float pauseMask = Mathf.Abs(Mathf.Cos(t * 0.34f + npcPhase[i])) > 0.25f ? 1f : 0f;
            float targetX = npcOrigin[i].x + cycle * 3.4f;
            float hop = pauseMask * Mathf.Abs(Mathf.Sin(t * 6.5f + i)) * 0.07f;

            Vector3 prev = npc.position;
            Vector3 next = new Vector3(Mathf.Lerp(prev.x, targetX, dt * 2.2f), npcOrigin[i].y + hop, npcOrigin[i].z);
            npc.position = next;

            Vector3 vel = new Vector3(next.x - prev.x, 0f, next.z - prev.z);
            if (vel.sqrMagnitude > 0.00002f)
                npc.rotation = Quaternion.Slerp(npc.rotation, Quaternion.LookRotation(-vel.normalized, Vector3.up), dt * 6f);
        }
    }

    private void UpdateStartledSeagulls(float t, float dt)
    {
        if (beachSeagulls == null) return;
        for (int i = 0; i < beachSeagulls.Length; i++)
        {
            Transform g = beachSeagulls[i];
            if (g == null) continue;

            if (gullFlyTimer[i] <= 0f && player != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(g.position.x, g.position.z));
                if (d < 3.4f)
                    gullFlyTimer[i] = 4.2f; // Take flight!
            }

            if (gullFlyTimer[i] > 0f)
            {
                gullFlyTimer[i] = Mathf.Max(0f, gullFlyTimer[i] - dt);
                float p = 1f - (gullFlyTimer[i] / 4.2f);
                float arcY = Mathf.Sin(p * Mathf.PI) * 5.2f;
                float circleX = Mathf.Sin(p * Mathf.PI * 2f) * 4.5f;
                float circleZ = Mathf.Cos(p * Mathf.PI * 2f) * 2.8f;
                g.position = gullBase[i] + new Vector3(circleX, arcY, circleZ);
                g.Rotate(0f, 110f * dt, 0f, Space.World);
            }
            else
            {
                // Cozy pecking/bobbing on the sand
                float peck = Mathf.Sin(t * 3.0f + i * 2.1f) > 0.85f ? 18f : 0f;
                g.position = gullBase[i];
                g.localRotation = Quaternion.Euler(peck, i * 65f, 0f);
            }
        }
    }

    private void UpdateShallowFish(float t)
    {
        if (shallowFish == null) return;
        for (int i = 0; i < shallowFish.Length; i++)
        {
            Transform f = shallowFish[i];
            if (f == null) continue;
            float ang = t * (0.75f + (i % 3) * 0.2f) + i * 1.6f;
            float rx = 2.4f + (i % 2) * 1.1f;
            float rz = 1.0f;
            Vector3 pos = fishBase[i] + new Vector3(Mathf.Cos(ang) * rx, Mathf.Sin(t * 2.4f + i) * 0.05f, Mathf.Sin(ang) * rz);
            Vector3 vel = new Vector3(-Mathf.Sin(ang) * rx, 0f, Mathf.Cos(ang) * rz);
            f.position = pos;
            if (vel.sqrMagnitude > 0.001f)
                f.rotation = Quaternion.LookRotation(-vel.normalized, Vector3.up);
        }
    }
}
