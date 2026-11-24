using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Street & House Painting System for Baía do Avesso!
/// Lets the player paint colorful Animal Crossing pastel tiles/art directly on the street & promenade,
/// and repaint any of the coastal houses' walls & roofs in real-time!
/// </summary>
public sealed class GirlPowerOrla3DPaintSystem : MonoBehaviour
{
    public Transform[] paintableHouses;
    public GirlPowerOrla3DAudio audioController;

    public int ColorIndex { get; private set; }
    public int StreetTilesPainted => paintedSpots.Count;

    private static readonly Color[] Palette =
    {
        new Color(0.95f, 0.43f, 0.59f), // Rosa Berry
        new Color(0.31f, 0.78f, 0.71f), // Menta Tropical
        new Color(1.00f, 0.83f, 0.37f), // Amarelo Sol
        new Color(0.42f, 0.75f, 0.94f), // Azul Céu
        new Color(0.72f, 0.61f, 0.88f), // Lavanda
        new Color(0.95f, 0.48f, 0.40f), // Coral
        new Color(1.00f, 0.95f, 0.86f)  // Creme Baunilha
    };

    private static readonly string[] PaletteNames =
    {
        "ROSA", "MENTA", "AMARELO", "AZUL CÉU", "LAVANDA", "CORAL", "CREME"
    };

    private readonly List<GameObject> paintedSpots = new List<GameObject>();
    private Material paintBaseMat;
    private Transform paintContainer;
    private bool[] nextIsRoof;
    private const int MaxStreetSpots = 160;

    private void Start()
    {
        GameObject container = new GameObject("Rua_Pintada_Container");
        paintContainer = container.transform;
        Shader sh = Shader.Find("GirlPower/OrlaACNHCozy");
        if (sh == null) sh = Shader.Find("Standard");
        paintBaseMat = new Material(sh);
        if (paintBaseMat.HasProperty("_ShadowColor"))
            paintBaseMat.SetColor("_ShadowColor", new Color(0.58f, 0.56f, 0.70f, 1f));
        if (paintBaseMat.HasProperty("_CurveStrength"))
            paintBaseMat.SetFloat("_CurveStrength", 0.0016f);

        int n = paintableHouses != null ? paintableHouses.Length : 0;
        nextIsRoof = new bool[n];
    }

    public Color CurrentColor => Palette[ColorIndex % Palette.Length];
    public string CurrentColorName => PaletteNames[ColorIndex % PaletteNames.Length];

    public void CycleColor()
    {
        ColorIndex = (ColorIndex + 1) % Palette.Length;
        if (audioController != null) audioController.PlayChime();
    }

    /// <summary>Paints a vibrant pastel street-art tile right under the player's feet on the street or boardwalk!</summary>
    public bool PaintStreetAt(Vector3 playerPos)
    {
        if (playerPos.z > 3.2f) return false; // Street/promenade/plazas only

        if (paintedSpots.Count >= MaxStreetSpots)
        {
            Destroy(paintedSpots[0]);
            paintedSpots.RemoveAt(0);
        }

        GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spot.name = "PinturaRua_" + paintedSpots.Count;
        spot.transform.SetParent(paintContainer, false);
        float y = playerPos.z < -3.6f ? -0.055f : 0.045f;
        spot.transform.position = new Vector3(playerPos.x, y, playerPos.z);
        spot.transform.localScale = new Vector3(1.15f, 0.015f, 1.15f);
        Collider col = spot.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = spot.GetComponent<Renderer>();
        r.sharedMaterial = paintBaseMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", CurrentColor);
        r.SetPropertyBlock(mpb);

        paintedSpots.Add(spot);
        if (audioController != null) audioController.PlayChime();
        return true;
    }

    /// <summary>Returns index of nearest paintable house within radius, or -1 if none.</summary>
    public int FindNearestHouse(Vector3 playerPos, float radius = 4.6f)
    {
        if (paintableHouses == null) return -1;
        int best = -1;
        float bestDist = radius;
        Vector2 p = new Vector2(playerPos.x, playerPos.z);
        for (int i = 0; i < paintableHouses.Length; i++)
        {
            if (paintableHouses[i] == null) continue;
            Vector2 h = new Vector2(paintableHouses[i].position.x, paintableHouses[i].position.z);
            float d = Vector2.Distance(p, h);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    /// <summary>Repaints the chosen house's walls (first click) or roof (second click) in real time!</summary>
    public string PaintHouse(int houseIndex)
    {
        if (paintableHouses == null || houseIndex < 0 || houseIndex >= paintableHouses.Length) return "";
        Transform house = paintableHouses[houseIndex];
        if (house == null) return "";

        bool paintRoof = nextIsRoof[houseIndex];
        nextIsRoof[houseIndex] = !paintRoof;
        string targetKey = paintRoof ? "AC_HouseRoof" : "AC_HouseWall";

        foreach (MeshRenderer mr in house.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] mats = mr.materials; // Instance materials on this house only!
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].name.StartsWith(targetKey))
                    mats[i].SetColor("_Color", CurrentColor);
            }
            mr.materials = mats;
        }

        if (audioController != null) audioController.PlayFanfare();
        return (paintRoof ? "Telhado" : "Paredes") + " da Casa " + (houseIndex + 1) + " pintado de " + CurrentColorName + "!";
    }

    public void ClearStreetPaint()
    {
        for (int i = 0; i < paintedSpots.Count; i++)
            if (paintedSpots[i] != null) Destroy(paintedSpots[i]);
        paintedSpots.Clear();
    }
}
