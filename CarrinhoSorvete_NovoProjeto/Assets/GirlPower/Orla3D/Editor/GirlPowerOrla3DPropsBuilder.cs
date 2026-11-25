using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds rich coastal props, maritime landmarks, micro-details, and ambient particles
/// for Baía do Avesso so the beach, promenade, and sea feel alive and hand-crafted.
/// </summary>
public static class GirlPowerOrla3DPropsBuilder
{
    public struct PropsResult
    {
        public List<Transform> floatingBoats;
        public List<Transform> floatingBuoys;
        public List<Transform> ambientLeaves;
        public List<Transform> waterSparkles;
        public List<Transform> parallaxFarIslands;
        public List<Transform> parallaxFarClouds;
        public List<Transform> parallaxMidSailboats;
        public List<Transform> parallaxShoreFoam;
        public List<GirlPowerOrla3DContactShadows.ShadowEntry> extraShadows;
    }

    public static PropsResult PopulateCoastalDetails(
        Transform root,
        Dictionary<string, Material> mats,
        List<BoxCollider> obstacles,
        Vector2[] lampCoords)
    {
        PropsResult res = new PropsResult
        {
            floatingBoats = new List<Transform>(),
            floatingBuoys = new List<Transform>(),
            ambientLeaves = new List<Transform>(),
            waterSparkles = new List<Transform>(),
            parallaxFarIslands = new List<Transform>(),
            parallaxFarClouds = new List<Transform>(),
            parallaxMidSailboats = new List<Transform>(),
            parallaxShoreFoam = new List<Transform>(),
            extraShadows = new List<GirlPowerOrla3DContactShadows.ShadowEntry>()
        };

        GameObject propsRoot = new GameObject("Microdetalhes_e_Mar_OrlaDasFadas");
        propsRoot.transform.SetParent(root, false);

        BuildCoastalRocks(propsRoot.transform, mats, obstacles);
        BuildSouthNeighborhoodGroundAndBoundaries(propsRoot.transform, mats, obstacles);
        BuildWoodenPier(propsRoot.transform, mats, obstacles);
        BuildAnchoredBoats(propsRoot.transform, mats, res.floatingBoats);
        BuildFloatingBuoys(propsRoot.transform, mats, res.floatingBuoys);
        BuildPromenadeTrashBins(propsRoot.transform, mats, obstacles, res.extraShadows);
        BuildBeachSigns(propsRoot.transform, mats, obstacles, res.extraShadows);
        BuildLampPowerLines(propsRoot.transform, mats, lampCoords);
        BuildBeachTowelsAndBalls(propsRoot.transform, mats, res.extraShadows);
        BuildCoastalGrassAndShrubs(propsRoot.transform, mats, obstacles);
        BuildAmbientLeavesAndSparkles(propsRoot.transform, mats, res.ambientLeaves, res.waterSparkles);
        BuildParallaxHorizonLayers(propsRoot.transform, mats, res.parallaxFarIslands, res.parallaxFarClouds, res.parallaxMidSailboats, res.parallaxShoreFoam);

        return res;
    }

    /// <summary>
    /// Extends the south coastal neighborhood ground behind the street/houses (eliminating the gray void!)
    /// and places natural wooden coastal fences, hedges, and edge boulders as visible physical boundaries.
    /// </summary>
    private static void BuildSouthNeighborhoodGroundAndBoundaries(
        Transform parent,
        Dictionary<string, Material> mats,
        List<BoxCollider> obstacles)
    {
        GameObject grp = new GameObject("Limites_Mapa_e_Extensao_Sul");
        grp.transform.SetParent(parent, false);

        Material grass = GetMat(mats, "AC_Grass");
        Material stoneWhite = GetMat(mats, "AC_StoneWhite");
        Material stoneWarm = GetMat(mats, "AC_StoneWarm");
        Material woodWarm = GetMat(mats, "AC_WoodWarm");
        Material woodLight = GetMat(mats, "AC_WoodLight");
        Material bush = GetMat(mats, "AC_BushGreen");
        Material stoneDark = GetMat(mats, "AC_StoneDark");

        // Crisp non-overlapping boardwalk curbs & widened south plaza (ZERO Z-fighting!)
        // Note: OrlaEnvironmentBase already has StreetCurb at z=-3.55, AsphaltRoad top=-0.08 (z=-8.05..-3.55).
        MakePrim(grp.transform, PrimitiveType.Cube, "MeioFio_Jardim_Interno",
            new Vector3(12f, 0.032f, -0.56f), new Vector3(90f, 0.06f, 0.16f), Quaternion.identity, stoneWarm, false);
        MakePrim(grp.transform, PrimitiveType.Cube, "MeioFio_Orla_Praia_Oeste",
            new Vector3(-3.2f, 0.04f, 3.42f), new Vector3(31.5f, 0.10f, 0.22f), Quaternion.identity, stoneWhite, true);
        MakePrim(grp.transform, PrimitiveType.Cube, "MeioFio_Orla_Praia_Leste",
            new Vector3(28.5f, 0.04f, 3.42f), new Vector3(20.5f, 0.10f, 0.22f), Quaternion.identity, stoneWhite, true);

        // Widened south promenade & town terrace (+50% spacious corridor between Fair stalls & South Houses!)
        MakePrim(grp.transform, PrimitiveType.Cube, "Calcada_Sul",
            new Vector3(12f, -0.135f, -11.4f), new Vector3(104f, 0.16f, 7.0f), Quaternion.identity, stoneWarm, false);
        MakePrim(grp.transform, PrimitiveType.Cube, "Gramado_Bairro_Sul",
            new Vector3(12f, -0.22f, -22.5f), new Vector3(118f, 0.20f, 22.0f), Quaternion.identity, grass, false);

        // Low coastal wooden fence pushed far south to z = -23.6f so +50% circulation corridor & Orchard stay 100% open!
        for (float x = -18f; x <= 38f; x += 4.0f)
        {
            MakePrim(grp.transform, PrimitiveType.Cube, "Cerca_Sul_Poste_" + x,
                new Vector3(x, 0.42f, -23.6f), new Vector3(0.32f, 0.90f, 0.32f), Quaternion.identity, woodWarm, true);
            MakePrim(grp.transform, PrimitiveType.Cube, "Cerca_Sul_Barra_" + x,
                new Vector3(x + 2.0f, 0.56f, -23.6f), new Vector3(4.0f, 0.18f, 0.20f), Quaternion.identity, woodLight, false);
            GameObject arb = MakePrim(grp.transform, PrimitiveType.Sphere, "Arbusto_Sul_" + x,
                new Vector3(x + 1.2f, 0.30f, -24.1f), new Vector3(1.25f, 0.65f, 0.95f), Quaternion.identity, bush, false);
            AddSolidCarveBox(arb, Vector3.zero, Vector3.one, obstacles);
        }

        // West & East map edge natural boulder/hedge borders (solid + NavMeshObstacle carve!)
        foreach (float edgeX in new[] { -18.8f, 38.2f })
        {
            for (float z = -22.0f; z <= 9.5f; z += 2.8f)
            {
                bool onBeach = z > 3.8f;
                GameObject edgeProp = MakePrim(grp.transform, PrimitiveType.Sphere, "Limite_Lateral_" + edgeX + "_" + z,
                    new Vector3(edgeX, onBeach ? 0.18f : 0.42f, z),
                    onBeach ? new Vector3(1.4f, 0.85f, 1.4f) : new Vector3(1.2f, 0.95f, 1.3f),
                    Quaternion.identity, onBeach ? stoneDark : bush, true);
                AddSolidCarveBox(edgeProp, Vector3.zero, Vector3.one, obstacles);
            }
        }

        // Continuous invisible blockers close the gaps between the visible edge props (spanning wide corridor + Orchard + Sea!).
        AddBoundaryBox(grp.transform, obstacles, "Parede_Invisivel_Oeste",
            new Vector3(-17.62f, 1.5f, 0.5f), new Vector3(0.24f, 4.0f, 48.0f));
        AddBoundaryBox(grp.transform, obstacles, "Parede_Invisivel_Leste",
            new Vector3(36.92f, 1.5f, 0.5f), new Vector3(0.24f, 4.0f, 48.0f));
        AddBoundaryBox(grp.transform, obstacles, "Parede_Invisivel_Sul",
            new Vector3(9.6f, 1.5f, -23.4f), new Vector3(55.0f, 4.0f, 0.28f));
        AddBoundaryBox(grp.transform, obstacles, "Parede_Invisivel_Mar",
            new Vector3(9.6f, 1.5f, 21.8f), new Vector3(55.0f, 4.0f, 0.28f));
    }

    private static void AddSolidCarveBox(GameObject go, Vector3 center, Vector3 size, List<BoxCollider> obstacles)
    {
        if (go == null) return;
        BoxCollider box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = false;
        if (obstacles != null) obstacles.Add(box);

        NavMeshObstacle nav = go.GetComponent<NavMeshObstacle>();
        if (nav == null) nav = go.AddComponent<NavMeshObstacle>();
        nav.shape = NavMeshObstacleShape.Box;
        nav.center = center;
        nav.size = size;
        nav.carving = true;
    }

    private static void AddBoundaryBox(Transform parent, List<BoxCollider> obstacles, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        AddSolidCarveBox(wall, Vector3.zero, size, obstacles);
    }

    private static void BuildCoastalRocks(Transform parent, Dictionary<string, Material> mats, List<BoxCollider> obstacles)
    {
        GameObject grp = new GameObject("Pedras_Costeiras");
        grp.transform.SetParent(parent, false);

        Material rockDark = GetMat(mats, "AC_StoneDark");
        Material rockWarm = GetMat(mats, "AC_StoneWarm");
        Material foam = GetMat(mats, "AC_Foam");

        Vector4[] rockSpecs =
        {
            new Vector4(-13.5f, 11.4f, 1.9f, 0.9f),
            new Vector4(-11.8f, 12.2f, 1.25f, 0.65f),
            new Vector4(-4.6f, 10.7f, 1.05f, 0.55f),
            new Vector4(3.2f, 12.8f, 1.45f, 0.72f),
            new Vector4(23.5f, 11.2f, 1.30f, 0.68f),
            new Vector4(31.5f, 11.8f, 2.10f, 1.05f),
            new Vector4(33.4f, 12.6f, 1.35f, 0.72f),
            new Vector4(-15.2f, 8.8f, 1.15f, 0.58f),
            new Vector4(29.8f, 8.6f, 1.10f, 0.54f)
        };

        for (int i = 0; i < rockSpecs.Length; i++)
        {
            Vector4 s = rockSpecs[i];
            GameObject rock = MakePrim(grp.transform, PrimitiveType.Sphere, "Pedra_" + (i + 1),
                new Vector3(s.x, -0.35f + s.w * 0.25f, s.y),
                new Vector3(s.z, s.w, s.z * 0.82f),
                Quaternion.Euler(6f * (i % 3), i * 37f, -4f * (i % 2)),
                i % 2 == 0 ? rockDark : rockWarm, true);

            // Solid physical collider + NavMeshObstacle (Carve = true) on every large rock!
            AddSolidCarveBox(rock, Vector3.zero, Vector3.one, obstacles);

            // Subtle sea-foam collar around submerged rocks
            if (s.y >= 10.2f && foam != null)
            {
                MakePrim(rock.transform, PrimitiveType.Cylinder, "Espuma_Pedra",
                    new Vector3(0f, -0.18f, 0f),
                    new Vector3(1.24f, 0.04f, 1.24f),
                    Quaternion.identity, foam, false);
            }
        }
    }

    private static void BuildWoodenPier(Transform parent, Dictionary<string, Material> mats, List<BoxCollider> obstacles)
    {
        GameObject pier = new GameObject("Trapiche_Madeira_Baia");
        pier.transform.SetParent(parent, false);
        pier.transform.position = new Vector3(-8.8f, -0.18f, 9.6f);

        Material woodWarm = GetMat(mats, "AC_WoodWarm");
        Material woodDark = GetMat(mats, "AC_WoodDark");
        Material woodLight = GetMat(mats, "AC_WoodLight");
        Material ropeCream = GetMat(mats, "AC_Cream");

        // Deck planks extending into the bay
        for (int i = 0; i < 11; i++)
        {
            float z = i * 0.52f;
            MakePrim(pier.transform, PrimitiveType.Cube, "Tabua_" + i,
                new Vector3(0f, 0.42f, z),
                new Vector3(2.35f, 0.10f, 0.44f),
                Quaternion.identity, i % 2 == 0 ? woodWarm : woodLight, true);
        }

        // Support pilings & mooring bollards
        for (int row = 0; row < 4; row++)
        {
            float z = 0.4f + row * 1.55f;
            for (int side = -1; side <= 1; side += 2)
            {
                MakePrim(pier.transform, PrimitiveType.Cylinder, "Estaca_" + row + "_" + side,
                    new Vector3(side * 1.05f, 0.20f, z),
                    new Vector3(0.20f, 0.85f, 0.20f),
                    Quaternion.identity, woodDark, true);
                if (row == 3 || row == 1)
                {
                    MakePrim(pier.transform, PrimitiveType.Cylinder, "Corda_" + row + "_" + side,
                        new Vector3(side * 1.05f, 0.82f, z),
                        new Vector3(0.24f, 0.08f, 0.24f),
                        Quaternion.identity, ropeCream, false);
                }
            }
        }
    }

    private static void BuildAnchoredBoats(Transform parent, Dictionary<string, Material> mats, List<Transform> boats)
    {
        GameObject grp = new GameObject("Barcos_Atracados");
        grp.transform.SetParent(parent, false);

        Material hullA = GetMat(mats, "AC_Cream");
        Material trimA = GetMat(mats, "AC_Coral");
        Material trimB = GetMat(mats, "AC_MintTeal");
        Material trimC = GetMat(mats, "AC_SkyBlue");
        Material wood = GetMat(mats, "AC_WoodWarm");
        Material mast = GetMat(mats, "AC_WoodDark");
        Material sail = GetMat(mats, "AC_SunnyYellow");
        Material sailPink = GetMat(mats, "AC_PinkSoft");

        var specs = new (Vector3 pos, float rotY, Material trim, Material sailMat, bool hasSail)[]
        {
            (new Vector3(-6.2f, -0.26f, 13.8f), 28f, trimA, sail, true),
            (new Vector3(6.8f, -0.26f, 16.5f), -18f, trimB, sailPink, true),
            (new Vector3(25.6f, -0.26f, 14.4f), -32f, trimC, sail, false)
        };

        for (int i = 0; i < specs.Length; i++)
        {
            var s = specs[i];
            GameObject boat = new GameObject("Barco_Baia_" + (i + 1));
            boat.transform.SetParent(grp.transform, false);
            boat.transform.position = s.pos;
            boat.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);

            // Hull body + colorful stripe + bow + inner bench
            MakePrim(boat.transform, PrimitiveType.Cube, "Casco",
                new Vector3(0f, 0.16f, 0f), new Vector3(1.15f, 0.42f, 2.55f), Quaternion.identity, hullA, true);
            MakePrim(boat.transform, PrimitiveType.Cube, "Faixa",
                new Vector3(0f, 0.28f, 0f), new Vector3(1.22f, 0.14f, 2.60f), Quaternion.identity, s.trim, false);
            MakePrim(boat.transform, PrimitiveType.Sphere, "Proa",
                new Vector3(0f, 0.22f, 1.25f), new Vector3(1.05f, 0.45f, 0.95f), Quaternion.identity, s.trim, true);
            MakePrim(boat.transform, PrimitiveType.Cube, "BancoBarco",
                new Vector3(0f, 0.36f, -0.15f), new Vector3(0.95f, 0.08f, 0.34f), Quaternion.identity, wood, false);

            if (s.hasSail)
            {
                MakePrim(boat.transform, PrimitiveType.Cylinder, "Mastro",
                    new Vector3(0f, 1.35f, 0.10f), new Vector3(0.08f, 1.15f, 0.08f), Quaternion.identity, mast, true);
                MakePrim(boat.transform, PrimitiveType.Cube, "Vela",
                    new Vector3(0.06f, 1.50f, -0.35f), new Vector3(0.06f, 1.35f, 1.05f),
                    Quaternion.Euler(0f, 12f, 0f), s.sailMat, true);
            }

            boats.Add(boat.transform);
        }
    }

    private static void BuildFloatingBuoys(Transform parent, Dictionary<string, Material> mats, List<Transform> buoys)
    {
        GameObject grp = new GameObject("Boias_Mar");
        grp.transform.SetParent(parent, false);

        Material coral = GetMat(mats, "AC_Coral");
        Material cream = GetMat(mats, "AC_Cream");
        Material yellow = GetMat(mats, "AC_SunnyYellow");

        Vector3[] positions =
        {
            new Vector3(-1.5f, -0.24f, 13.0f),
            new Vector3(12.2f, -0.24f, 13.5f),
            new Vector3(20.0f, -0.24f, 13.2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject buoy = new GameObject("Boia_" + (i + 1));
            buoy.transform.SetParent(grp.transform, false);
            buoy.transform.position = positions[i];
            MakePrim(buoy.transform, PrimitiveType.Sphere, "Corpo",
                Vector3.zero, new Vector3(0.46f, 0.42f, 0.46f), Quaternion.identity, i % 2 == 0 ? coral : yellow, true);
            MakePrim(buoy.transform, PrimitiveType.Cylinder, "FaixaBoia",
                new Vector3(0f, 0.12f, 0f), new Vector3(0.48f, 0.07f, 0.48f), Quaternion.identity, cream, false);
            MakePrim(buoy.transform, PrimitiveType.Cylinder, "TopoBoia",
                new Vector3(0f, 0.30f, 0f), new Vector3(0.10f, 0.16f, 0.10f), Quaternion.identity, cream, false);
            buoys.Add(buoy.transform);
        }
    }

    private static void BuildPromenadeTrashBins(
        Transform parent,
        Dictionary<string, Material> mats,
        List<BoxCollider> obstacles,
        List<GirlPowerOrla3DContactShadows.ShadowEntry> shadows)
    {
        GameObject grp = new GameObject("Lixeiras_Calcadao");
        grp.transform.SetParent(parent, false);

        Material binMint = GetMat(mats, "AC_MintTeal");
        Material binCoral = GetMat(mats, "AC_Coral");
        Material binBlue = GetMat(mats, "AC_SkyBlue");
        Material metal = GetMat(mats, "AC_MetalDark");
        Material cream = GetMat(mats, "AC_Cream");

        Vector3[] coords =
        {
            new Vector3(-4.2f, 0f, -2.95f),
            new Vector3(6.8f, 0f, -2.95f),
            new Vector3(17.2f, 0f, -2.95f),
            new Vector3(27.2f, 0f, -2.95f)
        };
        Material[] bodyMats = { binMint, binCoral, binBlue, binMint };

        for (int i = 0; i < coords.Length; i++)
        {
            GameObject bin = new GameObject("Lixeira_Praia_" + (i + 1));
            bin.transform.SetParent(grp.transform, false);
            bin.transform.position = coords[i];

            MakePrim(bin.transform, PrimitiveType.Cylinder, "Corpo",
                new Vector3(0f, 0.44f, 0f), new Vector3(0.48f, 0.42f, 0.48f), Quaternion.identity, bodyMats[i], true);
            MakePrim(bin.transform, PrimitiveType.Cylinder, "Tampa",
                new Vector3(0f, 0.88f, 0f), new Vector3(0.54f, 0.08f, 0.54f), Quaternion.identity, cream, true);
            MakePrim(bin.transform, PrimitiveType.Cube, "Abertura",
                new Vector3(0f, 0.72f, -0.22f), new Vector3(0.28f, 0.14f, 0.08f), Quaternion.identity, metal, false);

            BoxCollider box = bin.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(0.56f, 0.90f, 0.56f);
            obstacles.Add(box);

            shadows.Add(new GirlPowerOrla3DContactShadows.ShadowEntry
            {
                target = bin.transform,
                sizeXZ = new Vector2(0.88f, 0.88f),
                isDynamic = false
            });
        }
    }

    private static void BuildBeachSigns(
        Transform parent,
        Dictionary<string, Material> mats,
        List<BoxCollider> obstacles,
        List<GirlPowerOrla3DContactShadows.ShadowEntry> shadows)
    {
        GameObject grp = new GameObject("Placas_Sinalizacao_Praia");
        grp.transform.SetParent(parent, false);

        Material woodDark = GetMat(mats, "AC_WoodDark");
        Material woodLight = GetMat(mats, "AC_WoodLight");
        Material mint = GetMat(mats, "AC_MintTeal");
        Material coral = GetMat(mats, "AC_Coral");

        var specs = new (Vector3 pos, float rotY, string label, Material boardMat)[]
        {
            (new Vector3(-5.8f, 0f, 3.05f), 8f, "PRAIA ↓", mint),
            (new Vector3(12.2f, 0f, 3.10f), -6f, "PAVILHÃO →", coral),
            (new Vector3(26.8f, 0f, 3.05f), -12f, "SURF", mint)
        };

        for (int i = 0; i < specs.Length; i++)
        {
            var s = specs[i];
            GameObject sign = new GameObject("Placa_Praia_" + (i + 1));
            sign.transform.SetParent(grp.transform, false);
            sign.transform.position = s.pos;
            sign.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);

            MakePrim(sign.transform, PrimitiveType.Cylinder, "Haste",
                new Vector3(0f, 0.85f, 0f), new Vector3(0.11f, 0.85f, 0.11f), Quaternion.identity, woodDark, true);
            MakePrim(sign.transform, PrimitiveType.Cube, "Quadro",
                new Vector3(0f, 1.42f, 0f), new Vector3(1.18f, 0.42f, 0.08f), Quaternion.identity, s.boardMat, true);
            MakePrim(sign.transform, PrimitiveType.Cube, "Moldura",
                new Vector3(0f, 1.42f, 0.02f), new Vector3(1.24f, 0.48f, 0.05f), Quaternion.identity, woodLight, false);

            GameObject txtGo = new GameObject("Texto");
            txtGo.transform.SetParent(sign.transform, false);
            txtGo.transform.localPosition = new Vector3(0f, 1.42f, -0.055f);
            TextMeshPro tmp = txtGo.AddComponent<TextMeshPro>();
            tmp.text = s.label;
            tmp.fontSize = 1.35f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color32(255, 250, 236, 255);
            tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.36f);

            BoxCollider box = sign.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.85f, 0f);
            box.size = new Vector3(0.35f, 1.7f, 0.35f);
            obstacles.Add(box);

            shadows.Add(new GirlPowerOrla3DContactShadows.ShadowEntry
            {
                target = sign.transform,
                sizeXZ = new Vector2(0.72f, 0.72f),
                isDynamic = false
            });
        }
    }

    private static void BuildLampPowerLines(Transform parent, Dictionary<string, Material> mats, Vector2[] lampCoords)
    {
        if (lampCoords == null || lampCoords.Length < 2) return;
        GameObject grp = new GameObject("Fiacao_Postes_Orla");
        grp.transform.SetParent(parent, false);

        Material wireMat = GetMat(mats, "AC_MetalDark");
        for (int i = 0; i < lampCoords.Length - 1; i++)
        {
            Vector3 a = new Vector3(lampCoords[i].x, 3.65f, lampCoords[i].y);
            Vector3 b = new Vector3(lampCoords[i + 1].x, 3.65f, lampCoords[i + 1].y);
            if (Vector3.Distance(a, b) > 12.5f) continue;

            int segs = 8;
            float sag = 0.42f;
            Vector3 prev = a;
            for (int s = 1; s <= segs; s++)
            {
                float t = (float)s / segs;
                Vector3 next = Vector3.Lerp(a, b, t);
                next.y -= Mathf.Sin(t * Mathf.PI) * sag;

                Vector3 mid = (prev + next) * 0.5f;
                Vector3 dir = next - prev;
                float len = dir.magnitude;
                GameObject seg = MakePrim(grp.transform, PrimitiveType.Cylinder, "Cabo_" + i + "_" + s,
                    mid, new Vector3(0.028f, len * 0.5f, 0.028f),
                    Quaternion.FromToRotation(Vector3.up, dir.normalized), wireMat, false);
                seg.GetComponent<Renderer>().receiveShadows = false;
                prev = next;
            }
        }
    }

    private static void BuildBeachTowelsAndBalls(
        Transform parent,
        Dictionary<string, Material> mats,
        List<GirlPowerOrla3DContactShadows.ShadowEntry> shadows)
    {
        GameObject grp = new GameObject("Toalhas_e_Acessorios_Areia");
        grp.transform.SetParent(parent, false);

        Material pink = GetMat(mats, "AC_PinkSoft");
        Material mint = GetMat(mats, "AC_MintSoft");
        Material yellow = GetMat(mats, "AC_SunnyYellow");
        Material cream = GetMat(mats, "AC_Cream");
        Material coral = GetMat(mats, "AC_Coral");
        Material blue = GetMat(mats, "AC_SkyBlue");

        var towels = new (Vector3 pos, float rotY, Material baseMat, Material stripeMat)[]
        {
            (new Vector3(-1.3f, -0.14f, 6.7f), 14f, pink, cream),
            (new Vector3(3.8f, -0.15f, 6.9f), -18f, mint, cream),
            (new Vector3(20.2f, -0.14f, 6.6f), 11f, yellow, coral),
            (new Vector3(23.5f, -0.15f, 7.1f), -9f, blue, cream)
        };

        for (int i = 0; i < towels.Length; i++)
        {
            var t = towels[i];
            GameObject towel = new GameObject("Toalha_Praia_" + (i + 1));
            towel.transform.SetParent(grp.transform, false);
            towel.transform.position = t.pos;
            towel.transform.rotation = Quaternion.Euler(0f, t.rotY, 0f);

            MakePrim(towel.transform, PrimitiveType.Cube, "Tecido",
                Vector3.zero, new Vector3(1.15f, 0.03f, 1.85f), Quaternion.identity, t.baseMat, false);
            MakePrim(towel.transform, PrimitiveType.Cube, "Listra1",
                new Vector3(0f, 0.01f, -0.52f), new Vector3(1.16f, 0.025f, 0.22f), Quaternion.identity, t.stripeMat, false);
            MakePrim(towel.transform, PrimitiveType.Cube, "Listra2",
                new Vector3(0f, 0.01f, 0.52f), new Vector3(1.16f, 0.025f, 0.22f), Quaternion.identity, t.stripeMat, false);

            // Cute pastel beach ball beside towels 1 & 3
            if (i == 0 || i == 2)
            {
                GameObject ball = MakePrim(grp.transform, PrimitiveType.Sphere, "Bola_Praia_" + (i + 1),
                    t.pos + new Vector3(0.95f, 0.22f, 0.35f),
                    Vector3.one * 0.44f, Quaternion.identity, i == 0 ? coral : blue, true);
                shadows.Add(new GirlPowerOrla3DContactShadows.ShadowEntry
                {
                    target = ball.transform,
                    sizeXZ = new Vector2(0.58f, 0.58f),
                    isDynamic = false
                });
            }
        }
    }

    private static void BuildCoastalGrassAndShrubs(Transform parent, Dictionary<string, Material> mats, List<BoxCollider> obstacles)
    {
        GameObject grp = new GameObject("Vegetacao_Restinga_Orla");
        grp.transform.SetParent(parent, false);

        Material grass = GetMat(mats, "AC_Grass");
        Material grassLight = GetMat(mats, "AC_GrassLight");
        Material bush = GetMat(mats, "AC_BushGreen");
        Material flowerPink = GetMat(mats, "AC_FlowerPink");
        Material flowerYellow = GetMat(mats, "AC_FlowerYellow");

        Vector3[] tuftCoords =
        {
            new Vector3(-10.5f, -0.05f, 4.55f),
            new Vector3(-7.4f, -0.06f, 4.75f),
            new Vector3(-3.8f, -0.06f, 4.65f),
            new Vector3(1.6f, -0.06f, 4.70f),
            new Vector3(6.8f, -0.07f, 4.85f),
            new Vector3(10.8f, -0.06f, 4.60f),
            new Vector3(19.4f, -0.06f, 4.70f),
            new Vector3(24.6f, -0.06f, 4.65f),
            new Vector3(29.0f, -0.07f, 4.80f),
            new Vector3(-4.2f, -0.08f, -12.6f),
            new Vector3(2.4f, -0.08f, -12.8f),
            new Vector3(12.2f, -0.08f, -12.6f),
            new Vector3(16.8f, -0.08f, -12.8f),
            new Vector3(25.2f, -0.08f, -12.6f)
        };

        for (int i = 0; i < tuftCoords.Length; i++)
        {
            GameObject tuft = new GameObject("Restinga_" + (i + 1));
            tuft.transform.SetParent(grp.transform, false);
            tuft.transform.position = tuftCoords[i];

            for (int b = 0; b < 3; b++)
            {
                float ang = b * 115f + i * 23f;
                Vector3 off = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * 0.22f, 0.18f, Mathf.Sin(ang * Mathf.Deg2Rad) * 0.18f);
                MakePrim(tuft.transform, PrimitiveType.Sphere, "Tufo_" + b,
                    off, new Vector3(0.48f + (b % 2) * 0.14f, 0.34f + (i % 2) * 0.08f, 0.42f),
                    Quaternion.Euler(0f, ang, 0f),
                    b == 0 ? bush : (b == 1 ? grass : grassLight), true);
            }

            if (i % 2 == 0)
            {
                MakePrim(tuft.transform, PrimitiveType.Sphere, "Florzinha",
                    new Vector3(0.08f, 0.36f, -0.06f), Vector3.one * 0.15f,
                    Quaternion.identity, i % 4 == 0 ? flowerPink : flowerYellow, false);
            }

            // Solid Collider + NavMeshObstacle (Carve = true) so NO character walks through green bushes!
            AddSolidCarveBox(tuft, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.90f, 0.85f), obstacles);
        }
    }

    /// <summary>
    /// Builds 3 dedicated Parallax Ocean & Horizon layers for deep scenic depth:
    /// - Layer 1 (10% camera speed): Stylized distant tropical islands/hills + horizon clouds
    /// - Layer 2 (30% camera speed): 5 colorful sailboats gliding continuously across mid-sea
    /// - Layer 3 (100% world space): Looping soft shoreline wave-foam ribbons washing onto the sand!
    /// </summary>
    private static void BuildParallaxHorizonLayers(
        Transform parent,
        Dictionary<string, Material> mats,
        List<Transform> farIslands,
        List<Transform> farClouds,
        List<Transform> midSailboats,
        List<Transform> shoreFoam)
    {
        GameObject pxRoot = new GameObject("Parallax_Horizonte_3Camadas");
        pxRoot.transform.SetParent(parent, false);

        Material hillFar = GetMat(mats, "AC_HillFar");
        Material hillDist = GetMat(mats, "AC_HillDistant");
        Material sand = GetMat(mats, "AC_Sand");
        Material palmLeaf = GetMat(mats, "AC_PalmLeaf");
        Material palmTrunk = GetMat(mats, "AC_PalmTrunk");
        Material cream = GetMat(mats, "AC_Cream");
        Material teal = GetMat(mats, "AC_MintTeal");
        Material pink = GetMat(mats, "AC_PinkBerry");
        Material lilac = GetMat(mats, "AC_Lavender");
        Material woodDark = GetMat(mats, "AC_WoodDark");
        Material foam = GetMat(mats, "AC_Foam");

        // LAYER 1A: 5 Layered Tropical Horizon Islands (10% apparent camera speed!)
        var islandSpecs = new (Vector3 pos, Vector3 hillScale, bool tallPeak)[]
        {
            (new Vector3(-34f, -0.4f, 48f), new Vector3(22f, 6.2f, 10f), true),
            (new Vector3(-11f, -0.4f, 54f), new Vector3(28f, 8.0f, 12f), false),
            (new Vector3(14f,  -0.4f, 50f), new Vector3(24f, 6.8f, 11f), true),
            (new Vector3(38f,  -0.4f, 55f), new Vector3(30f, 8.6f, 12f), false),
            (new Vector3(60f,  -0.4f, 49f), new Vector3(22f, 5.8f, 10f), true)
        };
        for (int i = 0; i < islandSpecs.Length; i++)
        {
            var ispec = islandSpecs[i];
            GameObject isl = new GameObject("Parallax_Ilha_Distante_" + (i + 1));
            isl.transform.SetParent(pxRoot.transform, false);
            isl.transform.position = ispec.pos;

            // Sandy base rim + layered soft coastal hills
            MakePrim(isl.transform, PrimitiveType.Cylinder, "Praia_Ilha",
                new Vector3(0f, 0.25f, 0f), new Vector3(ispec.hillScale.x * 1.08f, 0.35f, ispec.hillScale.z * 1.05f),
                Quaternion.identity, sand, false);
            MakePrim(isl.transform, PrimitiveType.Sphere, "Morro_Principal",
                new Vector3(0f, ispec.hillScale.y * 0.25f, 0.4f), ispec.hillScale,
                Quaternion.identity, i % 2 == 0 ? hillDist : hillFar, false);
            MakePrim(isl.transform, PrimitiveType.Sphere, "Morro_Secundario",
                new Vector3(ispec.hillScale.x * 0.28f, ispec.hillScale.y * 0.16f, -0.5f),
                new Vector3(ispec.hillScale.x * 0.58f, ispec.hillScale.y * 0.68f, ispec.hillScale.z * 0.78f),
                Quaternion.identity, hillDist, false);
            if (ispec.tallPeak)
            {
                // Tiny stylized silhouette palms on the distant island ridge
                MakePrim(isl.transform, PrimitiveType.Cylinder, "Palmeira_Tronco",
                    new Vector3(-2.5f, ispec.hillScale.y * 0.58f, 0f), new Vector3(0.45f, 2.2f, 0.45f),
                    Quaternion.Euler(0f, 0f, 8f), palmTrunk, false);
                MakePrim(isl.transform, PrimitiveType.Sphere, "Palmeira_Copa",
                    new Vector3(-2.8f, ispec.hillScale.y * 0.58f + 2.2f, 0f), new Vector3(2.8f, 1.1f, 2.6f),
                    Quaternion.identity, palmLeaf, false);
            }
            farIslands.Add(isl.transform);
        }

        // LAYER 1B: 6 Fluffy Horizon Clouds (10% apparent camera speed + calm sky drift!)
        for (int i = 0; i < 6; i++)
        {
            GameObject cl = new GameObject("Parallax_Nuvem_Horizonte_" + (i + 1));
            cl.transform.SetParent(pxRoot.transform, false);
            cl.transform.position = new Vector3(-22f + i * 13.5f, 10.2f + (i % 3) * 1.8f, 38f + (i % 2) * 8f);
            MakePrim(cl.transform, PrimitiveType.Sphere, "PuffC", Vector3.zero, new Vector3(6.2f, 2.4f, 2.8f), Quaternion.identity, cream, false);
            MakePrim(cl.transform, PrimitiveType.Sphere, "PuffL", new Vector3(-2.5f, -0.25f, 0.2f), new Vector3(4.2f, 1.9f, 2.4f), Quaternion.identity, cream, false);
            MakePrim(cl.transform, PrimitiveType.Sphere, "PuffR", new Vector3(2.6f, -0.30f, -0.2f), new Vector3(4.5f, 2.0f, 2.5f), Quaternion.identity, cream, false);
            farClouds.Add(cl.transform);
        }

        // LAYER 2: 5 Stylized Mid-Sea Sailboats (30% apparent camera speed + continuous sailing!)
        Material[] sailCols = { cream, pink, teal, lilac, cream };
        Material[] trimCols = { teal, cream, pink, teal, lilac };
        for (int i = 0; i < 5; i++)
        {
            GameObject sb = new GameObject("Parallax_Veleiro_MarMedio_" + (i + 1));
            sb.transform.SetParent(pxRoot.transform, false);
            float zRow = 20.5f + (i % 3) * 4.4f;
            float xStart = -18f + i * 13.5f;
            sb.transform.position = new Vector3(xStart, -0.22f, zRow);
            sb.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0) ? 90f : -90f, 0f);
            float sc = 1.05f + (i % 2) * 0.25f;
            sb.transform.localScale = Vector3.one * sc;

            MakePrim(sb.transform, PrimitiveType.Cube, "Casco", new Vector3(0f, 0.22f, 0f), new Vector3(1.1f, 0.45f, 2.8f), Quaternion.identity, cream, false);
            MakePrim(sb.transform, PrimitiveType.Cube, "Faixa", new Vector3(0f, 0.34f, 0f), new Vector3(1.16f, 0.16f, 2.86f), Quaternion.identity, trimCols[i], false);
            MakePrim(sb.transform, PrimitiveType.Cylinder, "Mastro", new Vector3(0f, 1.55f, 0.1f), new Vector3(0.10f, 1.35f, 0.10f), Quaternion.identity, woodDark, false);
            // Main billowing sail + jib sail
            MakePrim(sb.transform, PrimitiveType.Cube, "Vela_Principal", new Vector3(0.06f, 1.65f, -0.45f), new Vector3(0.08f, 1.75f, 1.25f), Quaternion.Euler(0f, 10f, 0f), sailCols[i], false);
            MakePrim(sb.transform, PrimitiveType.Cube, "Vela_Proa", new Vector3(-0.04f, 1.32f, 0.65f), new Vector3(0.07f, 1.25f, 0.85f), Quaternion.Euler(0f, -12f, 0f), cream, false);
            midSailboats.Add(sb.transform);
        }

        // LAYER 3: 8 Looping Soft Shoreline Wave-Foam Ribbons on the Wet Sand!
        for (int i = 0; i < 8; i++)
        {
            GameObject fr = new GameObject("Parallax_Espuma_BeiraMar_" + (i + 1));
            fr.transform.SetParent(pxRoot.transform, false);
            fr.transform.position = new Vector3(-16f + i * 7.4f, -0.26f, 9.75f + (i % 2) * 0.25f);
            MakePrim(fr.transform, PrimitiveType.Cylinder, "Faixa_Espuma",
                Vector3.zero, new Vector3(7.8f, 0.025f, 1.25f), Quaternion.identity, foam, false);
            shoreFoam.Add(fr.transform);
        }
    }

    private static void BuildAmbientLeavesAndSparkles(
        Transform parent,
        Dictionary<string, Material> mats,
        List<Transform> leaves,
        List<Transform> sparkles)
    {
        GameObject leafGrp = new GameObject("Particulas_Folhas_Brisa");
        leafGrp.transform.SetParent(parent, false);
        Material leafA = GetMat(mats, "AC_PalmLeafTip");
        Material leafB = GetMat(mats, "AC_FlowerPink");
        Material leafC = GetMat(mats, "AC_SunnyYellow");

        for (int i = 0; i < 14; i++)
        {
            float x = -8f + i * 2.8f;
            float z = -1.8f + (i % 4) * 1.9f;
            GameObject leaf = MakePrim(leafGrp.transform, PrimitiveType.Sphere, "Folha_Brisa_" + (i + 1),
                new Vector3(x, 2.5f + (i % 3) * 0.9f, z),
                new Vector3(0.16f, 0.03f, 0.10f), Quaternion.identity,
                i % 3 == 0 ? leafA : (i % 3 == 1 ? leafB : leafC), false);
            leaves.Add(leaf.transform);
        }
        // Open ocean sparkles intentionally omitted so Depth-Buffer Stylized Water stays pure & calm!
    }

    private static GameObject MakePrim(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPos,
        Vector3 localScale,
        Quaternion localRot,
        Material mat,
        bool castShadows)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            if (mat != null) r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return go;
    }

    private static Material GetMat(Dictionary<string, Material> mats, string key)
    {
        return mats != null && mats.TryGetValue(key, out Material m) ? m : null;
    }
}
