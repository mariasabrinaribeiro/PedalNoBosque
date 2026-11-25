using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared illustrated cozy coastal UI theme for Baía do Avesso:
/// - Anti-aliased rounded 9-slice panels with nautical/wood/ceramic/glass borders & drop shadows
/// - Crisp procedurally-drawn 64x64 icons (portraits, orders, sun/moon, waves, coin, bag, basil, brush, paw, stars)
/// - Diegetic floating feedback numbers (+10 moedas, +1 Manjericão, +♥) rendered cleanly in screen space!
/// </summary>
public static class GirlPowerOrla3DUITheme
{
    public sealed class FloatingPop
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float age;
        public float duration;
    }

    private static bool initialized;
    private static readonly List<FloatingPop> floatingPops = new List<FloatingPop>(24);

    // Rounded panel 9-slice textures
    public static Texture2D CardCreamTex;
    public static Texture2D CardWoodTex;
    public static Texture2D CardGlassTex;
    public static Texture2D CardTealTex;
    public static Texture2D CardCoralTex;
    public static Texture2D CardGoldTex;
    public static Texture2D CardDarkModalTex;
    public static Texture2D DimOverlayTex;
    public static Texture2D PillDarkTex;
    public static Texture2D PillWhiteTex;
    public static Texture2D CircleWhiteTex;
    public static Texture2D CircleGlassTex;
    public static Texture2D CircleCoralTex;

    // Styles
    public static GUIStyle CardCreamStyle;
    public static GUIStyle CardGlassStyle;
    public static GUIStyle CardWoodStyle;
    public static GUIStyle CardDarkModalStyle;
    public static GUIStyle PillTealStyle;
    public static GUIStyle PillCoralStyle;
    public static GUIStyle PillGoldStyle;
    public static GUIStyle BtnPrimaryStyle;
    public static GUIStyle BtnSecondaryStyle;
    public static GUIStyle BtnGlassIconStyle;
    public static GUIStyle BtnTileStyle;
    public static GUIStyle BtnDangerStyle;
    public static GUIStyle TitleLogoStyle;
    public static GUIStyle SubtitleStyle;
    public static GUIStyle HeadingStyle;
    public static GUIStyle BodyDarkStyle;
    public static GUIStyle BodyLightStyle;
    public static GUIStyle SmallMutedStyle;
    public static GUIStyle BadgeTextStyle;
    public static GUIStyle FloatingPopStyle;

    // Illustrated Icons
    public static Texture2D IconLogoEmblem;
    public static Texture2D IconIceCreamLemonBasil;
    public static Texture2D IconPostcard;
    public static Texture2D IconGoldenKey;
    public static Texture2D IconFestivalTrio;
    public static Texture2D IconSun;
    public static Texture2D IconSunset;
    public static Texture2D IconMoon;
    public static Texture2D IconWaveLow;
    public static Texture2D IconWaveHigh;
    public static Texture2D IconPause;
    public static Texture2D IconGear;
    public static Texture2D IconPhoto;
    public static Texture2D IconCoin;
    public static Texture2D IconBag;
    public static Texture2D IconBasil;
    public static Texture2D IconBrush;
    public static Texture2D IconPaw;
    public static Texture2D IconHeart;
    public static Texture2D IconStarFilled;
    public static Texture2D IconStarEmpty;
    public static Texture2D IconCartClosedFail;
    public static Texture2D IconPortraitNina;
    public static Texture2D IconPortraitLuna;
    public static Texture2D IconPortraitMarina;
    public static Texture2D IconPortraitBia;
    public static Texture2D IconPortraitPavilion;
    public static Texture2D IconWaterDrop;
    public static Texture2D IconMenuBook;
    public static Texture2D IconMobility;
    public static Texture2D IconPetTreat;

    public static void EnsureInitialized()
    {
        if (initialized && CardCreamTex != null) return;
        initialized = true;

        // Fixed 3-Color Cohesive Palette (Turquoise #2AC6CC + Warm Sand/Wood #EED2A4/#C9996B + Fairy Pink-Lilac #EC6FA4)
        CardCreamTex = MakeRoundedBox(64, 64, 18, new Color(0.99f, 0.96f, 0.89f, 0.95f), new Color(0.788f, 0.600f, 0.420f, 1f), 3, true);
        CardWoodTex = MakeRoundedBox(64, 64, 16, new Color(0.62f, 0.43f, 0.27f, 0.96f), new Color(0.933f, 0.824f, 0.643f, 1f), 3, true);
        CardGlassTex = MakeRoundedBox(64, 64, 18, new Color(0.11f, 0.30f, 0.36f, 0.72f), new Color(0.68f, 0.94f, 0.95f, 0.55f), 2, true);
        CardTealTex = MakeRoundedBox(64, 64, 16, new Color(0.165f, 0.776f, 0.800f, 0.96f), new Color(0.84f, 0.98f, 0.97f, 0.90f), 2, true);
        CardCoralTex = MakeRoundedBox(64, 64, 18, new Color(0.925f, 0.435f, 0.643f, 0.98f), new Color(1.0f, 0.88f, 0.92f, 0.92f), 3, true);
        CardGoldTex = MakeRoundedBox(64, 64, 16, new Color(0.933f, 0.784f, 0.431f, 0.97f), new Color(1.0f, 0.95f, 0.78f, 0.95f), 2, true);
        CardDarkModalTex = MakeRoundedBox(64, 64, 22, new Color(0.14f, 0.20f, 0.25f, 0.94f), new Color(0.933f, 0.824f, 0.643f, 0.85f), 3, true);
        PillDarkTex = MakeRoundedBox(64, 40, 18, new Color(0.12f, 0.22f, 0.27f, 0.84f), new Color(0.933f, 0.824f, 0.643f, 0.40f), 2, false);
        PillWhiteTex = MakeRoundedBox(64, 40, 18, new Color(1f, 0.98f, 0.93f, 0.96f), new Color(0.788f, 0.600f, 0.420f, 0.90f), 2, true);
        DimOverlayTex = MakeSolid(new Color(0.08f, 0.12f, 0.16f, 0.58f));
        CircleWhiteTex = MakeCircleTex(64, Color.white, new Color(0.25f, 0.20f, 0.18f, 0.28f), 2);
        CircleGlassTex = MakeCircleTex(64, new Color(0.12f, 0.30f, 0.36f, 0.66f), new Color(0.84f, 0.98f, 0.98f, 0.55f), 2);
        CircleCoralTex = MakeCircleTex(64, new Color(0.925f, 0.435f, 0.643f, 0.96f), new Color(1f, 0.90f, 0.94f, 0.92f), 3);

        Texture2D btnPrimaryN = MakeRoundedBox(64, 48, 16, new Color(0.925f, 0.435f, 0.643f, 1f), new Color(1.0f, 0.88f, 0.93f, 0.94f), 3, true);
        Texture2D btnPrimaryH = MakeRoundedBox(64, 48, 16, new Color(0.96f, 0.52f, 0.72f, 1f), Color.white, 3, true);
        Texture2D btnPrimaryA = MakeRoundedBox(64, 48, 16, new Color(0.78f, 0.35f, 0.55f, 1f), new Color(1f, 0.84f, 0.90f, 1f), 3, false);

        Texture2D btnSecN = MakeRoundedBox(64, 48, 14, new Color(0.98f, 0.94f, 0.86f, 0.98f), new Color(0.788f, 0.600f, 0.420f, 1f), 2, true);
        Texture2D btnSecH = MakeRoundedBox(64, 48, 14, new Color(1.0f, 0.98f, 0.92f, 1f), new Color(0.165f, 0.776f, 0.800f, 1f), 3, true);
        Texture2D btnSecA = MakeRoundedBox(64, 48, 14, new Color(0.91f, 0.85f, 0.74f, 1f), new Color(0.65f, 0.47f, 0.31f, 1f), 2, false);

        Texture2D btnGlassN = MakeRoundedBox(64, 48, 16, new Color(0.12f, 0.34f, 0.40f, 0.78f), new Color(0.64f, 0.94f, 0.95f, 0.55f), 2, true);
        Texture2D btnGlassH = MakeRoundedBox(64, 48, 16, new Color(0.165f, 0.62f, 0.66f, 0.92f), new Color(1f, 0.96f, 0.84f, 0.90f), 2, true);
        Texture2D btnGlassA = MakeRoundedBox(64, 48, 16, new Color(0.10f, 0.25f, 0.30f, 0.92f), Color.white, 2, false);

        Texture2D btnTileN = MakeRoundedBox(64, 48, 14, new Color(0.98f, 0.95f, 0.87f, 0.96f), new Color(0.165f, 0.776f, 0.800f, 0.85f), 3, true);
        Texture2D btnTileH = MakeRoundedBox(64, 48, 14, new Color(1.0f, 0.98f, 0.93f, 1f), new Color(0.925f, 0.435f, 0.643f, 1f), 3, true);
        Texture2D btnTileA = MakeRoundedBox(64, 48, 14, new Color(0.90f, 0.85f, 0.75f, 1f), new Color(0.14f, 0.58f, 0.60f, 1f), 3, false);

        Texture2D btnDangerN = MakeRoundedBox(64, 48, 14, new Color(0.76f, 0.28f, 0.34f, 0.96f), new Color(1f, 0.78f, 0.78f, 0.80f), 2, true);
        Texture2D btnDangerH = MakeRoundedBox(64, 48, 14, new Color(0.86f, 0.34f, 0.40f, 1f), Color.white, 2, true);

        RectOffset b16 = new RectOffset(18, 18, 18, 18);
        RectOffset b14 = new RectOffset(14, 14, 14, 14);

        CardCreamStyle = new GUIStyle { normal = { background = CardCreamTex }, border = b16, padding = new RectOffset(12, 12, 10, 10) };
        CardGlassStyle = new GUIStyle { normal = { background = CardGlassTex }, border = b16, padding = new RectOffset(10, 10, 8, 8) };
        CardWoodStyle = new GUIStyle { normal = { background = CardWoodTex }, border = b16, padding = new RectOffset(12, 12, 10, 10) };
        CardDarkModalStyle = new GUIStyle { normal = { background = CardDarkModalTex }, border = b16, padding = new RectOffset(18, 18, 16, 16) };
        PillTealStyle = new GUIStyle { normal = { background = CardTealTex }, border = b14, alignment = TextAnchor.MiddleCenter };
        PillCoralStyle = new GUIStyle { normal = { background = CardCoralTex }, border = b14, alignment = TextAnchor.MiddleCenter };
        PillGoldStyle = new GUIStyle { normal = { background = CardGoldTex }, border = b14, alignment = TextAnchor.MiddleCenter };

        BtnPrimaryStyle = MakeBtnStyle(btnPrimaryN, btnPrimaryH, btnPrimaryA, Color.white, 17, b14);
        BtnSecondaryStyle = MakeBtnStyle(btnSecN, btnSecH, btnSecA, new Color(0.32f, 0.22f, 0.16f), 15, b14);
        BtnGlassIconStyle = MakeBtnStyle(btnGlassN, btnGlassH, btnGlassA, new Color(0.98f, 0.97f, 0.93f), 13, b14);
        BtnTileStyle = MakeBtnStyle(btnTileN, btnTileH, btnTileA, new Color(0.24f, 0.20f, 0.18f), 13, b14);
        BtnDangerStyle = MakeBtnStyle(btnDangerN, btnDangerH, btnDangerN, Color.white, 15, b14);

        TitleLogoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 44,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.90f) }
        };
        SubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.90f, 0.72f) }
        };
        HeadingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.32f, 0.21f, 0.16f) }
        };
        BodyDarkStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.30f, 0.22f, 0.18f) }
        };
        BodyLightStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.98f, 0.96f, 0.91f) }
        };
        SmallMutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.50f, 0.39f, 0.32f) }
        };
        BadgeTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        FloatingPopStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        BuildAllIcons();
    }

    public static void SpawnFloatingFeedback(Vector3 worldPos, string text, Color color, float duration = 1.65f)
    {
        if (string.IsNullOrEmpty(text)) return;
        floatingPops.Add(new FloatingPop
        {
            worldPos = worldPos + Vector3.up * 1.45f,
            text = text,
            color = color,
            age = 0f,
            duration = duration
        });
    }

    public static void UpdateAndDrawFloatingFeedback(Camera cam, float dt, float scale)
    {
        if (floatingPops.Count == 0 || cam == null) return;
        EnsureInitialized();

        for (int i = floatingPops.Count - 1; i >= 0; i--)
        {
            FloatingPop pop = floatingPops[i];
            if (Event.current.type == EventType.Repaint)
                pop.age += dt;
            if (pop.age >= pop.duration)
            {
                floatingPops.RemoveAt(i);
                continue;
            }

            float t = pop.age / pop.duration;
            Vector3 wp = pop.worldPos + Vector3.up * (t * 1.35f);
            Vector3 sp = cam.WorldToScreenPoint(wp);
            if (sp.z <= 0.1f) continue;

            float sx = sp.x;
            float sy = Screen.height - sp.y;
            float alpha = t < 0.7f ? 1f : 1f - ((t - 0.7f) / 0.3f);

            Color prev = GUI.color;
            FloatingPopStyle.fontSize = Mathf.RoundToInt(18f * scale);
            Rect r = new Rect(sx - 120f * scale, sy - 18f * scale, 240f * scale, 34f * scale);

            // Drop shadow
            GUI.color = new Color(0.12f, 0.08f, 0.14f, alpha * 0.75f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), pop.text, FloatingPopStyle);

            GUI.color = new Color(pop.color.r, pop.color.g, pop.color.b, alpha);
            GUI.Label(r, pop.text, FloatingPopStyle);
            GUI.color = prev;
        }
    }

    public static void DrawProgressBar(Rect rect, float progress01, Color fillColor)
    {
        progress01 = Mathf.Clamp01(progress01);
        Color prev = GUI.color;
        GUI.color = new Color(0.24f, 0.18f, 0.15f, 0.32f);
        GUI.DrawTexture(rect, PillDarkTex);
        if (progress01 > 0.01f)
        {
            float pad = 2f;
            Rect fill = new Rect(rect.x + pad, rect.y + pad, Mathf.Max(6f, (rect.width - pad * 2f) * progress01), rect.height - pad * 2f);
            GUI.color = fillColor;
            GUI.DrawTexture(fill, PillWhiteTex);
        }
        GUI.color = prev;
    }

    public static void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color shadowColor, float offset = 2f)
    {
        Color prev = style.normal.textColor;
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        style.normal.textColor = prev;
    }

    public static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, float thickness = 1.5f)
    {
        Color prev = style.normal.textColor;
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + thickness, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - thickness, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + thickness, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        style.normal.textColor = prev;
    }

    private static GUIStyle MakeBtnStyle(Texture2D normal, Texture2D hover, Texture2D active, Color txtColor, int fontSize, RectOffset border)
    {
        return new GUIStyle(GUI.skin.button)
        {
            normal = { background = normal, textColor = txtColor },
            hover = { background = hover, textColor = txtColor },
            active = { background = active, textColor = txtColor },
            focused = { background = hover, textColor = txtColor },
            border = border,
            padding = new RectOffset(10, 10, 6, 6),
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    // ------------------------------------------------------------------
    // Procedural Anti-Aliased Textures & Vector Icons
    // ------------------------------------------------------------------
    private static void BuildAllIcons()
    {
        IconLogoEmblem = BuildLogoEmblemIcon(128);
        IconIceCreamLemonBasil = BuildIceCreamIcon(64);
        IconPostcard = BuildPostcardIcon(64);
        IconGoldenKey = BuildKeyIcon(64);
        IconFestivalTrio = BuildFestivalIcon(64);
        IconSun = BuildSunIcon(64, false);
        IconSunset = BuildSunIcon(64, true);
        IconMoon = BuildMoonIcon(64);
        IconWaveLow = BuildWaveIcon(64, false);
        IconWaveHigh = BuildWaveIcon(64, true);
        IconPause = BuildPauseIcon(64);
        IconGear = BuildGearIcon(64);
        IconPhoto = BuildPhotoIcon(64);
        IconCoin = BuildCoinIcon(64);
        IconBag = BuildBagIcon(64);
        IconBasil = BuildBasilIcon(64);
        IconBrush = BuildBrushIcon(64);
        IconPaw = BuildPawIcon(64);
        IconHeart = BuildHeartIcon(64);
        IconStarFilled = BuildStarIcon(64, true);
        IconStarEmpty = BuildStarIcon(64, false);
        IconCartClosedFail = BuildCartClosedIcon(128);
        IconPortraitNina = BuildPortraitIcon(64, new Color32(72, 188, 170, 255), new Color32(94, 56, 42, 255), false);
        IconPortraitLuna = BuildPortraitIcon(64, new Color32(198, 120, 188, 255), new Color32(118, 78, 52, 255), true);
        IconPortraitMarina = BuildPortraitIcon(64, new Color32(236, 122, 102, 255), new Color32(84, 50, 40, 255), false);
        IconPortraitBia = BuildPortraitIcon(64, new Color32(158, 118, 214, 255), new Color32(58, 42, 66, 255), false);
        IconPortraitPavilion = BuildPavilionIcon(64);
        IconWaterDrop = BuildWaterDropIcon(64);
        IconMenuBook = BuildMenuBookIcon(64);
        IconMobility = BuildMobilityIcon(64);
        IconPetTreat = BuildPetTreatIcon(64);
    }

    private static Texture2D MakeRoundedBox(int w, int h, int radius, Color fill, Color border, int borderW, bool dropShadow)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[w * h];
        int sh = dropShadow ? 3 : 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dMain = RoundedBoxSDF(x + 0.5f, y + 0.5f, 1f, sh + 1f, w - 2f, h - sh - 2f, radius);
                Color col = Color.clear;

                if (dropShadow)
                {
                    float dSh = RoundedBoxSDF(x + 0.5f, y + 0.5f, 2f, 0.5f, w - 3f, h - sh - 2f, radius);
                    float shAlpha = Mathf.Clamp01(0.5f - dSh * 0.45f) * 0.24f;
                    col = new Color(0.12f, 0.08f, 0.16f, shAlpha);
                }

                float cov = Mathf.Clamp01(0.5f - dMain);
                if (cov > 0.001f)
                {
                    float innerCov = Mathf.Clamp01(0.5f - (dMain + borderW));
                    // Gentle vertical cozy gradient
                    float grad = Mathf.Lerp(0.95f, 1.04f, (float)y / h);
                    Color fCol = new Color(Mathf.Clamp01(fill.r * grad), Mathf.Clamp01(fill.g * grad), Mathf.Clamp01(fill.b * grad), fill.a);
                    Color shapeCol = Color.Lerp(border, fCol, innerCov);
                    shapeCol.a *= cov;
                    col = AlphaBlend(col, shapeCol);
                }
                px[y * w + x] = col;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private static float RoundedBoxSDF(float px, float py, float x0, float y0, float bw, float bh, float r)
    {
        float cx = x0 + bw * 0.5f;
        float cy = y0 + bh * 0.5f;
        float qx = Mathf.Abs(px - cx) - (bw * 0.5f - r);
        float qy = Mathf.Abs(py - cy) - (bh * 0.5f - r);
        return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
             + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    private static Texture2D MakeCircleTex(int sz, Color fill, Color border, int borderW)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[sz * sz];
        float c = sz * 0.5f;
        float r = c - 2f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) - r;
                float cov = Mathf.Clamp01(0.5f - d);
                float inCov = Mathf.Clamp01(0.5f - (d + borderW));
                Color col = Color.Lerp(border, fill, inCov);
                col.a *= cov;
                px[y * sz + x] = col;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private static Texture2D MakeSolid(Color c)
    {
        Texture2D t = new Texture2D(2, 2);
        t.SetPixels(new[] { c, c, c, c });
        t.Apply();
        return t;
    }

    private static Color AlphaBlend(Color dst, Color src)
    {
        float a = src.a + dst.a * (1f - src.a);
        if (a < 0.0001f) return Color.clear;
        return new Color(
            (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a,
            (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a,
            (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a,
            a);
    }

    private static Texture2D NewCanvas(int sz, out Color32[] px)
    {
        px = new Color32[sz * sz];
        return new Texture2D(sz, sz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
    }

    private static Texture2D FinishCanvas(Texture2D t, Color32[] px)
    {
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    private static void DrawDisc(Color32[] px, int sz, float cx, float cy, float r, Color32 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
        int x1 = Mathf.Min(sz - 1, Mathf.CeilToInt(cx + r + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
        int y1 = Mathf.Min(sz - 1, Mathf.CeilToInt(cy + r + 1));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(r - d + 0.6f);
                if (a > 0f) px[y * sz + x] = Blend32(px[y * sz + x], c, a);
            }
    }

    private static void DrawRect32(Color32[] px, int sz, int x0, int y0, int w, int h, Color32 c)
    {
        int ex = Mathf.Min(sz, x0 + w);
        int ey = Mathf.Min(sz, y0 + h);
        for (int y = Mathf.Max(0, y0); y < ey; y++)
            for (int x = Mathf.Max(0, x0); x < ex; x++)
                px[y * sz + x] = Blend32(px[y * sz + x], c, 1f);
    }

    private static void DrawTri(Color32[] px, int sz, Vector2 a, Vector2 b, Vector2 c, Color32 col)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int x1 = Mathf.Min(sz - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int y1 = Mathf.Min(sz - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
                bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                if (!(hasNeg && hasPos)) px[y * sz + x] = Blend32(px[y * sz + x], col, 1f);
            }
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    private static Color32 Blend32(Color32 dst, Color32 src, float factor)
    {
        float sa = (src.a / 255f) * factor;
        float da = dst.a / 255f;
        float oa = sa + da * (1f - sa);
        if (oa < 0.001f) return new Color32(0, 0, 0, 0);
        byte r = (byte)Mathf.Clamp((src.r * sa + dst.r * da * (1f - sa)) / oa, 0, 255);
        byte g = (byte)Mathf.Clamp((src.g * sa + dst.g * da * (1f - sa)) / oa, 0, 255);
        byte b = (byte)Mathf.Clamp((src.b * sa + dst.b * da * (1f - sa)) / oa, 0, 255);
        return new Color32(r, g, b, (byte)Mathf.Clamp(oa * 255f, 0, 255));
    }

    private static Texture2D BuildLogoEmblemIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Coastal golden badge disc
        DrawDisc(px, sz, 64, 64, 58, new Color32(255, 245, 224, 255));
        DrawDisc(px, sz, 64, 64, 52, new Color32(102, 198, 212, 255));
        // Golden sun
        DrawDisc(px, sz, 64, 82, 24, new Color32(255, 214, 108, 255));
        DrawDisc(px, sz, 64, 82, 16, new Color32(255, 242, 178, 255));
        // Turquoise ocean waves
        for (int x = 14; x < 114; x++)
        {
            float wy = 44 + Mathf.Sin(x * 0.14f) * 5f;
            for (int y = 12; y < wy; y++)
            {
                if (Vector2.Distance(new Vector2(x, y), new Vector2(64, 64)) < 51)
                    px[y * sz + x] = y > wy - 5 ? new Color32(244, 252, 255, 255) : new Color32(42, 152, 174, 255);
            }
        }
        // Ice cream silhouette center-front
        DrawTri(px, sz, new Vector2(64, 24), new Vector2(46, 62), new Vector2(82, 62), new Color32(230, 166, 92, 255));
        DrawDisc(px, sz, 54, 66, 14, new Color32(248, 126, 156, 255));
        DrawDisc(px, sz, 74, 66, 14, new Color32(142, 224, 154, 255));
        DrawDisc(px, sz, 64, 78, 15, new Color32(255, 244, 196, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildIceCreamIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(255, 248, 232, 255));
        DrawTri(px, sz, new Vector2(32, 8), new Vector2(19, 34), new Vector2(45, 34), new Color32(226, 162, 88, 255));
        // Lemon scoop + lime scoop + emerald basil leaf
        DrawDisc(px, sz, 26, 38, 11, new Color32(255, 236, 122, 255));
        DrawDisc(px, sz, 38, 38, 11, new Color32(168, 232, 136, 255));
        DrawDisc(px, sz, 32, 46, 11, new Color32(248, 250, 212, 255));
        DrawDisc(px, sz, 42, 50, 6, new Color32(58, 168, 86, 255));
        DrawDisc(px, sz, 46, 52, 4, new Color32(84, 200, 108, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPostcardIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(248, 236, 218, 255));
        DrawRect32(px, sz, 12, 18, 40, 28, new Color32(255, 252, 242, 255));
        DrawTri(px, sz, new Vector2(12, 46), new Vector2(52, 46), new Vector2(32, 30), new Color32(236, 220, 194, 255));
        DrawDisc(px, sz, 32, 31, 6, new Color32(232, 92, 124, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildKeyIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(62, 120, 136, 255));
        DrawDisc(px, sz, 22, 34, 11, new Color32(248, 198, 72, 255));
        DrawDisc(px, sz, 22, 34, 5, new Color32(62, 120, 136, 255));
        DrawRect32(px, sz, 30, 31, 22, 6, new Color32(248, 198, 72, 255));
        DrawRect32(px, sz, 41, 24, 4, 8, new Color32(248, 198, 72, 255));
        DrawRect32(px, sz, 48, 25, 4, 7, new Color32(248, 198, 72, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildFestivalIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(255, 232, 184, 255));
        DrawDisc(px, sz, 20, 44, 6, new Color32(242, 108, 134, 255));
        DrawDisc(px, sz, 32, 48, 6, new Color32(72, 196, 178, 255));
        DrawDisc(px, sz, 44, 44, 6, new Color32(255, 194, 64, 255));
        DrawRect32(px, sz, 14, 18, 36, 14, new Color32(188, 124, 76, 255));
        DrawRect32(px, sz, 16, 30, 32, 5, new Color32(255, 248, 232, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildSunIcon(int sz, bool sunset)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 core = sunset ? new Color32(255, 146, 86, 255) : new Color32(255, 218, 84, 255);
        Color32 ray = sunset ? new Color32(255, 196, 112, 255) : new Color32(255, 238, 148, 255);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            DrawDisc(px, sz, 32 + Mathf.Cos(a) * 21f, 32 + Mathf.Sin(a) * 21f, 4.2f, ray);
        }
        DrawDisc(px, sz, 32, 32, 14, core);
        if (sunset) DrawRect32(px, sz, 8, 10, 48, 12, new Color32(72, 152, 188, 235));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildMoonIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 30, 32, 17, new Color32(255, 236, 162, 255));
        DrawDisc(px, sz, 39, 36, 15, new Color32(0, 0, 0, 0));
        // Punch crescent cleanly
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                if (Vector2.Distance(new Vector2(x, y), new Vector2(39, 37)) < 14f)
                    px[y * sz + x] = new Color32(0, 0, 0, 0);
        DrawDisc(px, sz, 46, 46, 3, new Color32(255, 248, 210, 255));
        DrawDisc(px, sz, 48, 24, 2.2f, new Color32(255, 248, 210, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildWaveIcon(int sz, bool high)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 c1 = high ? new Color32(84, 186, 232, 255) : new Color32(102, 218, 206, 255);
        Color32 foam = new Color32(240, 252, 255, 255);
        int rows = high ? 3 : 2;
        for (int r = 0; r < rows; r++)
        {
            float baseY = 18 + r * 14;
            for (int x = 8; x < 56; x++)
            {
                float wy = baseY + Mathf.Sin(x * 0.24f + r) * (high ? 5.5f : 3.2f);
                DrawDisc(px, sz, x, wy, 3.2f, r == rows - 1 ? foam : c1);
            }
        }
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPauseIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 c = new Color32(255, 250, 240, 255);
        DrawRect32(px, sz, 18, 14, 10, 36, c);
        DrawRect32(px, sz, 36, 14, 10, 36, c);
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildGearIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 c = new Color32(255, 248, 232, 255);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            DrawDisc(px, sz, 32 + Mathf.Cos(a) * 19f, 32 + Mathf.Sin(a) * 19f, 5.2f, c);
        }
        DrawDisc(px, sz, 32, 32, 16, c);
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                if (Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) < 7.5f)
                    px[y * sz + x] = new Color32(0, 0, 0, 0);
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPhotoIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawRect32(px, sz, 12, 10, 40, 44, new Color32(255, 250, 240, 255));
        DrawRect32(px, sz, 17, 22, 30, 26, new Color32(114, 198, 212, 255));
        DrawDisc(px, sz, 38, 40, 5, new Color32(255, 224, 118, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildCoinIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 26, new Color32(216, 144, 24, 255));
        DrawDisc(px, sz, 32, 32, 22, new Color32(255, 206, 62, 255));
        DrawDisc(px, sz, 32, 32, 16, new Color32(246, 184, 42, 255));
        DrawDisc(px, sz, 26, 40, 5, new Color32(255, 246, 190, 220));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildBagIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 46, 12, new Color32(158, 102, 58, 255));
        DrawDisc(px, sz, 32, 44, 7, new Color32(0, 0, 0, 0));
        DrawRect32(px, sz, 14, 12, 36, 32, new Color32(212, 148, 86, 255));
        DrawRect32(px, sz, 14, 34, 36, 10, new Color32(184, 118, 64, 255));
        DrawDisc(px, sz, 32, 33, 4.5f, new Color32(255, 214, 86, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildBasilIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 25, 34, 14, new Color32(62, 178, 86, 255));
        DrawDisc(px, sz, 39, 36, 13, new Color32(84, 204, 104, 255));
        DrawDisc(px, sz, 32, 44, 11, new Color32(114, 224, 126, 255));
        DrawRect32(px, sz, 30, 12, 4, 18, new Color32(52, 136, 68, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildBrushIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        for (int i = 0; i < 34; i++)
            DrawDisc(px, sz, 14 + i, 14 + i, 4.2f, new Color32(174, 112, 64, 255));
        DrawDisc(px, sz, 48, 48, 8, new Color32(242, 104, 142, 255));
        DrawDisc(px, sz, 52, 52, 5, new Color32(255, 168, 192, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPawIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 c = new Color32(132, 86, 56, 255);
        DrawDisc(px, sz, 32, 24, 12, c);
        DrawDisc(px, sz, 17, 38, 6, c);
        DrawDisc(px, sz, 27, 45, 6, c);
        DrawDisc(px, sz, 37, 45, 6, c);
        DrawDisc(px, sz, 47, 38, 6, c);
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildHeartIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 c = new Color32(236, 86, 122, 255);
        DrawDisc(px, sz, 22, 39, 13, c);
        DrawDisc(px, sz, 42, 39, 13, c);
        DrawTri(px, sz, new Vector2(10, 35), new Vector2(54, 35), new Vector2(32, 11), c);
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildStarIcon(int sz, bool filled)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        Color32 col = filled ? new Color32(255, 206, 52, 255) : new Color32(150, 136, 124, 120);
        Vector2 c = new Vector2(32, 32);
        Vector2[] pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? 26f : 11f;
            pts[i] = c + new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        for (int i = 0; i < 10; i++)
            DrawTri(px, sz, c, pts[i], pts[(i + 1) % 10], col);
        if (filled) DrawDisc(px, sz, 32, 32, 7, new Color32(255, 238, 146, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPortraitIcon(int sz, Color32 bg, Color32 hair, bool hat)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(255, 248, 234, 255));
        DrawDisc(px, sz, 32, 32, 25, bg);
        DrawDisc(px, sz, 32, 16, 15, new Color32(255, 245, 232, 255));
        DrawDisc(px, sz, 32, 37, 14, hair);
        DrawDisc(px, sz, 32, 33, 12, new Color32(244, 198, 162, 255));
        DrawDisc(px, sz, 27, 34, 2, new Color32(42, 32, 42, 255));
        DrawDisc(px, sz, 37, 34, 2, new Color32(42, 32, 42, 255));
        DrawDisc(px, sz, 23, 30, 2.5f, new Color32(246, 142, 162, 255));
        DrawDisc(px, sz, 41, 30, 2.5f, new Color32(246, 142, 162, 255));
        if (hat)
        {
            DrawRect32(px, sz, 14, 43, 36, 5, new Color32(242, 206, 122, 255));
            DrawDisc(px, sz, 32, 49, 10, new Color32(242, 206, 122, 255));
        }
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPavilionIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        DrawDisc(px, sz, 32, 32, 29, new Color32(72, 146, 164, 255));
        DrawRect32(px, sz, 12, 14, 40, 8, new Color32(236, 224, 198, 255));
        DrawRect32(px, sz, 16, 22, 5, 18, new Color32(252, 246, 232, 255));
        DrawRect32(px, sz, 43, 22, 5, 18, new Color32(252, 246, 232, 255));
        DrawRect32(px, sz, 10, 39, 44, 9, new Color32(114, 212, 196, 255));
        DrawDisc(px, sz, 32, 32, 4, new Color32(255, 224, 112, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildCartClosedIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Twilight circular frame
        DrawDisc(px, sz, 64, 64, 58, new Color32(52, 46, 78, 255));
        DrawDisc(px, sz, 64, 64, 52, new Color32(84, 72, 114, 255));
        // Crescent moon
        DrawDisc(px, sz, 92, 92, 12, new Color32(255, 232, 152, 255));
        DrawDisc(px, sz, 98, 96, 10, new Color32(84, 72, 114, 255));
        // Closed cart silhouette with dimmed awning
        DrawRect32(px, sz, 32, 30, 64, 34, new Color32(214, 196, 178, 255));
        DrawRect32(px, sz, 26, 62, 76, 16, new Color32(182, 96, 116, 255));
        DrawDisc(px, sz, 44, 28, 10, new Color32(54, 48, 62, 255));
        DrawDisc(px, sz, 84, 28, 10, new Color32(54, 48, 62, 255));
        // "FECHADO" wooden plaque
        DrawRect32(px, sz, 44, 40, 40, 14, new Color32(138, 88, 56, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildWaterDropIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Clean coastal blue-turquoise pulsing water drop (diegetic empty bowl cue!)
        Color32 rim = new Color32(236, 252, 255, 255);
        Color32 body = new Color32(42, 198, 204, 255);
        Color32 deep = new Color32(28, 146, 178, 255);
        DrawDisc(px, sz, 32, 24, 18, rim);
        DrawTri(px, sz, new Vector2(15, 28), new Vector2(49, 28), new Vector2(32, 57), rim);
        DrawDisc(px, sz, 32, 24, 15, body);
        DrawTri(px, sz, new Vector2(18, 27), new Vector2(46, 27), new Vector2(32, 52), body);
        DrawDisc(px, sz, 34, 20, 10, deep);
        DrawDisc(px, sz, 25, 29, 4.5f, new Color32(255, 255, 255, 235));
        DrawDisc(px, sz, 23, 22, 2.2f, new Color32(255, 255, 255, 210));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildMenuBookIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Cozy warm wood & cream illustrated recipe book icon
        DrawRect32(px, sz, 11, 12, 42, 40, new Color32(201, 153, 107, 255));
        DrawRect32(px, sz, 14, 15, 17, 34, new Color32(252, 246, 232, 255));
        DrawRect32(px, sz, 33, 15, 17, 34, new Color32(248, 240, 222, 255));
        DrawRect32(px, sz, 30, 14, 4, 36, new Color32(166, 112, 68, 255));
        // Cute pink-lilac & turquoise dish accents on pages
        DrawDisc(px, sz, 22, 34, 5, new Color32(236, 111, 164, 255));
        DrawRect32(px, sz, 17, 22, 11, 3, new Color32(158, 124, 96, 255));
        DrawDisc(px, sz, 41, 34, 5, new Color32(42, 198, 204, 255));
        DrawRect32(px, sz, 36, 22, 11, 3, new Color32(158, 124, 96, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildMobilityIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Stylized beach scooter / wheel icon in warm cream & turquoise
        DrawDisc(px, sz, 19, 20, 8, new Color32(252, 246, 232, 255));
        DrawDisc(px, sz, 19, 20, 4, new Color32(42, 198, 204, 255));
        DrawDisc(px, sz, 45, 20, 8, new Color32(252, 246, 232, 255));
        DrawDisc(px, sz, 45, 20, 4, new Color32(42, 198, 204, 255));
        DrawRect32(px, sz, 18, 21, 28, 5, new Color32(236, 111, 164, 255));
        DrawRect32(px, sz, 41, 24, 5, 22, new Color32(252, 246, 232, 255));
        DrawRect32(px, sz, 36, 44, 13, 5, new Color32(238, 210, 164, 255));
        return FinishCanvas(t, px);
    }

    private static Texture2D BuildPetTreatIcon(int sz)
    {
        Texture2D t = NewCanvas(sz, out Color32[] px);
        // Warm golden pet treat / bone icon for pet order bubbles!
        Color32 bone = new Color32(240, 204, 132, 255);
        Color32 shade = new Color32(204, 158, 88, 255);
        DrawDisc(px, sz, 18, 25, 8, shade);
        DrawDisc(px, sz, 18, 39, 8, shade);
        DrawDisc(px, sz, 46, 25, 8, shade);
        DrawDisc(px, sz, 46, 39, 8, shade);
        DrawRect32(px, sz, 18, 26, 28, 12, shade);
        DrawDisc(px, sz, 18, 26, 6.5f, bone);
        DrawDisc(px, sz, 18, 38, 6.5f, bone);
        DrawDisc(px, sz, 46, 26, 6.5f, bone);
        DrawDisc(px, sz, 46, 38, 6.5f, bone);
        DrawRect32(px, sz, 18, 28, 28, 8, bone);
        return FinishCanvas(t, px);
    }
}
