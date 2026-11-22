using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 5 — Moradores da Vila e Missões Aleatórias:
    /// Criança, Senhor no Banco, Família no Piquenique, Viajante com Sede e Músico.
    /// Cada morador possui visual ilustrado, balão de pedido e recompensa com Estrelas + Pontos + Lembrança!
    /// </summary>
    public class VillageResident : MonoBehaviour
    {
        public enum ResidentKind
        {
            Crianca,
            SenhorBanco,
            FamiliaPiquenique,
            ViajanteSede,
            Musico,
            Feirante
        }

        public enum DeliveryResult { WrongItem, Completed, AlreadyHelped }

        public ResidentKind kind { get; private set; }
        public string residentName { get; private set; }
        public string missionTitle { get; private set; }
        public FoodType wantedFood { get; private set; }
        public int starReward { get; private set; }
        public int pointReward { get; private set; }
        public bool isHelped { get; private set; } = false;

        private SpriteRenderer bodySR;
        private SpriteRenderer bubbleSR;
        private Transform bubbleTransform;
        private Sprite happyIcon;
        private Sprite[] starParticles;
        private Vector3 basePos;
        private float bobPhase;

        private static readonly Dictionary<ResidentKind, Sprite> residentSpriteCache =
            new Dictionary<ResidentKind, Sprite>();
        private static Sprite cachedMusicNote;

        public void Setup(
            ResidentKind residentKind,
            Dictionary<FoodType, Sprite> foodSprites,
            Sprite happySprite,
            Sprite[] celebrationParticles)
        {
            kind = residentKind;
            happyIcon = happySprite;
            starParticles = celebrationParticles;
            basePos = transform.position;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            ConfigureMissionByKind();

            bodySR = gameObject.AddComponent<SpriteRenderer>();
            bodySR.sprite = GetOrCreateResidentSprite(kind);
            bodySR.sortingOrder = 6;

            GameObject bubbleObj = new GameObject("MissionBubble");
            bubbleObj.transform.SetParent(transform, false);
            bubbleObj.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            bubbleObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            bubbleTransform = bubbleObj.transform;

            bubbleSR = bubbleObj.AddComponent<SpriteRenderer>();
            bubbleSR.sortingOrder = 13;
            if (foodSprites != null && foodSprites.TryGetValue(wantedFood, out Sprite foodSp))
                bubbleSR.sprite = foodSp;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2.6f);
            col.offset = new Vector2(0f, 1.2f);
        }

        private void ConfigureMissionByKind()
        {
            switch (kind)
            {
                case ResidentKind.Crianca:
                    residentName = "Criança da Vila";
                    missionTitle = "Lanche da Tarde";
                    wantedFood = PickFrom(FoodType.Doces, FoodType.Pipoca, FoodType.Suco, FoodType.Pao);
                    starReward = 2;
                    pointReward = 130;
                    break;
                case ResidentKind.SenhorBanco:
                    residentName = "Senhor no Banco";
                    missionTitle = "Descanso na Praça";
                    wantedFood = PickFrom(FoodType.Maca, FoodType.Suco, FoodType.Sanduiche);
                    starReward = 2;
                    pointReward = 140;
                    break;
                case ResidentKind.FamiliaPiquenique:
                    residentName = "Família no Piquenique";
                    missionTitle = "Cesta de Piquenique";
                    wantedFood = PickFrom(FoodType.Sanduiche, FoodType.HotDog, FoodType.Mel);
                    starReward = 3;
                    pointReward = 190;
                    break;
                case ResidentKind.ViajanteSede:
                    residentName = "Viajante com Sede";
                    missionTitle = "Refresco da Estrada";
                    wantedFood = PickFrom(FoodType.Suco, FoodType.Refrigerante, FoodType.Maca);
                    starReward = 2;
                    pointReward = 150;
                    break;
                case ResidentKind.Musico:
                    residentName = "Músico da Vila";
                    missionTitle = "Serenata do Bosque";
                    wantedFood = PickFrom(FoodType.Suco, FoodType.Coxinha, FoodType.Doces);
                    starReward = 3;
                    pointReward = 175;
                    break;
                case ResidentKind.Feirante:
                    residentName = "Feirante do Bosque";
                    bool jamUnlocked = PlayerProgressManager.Instance != null
                        && PlayerProgressManager.Instance.IsRecipeUnlocked(FoodType.GeleiaSilvestre);
                    wantedFood = jamUnlocked && Random.value < 0.55f
                        ? FoodType.GeleiaSilvestre : FoodType.Mel;
                    missionTitle = wantedFood == FoodType.GeleiaSilvestre ? "Geleia para a Feira" : "Mel para a Feira";
                    starReward = 3;
                    pointReward = 185;
                    break;
            }
        }

        private static FoodType PickFrom(params FoodType[] options)
        {
            return options[Random.Range(0, options.Length)];
        }

        private void Update()
        {
            if (bubbleTransform != null)
            {
                float amp = isHelped ? 0.08f : 0.14f;
                float spd = isHelped ? 3.0f : 5.2f;
                bubbleTransform.localPosition = new Vector3(0f, 2.75f + Mathf.Sin(Time.time * spd + bobPhase) * amp, 0f);
            }

            // Movimento sutil e acolhedor do morador (músico balança no ritmo!)
            if (kind == ResidentKind.Musico)
            {
                float sway = Mathf.Sin(Time.time * 4.2f + bobPhase) * 3.5f;
                transform.localRotation = Quaternion.Euler(0f, 0f, sway);
            }
            else if (kind == ResidentKind.Crianca && !isHelped)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 3.2f + bobPhase)) * 0.14f;
                transform.position = basePos + new Vector3(0f, hop, 0f);
            }
        }

        public DeliveryResult ReceiveItem(FoodType given)
        {
            if (isHelped) return DeliveryResult.AlreadyHelped;

            if (given != wantedFood)
            {
                StartCoroutine(GentleShakeRoutine());
                return DeliveryResult.WrongItem;
            }

            isHelped = true;
            if (bubbleSR != null && happyIcon != null) bubbleSR.sprite = happyIcon;

            if (kind == ResidentKind.Musico)
                FlorestaSoundFX.Instance?.PlayMusicianTune();
            else
                FlorestaSoundFX.Instance?.PlayVillagerHappy();

            StartCoroutine(CelebrationRoutine());
            return DeliveryResult.Completed;
        }

        private IEnumerator GentleShakeRoutine()
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                transform.position = basePos + new Vector3(Mathf.Sin(t * 42f) * 0.12f, 0f, 0f);
                yield return null;
            }
            transform.position = basePos;
        }

        private IEnumerator CelebrationRoutine()
        {
            Sprite noteSp = (kind == ResidentKind.Musico) ? GetMusicNoteSprite() : null;
            for (int i = 0; i < 10; i++)
            {
                GameObject p = new GameObject("CelebracaoMorador");
                p.transform.position = transform.position + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(1.7f, 2.5f), 0f);
                float sc = Random.Range(0.75f, 1.1f);
                p.transform.localScale = new Vector3(sc, sc, 1f);
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                if (noteSp != null && i % 2 == 0)
                    sr.sprite = noteSp;
                else if (starParticles != null && starParticles.Length > 0)
                    sr.sprite = starParticles[Random.Range(0, starParticles.Length)];
                sr.sortingOrder = 19;
                p.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(1.6f, 2.8f), 0f),
                    gravity: 0.25f,
                    duration: 1.25f
                );
                if (i % 2 == 0) yield return new WaitForSeconds(0.08f);
            }
        }

        // ─── Artes Ilustradas Procedurais dos 5 Moradores ────────────

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

        private static void RectPx(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int minX = Mathf.Max(0, x0), maxX = Mathf.Min(tex.width - 1, x1);
            int minY = Mathf.Max(0, y0), maxY = Mathf.Min(tex.height - 1, y1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    tex.SetPixel(x, y, c);
        }

        private static void CirclePx(Texture2D tex, int cx, int cy, int r, Color c)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r2) tex.SetPixel(x, y, c);
                }
            }
        }

        private static void EllipsePx(Texture2D tex, int cx, int cy, int rx, int ry, Color c)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f) tex.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawPersonBase(Texture2D tex, int cx, int feetY, int scalePct, Color shirt, Color pants, Color hair, Color skin)
        {
            float s = scalePct / 100f;
            int legH = Mathf.RoundToInt(22 * s);
            int torsoH = Mathf.RoundToInt(28 * s);
            int headR = Mathf.RoundToInt(15 * s);
            int w = Mathf.RoundToInt(14 * s);

            // Sombra
            EllipsePx(tex, cx, feetY, Mathf.RoundToInt(22 * s), 5, new Color(0f, 0f, 0f, 0.20f));
            // Pernas
            RectPx(tex, cx - w / 2 - 4, feetY + 2, cx - 2, feetY + legH, pants);
            RectPx(tex, cx + 2, feetY + 2, cx + w / 2 + 4, feetY + legH, pants);
            // Tronco
            EllipsePx(tex, cx, feetY + legH + torsoH / 2, w + 4, torsoH / 2 + 3, shirt);
            // Cabeça
            int headY = feetY + legH + torsoH + headR - 2;
            CirclePx(tex, cx, headY, headR, skin);
            // Cabelo
            CirclePx(tex, cx, headY + 4, headR, hair);
            CirclePx(tex, cx, headY - 2, headR - 2, skin);
            // Olhos e sorriso
            CirclePx(tex, cx - 5, headY, 2, new Color(0.12f, 0.12f, 0.14f, 1f));
            CirclePx(tex, cx + 5, headY, 2, new Color(0.12f, 0.12f, 0.14f, 1f));
            RectPx(tex, cx - 4, headY - 7, cx + 4, headY - 6, new Color(0.75f, 0.30f, 0.30f, 1f));
        }

        private static Sprite GetOrCreateResidentSprite(ResidentKind kind)
        {
            if (residentSpriteCache.TryGetValue(kind, out Sprite cached) && cached != null)
                return cached;

            int w = 144, h = 144;
            Texture2D tex = BlankTex(w, h);
            Color skin = new Color(0.98f, 0.82f, 0.68f, 1f);
            Color skinWarm = new Color(0.86f, 0.64f, 0.46f, 1f);

            switch (kind)
            {
                case ResidentKind.Crianca:
                    DrawPersonBase(tex, 68, 10, 82, new Color(0.98f, 0.44f, 0.46f, 1f), new Color(0.24f, 0.48f, 0.86f, 1f), new Color(0.36f, 0.22f, 0.12f, 1f), skin);
                    // Boné colorido + balão amarelo flutuando na mão!
                    RectPx(tex, 54, 78, 86, 85, new Color(0.20f, 0.72f, 0.96f, 1f));
                    RectPx(tex, 96, 46, 98, 92, Color.white);
                    CirclePx(tex, 97, 106, 15, new Color(1f, 0.82f, 0.20f, 1f));
                    CirclePx(tex, 92, 111, 4, new Color(1f, 0.96f, 0.76f, 0.9f));
                    break;

                case ResidentKind.SenhorBanco:
                    // Banco aconchegante atrás
                    RectPx(tex, 18, 12, 126, 26, new Color(0.56f, 0.34f, 0.16f, 1f));
                    RectPx(tex, 24, 26, 120, 44, new Color(0.72f, 0.48f, 0.24f, 1f));
                    DrawPersonBase(tex, 72, 14, 98, new Color(0.32f, 0.58f, 0.44f, 1f), new Color(0.34f, 0.32f, 0.36f, 1f), new Color(0.88f, 0.88f, 0.90f, 1f), skin);
                    // Chapéu de palha e bigodinho branco simpático
                    EllipsePx(tex, 72, 95, 24, 5, new Color(0.92f, 0.78f, 0.42f, 1f));
                    RectPx(tex, 58, 96, 86, 107, new Color(0.92f, 0.78f, 0.42f, 1f));
                    RectPx(tex, 58, 96, 86, 99, new Color(0.76f, 0.24f, 0.24f, 1f));
                    EllipsePx(tex, 72, 72, 8, 3, Color.white);
                    break;

                case ResidentKind.FamiliaPiquenique:
                    // Toalha xadrez vermelha e branca + cesta de piquenique
                    EllipsePx(tex, 72, 16, 58, 12, new Color(0.96f, 0.96f, 0.96f, 1f));
                    for (int gx = 24; gx < 120; gx += 16)
                        RectPx(tex, gx, 10, gx + 8, 22, new Color(0.92f, 0.28f, 0.32f, 0.9f));
                    DrawPersonBase(tex, 46, 16, 88, new Color(0.96f, 0.62f, 0.22f, 1f), new Color(0.26f, 0.42f, 0.68f, 1f), new Color(0.28f, 0.18f, 0.12f, 1f), skinWarm);
                    DrawPersonBase(tex, 96, 16, 86, new Color(0.78f, 0.38f, 0.84f, 1f), new Color(0.32f, 0.56f, 0.42f, 1f), new Color(0.42f, 0.24f, 0.14f, 1f), skin);
                    // Cesta de vime no meio
                    RectPx(tex, 62, 16, 82, 32, new Color(0.76f, 0.52f, 0.24f, 1f));
                    RectPx(tex, 60, 30, 84, 34, new Color(0.58f, 0.36f, 0.16f, 1f));
                    break;

                case ResidentKind.ViajanteSede:
                    // Mochilão de trilha nas costas
                    RectPx(tex, 38, 34, 62, 76, new Color(0.20f, 0.52f, 0.32f, 1f));
                    RectPx(tex, 40, 62, 60, 74, new Color(0.94f, 0.66f, 0.18f, 1f));
                    DrawPersonBase(tex, 72, 10, 100, new Color(0.94f, 0.52f, 0.20f, 1f), new Color(0.42f, 0.36f, 0.26f, 1f), new Color(0.24f, 0.18f, 0.14f, 1f), skinWarm);
                    // Chapéu de explorador
                    EllipsePx(tex, 72, 92, 22, 5, new Color(0.56f, 0.40f, 0.24f, 1f));
                    RectPx(tex, 60, 93, 84, 104, new Color(0.56f, 0.40f, 0.24f, 1f));
                    break;

                case ResidentKind.Musico:
                    DrawPersonBase(tex, 68, 10, 100, new Color(0.24f, 0.46f, 0.82f, 1f), new Color(0.22f, 0.24f, 0.32f, 1f), new Color(0.34f, 0.20f, 0.12f, 1f), skin);
                    // Violão acústico dourado nos braços!
                    CirclePx(tex, 78, 46, 13, new Color(0.84f, 0.52f, 0.20f, 1f));
                    CirclePx(tex, 66, 52, 9, new Color(0.84f, 0.52f, 0.20f, 1f));
                    CirclePx(tex, 78, 46, 4, new Color(0.24f, 0.14f, 0.08f, 1f));
                    RectPx(tex, 86, 48, 118, 54, new Color(0.48f, 0.28f, 0.12f, 1f));
                    // Boina estilosa
                    EllipsePx(tex, 68, 92, 18, 7, new Color(0.84f, 0.22f, 0.34f, 1f));
                    break;

                case ResidentKind.Feirante:
                    DrawPersonBase(tex, 70, 10, 100, new Color(0.36f, 0.66f, 0.38f, 1f), new Color(0.56f, 0.34f, 0.24f, 1f), new Color(0.34f, 0.20f, 0.12f, 1f), skinWarm);
                    EllipsePx(tex, 70, 96, 28, 6, new Color(0.88f, 0.42f, 0.28f, 1f));
                    RectPx(tex, 54, 96, 86, 108, new Color(0.88f, 0.42f, 0.28f, 1f));
                    RectPx(tex, 90, 16, 128, 38, new Color(0.64f, 0.36f, 0.18f, 1f));
                    CirclePx(tex, 99, 41, 7, new Color(0.54f, 0.20f, 0.72f, 1f));
                    CirclePx(tex, 112, 42, 7, new Color(0.42f, 0.16f, 0.64f, 1f));
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), h / 2.55f);
            residentSpriteCache[kind] = sp;
            return sp;
        }

        private static Sprite GetMusicNoteSprite()
        {
            if (cachedMusicNote != null) return cachedMusicNote;
            Texture2D tex = BlankTex(32, 32);
            Color gold = new Color(1f, 0.86f, 0.22f, 1f);
            CirclePx(tex, 11, 9, 5, gold);
            RectPx(tex, 14, 9, 17, 27, gold);
            RectPx(tex, 14, 23, 25, 28, gold);
            tex.Apply();
            cachedMusicNote = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return cachedMusicNote;
        }
    }
}
