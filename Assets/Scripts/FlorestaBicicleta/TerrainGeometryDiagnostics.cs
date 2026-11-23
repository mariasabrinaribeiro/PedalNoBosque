using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Visualização de Diagnóstico da Geometria do Terreno (DESATIVADA por padrão — ligue/desligue com a tecla F7).
    /// Exibe em tempo real durante os testes:
    /// - Linha CIANO: superfície física calculada (PlayableGroundY + TerrainHeightProfile);
    /// - Linha AMARELA: margem superior visual da pista (UpperVergeY + TerrainHeightProfile);
    /// - Pontos VERDES: pontos de contato das rodas dianteira e traseira da bicicleta;
    /// - Postes MAGENTA: limites dos segmentos procedurais (provando continuidade h = 0 entre segmentos!).
    /// </summary>
    public class TerrainGeometryDiagnostics : MonoBehaviour
    {
        public bool ShowOverlay { get; private set; } = false;
        public System.Action<string, Color> OnOverlayMessage;

        private Camera cam;
        private BikeGirlController player;
        private InfiniteTrackGenerator trackGenerator;

        private LineRenderer physicalSurfaceLine;
        private LineRenderer upperVergeLine;
        private Transform rearWheelMarker;
        private Transform frontWheelMarker;
        private readonly List<LineRenderer> segmentBoundaryLines = new List<LineRenderer>();
        private readonly List<float> boundaryCache = new List<float>();

        private Material lineMat;
        private Sprite dotSprite;

        public void Initialize(Camera targetCam, BikeGirlController bike, InfiniteTrackGenerator generator)
        {
            cam = targetCam;
            player = bike;
            trackGenerator = generator;

            Shader sh = Shader.Find("Sprites/Default");
            lineMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color"));
            dotSprite = CreateDotSprite();

            physicalSurfaceLine = CreateLine("Diag_SuperficieFisica", new Color(0.10f, 0.95f, 1.00f, 0.95f), 0.08f, 30);
            upperVergeLine      = CreateLine("Diag_MargemVisualSuperior", new Color(1.00f, 0.92f, 0.20f, 0.85f), 0.06f, 30);

            rearWheelMarker  = CreateWheelMarker("Diag_RodaTraseira");
            frontWheelMarker = CreateWheelMarker("Diag_RodaDianteira");

            for (int i = 0; i < 6; i++)
                segmentBoundaryLines.Add(CreateLine($"Diag_LimiteSegmento_{i}", new Color(1f, 0.25f, 0.85f, 0.85f), 0.06f, 29));

            SetOverlayActive(false);
        }

        private LineRenderer CreateLine(string objName, Color col, float width, int sortOrder)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = lineMat;
            lr.startColor = col;
            lr.endColor = col;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.sortingOrder = sortOrder;
            return lr;
        }

        private Transform CreateWheelMarker(string objName)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = new Color(0.18f, 1.00f, 0.36f, 1f);
            sr.sortingOrder = 32;
            return go.transform;
        }

        private void SetOverlayActive(bool active)
        {
            ShowOverlay = active;
            if (physicalSurfaceLine != null) physicalSurfaceLine.enabled = active;
            if (upperVergeLine != null) upperVergeLine.enabled = active;
            if (rearWheelMarker != null) rearWheelMarker.gameObject.SetActive(active);
            if (frontWheelMarker != null) frontWheelMarker.gameObject.SetActive(active);
            for (int i = 0; i < segmentBoundaryLines.Count; i++)
                if (segmentBoundaryLines[i] != null) segmentBoundaryLines[i].enabled = active;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            {
                SetOverlayActive(!ShowOverlay);
                OnOverlayMessage?.Invoke(ShowOverlay
                    ? "Diagnóstico de Geometria (F7): LIGADO — Ciano=Piso Físico | Amarelo=Margem Superior | Verde=Rodas | Magenta=Limites de Segmento"
                    : "Diagnóstico de Geometria (F7): DESLIGADO.",
                    new Color(0.56f, 0.96f, 1f, 1f));
            }
        }

        private void LateUpdate()
        {
            if (!ShowOverlay || cam == null || trackGenerator == null || player == null) return;

            float camX = cam.transform.position.x;
            const int samples = 72;
            const float halfView = 16f;
            physicalSurfaceLine.positionCount = samples;
            upperVergeLine.positionCount = samples;

            for (int i = 0; i < samples; i++)
            {
                float x = (camX - halfView) + (i / (float)(samples - 1)) * (halfView * 2f);
                float h = trackGenerator.GetTrackElevationAtX(x, out _);
                physicalSurfaceLine.SetPosition(i, new Vector3(x, WorldDepth.PlayableGroundY + h, -0.2f));
                upperVergeLine.SetPosition(i, new Vector3(x, WorldDepth.UpperVergeY + h, -0.2f));
            }

            // Pontos de contato das rodas traseira e dianteira
            float px = player.transform.position.x;
            float py = player.transform.position.y;
            float hRear = trackGenerator.GetTrackElevationAtX(px - 0.62f, out _);
            float hFront = trackGenerator.GetTrackElevationAtX(px + 0.62f, out _);
            rearWheelMarker.position = new Vector3(px - 0.62f, WorldDepth.PlayableGroundY + hRear + player.jumpHeight, -0.3f);
            frontWheelMarker.position = new Vector3(px + 0.62f, WorldDepth.PlayableGroundY + hFront + player.jumpHeight, -0.3f);

            // Limites de segmentos procedurais
            trackGenerator.GetActiveSegmentBoundaries(boundaryCache);
            for (int i = 0; i < segmentBoundaryLines.Count; i++)
            {
                LineRenderer lr = segmentBoundaryLines[i];
                if (i < boundaryCache.Count)
                {
                    float bx = boundaryCache[i];
                    lr.enabled = true;
                    lr.positionCount = 2;
                    lr.SetPosition(0, new Vector3(bx, -4.5f, -0.25f));
                    lr.SetPosition(1, new Vector3(bx, -0.5f, -0.25f));
                }
                else
                {
                    lr.enabled = false;
                }
            }
        }

        private static Sprite CreateDotSprite()
        {
            const int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16));
                    tex.SetPixel(x, y, d <= 13f ? Color.white : (d <= 15f ? Color.black : Color.clear));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
