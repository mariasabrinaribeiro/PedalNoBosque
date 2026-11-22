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
        private static readonly Dictionary<int, Sprite> ambientDogCache =
            new Dictionary<int, Sprite>();

        public static Sprite GetAnimalSprite(ParkSpriteFactory.AnimalKind kind) => LoadOrCreateAnimalSprite(kind);

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
                    Vector2 pivot = GetResourcePivot(kind);
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.height / targetHeight);
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

        private static Vector2 GetResourcePivot(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo: return new Vector2(0.46f, 0.0947f);
                case ParkSpriteFactory.AnimalKind.Coelho:  return new Vector2(0.52f, 0.1143f);
                case ParkSpriteFactory.AnimalKind.Gato:    return new Vector2(0.53f, 0.0967f);
                case ParkSpriteFactory.AnimalKind.Lobo:    return new Vector2(0.56f, 0.0771f);
                case ParkSpriteFactory.AnimalKind.Quati:   return new Vector2(0.53f, 0.0928f);
                case ParkSpriteFactory.AnimalKind.Raposa:  return new Vector2(0.55f, 0.1338f);
                case ParkSpriteFactory.AnimalKind.Cervo:   return new Vector2(0.48f, 0.1553f);
                default:                                   return new Vector2(0.50f, 0.0947f);
            }
        }

        private static float GetTargetWorldHeight(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo:       return 1.48f; // ~1.20m visível
                case ParkSpriteFactory.AnimalKind.Coelho:        return 1.48f; // ~1.15m visível
                case ParkSpriteFactory.AnimalKind.Ourico:        return 1.15f;
                case ParkSpriteFactory.AnimalKind.Sapo:          return 1.05f;
                case ParkSpriteFactory.AnimalKind.Passarinhos:   return 0.95f;
                case ParkSpriteFactory.AnimalKind.Gato:          return 1.55f;
                case ParkSpriteFactory.AnimalKind.Quati:         return 1.65f;
                case ParkSpriteFactory.AnimalKind.Guaxinim:
                case ParkSpriteFactory.AnimalKind.Castor:        return 1.60f;
                case ParkSpriteFactory.AnimalKind.Raposa:        return 1.85f;
                case ParkSpriteFactory.AnimalKind.CorujaNoturna: return 1.50f;
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:   return 2.00f;
                case ParkSpriteFactory.AnimalKind.Lobo:          return 2.25f;
                case ParkSpriteFactory.AnimalKind.Cervo:         return 2.75f; // ~1.80m visível
                default:                                         return 1.48f;
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
            CirclePx(tex, cx, cy, r + 1, new Color(0.16f, 0.08f, 0.06f, 1f));
            CirclePx(tex, cx, cy, r, new Color(0.08f, 0.08f, 0.10f, 1f));
            CirclePx(tex, cx - Mathf.Max(1, r / 3), cy + Mathf.Max(1, r / 3), Mathf.Max(2, r / 2), Color.white);
        }

        private static void OutlinedEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color ink, Color fill, Color highlight, int outlinePx = 4)
        {
            EllipsePx(tex, cx, cy, rx + outlinePx, ry + outlinePx, ink);
            EllipsePx(tex, cx, cy, rx, ry, fill);
            if (rx > 8 && ry > 8)
                EllipsePx(tex, cx - rx / 5, cy + ry / 5, Mathf.Max(3, (rx * 3) / 5), Mathf.Max(3, (ry * 3) / 5), highlight);
        }

        private static void OutlinedCircle(Texture2D tex, int cx, int cy, int r, Color ink, Color fill, Color highlight, int outlinePx = 4)
        {
            CirclePx(tex, cx, cy, r + outlinePx, ink);
            CirclePx(tex, cx, cy, r, fill);
            if (r > 8)
                CirclePx(tex, cx - r / 5, cy + r / 5, Mathf.Max(3, (r * 3) / 5), highlight);
        }

        private static void DrawDetailedStorybookBear(
            Texture2D tex, int cx, int baseY, float scale,
            Color furDark, Color furMid, Color furLight, Color muzzleCol,
            Color accessoryCol, bool hasFlowerCrown, bool holdsHoneyPot)
        {
            Color ink = new Color(0.22f, 0.10f, 0.07f, 1f);
            Color blush = new Color(0.96f, 0.56f, 0.58f, 0.85f);
            int o = Mathf.Max(3, Mathf.RoundToInt(4f * scale));

            int bodyRX = Mathf.RoundToInt(46f * scale);
            int bodyRY = Mathf.RoundToInt(48f * scale);
            int bodyY = baseY + bodyRY + Mathf.RoundToInt(10f * scale);

            // Orelhas redondas contornadas em #381A12
            int headR = Mathf.RoundToInt(40f * scale);
            int headY = bodyY + Mathf.RoundToInt(58f * scale);
            int earDX = Mathf.RoundToInt(30f * scale);
            int earY = headY + Mathf.RoundToInt(28f * scale);
            int earR = Mathf.RoundToInt(16f * scale);

            OutlinedCircle(tex, cx - earDX, earY, earR, ink, furDark, furMid, o);
            CirclePx(tex, cx - earDX, earY, Mathf.RoundToInt(8f * scale), muzzleCol);
            OutlinedCircle(tex, cx + earDX, earY, earR, ink, furDark, furMid, o);
            CirclePx(tex, cx + earDX, earY, Mathf.RoundToInt(8f * scale), muzzleCol);

            // Tronco fofo com barriguinha creme/avelã e textura de pelo
            OutlinedEllipse(tex, cx, bodyY, bodyRX, bodyRY, ink, furDark, furMid, o);
            EllipsePx(tex, cx, bodyY - Mathf.RoundToInt(4f * scale), Mathf.RoundToInt(28f * scale), Mathf.RoundToInt(30f * scale), muzzleCol);
            EllipsePx(tex, cx - Mathf.RoundToInt(4f * scale), bodyY, Mathf.RoundToInt(20f * scale), Mathf.RoundToInt(22f * scale), new Color(0.98f, 0.91f, 0.78f, 1f));

            // Cachecol / colete acolhedor do bosque no pescoço
            int scarfY = bodyY + Mathf.RoundToInt(34f * scale);
            EllipsePx(tex, cx, scarfY, Mathf.RoundToInt(36f * scale) + o, Mathf.RoundToInt(11f * scale) + o, ink);
            EllipsePx(tex, cx, scarfY, Mathf.RoundToInt(36f * scale), Mathf.RoundToInt(11f * scale), accessoryCol);

            // Patinhas traseiras apoiadas no chão com almofadinhas
            int footDX = Mathf.RoundToInt(28f * scale);
            int footY = baseY + Mathf.RoundToInt(16f * scale);
            int footR = Mathf.RoundToInt(16f * scale);
            OutlinedCircle(tex, cx - footDX, footY, footR, ink, furDark, furMid, o);
            CirclePx(tex, cx - footDX, footY - 1, Mathf.RoundToInt(8f * scale), muzzleCol);
            OutlinedCircle(tex, cx + footDX, footY, footR, ink, furDark, furMid, o);
            CirclePx(tex, cx + footDX, footY - 1, Mathf.RoundToInt(8f * scale), muzzleCol);

            // Braços acolhedores
            int armDX = Mathf.RoundToInt(40f * scale);
            int armY = bodyY + Mathf.RoundToInt(6f * scale);
            OutlinedEllipse(tex, cx - armDX, armY, Mathf.RoundToInt(14f * scale), Mathf.RoundToInt(22f * scale), ink, furDark, furMid, o);
            OutlinedEllipse(tex, cx + armDX, armY, Mathf.RoundToInt(14f * scale), Mathf.RoundToInt(22f * scale), ink, furDark, furMid, o);

            // Cabeça sombreada estilo livro ilustrado
            OutlinedCircle(tex, cx, headY, headR, ink, furMid, furLight, o);

            // Bochechas coradas e focinho esculpido
            int eyeDX = Mathf.RoundToInt(16f * scale);
            int eyeY = headY + Mathf.RoundToInt(5f * scale);
            CirclePx(tex, cx - eyeDX - Mathf.RoundToInt(5f * scale), headY - Mathf.RoundToInt(6f * scale), Mathf.RoundToInt(6f * scale), blush);
            CirclePx(tex, cx + eyeDX + Mathf.RoundToInt(5f * scale), headY - Mathf.RoundToInt(6f * scale), Mathf.RoundToInt(6f * scale), blush);

            int muzzleY = headY - Mathf.RoundToInt(9f * scale);
            EllipsePx(tex, cx, muzzleY, Mathf.RoundToInt(20f * scale) + 2, Mathf.RoundToInt(15f * scale) + 2, ink);
            EllipsePx(tex, cx, muzzleY, Mathf.RoundToInt(20f * scale), Mathf.RoundToInt(15f * scale), muzzleCol);
            EllipsePx(tex, cx, muzzleY + Mathf.RoundToInt(4f * scale), Mathf.RoundToInt(9f * scale), Mathf.RoundToInt(6f * scale), ink);
            CirclePx(tex, cx - Mathf.RoundToInt(2f * scale), muzzleY + Mathf.RoundToInt(5f * scale), Mathf.Max(1, Mathf.RoundToInt(2f * scale)), Color.white);
            // Sorriso simpático
            RectPx(tex, cx - 1, muzzleY - Mathf.RoundToInt(7f * scale), cx + 1, muzzleY - 1, ink);
            RectPx(tex, cx - Mathf.RoundToInt(7f * scale), muzzleY - Mathf.RoundToInt(8f * scale), cx + Mathf.RoundToInt(7f * scale), muzzleY - Mathf.RoundToInt(6f * scale), ink);

            CuteEye(tex, cx - eyeDX, eyeY, Mathf.Max(3, Mathf.RoundToInt(5.5f * scale)));
            CuteEye(tex, cx + eyeDX, eyeY, Mathf.Max(3, Mathf.RoundToInt(5.5f * scale)));

            // Coroa de flores da Mamãe Ursa
            if (hasFlowerCrown)
            {
                int crownY = headY + headR - Mathf.RoundToInt(6f * scale);
                Color[] fCols =
                {
                    new Color(1f, 0.48f, 0.68f, 1f),
                    new Color(1f, 0.88f, 0.26f, 1f),
                    new Color(0.56f, 0.82f, 1f, 1f)
                };
                for (int i = -2; i <= 2; i++)
                {
                    int fx = cx + i * Mathf.RoundToInt(11f * scale);
                    int fy = crownY - Mathf.Abs(i) * 2;
                    CirclePx(tex, fx, fy, Mathf.RoundToInt(7f * scale), ink);
                    CirclePx(tex, fx, fy, Mathf.RoundToInt(5f * scale), fCols[Mathf.Abs(i) % fCols.Length]);
                    CirclePx(tex, fx, fy, Mathf.RoundToInt(2f * scale), Color.white);
                }
            }

            // Potinho de mel dourado nas patinhas
            if (holdsHoneyPot)
            {
                int hx = cx + Mathf.RoundToInt(22f * scale);
                int hy = bodyY - Mathf.RoundToInt(10f * scale);
                OutlinedEllipse(tex, hx, hy, Mathf.RoundToInt(15f * scale), Mathf.RoundToInt(14f * scale),
                    ink, new Color(0.96f, 0.72f, 0.18f, 1f), new Color(1f, 0.88f, 0.42f, 1f), 3);
                RectPx(tex, hx - Mathf.RoundToInt(12f * scale), hy + Mathf.RoundToInt(8f * scale),
                    hx + Mathf.RoundToInt(12f * scale), hy + Mathf.RoundToInt(14f * scale), new Color(0.98f, 0.94f, 0.82f, 1f));
            }
        }

        private static Sprite CreateProceduralAnimalSprite(ParkSpriteFactory.AnimalKind kind, float targetHeight)
        {
            if (kind == ParkSpriteFactory.AnimalKind.FilhoteUrso)
            {
                // Família de Ursos do Bosque em alta resolução (Papai Urso, Mamãe Ursa, Irmão Urso e Bebê Ursinho bem espaçados, sem amontoar!)
                int bw = 500, bh = 280;
                Texture2D bTex = BlankTex(bw, bh);

                // Papai Urso (à esquerda, castanho escuro, cachecol verde musgo e pote de mel)
                DrawDetailedStorybookBear(
                    bTex, 86, 14, 0.98f,
                    new Color(0.46f, 0.26f, 0.14f, 1f),
                    new Color(0.60f, 0.36f, 0.19f, 1f),
                    new Color(0.74f, 0.48f, 0.27f, 1f),
                    new Color(0.94f, 0.82f, 0.65f, 1f),
                    new Color(0.28f, 0.56f, 0.36f, 1f),
                    hasFlowerCrown: false, holdsHoneyPot: true);

                // Irmãozinho Urso (centro-esquerda, cachecol azul céu, espaçado do Papai Urso)
                DrawDetailedStorybookBear(
                    bTex, 200, 12, 0.66f,
                    new Color(0.50f, 0.29f, 0.15f, 1f),
                    new Color(0.66f, 0.41f, 0.22f, 1f),
                    new Color(0.80f, 0.54f, 0.31f, 1f),
                    new Color(0.96f, 0.85f, 0.68f, 1f),
                    new Color(0.36f, 0.68f, 0.90f, 1f),
                    hasFlowerCrown: false, holdsHoneyPot: false);

                // Bebê Ursinho / Neném (centro-direita, cachecol amarelo mel)
                DrawDetailedStorybookBear(
                    bTex, 290, 10, 0.52f,
                    new Color(0.56f, 0.34f, 0.18f, 1f),
                    new Color(0.74f, 0.47f, 0.26f, 1f),
                    new Color(0.88f, 0.62f, 0.36f, 1f),
                    new Color(0.98f, 0.89f, 0.74f, 1f),
                    new Color(0.98f, 0.78f, 0.24f, 1f),
                    hasFlowerCrown: false, holdsHoneyPot: false);

                // Mamãe Ursa (à direita, castanho canela dourado, coroa de flores e lenço coral)
                DrawDetailedStorybookBear(
                    bTex, 404, 14, 0.92f,
                    new Color(0.54f, 0.31f, 0.16f, 1f),
                    new Color(0.70f, 0.43f, 0.23f, 1f),
                    new Color(0.84f, 0.57f, 0.33f, 1f),
                    new Color(0.97f, 0.86f, 0.70f, 1f),
                    new Color(0.92f, 0.42f, 0.46f, 1f),
                    hasFlowerCrown: true, holdsHoneyPot: false);

                bTex.Apply();
                return Sprite.Create(bTex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 10f / (float)bh), bh / 2.15f);
            }

            // Guaxinim usa a arte 1024x1024 de estúdio de animal_quati se disponível
            if (kind == ParkSpriteFactory.AnimalKind.Guaxinim)
            {
                Texture2D quatiTex = Resources.Load<Texture2D>("Floresta/animal_quati");
                if (quatiTex != null)
                {
                    return Sprite.Create(quatiTex, new Rect(0, 0, quatiTex.width, quatiTex.height),
                        new Vector2(0.53f, 0.0928f), quatiTex.height / targetHeight);
                }
            }

            int w = 220, h = 220;
            Texture2D tex = BlankTex(w, h);
            Color ink = new Color(0.22f, 0.10f, 0.07f, 1f);

            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Ourico:
                    DrawOurico(tex, ink);
                    break;
                case ParkSpriteFactory.AnimalKind.Passarinhos:
                    DrawPassarinhos(tex, ink);
                    break;
                case ParkSpriteFactory.AnimalKind.Castor:
                    DrawCastor(tex, ink);
                    break;
                case ParkSpriteFactory.AnimalKind.Sapo:
                    DrawSapo(tex, ink);
                    break;
                case ParkSpriteFactory.AnimalKind.CorujaNoturna:
                    DrawCoruja(tex, ink);
                    break;
                default:
                    DrawDetailedStorybookBear(
                        tex, 110, 14, 0.82f,
                        new Color(0.52f, 0.30f, 0.16f, 1f),
                        new Color(0.68f, 0.42f, 0.22f, 1f),
                        new Color(0.82f, 0.56f, 0.32f, 1f),
                        new Color(0.96f, 0.85f, 0.68f, 1f),
                        new Color(0.92f, 0.42f, 0.46f, 1f),
                        false, true);
                    break;
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 14f / (float)h), h / targetHeight);
        }

        private static void DrawOurico(Texture2D tex, Color ink)
        {
            Color spineDark = new Color(0.38f, 0.24f, 0.15f, 1f);
            Color spineMid = new Color(0.54f, 0.36f, 0.23f, 1f);
            Color spineLight = new Color(0.70f, 0.50f, 0.33f, 1f);
            Color face = new Color(0.95f, 0.82f, 0.66f, 1f);
            Color cheek = new Color(0.98f, 0.56f, 0.64f, 0.90f);

            // Manto de espinhos em camadas contornadas em #381A12
            OutlinedEllipse(tex, 96, 72, 58, 44, ink, spineDark, spineMid, 4);
            for (int i = 0; i < 8; i++)
            {
                int bx = 48 + i * 14;
                int by = 82 + (i < 4 ? i * 8 : (7 - i) * 8);
                OutlinedCircle(tex, bx, by, 14, ink, (i % 2 == 0) ? spineDark : spineMid, spineLight, 3);
            }

            // Rostinho e focinho sorridente apontando à direita
            OutlinedEllipse(tex, 138, 62, 36, 28, ink, face, new Color(0.99f, 0.91f, 0.78f, 1f), 4);
            OutlinedCircle(tex, 126, 86, 11, ink, spineMid, face, 3);
            CirclePx(tex, 174, 60, 7, ink);
            CirclePx(tex, 172, 62, 2, Color.white);
            CirclePx(tex, 148, 56, 7, cheek);
            CuteEye(tex, 150, 70, 6);

            // Patinhas com contorno escuro
            OutlinedEllipse(tex, 74, 24, 14, 9, ink, spineMid, face, 3);
            OutlinedEllipse(tex, 128, 24, 14, 9, ink, spineMid, face, 3);
        }

        private static void DrawPassarinhos(Texture2D tex, Color ink)
        {
            // Galho florido de carvalho onde a família de passarinhos pousa
            RectPx(tex, 26, 26, 194, 42, ink);
            RectPx(tex, 29, 29, 191, 39, new Color(0.56f, 0.34f, 0.16f, 1f));
            RectPx(tex, 29, 35, 191, 39, new Color(0.74f, 0.48f, 0.24f, 1f));

            // Passarinho 1 (azul-celeste) à esquerda
            Color blueDark = new Color(0.18f, 0.54f, 0.86f, 1f);
            Color blueLight = new Color(0.42f, 0.78f, 0.98f, 1f);
            Color belly = new Color(0.94f, 0.98f, 1f, 1f);
            OutlinedEllipse(tex, 74, 76, 30, 24, ink, blueDark, blueLight, 4);
            EllipsePx(tex, 78, 70, 18, 14, belly);
            OutlinedCircle(tex, 86, 102, 20, ink, blueDark, blueLight, 4);
            TriPx(tex, 104, 108, 104, 94, 122, 101, ink);
            TriPx(tex, 105, 106, 105, 96, 119, 101, new Color(1f, 0.76f, 0.16f, 1f));
            CuteEye(tex, 90, 106, 5);

            // Passarinho 2 (amarelo dourado e coral) à direita
            Color goldDark = new Color(0.92f, 0.62f, 0.16f, 1f);
            Color goldLight = new Color(1f, 0.84f, 0.32f, 1f);
            OutlinedEllipse(tex, 148, 74, 28, 23, ink, goldDark, goldLight, 4);
            OutlinedCircle(tex, 136, 98, 19, ink, goldDark, goldLight, 4);
            TriPx(tex, 118, 104, 118, 90, 102, 97, ink);
            TriPx(tex, 117, 102, 117, 92, 105, 97, new Color(0.96f, 0.48f, 0.14f, 1f));
            CuteEye(tex, 132, 102, 5);
        }

        private static void DrawCastor(Texture2D tex, Color ink)
        {
            Color brownDark = new Color(0.44f, 0.24f, 0.12f, 1f);
            Color brownMid = new Color(0.62f, 0.38f, 0.19f, 1f);
            Color brownLight = new Color(0.78f, 0.52f, 0.28f, 1f);
            Color tailCol = new Color(0.32f, 0.22f, 0.18f, 1f);
            Color tan = new Color(0.92f, 0.76f, 0.56f, 1f);

            // Cauda achatada quadriculada clássica do castor
            OutlinedEllipse(tex, 48, 44, 34, 18, ink, tailCol, brownDark, 4);

            // Corpo robusto e barriguinha avelã
            OutlinedEllipse(tex, 114, 72, 46, 44, ink, brownDark, brownMid, 4);
            EllipsePx(tex, 118, 66, 28, 28, tan);

            // Cabeça, orelhas e dentinhos brancos contornados
            OutlinedCircle(tex, 92, 158, 12, ink, brownDark, brownMid, 3);
            OutlinedCircle(tex, 148, 158, 12, ink, brownDark, brownMid, 3);
            OutlinedCircle(tex, 120, 130, 36, ink, brownMid, brownLight, 4);

            RectPx(tex, 112, 94, 128, 112, ink);
            RectPx(tex, 114, 96, 120, 110, Color.white);
            RectPx(tex, 121, 96, 127, 110, Color.white);
            OutlinedEllipse(tex, 110, 114, 13, 10, ink, tan, tan, 3);
            OutlinedEllipse(tex, 130, 114, 13, 10, ink, tan, tan, 3);
            CirclePx(tex, 120, 122, 7, ink);

            CuteEye(tex, 104, 136, 5);
            CuteEye(tex, 136, 136, 5);

            OutlinedEllipse(tex, 92, 26, 16, 10, ink, brownDark, brownMid, 3);
            OutlinedEllipse(tex, 138, 26, 16, 10, ink, brownDark, brownMid, 3);
        }

        private static void DrawSapo(Texture2D tex, Color ink)
        {
            Color lilyDark = new Color(0.18f, 0.54f, 0.24f, 1f);
            Color lilyLight = new Color(0.36f, 0.72f, 0.32f, 1f);
            Color greenDark = new Color(0.20f, 0.58f, 0.22f, 1f);
            Color greenMid = new Color(0.34f, 0.76f, 0.30f, 1f);
            Color greenLight = new Color(0.52f, 0.88f, 0.38f, 1f);
            Color belly = new Color(0.92f, 0.98f, 0.70f, 1f);

            // Vitória-régia contornada sob o sapinho
            OutlinedEllipse(tex, 110, 26, 70, 16, ink, lilyDark, lilyLight, 4);

            // Perninhas e corpo
            OutlinedEllipse(tex, 68, 52, 22, 20, ink, greenDark, greenMid, 4);
            OutlinedEllipse(tex, 152, 52, 22, 20, ink, greenDark, greenMid, 4);
            OutlinedEllipse(tex, 110, 72, 46, 36, ink, greenMid, greenLight, 4);
            EllipsePx(tex, 110, 62, 30, 22, belly);

            // Olhos expressivos no topo
            OutlinedCircle(tex, 84, 112, 20, ink, greenMid, greenLight, 4);
            OutlinedCircle(tex, 136, 112, 20, ink, greenMid, greenLight, 4);
            CirclePx(tex, 84, 112, 13, Color.white);
            CirclePx(tex, 136, 112, 13, Color.white);
            CuteEye(tex, 84, 112, 7);
            CuteEye(tex, 136, 112, 7);

            CirclePx(tex, 76, 84, 7, new Color(1f, 0.56f, 0.66f, 0.9f));
            CirclePx(tex, 144, 84, 7, new Color(1f, 0.56f, 0.66f, 0.9f));
            RectPx(tex, 88, 82, 132, 85, ink);
        }

        private static void DrawCoruja(Texture2D tex, Color ink)
        {
            Color featherDark = new Color(0.42f, 0.28f, 0.20f, 1f);
            Color featherMid = new Color(0.58f, 0.42f, 0.30f, 1f);
            Color featherLight = new Color(0.76f, 0.60f, 0.44f, 1f);
            Color belly = new Color(0.95f, 0.89f, 0.76f, 1f);
            Color gold = new Color(1f, 0.84f, 0.22f, 1f);

            // Corpo e asas em camadas contornadas
            OutlinedEllipse(tex, 64, 84, 18, 36, ink, featherDark, featherMid, 4);
            OutlinedEllipse(tex, 156, 84, 18, 36, ink, featherDark, featherMid, 4);
            OutlinedEllipse(tex, 110, 84, 48, 54, ink, featherDark, featherMid, 4);
            EllipsePx(tex, 110, 72, 30, 36, belly);

            // Cabeça e grandes olhos dourados
            OutlinedCircle(tex, 110, 140, 42, ink, featherMid, featherLight, 4);
            OutlinedCircle(tex, 90, 142, 20, ink, belly, Color.white, 3);
            OutlinedCircle(tex, 130, 142, 20, ink, belly, Color.white, 3);
            CirclePx(tex, 90, 142, 14, gold);
            CirclePx(tex, 130, 142, 14, gold);
            CuteEye(tex, 90, 142, 8);
            CuteEye(tex, 130, 142, 8);

            TriPx(tex, 102, 130, 118, 130, 110, 112, ink);
            TriPx(tex, 104, 128, 116, 128, 110, 115, new Color(0.98f, 0.66f, 0.16f, 1f));

            OutlinedEllipse(tex, 92, 26, 14, 8, ink, gold, gold, 3);
            OutlinedEllipse(tex, 128, 26, 14, 8, ink, gold, gold, 3);
        }

        /// <summary>
        /// Cãozinho ambiental amigo da floresta com variações de pelagem (malhado caramelo/branco, chocolate com lenço azul,
        /// creme com manchas avelã e lenço verde) para diferenciar claramente os cãezinhos do cenário do cachorrinho
        /// companheiro dourado da protagonista!
        /// </summary>
        public static Sprite LoadOrCreateAmbientForestDogSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 3;
            if (ambientDogCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            int w = 220, h = 210;
            Texture2D tex = BlankTex(w, h);
            Color ink = new Color(0.20f, 0.09f, 0.06f, 1f);

            Color furMain, furSpot, furBelly, scarfCol;
            if (key == 0)
            {
                // Cãozinho Malhado (branco creme com manchas caramelo e lenço azul céu)
                furMain = new Color(0.96f, 0.92f, 0.84f, 1f);
                furSpot = new Color(0.68f, 0.38f, 0.18f, 1f);
                furBelly = new Color(1.00f, 0.98f, 0.94f, 1f);
                scarfCol = new Color(0.32f, 0.66f, 0.92f, 1f);
            }
            else if (key == 1)
            {
                // Cãozinho Chocolate com peito creme e lenço verde musgo
                furMain = new Color(0.54f, 0.32f, 0.18f, 1f);
                furSpot = new Color(0.38f, 0.21f, 0.11f, 1f);
                furBelly = new Color(0.94f, 0.86f, 0.72f, 1f);
                scarfCol = new Color(0.34f, 0.68f, 0.36f, 1f);
            }
            else
            {
                // Cãozinho Tricolor do Bosque com lenço amarelo sol
                furMain = new Color(0.84f, 0.56f, 0.30f, 1f);
                furSpot = new Color(0.34f, 0.22f, 0.16f, 1f);
                furBelly = new Color(0.97f, 0.93f, 0.86f, 1f);
                scarfCol = new Color(0.98f, 0.78f, 0.22f, 1f);
            }

            // Rabinho alegre levantado
            OutlinedEllipse(tex, 54, 76, 20, 10, ink, furSpot, furMain, 4);

            // Patinhas apoiadas no chão
            int[] px = { 76, 98, 128, 150 };
            for (int i = 0; i < px.Length; i++)
            {
                OutlinedEllipse(tex, px[i], 28, 11, 16, ink, (i % 2 == 0) ? furSpot : furMain, furBelly, 3);
                OutlinedEllipse(tex, px[i] + 2, 16, 11, 6, ink, furBelly, Color.white, 3);
            }

            // Tronco e manchinha nas costas
            OutlinedEllipse(tex, 112, 56, 44, 26, ink, furMain, furBelly, 4);
            EllipsePx(tex, 98, 66, 18, 12, furSpot);
            EllipsePx(tex, 118, 48, 26, 13, furBelly);

            // Lenço colorido de explorador no pescoço
            OutlinedEllipse(tex, 130, 74, 24, 8, ink, scarfCol, Color.white, 3);

            // Cabeça, mancha no olho e orelhas pontudas/dobradas distintas do companheiro
            OutlinedCircle(tex, 134, 112, 34, ink, furMain, furBelly, 4);
            CirclePx(tex, 122, 118, 14, furSpot);

            // Orelhas erguidas/dobradas charmosas
            OutlinedEllipse(tex, 106, 138, 13, 20, ink, furSpot, furMain, 3);
            OutlinedEllipse(tex, 156, 138, 13, 20, ink, furSpot, furMain, 3);

            // Focinho sorridente com linguinha rosada
            OutlinedEllipse(tex, 144, 102, 16, 11, ink, furBelly, Color.white, 3);
            EllipsePx(tex, 150, 107, 6, 4, ink);
            CuteEye(tex, 124, 118, 5);
            CuteEye(tex, 146, 118, 5);

            // Linguinha rosada simpática
            OutlinedEllipse(tex, 146, 91, 5, 6, ink, new Color(1f, 0.48f, 0.60f, 1f), new Color(1f, 0.70f, 0.78f, 1f), 2);

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.52f, 12f / (float)h), 175f);
            ambientDogCache[key] = sp;
            return sp;
        }
    }
}
