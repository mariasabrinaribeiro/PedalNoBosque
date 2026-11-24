using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Iconic Animal Crossing: New Horizons bottom-center cozy dialogue bubble!
/// Features colorful villager name badge, typewriter text reveal synced with Animalese voice blips,
/// animated bouncing continue triangle, and keyboard/click advance.
/// </summary>
public sealed class GirlPowerOrla3DDialogue : MonoBehaviour
{
    public GirlPowerOrla3DAudio audioController;
    public float charsPerSecond = 44f;

    public bool IsOpen { get; private set; }

    private string speakerName = "";
    private Color speakerColor = new Color(0.92f, 0.42f, 0.56f, 1f);
    private float voicePitch = 1.0f;
    private string[] lines;
    private int lineIndex;
    private float charTimer;
    private int visibleChars;
    private int lastBlipChar = -1;
    private Action onCompleteCallback;

    private GUIStyle bubbleTextStyle;
    private GUIStyle nameTagStyle;
    private GUIStyle arrowStyle;
    private Texture2D creamTex;
    private Texture2D borderTex;
    private Texture2D tagTex;
    private Color lastTagColor;

    public void ShowDialogue(string speaker, Color tagColor, float pitch, string[] dialogueLines, Action onComplete = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;
        speakerName = speaker;
        speakerColor = tagColor;
        voicePitch = pitch;
        lines = dialogueLines;
        lineIndex = 0;
        charTimer = 0f;
        visibleChars = 0;
        lastBlipChar = -1;
        onCompleteCallback = onComplete;
        IsOpen = true;
    }

    public void CloseImmediate()
    {
        IsOpen = false;
        lines = null;
        onCompleteCallback = null;
    }

    private void Update()
    {
        if (!IsOpen || lines == null || Time.timeScale <= 0.01f) return;

        string current = lines[lineIndex];
        if (visibleChars < current.Length)
        {
            charTimer += Time.deltaTime * charsPerSecond;
            int target = Mathf.Min(current.Length, Mathf.FloorToInt(charTimer));
            if (target > visibleChars)
            {
                visibleChars = target;
                // Cute Animalese blip every 3 visible letters
                if (visibleChars - lastBlipChar >= 3)
                {
                    char c = current[Mathf.Clamp(visibleChars - 1, 0, current.Length - 1)];
                    if (char.IsLetterOrDigit(c) && audioController != null)
                        audioController.PlayVoiceBlip(voicePitch);
                    lastBlipChar = visibleChars;
                }
            }
        }

        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.eKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)
                AdvanceOrComplete();
            else if (keys.escapeKey.wasPressedThisFrame)
                FinishDialogue();
        }
    }

    public void AdvanceOrComplete()
    {
        if (!IsOpen || lines == null) return;
        string current = lines[lineIndex];
        if (visibleChars < current.Length)
        {
            visibleChars = current.Length;
            return;
        }

        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            FinishDialogue();
        }
        else
        {
            charTimer = 0f;
            visibleChars = 0;
            lastBlipChar = -1;
        }
    }

    private void FinishDialogue()
    {
        Action cb = onCompleteCallback;
        IsOpen = false;
        lines = null;
        onCompleteCallback = null;
        cb?.Invoke();
    }

    private void OnGUI()
    {
        if (!IsOpen || lines == null || Time.timeScale <= 0.01f) return;
        EnsureStyles();
        GirlPowerOrla3DUITheme.EnsureInitialized();
        GUI.depth = -10;

        float s = Mathf.Clamp(Screen.width / 1600f, 0.70f, 1.15f);
        float bw = 780f * s;
        float bh = 152f * s;
        float bx = (Screen.width - bw) * 0.5f;
        float by = Screen.height - bh - 28f * s;

        // Rounded cozy Animal Crossing speech box with soft shadow
        Rect boxRect = new Rect(bx, by, bw, bh);
        GUI.Box(boxRect, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        // Rounded speaker badge overlapping top-left corner
        Rect tagRect = new Rect(bx + 24f * s, by - 22f * s, 275f * s, 40f * s);
        Color prev = GUI.color;
        GUI.color = speakerColor;
        GUI.DrawTexture(tagRect, GirlPowerOrla3DUITheme.PillWhiteTex);
        GUI.color = prev;
        GUI.Label(tagRect, speakerName, nameTagStyle);

        // Typewriter dialogue line
        string shown = lines[lineIndex].Substring(0, Mathf.Clamp(visibleChars, 0, lines[lineIndex].Length));
        bubbleTextStyle.fontSize = Mathf.RoundToInt(21f * s);
        GUI.Label(new Rect(bx + 32f * s, by + 28f * s, bw - 64f * s, bh - 54f * s), shown, bubbleTextStyle);

        // Bouncing continue triangle
        if (visibleChars >= lines[lineIndex].Length)
        {
            float bounce = Mathf.Sin(Time.time * 7.5f) * 4f * s;
            Rect arrowRect = new Rect(bx + bw - 52f * s, by + bh - 42f * s + bounce, 34f * s, 30f * s);
            GUI.Label(arrowRect, "▼", arrowStyle);
        }

        if (Event.current.type == EventType.MouseDown && boxRect.Contains(Event.current.mousePosition))
        {
            AdvanceOrComplete();
            Event.current.Use();
        }
    }

    private void EnsureStyles()
    {
        if (bubbleTextStyle != null) return;
        creamTex = MakeTex(new Color(1.0f, 0.98f, 0.91f, 0.98f));
        borderTex = MakeTex(new Color(0.95f, 0.85f, 0.68f, 0.98f));

        bubbleTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 23,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.35f, 0.25f, 0.19f) }
        };
        nameTagStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.99f, 0.94f) }
        };
        arrowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.44f, 0.56f) }
        };
    }

    private static Texture2D MakeTex(Color c)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { c, c, c, c });
        tex.Apply();
        return tex;
    }
}
