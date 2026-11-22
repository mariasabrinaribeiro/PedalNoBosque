using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 3 — Fábrica visual dos 7 novos animais do ecossistema da floresta:
    /// Ouriço, Guaxinim, Filhote de Urso, Passarinhos, Castor, Sapo e Coruja Noturna.
    /// Carrega PNG ilustrado de Resources/Floresta/ se existir, ou gera sprite procedural detalhado com cache.
    /// </summary>
    public static class EcosystemSpriteFactory
    {
        private static readonly Dictionary<ParkSpriteFactory.AnimalKind, Sprite> spriteCache =
            new Dictionary<ParkSpriteFactory.AnimalKind, Sprite>();

        public static Sprite LoadOrCreateAnimalSprite(ParkSpriteFactory.AnimalKind kind)
        {
            if (spriteCache.TryGetValue(kind, out Sprite cached) && cached != null)
                return cached;

            string resourceName = GetResourceFileName(kind);
            float targetHeight = GetTargetWorldHeight(kind);

            if (!string.IsNullOrEmpty(resourceName))
            {
                Texture2D tex = Resources.Load<Texture2D>($"Floresta/{resourceName}");
                if (tex != null)
                {
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.05f), tex.height / targetHeight);
                    spriteCache[kind] = sp;
                    return sp;
                }
            }

            Sprite generated = CreateProceduralAnimalSprite(kind, targetHeight);
            spriteCache[kind] = generated;
            return generated;
        }

        private static string GetResourceFileName(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo:       return "animal_esquilo";
                case ParkSpriteFactory.AnimalKind.Coelho:        return "animal_coelho";
                case ParkSpriteFactory.AnimalKind.Gato:          return "animal_gato";
                case ParkSpriteFactory.AnimalKind.Lobo:          return "animal_lobo";
                case ParkSpriteFactory.AnimalKind.Quati:         return "animal_quati";
                case ParkSpriteFactory.AnimalKind.Raposa:        return "animal_raposa";
                case ParkSpriteFactory.AnimalKind.Cervo:         return "animal_cervo";
                case ParkSpriteFactory.AnimalKind.Ourico:        return "animal_ourico";
                case ParkSpriteFactory.AnimalKind.Guaxinim:      return "animal_guaxinim";
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:   return "animal_urso";
                case ParkSpriteFactory.AnimalKind.Passarinhos:   return "animal_passarinhos";
                case ParkSpriteFactory.AnimalKind.Castor:        return "animal_castor";
                case ParkSpriteFactory.AnimalKind.Sapo:          return "animal_sapo";
                case ParkSpriteFactory.AnimalKind.CorujaNoturna: return "animal_coruja";
                default:                                         return "animal_esquilo";
            }
        }

        private static float GetTargetWorldHeight(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Lobo:
                case ParkSpriteFactory.AnimalKind.Cervo:       return 2.4f;
                case ParkSpriteFactory.AnimalKind.FilhoteUrso: return 2.15f;
                case ParkSpriteFactory.AnimalKind.Guaxinim:
                case ParkSpriteFactory.AnimalKind.Castor:
                case ParkSpriteFactory.AnimalKind.CorujaNoturna: return 1.95f;
                case ParkSpriteFactory.AnimalKind.Ourico:
                case ParkSpriteFactory.AnimalKind.Passarinhos: return 1.55f;
                case ParkSpriteFactory.AnimalKind.Sapo:        return 1.45f;
                default:                                       return 2.0f;
            }
        }

        // ─── Helpers de Desenho ──────────────────────────────────────

        private static Texture2D BlankTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void RectPx(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            int minX = Mathf.Max(0, x0);
            int maxX = Mathf.Min(tex.width - 1, x1);
            int minY = Mathf.Max(0, y0);
            int maxY = Mathf.Min(tex.height - 1, y1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    tex.SetPixel(x, y, col);
        }

        private static void CirclePx(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    int dx = x - cx; int dy = y - cy;
                    if (dx * dx + dy * dy <= r2) tex.SetPixel(x, y, col);
                }
            }
        }

        private static void EllipsePx(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f) tex.SetPixel(x, y, col);
                }
            }
        }

        private static void TriPx(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color col)
        {
            int minX = Mathf.Max(0, Mathf.Min(x0, Mathf.Min(x1, x2)));
            int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x0, Mathf.Max(x1, x2)));
            int minY = Mathf.Max(0, Mathf.Min(y0, Mathf.Min(y1, y2)));
            int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y0, Mathf.Max(y1, y2)));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d1 = (x - x1) * (y0 - y1) - (x0 - x1) * (y - y1);
                    float d2 = (x - x2) * (y1 - y2) - (x1 - x2) * (y - y2);
                    float d3 = (x - x0) * (y2 - y0) - (x2 - x0) * (y - y0);
                    if (!(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0))))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void CuteEye(Texture2D tex, int cx, int cy, int r = 4)
        {
            CirclePx(tex, cx, cy, r, new Color(0.10f, 0.10f, 0.12f, 1f));
            CirclePx(tex, cx - 1, cy + 1, Mathf.Max(1, r / 3), Color.white);
        }

        private static Sprite CreateProceduralAnimalSprite(ParkSpriteFactory.AnimalKind kind, float targetHeight)
        {
            int w = 128, h = 128;
            Texture2D tex = BlankTex(w, h);
            Color shadow = new Color(0f, 0f, 0f, 0.20f);
            EllipsePx(tex, 64, 10, 34, 7, shadow);

            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Ourico:
                    DrawOurico(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.Guaxinim:
                    DrawGuaxinim(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:
                    DrawFilhoteUrso(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.Passarinhos:
                    DrawPassarinhos(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.Castor:
                    DrawCastor(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.Sapo:
                    DrawSapo(tex);
                    break;
                case ParkSpriteFactory.AnimalKind.CorujaNoturna:
                    DrawCoruja(tex);
                    break;
                default:
                    CirclePx(tex, 64, 52, 28, new Color(0.74f, 0.52f, 0.32f, 1f));
                    CuteEye(tex, 54, 60);
                    CuteEye(tex, 74, 60);
                    break;
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.06f), h / targetHeight);
        }

        private static void DrawOurico(Texture2D tex)
        {
            Color spineDark = new Color(0.36f, 0.25f, 0.18f, 1f);
            Color spineLight = new Color(0.52f, 0.38f, 0.26f, 1f);
            Color face = new Color(0.92f, 0.78f, 0.62f, 1f);
            Color cheek = new Color(0.98f, 0.58f, 0.66f, 0.85f);

            // Espinhos triangulares no dorso
            for (int i = 0; i < 7; i++)
            {
                int bx = 26 + i * 11;
                int by = 48 + (i < 4 ? i * 7 : (6 - i) * 7);
                TriPx(tex, bx - 8, by - 10, bx + 8, by - 10, bx - 4, by + 18, (i % 2 == 0) ? spineDark : spineLight);
            }
            EllipsePx(tex, 56, 38, 34, 24, spineDark);
            EllipsePx(tex, 58, 42, 28, 20, spineLight);

            // Rostinho e focinho fofo apontando à direita
            EllipsePx(tex, 80, 34, 24, 18, face);
            TriPx(tex, 92, 42, 92, 24, 112, 32, face);
            CirclePx(tex, 110, 32, 4, new Color(0.14f, 0.12f, 0.12f, 1f)); // Narizinho
            CirclePx(tex, 76, 48, 6, face); // Orelhinha
            CirclePx(tex, 86, 32, 4, cheek);
            CuteEye(tex, 88, 40, 4);

            // Patinhas
            EllipsePx(tex, 44, 16, 8, 5, face);
            EllipsePx(tex, 74, 16, 8, 5, face);
        }

        private static void DrawGuaxinim(Texture2D tex)
        {
            Color grayDark = new Color(0.32f, 0.34f, 0.38f, 1f);
            Color grayMid = new Color(0.54f, 0.56f, 0.60f, 1f);
            Color cream = new Color(0.92f, 0.92f, 0.90f, 1f);
            Color mask = new Color(0.14f, 0.15f, 0.18f, 1f);

            // Cauda listrada fofa à esquerda
            EllipsePx(tex, 26, 38, 18, 12, grayMid);
            RectPx(tex, 14, 28, 20, 48, mask);
            RectPx(tex, 26, 28, 32, 48, mask);

            // Corpo arredondado
            EllipsePx(tex, 64, 42, 26, 28, grayMid);
            EllipsePx(tex, 66, 38, 16, 18, cream);

            // Cabeça e orelhas
            CirclePx(tex, 66, 78, 24, grayMid);
            TriPx(tex, 44, 92, 58, 96, 46, 114, grayDark);
            TriPx(tex, 74, 96, 88, 92, 86, 114, grayDark);
            TriPx(tex, 47, 94, 55, 97, 48, 108, cream);
            TriPx(tex, 77, 97, 85, 94, 84, 108, cream);

            // Máscara clássica de guaxinim
            EllipsePx(tex, 54, 80, 11, 8, mask);
            EllipsePx(tex, 78, 80, 11, 8, mask);
            EllipsePx(tex, 66, 70, 11, 8, cream);
            CirclePx(tex, 66, 72, 4, mask); // Focinho
            CuteEye(tex, 55, 80, 4);
            CuteEye(tex, 77, 80, 4);

            // Patinhas
            EllipsePx(tex, 50, 16, 9, 6, grayDark);
            EllipsePx(tex, 78, 16, 9, 6, grayDark);
        }

        private static void DrawFilhoteUrso(Texture2D tex)
        {
            Color brown = new Color(0.56f, 0.34f, 0.18f, 1f);
            Color brownLight = new Color(0.72f, 0.48f, 0.28f, 1f);
            Color muzzle = new Color(0.94f, 0.82f, 0.64f, 1f);

            // Corpo fofinho
            EllipsePx(tex, 64, 42, 30, 30, brown);
            EllipsePx(tex, 64, 38, 18, 18, brownLight);

            // Orelhas redondas
            CirclePx(tex, 42, 98, 12, brown);
            CirclePx(tex, 86, 98, 12, brown);
            CirclePx(tex, 42, 98, 6, muzzle);
            CirclePx(tex, 86, 98, 6, muzzle);

            // Cabeça e focinho creme
            CirclePx(tex, 64, 80, 26, brown);
            EllipsePx(tex, 64, 72, 13, 10, muzzle);
            CirclePx(tex, 64, 76, 5, new Color(0.18f, 0.12f, 0.10f, 1f));
            CuteEye(tex, 52, 84, 4);
            CuteEye(tex, 76, 84, 4);

            // Patinhas
            CirclePx(tex, 46, 18, 10, brown);
            CirclePx(tex, 82, 18, 10, brown);
            CirclePx(tex, 46, 18, 5, muzzle);
            CirclePx(tex, 82, 18, 5, muzzle);
        }

        private static void DrawPassarinhos(Texture2D tex)
        {
            // Galhinho de madeira onde os dois passarinhos pousam
            RectPx(tex, 18, 18, 110, 25, new Color(0.50f, 0.30f, 0.14f, 1f));

            // Passarinho 1 (azul-celeste) à esquerda
            Color blue = new Color(0.24f, 0.68f, 0.98f, 1f);
            Color blueBelly = new Color(0.86f, 0.96f, 1f, 1f);
            EllipsePx(tex, 44, 48, 18, 15, blue);
            EllipsePx(tex, 46, 44, 11, 9, blueBelly);
            CirclePx(tex, 52, 62, 12, blue);
            TriPx(tex, 62, 65, 62, 57, 73, 61, new Color(1f, 0.72f, 0.14f, 1f)); // Bico
            TriPx(tex, 20, 56, 32, 50, 26, 40, blue); // Cauda
            CuteEye(tex, 54, 65, 3);

            // Passarinho 2 (amarelo/coral) à direita
            Color yellow = new Color(1f, 0.78f, 0.22f, 1f);
            Color coral = new Color(0.98f, 0.44f, 0.38f, 1f);
            EllipsePx(tex, 86, 46, 17, 14, yellow);
            EllipsePx(tex, 88, 46, 10, 8, coral);
            CirclePx(tex, 78, 60, 12, yellow);
            TriPx(tex, 68, 63, 68, 55, 58, 59, new Color(0.96f, 0.50f, 0.12f, 1f)); // Bico
            TriPx(tex, 108, 54, 96, 48, 102, 38, coral); // Cauda
            CuteEye(tex, 75, 63, 3);
        }

        private static void DrawCastor(Texture2D tex)
        {
            Color brownDark = new Color(0.42f, 0.24f, 0.12f, 1f);
            Color brownMid = new Color(0.60f, 0.36f, 0.18f, 1f);
            Color tailCol = new Color(0.28f, 0.20f, 0.16f, 1f);
            Color tan = new Color(0.84f, 0.66f, 0.46f, 1f);

            // Cauda achatada de castor à esquerda
            EllipsePx(tex, 26, 26, 20, 11, tailCol);
            RectPx(tex, 14, 24, 38, 26, brownDark);

            // Corpo robusto
            EllipsePx(tex, 66, 42, 28, 26, brownMid);
            EllipsePx(tex, 70, 38, 16, 16, tan);

            // Cabeça e orelhinhas
            CirclePx(tex, 72, 76, 22, brownMid);
            CirclePx(tex, 54, 92, 7, brownDark);
            CirclePx(tex, 88, 92, 7, brownDark);

            // Bochechas e dentinhos clássicos de castor!
            CirclePx(tex, 66, 66, 8, tan);
            CirclePx(tex, 80, 66, 8, tan);
            RectPx(tex, 68, 54, 73, 64, Color.white);
            RectPx(tex, 74, 54, 79, 64, Color.white);
            CirclePx(tex, 73, 72, 4, brownDark); // Nariz

            CuteEye(tex, 62, 80, 4);
            CuteEye(tex, 82, 80, 4);

            // Patinhas
            EllipsePx(tex, 52, 16, 9, 5, brownDark);
            EllipsePx(tex, 80, 16, 9, 5, brownDark);
        }

        private static void DrawSapo(Texture2D tex)
        {
            Color lily = new Color(0.16f, 0.56f, 0.24f, 1f);
            Color green = new Color(0.28f, 0.78f, 0.30f, 1f);
            Color greenDark = new Color(0.18f, 0.60f, 0.22f, 1f);
            Color belly = new Color(0.84f, 0.96f, 0.62f, 1f);

            // Vitória-régia sob o sapinho
            EllipsePx(tex, 64, 16, 42, 10, lily);

            // Perninhas dobradas
            EllipsePx(tex, 38, 30, 14, 12, greenDark);
            EllipsePx(tex, 90, 30, 14, 12, greenDark);

            // Corpo e barriguinha clara
            EllipsePx(tex, 64, 42, 28, 22, green);
            EllipsePx(tex, 64, 36, 18, 14, belly);

            // Olhos saltados no topo da cabeça
            CirclePx(tex, 48, 66, 12, green);
            CirclePx(tex, 80, 66, 12, green);
            CirclePx(tex, 48, 66, 8, Color.white);
            CirclePx(tex, 80, 66, 8, Color.white);
            CuteEye(tex, 48, 66, 5);
            CuteEye(tex, 80, 66, 5);

            // Sorriso largo e bochechas rosadas
            RectPx(tex, 50, 48, 78, 50, greenDark);
            CirclePx(tex, 44, 50, 4, new Color(1f, 0.58f, 0.68f, 0.85f));
            CirclePx(tex, 84, 50, 4, new Color(1f, 0.58f, 0.68f, 0.85f));
        }

        private static void DrawCoruja(Texture2D tex)
        {
            Color feather = new Color(0.46f, 0.34f, 0.26f, 1f);
            Color featherLight = new Color(0.68f, 0.54f, 0.42f, 1f);
            Color belly = new Color(0.92f, 0.86f, 0.74f, 1f);
            Color gold = new Color(1f, 0.82f, 0.20f, 1f);

            // Corpo arredondado + asas
            EllipsePx(tex, 64, 50, 30, 34, feather);
            EllipsePx(tex, 36, 48, 10, 22, featherLight);
            EllipsePx(tex, 92, 48, 10, 22, featherLight);
            EllipsePx(tex, 64, 42, 18, 22, belly);

            // Tufos de orelha de coruja
            TriPx(tex, 38, 86, 54, 92, 40, 112, feather);
            TriPx(tex, 74, 92, 90, 86, 88, 112, feather);

            // Cabeça e grandes olhos dourados noturnos
            CirclePx(tex, 64, 80, 26, feather);
            CirclePx(tex, 52, 82, 12, belly);
            CirclePx(tex, 76, 82, 12, belly);
            CirclePx(tex, 52, 82, 9, gold);
            CirclePx(tex, 76, 82, 9, gold);
            CuteEye(tex, 52, 82, 5);
            CuteEye(tex, 76, 82, 5);

            // Bico dourado triangular
            TriPx(tex, 59, 75, 69, 75, 64, 64, new Color(0.96f, 0.64f, 0.14f, 1f));

            // Garrinhas
            RectPx(tex, 50, 14, 58, 20, gold);
            RectPx(tex, 70, 14, 78, 20, gold);
        }
    }
}
