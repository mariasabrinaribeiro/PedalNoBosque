using System;
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
        public Transform legL;
        public Transform legR;
        public Transform armL;
        public Transform armR;
        public Quaternion legLBase;
        public Quaternion legRBase;
        public Quaternion armLBase;
        public Quaternion armRBase;
        public Vector3[] route;
        public int wpIdx;
        public float waitTimer;
        public float speed;
        public float baseY;
        public float gaitPhase;
        public float faceSign;
        public Vector3 prevWorldPos;
        public Quaternion lastValidRot;
        public List<Vector3> subPath;
        public int subIdx;
        public int failedPathCount;
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
        public bool hasDispatchOrder;
        public Vector3 dispatchPos;
        public Action onDispatchArrive;
        public float faceSign;
        public Vector3 prevWorldPos;
        public Quaternion lastValidRot;
        public float glideTimer;
        public float hoverPauseTimer;
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
        // All villager stops sit strictly at the SIDES of the 5m promenade & beside lateral plaza counters
        // (never parking in the middle of the main Cart -> Florist corridor!)
        Vector3[][] routes =
        {
            new[] { new Vector3(-8.5f, 0f, -2.5f), new Vector3(-2.5f, 0f, -2.6f), new Vector3(4.2f, 0f, -2.5f), new Vector3(-4.5f, 0f, -5.2f) },
            new[] { new Vector3(10.5f, 0f, -2.5f), new Vector3(16.5f, 0f, -2.5f), new Vector3(21.5f, 0f, -2.6f), new Vector3(13.5f, 0f, -5.4f) },
            new[] { new Vector3(-12.5f, 0f, 1.8f), new Vector3(-5.5f, 0f, 2.0f), new Vector3(2.0f, 0f, 1.9f), new Vector3(-6.5f, 0f, 5.2f) },
            new[] { new Vector3(-3.5f, 0f, 5.2f), new Vector3(2.0f, 0f, 5.4f), new Vector3(7.5f, 0f, 5.2f), new Vector3(0.5f, 0f, 2.0f) },
            new[] { new Vector3(7.5f, 0f, 1.8f), new Vector3(14.0f, 0f, 2.0f), new Vector3(20.0f, 0f, 1.9f), new Vector3(15.5f, 0f, 5.4f) },
            new[] { new Vector3(27.0f, 0f, 1.8f), new Vector3(29.0f, 0f, 5.2f), new Vector3(32.0f, 0f, 5.0f), new Vector3(28.5f, 0f, -2.4f) }
        };

        for (int i = 0; i < strollingNpcs.Length; i++)
        {
            Transform npc = strollingNpcs[i];
            if (npc == null) continue;
            GirlPowerOrla3DCharacterFacing.DisableRootMotion(npc);
            walkers.Add(new WaypointWalker
            {
                tr = npc,
                legL = FindNamedJoint(npc, "LegL", "Protag_LegL", "Leg_L", "LeftLeg", "LegLeft", "leg.l"),
                legR = FindNamedJoint(npc, "LegR", "Protag_LegR", "Leg_R", "RightLeg", "LegRight", "leg.r"),
                armL = FindNamedJoint(npc, "ArmL", "Protag_ArmL", "Arm_L", "LeftArm", "ArmLeft", "arm.l"),
                armR = FindNamedJoint(npc, "ArmR", "Protag_ArmR", "Arm_R", "RightArm", "ArmRight", "arm.r"),
                route = routes[i % routes.Length],
                wpIdx = 0,
                waitTimer = 1.5f + i * 1.2f,
                speed = 1.15f + (i % 3) * 0.12f,
                baseY = npc.position.y,
                faceSign = GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign(npc, 1f),
                prevWorldPos = npc.position,
                lastValidRot = npc.rotation,
                subPath = new List<Vector3>(16),
                subIdx = 0
            });
            WaypointWalker walker = walkers[walkers.Count - 1];
            if (walker.legL != null) walker.legLBase = walker.legL.localRotation;
            if (walker.legR != null) walker.legRBase = walker.legR.localRotation;
            if (walker.armL != null) walker.armLBase = walker.armL.localRotation;
            if (walker.armR != null) walker.armRBase = walker.armR.localRotation;
        }
    }

    private static Transform FindNamedJoint(Transform root, params string[] names)
    {
        if (root == null) return null;
        for (int i = 0; i < names.Length; i++)
        {
            Transform direct = FindChildRec(root, names[i]);
            if (direct != null) return direct;
        }
        return null;
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
            GirlPowerOrla3DCharacterFacing.DisableRootMotion(f);
            fairies.Add(new FairyAgent
            {
                root = f,
                wingL = FindChildRec(f, "Fairy_WingL"),
                wingR = FindChildRec(f, "Fairy_WingR"),
                stops = fairyRoutes[i % fairyRoutes.Length],
                stopIdx = 0,
                serveWait = 1.0f + i * 0.8f,
                phase = i * 2.1f,
                faceSign = GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign(f, 1f),
                prevWorldPos = f.position,
                lastValidRot = f.rotation,
                glideTimer = 1.8f + i * 0.6f,
                hoverPauseTimer = 0f
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
                    if (GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(look, w.faceSign, 0.04f, out Quaternion greetRot))
                    {
                        w.lastValidRot = greetRot;
                        w.tr.rotation = Quaternion.Slerp(w.tr.rotation, greetRot, dt * 6f);
                    }
                    w.prevWorldPos = w.tr.position;
                    AnimateWalkerGait(w, 0f, t, dt, true);
                    continue;
                }
            }

            if (w.waitTimer > 0f)
            {
                w.waitTimer -= dt;
                // Keep last valid facing smoothly when paused (no flipping!)
                w.tr.rotation = Quaternion.Slerp(w.tr.rotation, w.lastValidRot, dt * 4f);
                float breathe = Mathf.Abs(Mathf.Sin(t * 2.6f + i)) * 0.03f;
                w.tr.position = new Vector3(w.tr.position.x, w.baseY + breathe, w.tr.position.z);
                w.prevWorldPos = w.tr.position;
                AnimateWalkerGait(w, 0f, t, dt, false);
                continue;
            }

            // Single-owner obstacle-avoiding A* pathing for strolling villagers (NEVER falls back to straight-line clipping!)
            GirlPowerOrla3DPathfinder pf = GirlPowerOrla3DPathfinder.Instance;
            Vector3 finalStop = w.route[w.wpIdx];
            if (w.subPath.Count == 0 || w.subIdx >= w.subPath.Count)
            {
                if (pf != null)
                {
                    Vector3 safeStop = pf.FindNearestWalkablePoint(finalStop, Vector3.back, 0.45f);
                    pf.FindPath(w.tr.position, safeStop, w.subPath, 0.45f);
                }
                if (w.subPath.Count == 0)
                {
                    // No walkable route found right now: wait calmly and recalculate (or skip blocked stop) — never walk straight through obstacles!
                    w.failedPathCount++;
                    w.waitTimer = 1.15f;
                    if (w.failedPathCount >= 2)
                    {
                        w.failedPathCount = 0;
                        w.wpIdx = (w.wpIdx + 1) % w.route.Length;
                    }
                    w.prevWorldPos = w.tr.position;
                    AnimateWalkerGait(w, 0f, t, dt, false);
                    continue;
                }
                w.failedPathCount = 0;
                w.subIdx = 0;
            }

            Vector3 goal = w.subPath[w.subIdx];
            Vector2 delta = new Vector2(goal.x - w.tr.position.x, goal.z - w.tr.position.z);
            float dist = delta.magnitude;
            if (dist < 0.45f)
            {
                w.subIdx++;
                if (w.subIdx >= w.subPath.Count)
                {
                    w.subPath.Clear();
                    w.subIdx = 0;
                    w.wpIdx = (w.wpIdx + 1) % w.route.Length;
                    w.waitTimer = 4.5f + (i % 3) * 0.8f; // Pause ~5s at side counter/bench!
                }
                w.prevWorldPos = w.tr.position;
                continue;
            }

            Vector2 pathDir = delta / dist;
            float step = Mathf.Min(dist, w.speed * dt);
            Vector3 curPos = w.tr.position;

            // Validate lateral yield & path step against OBB obstacles BEFORE applying position!
            bool yieldingToPlayer = false;
            Vector2 yieldPref = pathDir;
            Vector2 yieldAlt = pathDir;
            if (player != null)
            {
                Vector2 fromPlayer = new Vector2(curPos.x - player.position.x, curPos.z - player.position.z);
                float pDist = fromPlayer.magnitude;
                if (pDist < 1.35f && pDist > 0.01f)
                {
                    yieldingToPlayer = true;
                    float weight = Mathf.Clamp01((1.35f - pDist) / 0.85f) * 0.85f;
                    Vector2 sideAway = fromPlayer / pDist;
                    Vector2 perpR = new Vector2(pathDir.y, -pathDir.x);
                    Vector2 perpL = -perpR;
                    Vector2 prefSide = (Vector2.Dot(sideAway, perpR) >= 0f) ? perpR : perpL;
                    Vector2 altSide = -prefSide;
                    yieldPref = (pathDir + prefSide * weight).normalized;
                    yieldAlt = (pathDir + altSide * weight).normalized;
                }
            }

            Vector3 chosenPos = curPos;
            bool stepValid = false;
            const float NpcCheckRadius = 0.43f;
            Vector2[] candidates = yieldingToPlayer
                ? new[] { yieldPref, yieldAlt, pathDir }
                : new[] { pathDir };

            for (int c = 0; c < candidates.Length; c++)
            {
                Vector3 cand = new Vector3(curPos.x + candidates[c].x * step, w.baseY, curPos.z + candidates[c].y * step);
                if (pf == null || pf.IsMoveStepClear(curPos, cand, NpcCheckRadius))
                {
                    chosenPos = cand;
                    stepValid = true;
                    break;
                }
            }

            if (!stepValid)
            {
                // No unobstructed lateral or forward step available -> pause politely & force A* recalculation instead of clipping!
                w.subPath.Clear();
                w.subIdx = 0;
                w.waitTimer = 0.65f;
                w.prevWorldPos = curPos;
                AnimateWalkerGait(w, 0f, t, dt, false);
                continue;
            }

            Vector3 nextPos = chosenPos;
            w.tr.position = nextPos;

            // Orient villager strictly by REAL frame-to-frame planar displacement (including curves!)
            Vector3 realMove = new Vector3(nextPos.x - w.prevWorldPos.x, 0f, nextPos.z - w.prevWorldPos.z);
            float realSpeed = dt > 0.0001f ? (realMove.magnitude / dt) : 0f;
            w.prevWorldPos = nextPos;

            w.gaitPhase += realMove.magnitude * (Mathf.PI * 2f / 1.48f);
            AnimateWalkerGait(w, realSpeed, t, dt, false);
            if (realSpeed > 0.25f && GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realMove, w.faceSign, 0.00001f, out Quaternion face))
                w.lastValidRot = face;
            w.tr.rotation = Quaternion.Slerp(w.tr.rotation, w.lastValidRot, dt * 7.5f);
        }
    }

    private static void AnimateWalkerGait(WaypointWalker w, float speed, float time, float dt, bool greeting)
    {
        float stride = speed > 0.08f ? Mathf.Sin(w.gaitPhase) : 0f;
        float swing = greeting ? Mathf.Sin(time * 3.4f + w.gaitPhase) : stride;
        if (w.legL != null) w.legL.localRotation = Quaternion.Slerp(w.legL.localRotation, w.legLBase * Quaternion.Euler(swing * 22f, 0f, 0f), dt * 12f);
        if (w.legR != null) w.legR.localRotation = Quaternion.Slerp(w.legR.localRotation, w.legRBase * Quaternion.Euler(-swing * 22f, 0f, 0f), dt * 12f);
        if (w.armL != null) w.armL.localRotation = Quaternion.Slerp(w.armL.localRotation, w.armLBase * Quaternion.Euler(-swing * 13f, 0f, 0f), dt * 10f);
        if (w.armR != null) w.armR.localRotation = Quaternion.Slerp(w.armR.localRotation, w.armRBase * Quaternion.Euler(swing * 13f, 0f, 0f), dt * 10f);
        float bob = speed > 0.08f ? Mathf.Abs(stride) * 0.025f : Mathf.Sin(time * 2.2f + w.gaitPhase) * 0.012f;
        w.tr.position = new Vector3(w.tr.position.x, w.baseY + bob, w.tr.position.z);
    }

    /// <summary>
    /// Direct mobile Drag & Drop dispatch: immediately sends the selected Fairy Waitress
    /// gliding straight to a Table or Empty Water Bowl with fairy sparkles!
    /// </summary>
    public bool DispatchFairyTo(Transform fairyRoot, Vector3 destination, Action onArrive)
    {
        FairyAgent chosen = null;
        for (int i = 0; i < fairies.Count; i++)
        {
            if (fairies[i].root == fairyRoot)
            {
                chosen = fairies[i];
                break;
            }
        }
        if (chosen == null && fairies.Count > 0)
        {
            float bestD = float.MaxValue;
            for (int i = 0; i < fairies.Count; i++)
            {
                if (fairies[i].root == null) continue;
                float d = Vector3.Distance(fairies[i].root.position, destination);
                if (d < bestD) { bestD = d; chosen = fairies[i]; }
            }
        }
        if (chosen == null) return false;

        chosen.hasDispatchOrder = true;
        chosen.dispatchPos = new Vector3(destination.x, 0.72f, destination.z);
        chosen.onDispatchArrive = onArrive;
        chosen.serveWait = 0f;
        GirlPowerOrla3DJuice.PopScale(chosen.root, 1.22f, 0.28f);
        for (int s = 0; s < 5; s++)
            SpawnFairySparkle(chosen.root.position + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(0.2f, 0.9f), UnityEngine.Random.Range(-0.35f, 0.35f)));
        return true;
    }

    private void UpdateFairyWaitresses(float t, float dt)
    {
        for (int i = 0; i < fairies.Count; i++)
        {
            FairyAgent f = fairies[i];
            if (f.root == null || !f.root.gameObject.activeInHierarchy) continue;

            // Fairy aerial movement language: soft flutter while gliding, rapid bright flutter during mid-air hover pauses!
            bool midAirPause = !f.hasDispatchOrder && f.serveWait <= 0f && f.hoverPauseTimer > 0f;
            float wingRate = f.hasDispatchOrder ? 30f : (midAirPause ? 32f : 19f);
            float wingAmp = midAirPause ? 40f : 30f;
            float flap = Mathf.Sin(t * wingRate + f.phase) * wingAmp;
            if (f.wingL != null) f.wingL.localRotation = Quaternion.Euler(0f, flap, 0f);
            if (f.wingR != null) f.wingR.localRotation = Quaternion.Euler(0f, -flap, 0f);

            float hover = Mathf.Sin(t * (midAirPause ? 4.8f : 3.0f) + f.phase) * (midAirPause ? 0.12f : 0.17f);
            float leanSign = f.faceSign >= 0f ? 1f : -1f;
            Vector3 cur = f.root.position;

            if (f.hasDispatchOrder)
            {
                Vector3 target = f.dispatchPos;
                Vector2 dXZ = new Vector2(target.x - cur.x, target.z - cur.z);
                float dist = dXZ.magnitude;
                if (dist < 0.72f)
                {
                    Action cb = f.onDispatchArrive;
                    f.hasDispatchOrder = false;
                    f.onDispatchArrive = null;
                    f.serveWait = 1.65f;
                    GirlPowerOrla3DJuice.PopScale(f.root, 1.20f, 0.28f);
                    for (int s = 0; s < 6; s++)
                        SpawnFairySparkle(cur + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.15f, 0.9f), UnityEngine.Random.Range(-0.4f, 0.4f)));
                    cb?.Invoke();
                }
                else
                {
                    Vector2 dir = dXZ / dist;
                    float spd = 5.2f;
                    float step = Mathf.Min(dist, spd * dt);
                    float nextY = Mathf.Lerp(cur.y, target.y + hover, dt * 6f);
                    Vector3 nextPos = new Vector3(cur.x + dir.x * step, nextY, cur.z + dir.y * step);
                    f.root.position = nextPos;
                    Vector3 realMove = new Vector3(nextPos.x - f.prevWorldPos.x, 0f, nextPos.z - f.prevWorldPos.z);
                    f.prevWorldPos = nextPos;
                    if (GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realMove, f.faceSign, 0.0001f, out Quaternion look))
                        f.lastValidRot = look;
                    f.root.rotation = Quaternion.Slerp(f.root.rotation, f.lastValidRot * Quaternion.Euler(12f * leanSign, 0f, Mathf.Sin(t * 4.5f) * 6f), dt * 8f);
                }
            }
            else if (f.serveWait > 0f)
            {
                f.serveWait -= dt;
                Vector3 stop = f.stops[f.stopIdx];
                f.root.position = Vector3.Lerp(cur, new Vector3(stop.x, stop.y + hover, stop.z), dt * 3.5f);
                f.prevWorldPos = f.root.position;
                // Gentle graceful serve hover
                f.root.rotation = Quaternion.Slerp(f.root.rotation, f.lastValidRot * Quaternion.Euler(0f, Mathf.Sin(t * 1.8f + f.phase) * 18f, Mathf.Sin(t * 2.4f) * 4f), dt * 4f);
            }
            else if (f.hoverPauseTimer > 0f)
            {
                // Brief poised mid-air hover pause between graceful glides!
                f.hoverPauseTimer -= dt;
                float poisedY = Mathf.Lerp(cur.y, f.stops[f.stopIdx].y + 0.16f + hover, dt * 4.2f);
                f.root.position = new Vector3(cur.x, poisedY, cur.z);
                f.prevWorldPos = f.root.position;
                f.root.rotation = Quaternion.Slerp(f.root.rotation, f.lastValidRot * Quaternion.Euler(-4f * leanSign, Mathf.Sin(t * 3.0f) * 10f, 0f), dt * 5f);
            }
            else
            {
                Vector3 target = f.stops[f.stopIdx];
                Vector2 dXZ = new Vector2(target.x - cur.x, target.z - cur.z);
                float dist = dXZ.magnitude;
                if (dist < 0.45f)
                {
                    f.serveWait = 2.8f; // Pause gracefully to serve a table or pick up drinks!
                    f.stopIdx = (f.stopIdx + 1) % f.stops.Length;
                    f.glideTimer = UnityEngine.Random.Range(1.8f, 2.8f);
                    f.prevWorldPos = f.root.position;
                }
                else
                {
                    // Glide smoothly, then pause briefly mid-air every ~2.2 seconds!
                    f.glideTimer -= dt;
                    if (f.glideTimer <= 0f && dist > 1.4f)
                    {
                        f.hoverPauseTimer = UnityEngine.Random.Range(0.55f, 0.85f);
                        f.glideTimer = UnityEngine.Random.Range(2.0f, 3.0f);
                        SpawnFairySparkle(cur + Vector3.up * 0.45f);
                        continue;
                    }
                    Vector2 dir = dXZ / dist;
                    float spd = 2.85f;
                    float step = Mathf.Min(dist, spd * dt);
                    float nextY = Mathf.Lerp(cur.y, target.y + hover, dt * 4.5f);
                    Vector3 nextPos = new Vector3(cur.x + dir.x * step, nextY, cur.z + dir.y * step);
                    f.root.position = nextPos;
                    Vector3 realMove = new Vector3(nextPos.x - f.prevWorldPos.x, 0f, nextPos.z - f.prevWorldPos.z);
                    f.prevWorldPos = nextPos;
                    if (GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realMove, f.faceSign, 0.0001f, out Quaternion look))
                        f.lastValidRot = look;
                    f.root.rotation = Quaternion.Slerp(f.root.rotation, f.lastValidRot * Quaternion.Euler(10f * leanSign, 0f, Mathf.Sin(t * 3.2f) * 6f), dt * 6f);
                }
            }

            // Soft glowing fairy-dust trail!
            f.sparkleClock += dt;
            if (f.sparkleClock >= (f.hasDispatchOrder ? 0.08f : 0.16f))
            {
                f.sparkleClock = 0f;
                SpawnFairySparkle(f.root.position + new Vector3(UnityEngine.Random.Range(-0.22f, 0.22f), UnityEngine.Random.Range(0.2f, 0.7f), UnityEngine.Random.Range(-0.22f, 0.22f)));
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
        s.vel = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.25f, 0.15f), UnityEngine.Random.Range(-0.2f, 0.2f));
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
