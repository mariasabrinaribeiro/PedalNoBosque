using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Fonte ÚNICA de verdade para a geometria vertical do terreno (estrada plana, pontes, subidas suaves
    /// e subidas/descidas acentuadas de montanha).
    ///
    /// TANTO a física da bicicleta, QUANTO a representação visual da pista curva, o EdgeCollider2D,
    /// e o apoio de animais/árvores/placas consultam exatamente estas funções — impossibilitando
    /// divergência entre a estrada desenhada e a trajetória física das rodas!
    /// </summary>
    public static class TerrainHeightProfile
    {
        public const float HillStartOffset = 6f;
        public const float HillSpanUnits   = 24f;
        public const float HillEndOffset   = HillStartOffset + HillSpanUnits; // 30f

        public const float GentleHillPeakHeight = 0.95f;
        public const float SteepHillPeakHeight  = 1.32f;

        public const float BridgeCenterOffset = 22f;
        public const float BridgeHalfSpan     = 4.25f;
        public const float BridgePeakHeight   = 0.375f;

        /// <summary>
        /// Smoothstep estilo GLSL: retorna [0..1] suave quando x atravessa [edge0..edge1].
        /// (Diferente de UnityEngine.Mathf.SmoothStep(from, to, t) que interpola valores entre from e to!)
        /// </summary>
        public static float SmoothRange(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge1, edge0)) return x < edge0 ? 0f : 1f;
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Retorna a altura de pico da colina para um dado índice de segmento (alterna suave e acentuada).</summary>
        public static float GetHillPeakForSegment(int segmentIndex)
        {
            return (segmentIndex % 2 == 0) ? GentleHillPeakHeight : SteepHillPeakHeight;
        }

        /// <summary>
        /// Curva C1-contínua da colina para t em [0..1]:
        /// h(0) = 0, h'(0) = 0 | h(0.5) = peakHeight, h'(0.5) = 0 | h(1) = 0, h'(1) = 0.
        /// Garante transição perfeitamente tangente com a estrada plana sem degraus nem buracos!
        /// </summary>
        public static float EvaluateHill(float t, float peakHeight, out float pitchAngleDeg)
        {
            if (t <= 0f || t >= 1f)
            {
                pitchAngleDeg = 0f;
                return 0f;
            }
            float h = 0.5f * (1f - Mathf.Cos(t * 2f * Mathf.PI)) * peakHeight;
            float dhdx = (Mathf.Sin(t * 2f * Mathf.PI) * Mathf.PI / HillSpanUnits) * peakHeight;
            pitchAngleDeg = Mathf.Atan(dhdx) * Mathf.Rad2Deg;
            return h;
        }

        /// <summary>Curva suave do tablado arqueado da ponte sobre o rio.</summary>
        public static float EvaluateBridge(float dxFromCenter, out float pitchAngleDeg)
        {
            if (Mathf.Abs(dxFromCenter) > BridgeHalfSpan)
            {
                pitchAngleDeg = 0f;
                return 0f;
            }
            float t = (dxFromCenter + BridgeHalfSpan) / (2f * BridgeHalfSpan);
            float h = Mathf.Sin(t * Mathf.PI) * BridgePeakHeight;
            float dhdx = (Mathf.Cos(t * Mathf.PI) * Mathf.PI / (2f * BridgeHalfSpan)) * BridgePeakHeight;
            pitchAngleDeg = Mathf.Atan(dhdx) * Mathf.Rad2Deg;
            return h;
        }

        /// <summary>Avalia a elevação e inclinação exatas do terreno para um segmento e coordenada worldX.</summary>
        public static float EvaluateSegment(BiomeType biome, int segmentIndex, float segStartX, float worldX, out float pitchAngleDeg)
        {
            pitchAngleDeg = 0f;
            if (biome == BiomeType.RegiaoDoRio)
            {
                float dx = worldX - (segStartX + BridgeCenterOffset);
                return EvaluateBridge(dx, out pitchAngleDeg);
            }
            if (biome == BiomeType.Colinas)
            {
                float hillStart = segStartX + HillStartOffset;
                float hillEnd = segStartX + HillEndOffset;
                if (worldX >= hillStart && worldX <= hillEnd)
                {
                    float t = (worldX - hillStart) / HillSpanUnits;
                    return EvaluateHill(t, GetHillPeakForSegment(segmentIndex), out pitchAngleDeg);
                }
            }
            return 0f;
        }

        /// <summary>
        /// Constrói um EdgeCollider2D (isTrigger = true) exatamente sobre a superfície física da colina,
        /// derivado do mesmo perfil compartilhado EvaluateHill (zero divergência visual/física!).
        /// </summary>
        public static EdgeCollider2D AttachHillSurfaceCollider(GameObject target, float peakHeight, int samples = 32)
        {
            EdgeCollider2D edge = target.AddComponent<EdgeCollider2D>();
            edge.isTrigger = true;
            Vector2[] pts = new Vector2[samples + 1];
            float halfW = HillSpanUnits * 0.5f;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float lx = -halfW + t * HillSpanUnits;
                float ly = EvaluateHill(t, peakHeight, out _);
                pts[i] = new Vector2(lx, ly);
            }
            edge.points = pts;
            return edge;
        }
    }
}
