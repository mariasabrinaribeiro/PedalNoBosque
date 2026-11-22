using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Carrega sprites ilustrados de Resources/Floresta/ e gera props nítidos de parque visto de cima
    /// (árvores, flores, vasos, bancos de praça, colunas de água, setas de subida/descida, comida e lágrimas).
    /// </summary>
    public static class ParkSpriteFactory
    {
        public enum AnimalKind
        {
            Esquilo,
            Coelho,
            Gato,
            Lobo,
            Quati,
            Raposa,
            Cervo,
            // Novos animais do ecossistema (Etapa 3)
            Ourico,
            Guaxinim,
            FilhoteUrso,
            Passarinhos,
            Castor,
            Sapo,
            CorujaNoturna
        }

        private static Sprite LoadRes(string path, float targetHeight, float pivotY = 0.5f)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            float ppu = tex.height / Mathf.Max(0.1f, targetHeight);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, pivotY), ppu);
        }

        public static Sprite LoadTopDownBikeSprite()
        {
            Sprite s = LoadRes("Floresta/girl_bike_topdown", 2.4f, 0.5f);
            if (s != null) return s;
            return LoadRes("Floresta/girl_bike", 2.4f, 0.5f);
        }

        public static Sprite LoadAnimalSprite(AnimalKind kind)
        {
            string file = kind switch
            {
                AnimalKind.Esquilo => "animal_esquilo",
                AnimalKind.Coelho  => "animal_coelho",
                AnimalKind.Gato    => "animal_gato",
                AnimalKind.Lobo    => "animal_lobo",
                AnimalKind.Quati   => "animal_quati",
                AnimalKind.Raposa  => "animal_raposa",
                AnimalKind.Cervo   => "animal_cervo",
                _ => "animal_esquilo"
            };
            return LoadRes($"Floresta/{file}", 1.85f, 0.5f);
        }

        // ─── Helpers de Desenho ──────────────────────────────────────

        private static Texture2D NewTex(int w, int h, FilterMode filter = FilterMode.Bilinear)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = filter;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int w, int h, Color c)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = x0; x < x0 + w; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    tex.SetPixel(x, y, c);
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    int dx = x - cx; int dy = y - cy;
                    if (dx * dx + dy * dy <= r2) tex.SetPixel(x, y, c);
                }
            }
        }

        private static void FillTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
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
                        tex.SetPixel(x, y, c);
                }
            }
        }

        // ─── Cenário Top-Down do Parque ──────────────────────────────

        public static Sprite CreateGrassTileSprite()
        {
            Texture2D tex = NewTex(128, 128);
            Color baseGrass = new Color(0.42f, 0.76f, 0.34f, 1f);
            Color patchLight = new Color(0.49f, 0.82f, 0.39f, 1f);
            Color patchDark = new Color(0.36f, 0.69f, 0.29f, 1f);
            FillRect(tex, 0, 0, 128, 128, baseGrass);
            FillCircle(tex, 26, 30, 16, patchLight);
            FillCircle(tex, 96, 86, 18, patchLight);
            FillCircle(tex, 84, 24, 14, patchDark);
            FillCircle(tex, 24, 98, 15, patchDark);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateTrackTileSprite()
        {
            Texture2D tex = NewTex(128, 128);
            Color border = new Color(0.68f, 0.58f, 0.44f, 1f);
            Color road = new Color(0.88f, 0.80f, 0.64f, 1f);
            Color centerLine = new Color(0.96f, 0.92f, 0.80f, 0.85f);
            FillRect(tex, 0, 0, 128, 128, border);
            FillRect(tex, 8, 8, 112, 112, road);
            FillRect(tex, 60, 18, 8, 32, centerLine);
            FillRect(tex, 60, 78, 8, 32, centerLine);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateSlopeChevronSprite(bool isUphill)
        {
            Texture2D tex = NewTex(96, 96);
            Color bg = isUphill ? new Color(0.95f, 0.45f, 0.18f, 0.28f) : new Color(0.18f, 0.72f, 0.95f, 0.28f);
            Color arrow = isUphill ? new Color(1f, 0.48f, 0.16f, 0.92f) : new Color(0.14f, 0.78f, 0.98f, 0.92f);
            FillRect(tex, 8, 8, 80, 80, bg);
            // Duas setas apontando para cima (se subida) ou para baixo (se descida)
            if (isUphill)
            {
                FillTriangle(tex, 20, 22, 76, 22, 48, 50, arrow);
                FillTriangle(tex, 20, 52, 76, 52, 48, 80, arrow);
            }
            else
            {
                FillTriangle(tex, 20, 74, 76, 74, 48, 46, arrow);
                FillTriangle(tex, 20, 44, 76, 44, 48, 16, arrow);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateTopDownTreeSprite(int variant = 0)
        {
            Texture2D tex = NewTex(96, 96);
            FillCircle(tex, 52, 44, 34, new Color(0f, 0f, 0f, 0.22f)); // Sombra suave
            Color dark = variant == 0 ? new Color(0.13f, 0.44f, 0.18f, 1f) : new Color(0.18f, 0.50f, 0.20f, 1f);
            Color mid = variant == 0 ? new Color(0.22f, 0.62f, 0.26f, 1f) : new Color(0.30f, 0.68f, 0.28f, 1f);
            Color light = new Color(0.45f, 0.80f, 0.36f, 1f);
            FillCircle(tex, 46, 50, 32, dark);
            FillCircle(tex, 36, 56, 20, mid);
            FillCircle(tex, 58, 54, 18, mid);
            FillCircle(tex, 46, 42, 18, mid);
            FillCircle(tex, 42, 58, 13, light);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateFlowerClusterSprite(Color petalColor)
        {
            Texture2D tex = NewTex(48, 48);
            Color leaf = new Color(0.20f, 0.58f, 0.22f, 1f);
            Color center = new Color(1f, 0.90f, 0.24f, 1f);
            FillCircle(tex, 24, 24, 15, leaf);
            // 3 florzinhas
            int[,] pts = { { 16, 28 }, { 32, 26 }, { 24, 16 } };
            for (int i = 0; i < 3; i++)
            {
                int fx = pts[i, 0]; int fy = pts[i, 1];
                FillCircle(tex, fx - 4, fy, 4, petalColor);
                FillCircle(tex, fx + 4, fy, 4, petalColor);
                FillCircle(tex, fx, fy - 4, 4, petalColor);
                FillCircle(tex, fx, fy + 4, 4, petalColor);
                FillCircle(tex, fx, fy, 3, center);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateFlowerPotSprite()
        {
            Texture2D tex = NewTex(56, 56);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color potOuter = new Color(0.76f, 0.38f, 0.22f, 1f);
            Color potInner = new Color(0.42f, 0.24f, 0.14f, 1f);
            Color bush = new Color(0.22f, 0.64f, 0.26f, 1f);
            Color flower = new Color(0.98f, 0.42f, 0.68f, 1f);

            FillCircle(tex, 31, 25, 20, shadow);
            FillCircle(tex, 28, 28, 20, potOuter);
            FillCircle(tex, 28, 28, 15, potInner);
            FillCircle(tex, 24, 30, 9, bush);
            FillCircle(tex, 33, 27, 9, bush);
            FillCircle(tex, 28, 23, 8, bush);
            FillCircle(tex, 24, 31, 4, flower);
            FillCircle(tex, 33, 27, 4, Color.yellow);
            FillCircle(tex, 28, 23, 4, flower);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 56, 56), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateParkBenchSprite()
        {
            Texture2D tex = NewTex(96, 48);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);
            Color metal = new Color(0.24f, 0.28f, 0.30f, 1f);
            Color woodLight = new Color(0.76f, 0.50f, 0.26f, 1f);
            Color woodDark = new Color(0.58f, 0.36f, 0.16f, 1f);

            FillRect(tex, 10, 4, 80, 34, shadow);
            // Braços laterais de ferro
            FillRect(tex, 8, 8, 8, 32, metal);
            FillRect(tex, 80, 8, 8, 32, metal);
            // Tábuas do assento e encosto
            FillRect(tex, 12, 10, 72, 10, woodLight);
            FillRect(tex, 12, 22, 72, 8, woodLight);
            FillRect(tex, 12, 32, 72, 7, woodDark);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 96, 48), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateWaterColumnSprite()
        {
            Texture2D tex = NewTex(72, 72);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color stone = new Color(0.84f, 0.87f, 0.90f, 1f);
            Color stoneEdge = new Color(0.64f, 0.70f, 0.76f, 1f);
            Color water = new Color(0.18f, 0.74f, 0.98f, 1f);
            Color sparkle = new Color(0.82f, 0.96f, 1f, 1f);

            FillCircle(tex, 39, 33, 28, shadow);
            FillCircle(tex, 36, 36, 28, stoneEdge);
            FillCircle(tex, 36, 36, 24, stone);
            FillCircle(tex, 36, 36, 17, water);
            FillCircle(tex, 36, 36, 6, stone);
            FillCircle(tex, 31, 42, 4, sparkle);
            FillCircle(tex, 42, 30, 3, sparkle);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 72, 72), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateFoodProjectileSprite()
        {
            Texture2D tex = NewTex(40, 40);
            FillCircle(tex, 20, 20, 16, new Color(1f, 0.95f, 0.55f, 0.65f));
            FillCircle(tex, 20, 20, 11, new Color(0.96f, 0.34f, 0.24f, 1f));
            FillCircle(tex, 16, 24, 3, new Color(1f, 0.78f, 0.72f, 1f));
            FillCircle(tex, 22, 31, 4, new Color(0.24f, 0.72f, 0.28f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 40, 40), new Vector2(0.5f, 0.5f), 40f);
        }

        public static Sprite CreateTearDropSprite()
        {
            Texture2D tex = NewTex(32, 32);
            Color blue = new Color(0.25f, 0.72f, 1f, 0.95f);
            Color shine = new Color(0.85f, 0.97f, 1f, 1f);
            FillCircle(tex, 16, 12, 8, blue);
            FillTriangle(tex, 9, 15, 23, 15, 16, 29, blue);
            FillCircle(tex, 13, 13, 2, shine);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 40f);
        }

        public static Sprite CreateEmotionIcon(string type)
        {
            Texture2D tex = NewTex(64, 64);
            FillCircle(tex, 32, 32, 28, new Color(1f, 1f, 1f, 0.95f));
            if (type == "happy")
            {
                Color heart = new Color(0.98f, 0.26f, 0.48f, 1f);
                FillCircle(tex, 24, 36, 10, heart);
                FillCircle(tex, 40, 36, 10, heart);
                FillTriangle(tex, 15, 32, 49, 32, 32, 14, heart);
            }
            else if (type == "cry")
            {
                Color blue = new Color(0.20f, 0.65f, 0.98f, 1f);
                FillCircle(tex, 23, 26, 7, blue);
                FillTriangle(tex, 17, 29, 29, 29, 23, 44, blue);
                FillCircle(tex, 41, 22, 7, blue);
                FillTriangle(tex, 35, 25, 47, 25, 41, 40, blue);
            }
            else if (type == "water")
            {
                Color water = new Color(0.16f, 0.72f, 0.98f, 1f);
                FillCircle(tex, 32, 24, 12, water);
                FillTriangle(tex, 21, 28, 43, 28, 32, 48, water);
            }
            else // hungry
            {
                FillCircle(tex, 32, 32, 24, new Color(1f, 0.95f, 0.82f, 1f));
                FillCircle(tex, 25, 34, 7, new Color(0.94f, 0.28f, 0.32f, 1f));
                FillCircle(tex, 39, 34, 7, new Color(0.96f, 0.66f, 0.16f, 1f));
                FillCircle(tex, 32, 23, 7, new Color(0.32f, 0.72f, 0.30f, 1f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        // ─── Os 7 Alimentos, Cozinha, Terra, Diamante e Corações ─────

        public static Sprite CreateFoodSprite(FoodType type)
        {
            Texture2D tex = NewTex(64, 64);
            // Fundo circular clarinho para destacar o ícone no balão e no arremesso
            FillCircle(tex, 32, 32, 28, new Color(1f, 0.99f, 0.92f, 0.96f));
            FillCircle(tex, 32, 32, 25, new Color(1f, 0.96f, 0.84f, 1f));

            switch (type)
            {
                case FoodType.Refrigerante:
                    // Lata vermelha com listra branca e canudinho
                    FillRect(tex, 22, 14, 20, 32, new Color(0.88f, 0.16f, 0.20f, 1f));
                    FillRect(tex, 22, 27, 20, 6, Color.white);
                    FillRect(tex, 24, 46, 16, 4, new Color(0.80f, 0.84f, 0.88f, 1f));
                    FillRect(tex, 33, 48, 3, 10, new Color(0.98f, 0.85f, 0.22f, 1f));
                    break;

                case FoodType.Suco:
                    // Copo de suco de laranja com canudo verde e folhinha
                    FillRect(tex, 22, 14, 20, 30, new Color(1f, 0.62f, 0.12f, 1f));
                    FillRect(tex, 20, 42, 24, 5, Color.white);
                    FillRect(tex, 34, 44, 3, 12, new Color(0.24f, 0.78f, 0.34f, 1f));
                    FillCircle(tex, 26, 38, 4, new Color(1f, 0.85f, 0.35f, 1f));
                    break;

                case FoodType.Coxinha:
                    // Coxinha dourada em formato de gota crocante
                    FillCircle(tex, 32, 24, 13, new Color(0.86f, 0.56f, 0.20f, 1f));
                    FillTriangle(tex, 19, 27, 45, 27, 32, 52, new Color(0.86f, 0.56f, 0.20f, 1f));
                    FillCircle(tex, 28, 26, 4, new Color(0.95f, 0.72f, 0.34f, 1f));
                    break;

                case FoodType.Doces:
                    // Pirulito / bombom colorido rosa e azul
                    FillRect(tex, 30, 12, 4, 20, Color.white);
                    FillCircle(tex, 32, 38, 13, new Color(0.98f, 0.36f, 0.68f, 1f));
                    FillCircle(tex, 32, 38, 8, new Color(1f, 0.88f, 0.35f, 1f));
                    FillCircle(tex, 32, 38, 4, new Color(0.35f, 0.82f, 1f, 1f));
                    break;

                case FoodType.Pipoca:
                    // Balde listrado vermelho/branco com pipocas amarelas no topo
                    FillRect(tex, 20, 12, 24, 24, Color.white);
                    FillRect(tex, 22, 12, 5, 24, new Color(0.90f, 0.20f, 0.22f, 1f));
                    FillRect(tex, 31, 12, 5, 24, new Color(0.90f, 0.20f, 0.22f, 1f));
                    FillRect(tex, 39, 12, 4, 24, new Color(0.90f, 0.20f, 0.22f, 1f));
                    FillCircle(tex, 24, 40, 7, new Color(1f, 0.94f, 0.56f, 1f));
                    FillCircle(tex, 32, 44, 8, new Color(1f, 0.96f, 0.68f, 1f));
                    FillCircle(tex, 40, 40, 7, new Color(1f, 0.90f, 0.45f, 1f));
                    break;

                case FoodType.Sanduiche:
                    // Sanduíche com pão, alface verde, queijo amarelo e tomate vermelho
                    FillRect(tex, 15, 16, 34, 8, new Color(0.86f, 0.64f, 0.34f, 1f));
                    FillRect(tex, 14, 24, 36, 5, new Color(0.28f, 0.78f, 0.30f, 1f));
                    FillRect(tex, 16, 29, 32, 5, new Color(0.92f, 0.24f, 0.22f, 1f));
                    FillRect(tex, 15, 34, 34, 4, new Color(1f, 0.84f, 0.20f, 1f));
                    FillRect(tex, 15, 38, 34, 9, new Color(0.90f, 0.68f, 0.38f, 1f));
                    break;

                case FoodType.HotDog:
                    // Pão de hot dog, salsicha e mostarda em zigue-zague
                    FillRect(tex, 12, 20, 40, 22, new Color(0.90f, 0.66f, 0.34f, 1f));
                    FillRect(tex, 10, 26, 44, 10, new Color(0.76f, 0.26f, 0.18f, 1f));
                    FillRect(tex, 14, 30, 36, 3, new Color(1f, 0.88f, 0.18f, 1f));
                    break;

                case FoodType.Nozes:
                    // Bolota / noz dourada com chapéu marrom
                    FillCircle(tex, 32, 25, 13, new Color(0.68f, 0.42f, 0.18f, 1f));
                    FillCircle(tex, 28, 28, 4, new Color(0.86f, 0.62f, 0.34f, 1f));
                    FillCircle(tex, 32, 37, 12, new Color(0.42f, 0.24f, 0.10f, 1f));
                    FillRect(tex, 20, 32, 24, 7, new Color(0.42f, 0.24f, 0.10f, 1f));
                    FillRect(tex, 30, 46, 4, 7, new Color(0.36f, 0.20f, 0.08f, 1f));
                    break;

                case FoodType.Cenoura:
                    // Cenoura laranja vibrante com folhas verdes
                    FillTriangle(tex, 22, 40, 42, 40, 32, 11, new Color(0.98f, 0.48f, 0.10f, 1f));
                    FillCircle(tex, 32, 38, 10, new Color(0.98f, 0.48f, 0.10f, 1f));
                    FillRect(tex, 27, 29, 10, 2, new Color(0.84f, 0.36f, 0.06f, 1f));
                    FillRect(tex, 29, 22, 6, 2, new Color(0.84f, 0.36f, 0.06f, 1f));
                    FillTriangle(tex, 24, 46, 34, 46, 28, 56, new Color(0.22f, 0.72f, 0.26f, 1f));
                    FillTriangle(tex, 30, 46, 40, 46, 36, 56, new Color(0.18f, 0.62f, 0.22f, 1f));
                    break;

                case FoodType.Maca:
                    // Maçã vermelha brilhante com cabinho e folha verde
                    FillCircle(tex, 26, 28, 13, new Color(0.90f, 0.16f, 0.20f, 1f));
                    FillCircle(tex, 38, 28, 13, new Color(0.90f, 0.16f, 0.20f, 1f));
                    FillCircle(tex, 24, 33, 4, new Color(1f, 0.62f, 0.62f, 0.85f));
                    FillRect(tex, 31, 39, 3, 11, new Color(0.46f, 0.26f, 0.12f, 1f));
                    FillCircle(tex, 38, 46, 5, new Color(0.24f, 0.74f, 0.28f, 1f));
                    break;

                case FoodType.Mel:
                    // Pote de mel dourado com tampa e gotinha escorrendo
                    FillCircle(tex, 32, 26, 15, new Color(0.98f, 0.72f, 0.12f, 1f));
                    FillRect(tex, 20, 20, 24, 10, new Color(1f, 0.94f, 0.72f, 1f));
                    FillRect(tex, 22, 38, 20, 7, new Color(0.86f, 0.44f, 0.10f, 1f));
                    FillCircle(tex, 42, 36, 4, new Color(1f, 0.80f, 0.16f, 1f));
                    FillCircle(tex, 43, 30, 3, new Color(1f, 0.80f, 0.16f, 1f));
                    break;

                case FoodType.Sementes:
                    // Sementinhas de girassol em cima de uma folhinha verde
                    FillCircle(tex, 32, 24, 15, new Color(0.28f, 0.72f, 0.30f, 1f));
                    FillCircle(tex, 25, 29, 6, new Color(0.32f, 0.28f, 0.26f, 1f));
                    FillCircle(tex, 38, 30, 6, new Color(0.32f, 0.28f, 0.26f, 1f));
                    FillCircle(tex, 32, 37, 6, new Color(0.36f, 0.32f, 0.30f, 1f));
                    FillRect(tex, 24, 26, 2, 6, new Color(0.94f, 0.90f, 0.78f, 1f));
                    FillRect(tex, 37, 27, 2, 6, new Color(0.94f, 0.90f, 0.78f, 1f));
                    FillRect(tex, 31, 34, 2, 6, new Color(0.94f, 0.90f, 0.78f, 1f));
                    break;

                case FoodType.SaladaFrutasBosque:
                    // Tigela de frutas, folhas e sementes para o piquenique.
                    FillRect(tex, 15, 22, 34, 18, new Color(0.62f, 0.34f, 0.20f, 1f));
                    FillRect(tex, 19, 18, 26, 4, new Color(0.90f, 0.65f, 0.35f, 1f));
                    FillCircle(tex, 24, 43, 8, new Color(0.92f, 0.18f, 0.22f, 1f));
                    FillCircle(tex, 38, 42, 8, new Color(1f, 0.66f, 0.12f, 1f));
                    FillCircle(tex, 31, 48, 7, new Color(0.42f, 0.76f, 0.30f, 1f));
                    FillCircle(tex, 31, 36, 4, new Color(0.98f, 0.86f, 0.38f, 1f));
                    break;

                case FoodType.CestaDaLua:
                    // Cesta noturna com frutas, mel e sementes brilhantes.
                    FillRect(tex, 15, 19, 34, 20, new Color(0.60f, 0.34f, 0.18f, 1f));
                    FillRect(tex, 18, 39, 4, 10, new Color(0.72f, 0.46f, 0.26f, 1f));
                    FillRect(tex, 42, 39, 4, 10, new Color(0.72f, 0.46f, 0.26f, 1f));
                    FillCircle(tex, 23, 46, 7, new Color(0.92f, 0.18f, 0.22f, 1f));
                    FillCircle(tex, 34, 48, 7, new Color(1f, 0.78f, 0.22f, 1f));
                    FillCircle(tex, 42, 46, 5, new Color(0.76f, 0.62f, 1f, 1f));
                    break;

                case FoodType.FrutasSilvestres:
                    FillCircle(tex, 22, 28, 10, new Color(0.42f, 0.16f, 0.68f, 1f));
                    FillCircle(tex, 36, 28, 10, new Color(0.56f, 0.18f, 0.70f, 1f));
                    FillCircle(tex, 29, 40, 10, new Color(0.38f, 0.12f, 0.62f, 1f));
                    FillCircle(tex, 25, 32, 3, new Color(0.90f, 0.72f, 1f, 1f));
                    FillRect(tex, 28, 45, 8, 3, new Color(0.20f, 0.64f, 0.26f, 1f));
                    break;

                case FoodType.Pao:
                    FillCircle(tex, 23, 28, 13, new Color(0.86f, 0.58f, 0.26f, 1f));
                    FillCircle(tex, 40, 28, 13, new Color(0.92f, 0.68f, 0.34f, 1f));
                    FillRect(tex, 20, 16, 24, 7, new Color(0.76f, 0.42f, 0.16f, 1f));
                    FillRect(tex, 29, 19, 3, 8, new Color(1f, 0.84f, 0.54f, 1f));
                    break;

                case FoodType.GeleiaSilvestre:
                    FillRect(tex, 20, 17, 24, 27, new Color(0.52f, 0.20f, 0.56f, 1f));
                    FillRect(tex, 23, 42, 18, 7, new Color(0.82f, 0.36f, 0.72f, 1f));
                    FillRect(tex, 24, 44, 16, 3, new Color(0.96f, 0.78f, 0.56f, 1f));
                    FillCircle(tex, 32, 30, 3, new Color(0.94f, 0.64f, 0.90f, 1f));
                    break;

                case FoodType.TortinhaDeMaca:
                    FillCircle(tex, 32, 24, 18, new Color(0.70f, 0.38f, 0.16f, 1f));
                    FillCircle(tex, 32, 28, 14, new Color(0.98f, 0.74f, 0.30f, 1f));
                    FillRect(tex, 19, 31, 26, 4, new Color(0.90f, 0.30f, 0.20f, 1f));
                    FillRect(tex, 27, 22, 26, 4, new Color(0.90f, 0.30f, 0.20f, 1f));
                    FillRect(tex, 27, 31, 4, 14, new Color(0.58f, 0.28f, 0.12f, 1f));
                    break;
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 58f);
        }

        public static Sprite CreateKitchenStallSprite()
        {
            Texture2D tex = NewTex(128, 96);
            FillRect(tex, 10, 6, 112, 78, new Color(0f, 0f, 0f, 0.22f));
            // Balcão de madeira
            FillRect(tex, 12, 12, 104, 72, new Color(0.68f, 0.44f, 0.22f, 1f));
            // Toldo listrado rosa e branco visto de cima
            for (int i = 0; i < 6; i++)
            {
                Color stripe = (i % 2 == 0) ? new Color(0.96f, 0.36f, 0.60f, 1f) : Color.white;
                FillRect(tex, 14 + i * 16, 18, 16, 58, stripe);
            }
            FillRect(tex, 14, 14, 96, 6, new Color(0.78f, 0.22f, 0.44f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 96), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateDirtMoundSprite()
        {
            Texture2D tex = NewTex(96, 96);
            FillCircle(tex, 48, 48, 40, new Color(0.48f, 0.32f, 0.18f, 1f));
            FillCircle(tex, 48, 48, 30, new Color(0.36f, 0.22f, 0.12f, 1f));
            FillCircle(tex, 44, 52, 14, new Color(0.24f, 0.14f, 0.08f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite CreateDiamondSprite()
        {
            Texture2D tex = NewTex(64, 64);
            Color outer = new Color(0.18f, 0.80f, 1f, 1f);
            Color mid = new Color(0.56f, 0.94f, 1f, 1f);
            Color shine = Color.white;
            FillTriangle(tex, 12, 38, 52, 38, 32, 8, outer);
            FillTriangle(tex, 12, 38, 52, 38, 32, 56, mid);
            FillTriangle(tex, 22, 38, 42, 38, 32, 52, shine);
            FillCircle(tex, 26, 42, 3, shine);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 50f);
        }

        public static Sprite CreateHeartParticleSprite(Color heartColor)
        {
            Texture2D tex = NewTex(40, 40);
            FillCircle(tex, 14, 24, 8, heartColor);
            FillCircle(tex, 26, 24, 8, heartColor);
            FillTriangle(tex, 7, 21, 33, 21, 20, 6, heartColor);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 40, 40), new Vector2(0.5f, 0.5f), 48f);
        }

        public static Sprite CreateDirtClodSprite()
        {
            Texture2D tex = NewTex(24, 24);
            FillCircle(tex, 12, 12, 9, new Color(0.46f, 0.30f, 0.16f, 1f));
            FillCircle(tex, 10, 14, 4, new Color(0.62f, 0.42f, 0.24f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 40f);
        }
    }
}
