using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brings the coastal village, magical fairies, and ecology of Baía do Avesso to life:
/// - Waypoint Tourists strolling along the promenade, pausing ~5s near stalls/benches to browse & chat!
/// - Floating Fairy Waitresses ("Garçonetes Fadas") hovering smoothly with fluttering wings & sparkle trails
///   flying between the Café das Fadas, Quiosque, and Beach/Restaurant tables!
/// - Floating magical lanterns & distant gliding sailboats on the ocean horizon!
/// - Startled beach seagulls & swimming tropical 3D fish!
/// </summary>
public sealed class GirlPowerOrla3DNpcAI : MonoBehaviour
{
    public Transform player;
    public Transform[] strollingNpcs;
    public Transform[] fairyWaitresses;
    public Transform[] floatingLanterns;
    public Transform[] distantSailboats;
    public Transform[] beachSeagulls;
    public Transform[] shallowFish;

    private sealed class WaypointWalker
    {
        public Transform tr;
        public Vector3[] route;
        public int wpIdx;
        public float waitTimer;
        public float speed;
        public float baseY;
    }

    private sealed class FairyAgent
    {
        public Transform root;
        public Transform wingL;
        public Transform wingR;
        public Vector3[] stops;
        public int stopIdx;
        public float serveWait;
        public float phase;
        public float sparkleClock;
    }

    private sealed class SparkleDust
    {
        public GameObject go;
        public float age;
        public float life;
        public Vector3 vel;
    }

    private readonly List<WaypointWalker> walkers = new List<WaypointWalker>();
    private readonly List<FairyAgent> fairies = new List<FairyAgent>();
    private readonly List<SparkleDust> sparkles = new List<SparkleDust>(48);
    private readonly Stack<SparkleDust> sparklePool = new Stack<SparkleDust>(48);
    private Material sparkleMat;
    private Transform sparkleContainer;

    private Vector3[] lanternBase;
    private Vector3[] sailboatBase;
    private Vector3[] gullBase;
    private float[] gullFlyTimer;
    private Vector3[] fishBase;

    private void Start()
    {
        SetupSparklePool();
        InitWaypointWalkers();
        InitFairyWaitresses();

        if (floatingLanterns != null)
        {
            lanternBase = new Vector3[floatingLanterns.Length];
            for (int i = 0; i < floatingLanterns.Length; i++)
                if (floatingLanterns[i] != null) lanternBase[i] = floatingLanterns[i].position;
        }

        if (distantSailboats != null)
        {
            sailboatBase = new Vector3[distantSailboats.Length];
            for (int i = 0; i < distantSailboats.Length; i++)
                if (distantSailboats[i] != null) sailboatBase[i] = distantSailboats[i].position;
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

    private void SetupSparklePool()
    {
        GameObject cont = new GameObject("Fadas_Brilho_Particulas");
        cont.transform.SetParent(transform, false);
        sparkleContainer = cont.transform;
        Shader sh = Shader.Find("GirlPower/OrlaACNHCozy");
        if (sh == null) sh = Shader.Find("Standard");
        sparkleMat = new Material(sh);
        if (sparkleMat.HasProperty("_Color")) sparkleMat.SetColor("_Color", new Color(1.0f, 0.90f, 0.56f, 1f));
        if (sparkleMat.HasProperty("_EmissionColor")) sparkleMat.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.55f) * 1.4f);
        if (sparkleMat.HasProperty("_CurveStrength")) sparkleMat.SetFloat("_CurveStrength", 0.0008f);
    }

    private void InitWaypointWalkers()
    {
        if (strollingNpcs == null) return;
        Vector3[][] routes =
        {
            new[] { new Vector3(-9.5f, 0f, -1.6f), new Vector3(-2.5f, 0f, -1.5f), new Vector3(4.5f, 0f, -1.9f), new Vector3(-5.5f, 0f, -2.4f) },
            new[] { new Vector3(9.5f, 0f, -1.6f), new Vector3(16.8f, 0f, -1.5f), new Vector3(22.5f, 0f, -1.8f), new Vector3(13.2f, 0f, -2.3f) },
            new[] { new Vector3(-13.5f, 0f, -2.0f), new Vector3(-6.8f, 0f, 0.9f), new Vector3(0.5f, 0f, -2.2f), new Vector3(-10.5f, 0f, -2.1f) },
            new[] { new Vector3(26.5f, 0f, -1.7f), new Vector3(32.5f, 0f, -1.6f), new Vector3(28.0f, 0f, 0.8f), new Vector3(22.5f, 0f, -1.9f) }
        };

        for (int i = 0; i < strollingNpcs.Length; i++)
        {
            Transform npc = strollingNpcs[i];
            if (npc == null) continue;
            walkers.Add(new WaypointWalker
            {
                tr = npc,
                route = routes[i % routes.Length],
                wpIdx = i % 4,
                waitTimer = 1.5f + i * 1.2f,
                speed = 1.65f + (i % 2) * 0.25f,
                baseY = npc.position.y
            });
        }
    }

    private void InitFairyWaitresses()
    {
        if (fairyWaitresses == null) return;
        Vector3[][] fairyRoutes =
        {
            // Fairy 1: Flies between Café das Fadas interior -> Kiosk -> Beach Umbrellas!
            new[] { new Vector3(5.6f, 0.65f, -7.8f), new Vector3(5.6f, 0.72f, -4.2f), new Vector3(0.4f, 0.75f, 0.6f), new Vector3(-1.4f, 0.62f, 6.0f), new Vector3(4.8f, 0.68f, 5.8f) },
            // Fairy 2: Flies between Batata & Mar Restaurant -> Restaurant Tables -> Promenade!
            new[] { new Vector3(-14.0f, 0.72f, -4.6f), new Vector3(-11.2f, 0.68f, -1.6f), new Vector3(-15.2f, 0.68f, -1.6f), new Vector3(-7.4f, 0.76f, 0.8f) },
            // Fairy 3: Flies between Florist -> Feira -> Beach Pavilion & East Table!
            new[] { new Vector3(24.0f, 0.74f, 0.6f), new Vector3(31.8f, 0.68f, -1.5f), new Vector3(21.6f, 0.64f, 5.9f), new Vector3(15.6f, 0.82f, 4.4f) }
        };

        for (int i = 0; i < fairyWaitresses.Length; i++)
        {
            Transform f = fairyWaitresses[i];
            if (f == null) continue;
            fairies.Add(new FairyAgent
            {
                root = f,
                wingL = FindChildRec(f, "Fairy_WingL"),
                wingR = FindChildRec(f, "Fairy_WingR"),
                stops = fairyRoutes[i % fairyRoutes.Length],
                stopIdx = 0,
                serveWait = 1.0f + i * 0.8f,
                phase = i * 2.1f
            });
        }
    }

    private static Transform FindChildRec(Transform root, string n)
    {
        if (root.name == n) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindChildRec(root.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }

    private void Update()
    {
        float t = Time.time;
        float dt = Time.deltaTime;

        UpdateWaypointWalkers(t, dt);
        UpdateFairyWaitresses(t, dt);
        UpdateFairySparkles(dt);
        UpdateFloatingLanterns(t);
        UpdateDistantSailboats(t);
        UpdateStartledSeagulls(t, dt);
        UpdateShallowFish(t);
    }

    private void UpdateWaypointWalkers(float t, float dt)
    {
        for (int i = 0; i < walkers.Count; i++)
        {
            WaypointWalker w = walkers[i];
            if (w.tr == null || !w.tr.gameObject.activeInHierarchy) continue;

            // Friendly wave & greeting when player walks near!
            if (player != null)
            {
                float dPlayer = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(w.tr.position.x, w.tr.position.z));
                if (dPlayer < 2.8f)
                {
                    Vector3 look = new Vector3(player.position.x - w.tr.position.x, 0f, player.position.z - w.tr.position.z);
                    if (look.sqrMagnitude > 0.01f)
                        w.tr.rotation = Quaternion.Slerp(w.tr.rotation, Quaternion.LookRotation(-look.normalized, Vector3.up), dt * 6f);
                    float waveHop = Mathf.Abs(Mathf.Sin(t * 7.2f + i)) * 0.12f;
                    w.tr.position = new Vector3(w.tr.position.x, w.baseY + waveHop, w.tr.position.z);
                    continue;
                }
            }

            if (w.waitTimer > 0f)
            {
                w.waitTimer -= dt;
                // Cozy browsing / chatting animation while paused at a stall or bench!
                float browseYaw = Mathf.Sin(t * 1.4f + i * 2f) * 28f;
                w.tr.rotation = Quaternion.Slerp(w.tr.rotation, Quaternion.Euler(0f, browseYaw, 0f), dt * 2.5f);
                float breathe = Mathf.Abs(Mathf.Sin(t * 2.6f + i)) * 0.03f;
                w.tr.position = new Vector3(w.tr.position.x, w.baseY + breathe, w.tr.position.z);
                continue;
            }

            Vector3 goal = w.route[w.wpIdx];
            Vector2 delta = new Vector2(goal.x - w.tr.position.x, goal.z - w.tr.position.z);
            float dist = delta.magnitude;
            if (dist < 0.35f)
            {
                w.wpIdx = (w.wpIdx + 1) % w.route.Length;
                w.waitTimer = 4.5f + (i % 3) * 0.8f; // Pause ~5 seconds browsing!
                continue;
            }

            Vector2 dir = delta / dist;
            float step = Mathf.Min(dist, w.speed * dt);
            float walkHop = Mathf.Abs(Mathf.Sin(t * 7.4f + i)) * 0.075f;
            w.tr.position = new Vector3(w.tr.position.x + dir.x * step, w.baseY + walkHop, w.tr.position.z + dir.y * step);
            Quaternion face = Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.y), Vector3.up);
            w.tr.rotation = Quaternion.Slerp(w.tr.rotation, face, dt * 6.5f);
        }
    }

    private void UpdateFairyWaitresses(float t, float dt)
    {
        for (int i = 0; i < fairies.Count; i++)
        {
            FairyAgent f = fairies[i];
            if (f.root == null || !f.root.gameObject.activeInHierarchy) continue;

            // Rapid magical butterfly/fairy wing flutter!
            float flap = Mathf.Sin(t * 23f + f.phase) * 34f;
            if (f.wingL != null) f.wingL.localRotation = Quaternion.Euler(0f, flap, 0f);
            if (f.wingR != null) f.wingR.localRotation = Quaternion.Euler(0f, -flap, 0f);

            float hover = Mathf.Sin(t * 3.2f + f.phase) * 0.16f;
            Vector3 cur = f.root.position;

            if (f.serveWait > 0f)
            {
                f.serveWait -= dt;
                Vector3 stop = f.stops[f.stopIdx];
                f.root.position = Vector3.Lerp(cur, new Vector3(stop.x, stop.y + hover, stop.z), dt * 4f);
                // Gentle graceful serve spin/bob
                f.root.Rotate(0f, Mathf.Sin(t * 2.2f + f.phase) * 24f * dt, 0f, Space.World);
            }
            else
            {
                Vector3 target = f.stops[f.stopIdx];
                Vector2 dXZ = new Vector2(target.x - cur.x, target.z - cur.z);
                float dist = dXZ.magnitude;
                if (dist < 0.45f)
                {
                    f.serveWait = 3.2f; // Pause gracefully to serve a table or pick up drinks!
                    f.stopIdx = (f.stopIdx + 1) % f.stops.Length;
                }
                else
                {
                    Vector2 dir = dXZ / dist;
                    float spd = 2.65f;
                    float step = Mathf.Min(dist, spd * dt);
                    float nextY = Mathf.Lerp(cur.y, target.y + hover, dt * 4.5f);
                    f.root.position = new Vector3(cur.x + dir.x * step, nextY, cur.z + dir.y * step);
                    Quaternion look = Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.y), Vector3.up);
                    f.root.rotation = Quaternion.Slerp(f.root.rotation, look * Quaternion.Euler(-8f, 0f, Mathf.Sin(t * 3f) * 5f), dt * 6f);
                }
            }

            // Soft glowing fairy-dust trail!
            f.sparkleClock += dt;
            if (f.sparkleClock >= 0.16f)
            {
                f.sparkleClock = 0f;
                SpawnFairySparkle(f.root.position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(0.2f, 0.7f), Random.Range(-0.22f, 0.22f)));
            }
        }
    }

    private void SpawnFairySparkle(Vector3 pos)
    {
        SparkleDust s;
        if (sparklePool.Count > 0)
        {
            s = sparklePool.Pop();
            s.go.SetActive(true);
        }
        else
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BrilhoFada";
            go.transform.SetParent(sparkleContainer, false);
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = sparkleMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            s = new SparkleDust { go = go };
        }
        s.go.transform.position = pos;
        s.go.transform.localScale = Vector3.one * 0.085f;
        s.age = 0f;
        s.life = 0.75f;
        s.vel = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.25f, 0.15f), Random.Range(-0.2f, 0.2f));
        sparkles.Add(s);
    }

    private void UpdateFairySparkles(float dt)
    {
        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            SparkleDust s = sparkles[i];
            s.age += dt;
            if (s.age >= s.life)
            {
                s.go.SetActive(false);
                sparklePool.Push(s);
                sparkles.RemoveAt(i);
                continue;
            }
            float p = s.age / s.life;
            s.go.transform.position += s.vel * dt;
            s.go.transform.localScale = Vector3.one * (0.09f * (1f - p));
        }
    }

    private void UpdateFloatingLanterns(float t)
    {
        if (floatingLanterns == null || lanternBase == null) return;
        for (int i = 0; i < floatingLanterns.Length; i++)
        {
            Transform l = floatingLanterns[i];
            if (l == null) continue;
            float bob = Mathf.Sin(t * 1.8f + i * 1.4f) * 0.16f;
            float sway = Mathf.Cos(t * 1.1f + i * 1.7f) * 0.10f;
            l.position = lanternBase[i] + new Vector3(sway, bob, 0f);
            l.localRotation = Quaternion.Euler(0f, t * 18f + i * 35f, Mathf.Sin(t * 1.5f + i) * 5f);
        }
    }

    private void UpdateDistantSailboats(float t)
    {
        if (distantSailboats == null || sailboatBase == null) return;
        for (int i = 0; i < distantSailboats.Length; i++)
        {
            Transform b = distantSailboats[i];
            if (b == null) continue;
            float dir = (i % 2 == 0) ? 1f : -1f;
            float x = Mathf.Repeat((sailboatBase[i].x + 28f) + t * (0.55f + i * 0.15f) * dir, 78f) - 28f;
            float bob = Mathf.Sin(t * 1.4f + i * 1.8f) * 0.09f;
            b.position = new Vector3(x, sailboatBase[i].y + bob, sailboatBase[i].z);
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
                    gullFlyTimer[i] = 4.2f;
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
