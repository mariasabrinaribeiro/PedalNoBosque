using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Displays the nostalgic Polaroid photograph of the two sisters in front of the Baía do Avesso Pavilion.
/// Unlocked during the first delivery when the Florist recognizes the Lemon & Basil flavor!
/// </summary>
public sealed class GirlPowerOrla3DPhotoOverlay : MonoBehaviour
{
    public bool IsUnlocked { get; private set; }
    public bool IsShowing { get; private set; }

    private Texture2D photoTex;
    private Texture2D polaroidFrameTex;
    private Texture2D dimTex;
    private GUIStyle captionStyle;
    private GUIStyle subStyle;
    private GUIStyle closeBtnStyle;

    public void UnlockAndShow()
    {
        IsUnlocked = true;
        IsShowing = true;
    }

    public void UnlockSilent()
    {
        IsUnlocked = true;
    }

    public void ToggleShow()
    {
        if (!IsUnlocked) return;
        IsShowing = !IsShowing;
    }

    public void Close()
    {
        IsShowing = false;
    }

    private void Update()
    {
        if (!IsUnlocked) return;
        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.fKey.wasPressedThisFrame)
                IsShowing = !IsShowing;
            else if (IsShowing && keys.escapeKey.wasPressedThisFrame)
                IsShowing = false;
        }
    }

    private void OnGUI()
    {
        if (!IsShowing || Time.timeScale <= 0.01f) return;
        EnsureAssets();
        GirlPowerOrla3DUITheme.EnsureInitialized();
        GUI.depth = -20;

        float s = Mathf.Clamp(Screen.width / 1600f, 0.70f, 1.15f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dimTex);

        float fw = 380f * s;
        float fh = 442f * s;
        float fx = (Screen.width - fw) * 0.5f;
        float fy = Mathf.Max(28f * s, Screen.height * 0.09f);

        Rect frame = new Rect(fx, fy, fw, fh);
        GUI.Box(frame, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect pic = new Rect(fx + 22f * s, fy + 22f * s, fw - 44f * s, 296f * s);
        GUI.DrawTexture(pic, photoTex);

        GUI.Label(new Rect(fx + 18f * s, fy + 328f * s, fw - 36f * s, 34f * s),
            "Pavilhão da Baía do Avesso", captionStyle);
        GUI.Label(new Rect(fx + 18f * s, fy + 360f * s, fw - 36f * s, 30f * s),
            "\"Duas irmãs em frente ao letreiro antigo\"", subStyle);

        if (GUI.Button(new Rect(fx + (fw - 130f * s) * 0.5f, fy + fh - 44f * s, 130f * s, 32f * s), "Fechar [F]", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            IsShowing = false;
    }

    private void EnsureAssets()
    {
        if (photoTex != null) return;
        photoTex = GenerateSistersPavilionPhoto(320, 240);
        polaroidFrameTex = MakeSolid(new Color(0.99f, 0.97f, 0.92f, 1f));
        dimTex = MakeSolid(new Color(0.14f, 0.11f, 0.16f, 0.45f));

        captionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.32f, 0.24f, 0.20f) }
        };
        subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.50f, 0.40f, 0.35f) }
        };
        closeBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
    }

    /// <summary>
    /// Draws a charming nostalgic pastel illustration of the two sisters in front of the old Pavilion by the bay.
    /// </summary>
    private static Texture2D GenerateSistersPavilionPhoto(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[w * h];

        // Warm sunset sky gradient
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            Color sky = Color.Lerp(new Color(0.99f, 0.80f, 0.64f), new Color(0.68f, 0.72f, 0.88f), Mathf.Clamp01((t - 0.45f) / 0.55f));
            for (int x = 0; x < w; x++)
                px[y * w + x] = sky;
        }

        // Golden sun glow over the sea
        FillCircle(px, w, h, 248, 178, 28, new Color32(255, 224, 152, 255));
        FillCircle(px, w, h, 248, 178, 18, new Color32(255, 245, 200, 255));

        // Turquoise bay & sandy beach
        FillRect(px, w, h, 0, 82, w, 58, new Color32(84, 184, 194, 255));
        FillRect(px, w, h, 0, 76, w, 8, new Color32(238, 246, 244, 255));
        FillRect(px, w, h, 0, 0, w, 78, new Color32(236, 212, 170, 255));

        // Old Pavilion (low mint roof, columns, stone floor, signboard!)
        FillRect(px, w, h, 46, 56, 228, 16, new Color32(226, 216, 196, 255));
        FillRect(px, w, h, 64, 72, 14, 74, new Color32(246, 238, 222, 255));
        FillRect(px, w, h, 242, 72, 14, 74, new Color32(246, 238, 222, 255));
        FillRect(px, w, h, 42, 142, 236, 28, new Color32(120, 204, 192, 255));
        FillRect(px, w, h, 96, 152, 128, 28, new Color32(126, 98, 80, 255));
        FillRect(px, w, h, 102, 157, 116, 18, new Color32(235, 224, 196, 255));

        // Sister 1 (Florist - straw hat & mint dress, left)
        DrawPhotoSister(px, w, h, 128, 42, new Color32(132, 208, 190, 255), new Color32(115, 76, 52, 255), true);
        // Sister 2 (Sign-maker sister holding a little paintbrush/key - coral dress, right)
        DrawPhotoSister(px, w, h, 190, 42, new Color32(238, 124, 114, 255), new Color32(94, 58, 44, 255), false);

        // Warm vintage sepia vignette wash
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = px[y * w + x];
                float dx = (x - w * 0.5f) / (w * 0.5f);
                float dy = (y - h * 0.5f) / (h * 0.5f);
                float vig = 1f - Mathf.Clamp01((dx * dx + dy * dy) * 0.24f);
                c.r = Mathf.Clamp01(c.r * 1.03f * vig + 0.04f);
                c.g = Mathf.Clamp01(c.g * 0.99f * vig + 0.03f);
                c.b = Mathf.Clamp01(c.b * 0.91f * vig + 0.02f);
                px[y * w + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private static void DrawPhotoSister(Color32[] px, int w, int h, int cx, int by, Color32 dress, Color32 hair, bool strawHat)
    {
        // Body/dress
        FillCircle(px, w, h, cx, by + 26, 18, dress);
        FillRect(px, w, h, cx - 15, by + 4, 30, 24, dress);
        // Head
        FillCircle(px, w, h, cx, by + 62, 19, new Color32(240, 194, 158, 255));
        // Hair
        FillCircle(px, w, h, cx, by + 69, 20, hair);
        FillCircle(px, w, h, cx, by + 58, 16, new Color32(240, 194, 158, 255));
        // Eyes & rosy cheeks
        FillCircle(px, w, h, cx - 6, by + 60, 2, new Color32(42, 32, 42, 255));
        FillCircle(px, w, h, cx + 6, by + 60, 2, new Color32(42, 32, 42, 255));
        FillCircle(px, w, h, cx - 10, by + 55, 3, new Color32(245, 148, 166, 255));
        FillCircle(px, w, h, cx + 10, by + 55, 3, new Color32(245, 148, 166, 255));

        if (strawHat)
        {
            FillRect(px, w, h, cx - 26, by + 76, 52, 6, new Color32(242, 206, 126, 255));
            FillCircle(px, w, h, cx, by + 84, 14, new Color32(242, 206, 126, 255));
            FillRect(px, w, h, cx - 14, by + 80, 28, 4, new Color32(240, 110, 150, 255));
        }
        else
        {
            // Little yellow hair bow on the sister!
            FillCircle(px, w, h, cx + 14, by + 78, 5, new Color32(255, 212, 94, 255));
        }
    }

    private static void FillRect(Color32[] px, int w, int h, int x, int y, int rw, int rh, Color32 col)
    {
        int x0 = Mathf.Clamp(x, 0, w), x1 = Mathf.Clamp(x + rw, 0, w);
        int y0 = Mathf.Clamp(y, 0, h), y1 = Mathf.Clamp(y + rh, 0, h);
        for (int iy = y0; iy < y1; iy++)
            for (int ix = x0; ix < x1; ix++)
                px[iy * w + ix] = col;
    }

    private static void FillCircle(Color32[] px, int w, int h, int cx, int cy, int r, Color32 col)
    {
        int x0 = Mathf.Clamp(cx - r, 0, w - 1), x1 = Mathf.Clamp(cx + r, 0, w - 1);
        int y0 = Mathf.Clamp(cy - r, 0, h - 1), y1 = Mathf.Clamp(cy + r, 0, h - 1);
        int r2 = r * r;
        for (int iy = y0; iy <= y1; iy++)
            for (int ix = x0; ix <= x1; ix++)
                if ((ix - cx) * (ix - cx) + (iy - cy) * (iy - cy) <= r2)
                    px[iy * w + ix] = col;
    }

    private static Texture2D MakeSolid(Color c)
    {
        Texture2D t = new Texture2D(2, 2);
        t.SetPixels(new[] { c, c, c, c });
        t.Apply();
        return t;
    }
}
