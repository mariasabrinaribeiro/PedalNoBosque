using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Orla3DParallaxVerifier
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Orla 3D (Animal Crossing).unity";

    public static void RunPass()
    {
        string outDir = Environment.GetEnvironmentVariable("ORLA_VERIFY_OUT");
        string[] cliArgs = Environment.GetCommandLineArgs();
        for (int i = 0; i < cliArgs.Length - 1; i++)
            if (cliArgs[i] == "-verifyOut") outDir = cliArgs[i + 1];
        if (string.IsNullOrEmpty(outDir))
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ParallaxVerify"));
        Directory.CreateDirectory(outDir);

        // 1. Rebuild the scene from latest code & assets so we test the exact current state
        GirlPowerOrla3DSceneBuilder.Build();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
        GirlPowerOrla3DFirstDelivery story = UnityEngine.Object.FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
        GirlPowerOrla3DAmbient ambient = UnityEngine.Object.FindObjectOfType<GirlPowerOrla3DAmbient>();
        GirlPowerOrla3DParallaxHorizon parallax = UnityEngine.Object.FindObjectOfType<GirlPowerOrla3DParallaxHorizon>();
        GirlPowerOrla3DContactShadows contactShadows = UnityEngine.Object.FindObjectOfType<GirlPowerOrla3DContactShadows>();
        GirlPowerOrla3DGameFlow gameFlow = UnityEngine.Object.FindObjectOfType<GirlPowerOrla3DGameFlow>();

        // Ensure runtime initialization equivalents run cleanly
        if (ambient != null)
            InvokePrivate(ambient, "Start");
        if (contactShadows != null)
            InvokePrivate(contactShadows, "Start");
        if (parallax != null)
            InvokePrivate(parallax, "Start");

        // Disable title camera orbit so we evaluate real gameplay camera
        if (gameFlow != null) gameFlow.enabled = false;

        Transform player = story != null ? story.player : GameObject.Find("Protagonista 3D")?.transform;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== ORLA 3D PARALLAX & LAYER AUDIT ===");
        sb.AppendLine("Output: " + outDir);
        sb.AppendLine("Camera FOV: " + cam.fieldOfView + ", Offset: " + (story != null ? story.cameraOffset.ToString() : "n/a") + ", Pitch: " + (story != null ? story.cameraPitch.ToString("F1") : "n/a"));
        if (parallax != null)
            sb.AppendLine("Parallax follow factors: far=" + parallax.farLayerFollowFactor.ToString("F2") + ", mid=" + parallax.midLayerFollowFactor.ToString("F2"));

        // Capture shots matching real player movement & camera follow
        var shots = new (string name, Vector3 playerPos, float simTime)[]
        {
            ("01_center_cart",           new Vector3(-1.4f, 0f, -2.0f),   0.0f),
            ("02_walk_left_edge",        new Vector3(-16.5f, 0f, -1.5f),  2.0f),
            ("03_walk_right_florist",    new Vector3(20.5f, 0f, -5.0f),   4.0f),
            ("04_walk_far_right_edge",   new Vector3(35.2f, 0f, -1.5f),   6.0f),
            ("05_beach_shoreline_center",new Vector3(1.5f, -0.14f, 6.8f), 8.0f),
            ("06_beach_far_left",        new Vector3(-15.5f, -0.14f, 6.6f), 10.0f),
            ("07_beach_far_right",       new Vector3(33.5f, -0.14f, 6.6f), 12.0f),
            ("08_south_plaza",           new Vector3(2.0f, -0.04f, -17.5f), 14.0f),
        };

        foreach (var shot in shots)
        {
            PositionPlayerAndCamera(player, cam, story, shot.playerPos);
            StepParallaxAndShadows(parallax, contactShadows);
            RenderToPng(cam, Path.Combine(outDir, shot.name + ".png"), 1280, 720);
            sb.AppendLine();
            sb.AppendLine("--- SHOT: " + shot.name + " | Player=" + shot.playerPos + " | Cam=" + cam.transform.position + " | Pitch=" + cam.transform.eulerAngles.x.ToString("F1") + " ---");
            LogLayerDiagnostics(sb, cam, parallax, ambient, story);
        }

        // Quantitative parallax sweep check: player at X = -8 vs X = +8 on promenade (Z = 0)
        sb.AppendLine();
        sb.AppendLine("=== QUANTITATIVE PARALLAX DIRECTION & RATE CHECK (X=-8 -> X=+8 at Z=0) ===");
        PositionPlayerAndCamera(player, cam, story, new Vector3(-8f, 0f, 0f));
        StepParallaxAndShadows(parallax, contactShadows);
        var vpLeft = SampleKeyTransforms(cam, parallax, ambient, story);

        PositionPlayerAndCamera(player, cam, story, new Vector3(8f, 0f, 0f));
        StepParallaxAndShadows(parallax, contactShadows);
        var vpRight = SampleKeyTransforms(cam, parallax, ambient, story);

        foreach (var kv in vpLeft)
        {
            if (vpRight.TryGetValue(kv.Key, out Vector3 r))
            {
                Vector3 l = kv.Value;
                float du = r.x - l.x;
                sb.AppendLine(string.Format("  {0,-28} | left_u={1,6:F3} right_u={2,6:F3} | du={3,+7:F3} | v={4,5:F3} | depthZ={5,6:F1}",
                    kv.Key, l.x, r.x, du, r.y, r.z));
            }
        }

        // Grounding & Water-level audit
        sb.AppendLine();
        sb.AppendLine("=== GROUNDING & WATER ALIGNMENT AUDIT ===");
        AuditGrounding(sb, ambient, parallax);

        File.WriteAllText(Path.Combine(outDir, "report.txt"), sb.ToString());
        Debug.Log("[Orla3DParallaxVerifier] Done! Saved screenshots & report to " + outDir);
    }

    private static void PositionPlayerAndCamera(Transform player, Camera cam, GirlPowerOrla3DFirstDelivery story, Vector3 playerPos)
    {
        if (player != null) player.position = playerPos;
        if (cam == null) return;

        Vector3 camOff = story != null ? story.cameraOffset : new Vector3(0f, 7.3f, -11.0f);
        float pitch = story != null ? story.cameraPitch : 29.5f;
        float minX = story != null ? story.minWorldX : -17.5f;
        float maxX = story != null ? story.maxWorldX : 36.8f;
        float minZ = story != null ? story.minWorldZ : -22.2f;
        float maxZ = story != null ? story.maxWorldZ : 7.85f;

        float coastBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-4.5f, 3.0f, playerPos.z));
        Vector3 landOff = Vector3.Lerp(camOff, new Vector3(0f, 6.4f, -10.0f), coastBlend);
        float landPitch = Mathf.Lerp(pitch, 28.0f, coastBlend);

        float focusX = Mathf.Clamp(playerPos.x, minX + 2.5f, maxX - 2.5f);
        float focusZ = Mathf.Clamp(playerPos.z + 0.20f, minZ + 1.2f, maxZ - 2.0f);

        cam.transform.position = new Vector3(focusX, playerPos.y + landOff.y, focusZ + landOff.z);
        cam.transform.rotation = Quaternion.Euler(landPitch, 0f, 0f);
        cam.fieldOfView = 42f;
    }

    private static void StepParallaxAndShadows(GirlPowerOrla3DParallaxHorizon parallax, GirlPowerOrla3DContactShadows shadows)
    {
        if (parallax != null) InvokePrivate(parallax, "LateUpdate");
        if (shadows != null) InvokePrivate(shadows, "LateUpdate");
    }

    private static Dictionary<string, Vector3> SampleKeyTransforms(Camera cam, GirlPowerOrla3DParallaxHorizon parallax, GirlPowerOrla3DAmbient ambient, GirlPowerOrla3DFirstDelivery story)
    {
        var dict = new Dictionary<string, Vector3>();
        void Add(string label, Transform t)
        {
            if (t == null) return;
            Vector3 vp = cam.WorldToViewportPoint(t.position);
            dict[label] = vp;
        }
        if (parallax != null)
        {
            if (parallax.farHorizonClouds != null && parallax.farHorizonClouds.Length > 1) Add("Layer1_Cloud_2", parallax.farHorizonClouds[1]);
            if (parallax.farHorizonIslands != null && parallax.farHorizonIslands.Length > 2) Add("Layer1_Island_3", parallax.farHorizonIslands[2]);
            if (parallax.midSeaSailboats != null && parallax.midSeaSailboats.Length > 1) Add("Layer2_Sailboat_2", parallax.midSeaSailboats[1]);
            if (parallax.shorelineFoamRibbons != null && parallax.shorelineFoamRibbons.Length > 3) Add("Layer3_Foam_4", parallax.shorelineFoamRibbons[3]);
        }
        if (ambient != null)
        {
            if (ambient.floatingBoats != null && ambient.floatingBoats.Length > 0) Add("Sea_AnchoredBoat_1", ambient.floatingBoats[0]);
            if (ambient.beachCrabs != null && ambient.beachCrabs.Length > 0) Add("Beach_Crab_1", ambient.beachCrabs[0]);
            if (ambient.swayingPalms != null && ambient.swayingPalms.Length > 1) Add("Beach_Palm_2", ambient.swayingPalms[1]);
        }
        if (story != null)
        {
            Add("Play_Cart", story.cart);
            Add("Play_Player", story.player);
        }
        GameObject kiosk = GameObject.Find("Quiosque Orla das Fadas");
        if (kiosk != null) Add("Mid_Kiosk", kiosk.transform);
        return dict;
    }

    private static void LogLayerDiagnostics(StringBuilder sb, Camera cam, GirlPowerOrla3DParallaxHorizon parallax, GirlPowerOrla3DAmbient ambient, GirlPowerOrla3DFirstDelivery story)
    {
        var samples = SampleKeyTransforms(cam, parallax, ambient, story);
        foreach (var kv in samples)
        {
            Vector3 vp = kv.Value;
            bool inView = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            sb.AppendLine(string.Format("  {0,-24} vp=({1,6:F3}, {2,6:F3}, z={3,5:F1}) {4}",
                kv.Key, vp.x, vp.y, vp.z, inView ? "IN_VIEW" : "OUT"));
        }
    }

    private static void AuditGrounding(StringBuilder sb, GirlPowerOrla3DAmbient ambient, GirlPowerOrla3DParallaxHorizon parallax)
    {
        if (ambient != null && ambient.beachCrabs != null)
        {
            foreach (Transform c in ambient.beachCrabs)
            {
                if (c == null) continue;
                sb.AppendLine(string.Format("  Crab {0}: pos=({1:F2}, {2:F3}, {3:F2}) [dry sand ends z=8.15]", c.name, c.position.x, c.position.y, c.position.z));
            }
        }
        if (ambient != null && ambient.floatingBoats != null)
        {
            foreach (Transform b in ambient.floatingBoats)
            {
                if (b == null) continue;
                sb.AppendLine(string.Format("  AnchoredBoat {0}: pos=({1:F2}, {2:F3}, {3:F2}) [ocean y=-0.18, shore z=8.15]", b.name, b.position.x, b.position.y, b.position.z));
            }
        }
        if (parallax != null && parallax.midSeaSailboats != null)
        {
            foreach (Transform s in parallax.midSeaSailboats)
            {
                if (s == null) continue;
                sb.AppendLine(string.Format("  MidSailboat {0}: pos=({1:F2}, {2:F3}, {3:F2})", s.name, s.position.x, s.position.y, s.position.z));
            }
        }
        if (parallax != null && parallax.farHorizonIslands != null)
        {
            foreach (Transform isl in parallax.farHorizonIslands)
            {
                if (isl == null) continue;
                sb.AppendLine(string.Format("  FarIsland {0}: pos=({1:F2}, {2:F3}, {3:F2})", isl.name, isl.position.x, isl.position.y, isl.position.z));
            }
        }
        if (parallax != null && parallax.farHorizonClouds != null)
        {
            foreach (Transform cl in parallax.farHorizonClouds)
            {
                if (cl == null) continue;
                sb.AppendLine(string.Format("  FarCloud {0}: pos=({1:F2}, {2:F3}, {3:F2})", cl.name, cl.position.x, cl.position.y, cl.position.z));
            }
        }
    }

    private static void RenderToPng(Camera cam, string filePath, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    private static void InvokePrivate(object obj, string methodName)
    {
        if (obj == null) return;
        MethodInfo m = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (m != null && m.GetParameters().Length == 0)
            m.Invoke(obj, null);
    }
}
