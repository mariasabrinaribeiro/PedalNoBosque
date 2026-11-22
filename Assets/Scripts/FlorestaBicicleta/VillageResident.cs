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

        private static readonly Dictionary<ResidentKind, Sprite> residentPropCache =
            new Dictionary<ResidentKind, Sprite>();
        private static Sprite cachedMusicNote;
        private const float BubbleBaseY = 1.52f;

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

            // Adereço ilustrado temático da missão do morador (violão, cesta de piquenique, caixote de feira, balão...)
            Sprite propSp = GetOrCreateResidentPropSprite(kind);
            if (propSp != null)
            {
                GameObject propObj = new GameObject("AderecoMorador");
                propObj.transform.SetParent(transform, false);
                propObj.transform.localPosition = new Vector3(0.58f, 0.02f, 0f);
                SpriteRenderer propSR = propObj.AddComponent<SpriteRenderer>();
                propSR.sprite = propSp;
                propSR.sortingOrder = 7;
            }

            GameObject bubbleObj = new GameObject("MissionBubble");
            bubbleObj.transform.SetParent(transform, false);
            bubbleObj.transform.localPosition = new Vector3(0f, BubbleBaseY, 0f);
            bubbleObj.transform.localScale = new Vector3(0.76f, 0.76f, 1f);
            bubbleTransform = bubbleObj.transform;

            bubbleSR = bubbleObj.AddComponent<SpriteRenderer>();
            bubbleSR.sortingOrder = 13;
            if (foodSprites != null && foodSprites.TryGetValue(wantedFood, out Sprite foodSp))
                bubbleSR.sprite = foodSp;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2.2f);
            col.offset = new Vector2(0f, 0.9f);
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
                float amp = isHelped ? 0.06f : 0.10f;
                float spd = isHelped ? 3.0f : 5.2f;
                bubbleTransform.localPosition = new Vector3(0f, BubbleBaseY + Mathf.Sin(Time.time * spd + bobPhase) * amp, 0f);
            }

            // Movimento sutil e acolhedor do morador (músico balança no ritmo!)
            if (kind == ResidentKind.Musico)
            {
                float sway = Mathf.Sin(Time.time * 4.2f + bobPhase) * 2.5f;
                transform.localRotation = Quaternion.Euler(0f, 0f, sway);
            }
            else if (kind == ResidentKind.Crianca && !isHelped)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 3.2f + bobPhase)) * 0.08f;
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
                p.transform.position = transform.position + new Vector3(Random.Range(-0.9f, 0.9f), Random.Range(1.4f, 2.1f), 0f);
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

        // ─── Artes Ilustradas em Alta Resolução dos Moradores da Vila ────

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

        /// <summary>
        /// Retorna a arte 1024x1024 de estúdio (Cozy Storybook) correspondente ao morador do bosque,
        /// garantindo 100% de coesão visual com a protagonista, banco e barraca.
        /// </summary>
        private static Sprite GetOrCreateResidentSprite(ResidentKind kind)
        {
            switch (kind)
            {
                case ResidentKind.Crianca:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Coelho);
                case ResidentKind.SenhorBanco:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Quati);
                case ResidentKind.FamiliaPiquenique:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Raposa);
                case ResidentKind.ViajanteSede:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Cervo);
                case ResidentKind.Musico:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Gato);
                case ResidentKind.Feirante:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.FilhoteUrso);
                default:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Coelho);
            }
        }

        /// <summary>
        /// Adereço ilustrado Cozy Storybook com contorno #3B1C14 ao lado de cada morador da vila
        /// (cesta de piquenique xadrez, alaúde/violão de madeira, caixote de feira com potes de mel/geleia, etc.).
        /// </summary>
        private static Sprite GetOrCreateResidentPropSprite(ResidentKind kind)
        {
            if (residentPropCache.TryGetValue(kind, out Sprite cached) && cached != null)
                return cached;

            int w = 96, h = 96;
            Texture2D tex = BlankTex(w, h);
            Color ink = new Color(0.23f, 0.11f, 0.08f, 1f);
            Color woodDark = new Color(0.52f, 0.28f, 0.14f, 1f);
            Color woodMid = new Color(0.76f, 0.48f, 0.23f, 1f);
            Color woodLight = new Color(0.90f, 0.66f, 0.36f, 1f);

            switch (kind)
            {
                case ResidentKind.FamiliaPiquenique:
                case ResidentKind.Crianca:
                    // Cesta de vime de piquenique com toalhinha xadrez coral e maçã
                    EllipsePx(tex, 48, 16, 34, 10, ink);
                    EllipsePx(tex, 48, 16, 31, 8, new Color(0.94f, 0.42f, 0.44f, 1f));
                    EllipsePx(tex, 48, 17, 22, 5, new Color(0.99f, 0.94f, 0.84f, 1f));
                    RectPx(tex, 24, 16, 72, 46, ink);
                    RectPx(tex, 27, 19, 69, 43, woodMid);
                    RectPx(tex, 27, 36, 69, 43, woodLight);
                    for (int x = 33; x <= 63; x += 10)
                        RectPx(tex, x, 19, x + 2, 43, woodDark);
                    // Maçã vermelha e pãozinho na cesta
                    CirclePx(tex, 39, 49, 9, ink);
                    CirclePx(tex, 39, 49, 7, new Color(0.92f, 0.26f, 0.24f, 1f));
                    EllipsePx(tex, 56, 48, 11, 7, ink);
                    EllipsePx(tex, 56, 48, 9, 5, woodLight);
                    break;

                case ResidentKind.Musico:
                    // Violão acústico de madeira esculpida apoiado ao lado do músico
                    CirclePx(tex, 46, 26, 20, ink);
                    CirclePx(tex, 46, 26, 17, woodMid);
                    CirclePx(tex, 44, 28, 13, woodLight);
                    CirclePx(tex, 46, 44, 15, ink);
                    CirclePx(tex, 46, 44, 12, woodMid);
                    CirclePx(tex, 46, 35, 6, ink);
                    RectPx(tex, 42, 56, 50, 86, ink);
                    RectPx(tex, 44, 58, 48, 84, woodDark);
                    break;

                case ResidentKind.Feirante:
                    // Caixote de madeira da feira com potes de mel dourado e geleia silvestre
                    RectPx(tex, 18, 12, 78, 46, ink);
                    RectPx(tex, 21, 15, 75, 43, woodMid);
                    RectPx(tex, 21, 28, 75, 31, ink);
                    // Pote de mel e pote de geleia
                    RectPx(tex, 28, 46, 46, 68, ink);
                    RectPx(tex, 31, 49, 43, 64, new Color(0.98f, 0.76f, 0.22f, 1f));
                    RectPx(tex, 29, 64, 45, 70, new Color(0.96f, 0.92f, 0.80f, 1f));
                    RectPx(tex, 50, 46, 68, 68, ink);
                    RectPx(tex, 53, 49, 65, 64, new Color(0.68f, 0.26f, 0.62f, 1f));
                    RectPx(tex, 51, 64, 67, 70, new Color(0.94f, 0.44f, 0.46f, 1f));
                    break;

                default:
                    // Mochila e cantil de trilha de couro com contorno #3B1C14
                    EllipsePx(tex, 48, 30, 22, 20, ink);
                    EllipsePx(tex, 48, 30, 19, 17, new Color(0.36f, 0.58f, 0.34f, 1f));
                    EllipsePx(tex, 48, 36, 16, 9, ink);
                    EllipsePx(tex, 48, 36, 13, 7, woodMid);
                    CirclePx(tex, 48, 33, 4, new Color(1f, 0.84f, 0.26f, 1f));
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 10f / (float)h), 96f);
            residentPropCache[kind] = sp;
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
