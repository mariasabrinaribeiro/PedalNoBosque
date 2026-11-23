using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    public enum BiomeType
    {
        CampoFlorido,
        Colinas,
        RegiaoDoRio,
        TrilhaDaFloresta,
        PequenaVila,
        BosqueDaLua
    }

    /// <summary>
    /// Etapa 2 — Gerador Procedural Infinito por Segmentos:
    /// Cria módulos contínuos de 5 biomas (Campo Florido, Colinas, Região do Rio com pontes de madeira,
    /// Trilha da Floresta e Pequena Vila) com regras de segurança, garantias de água/cozinha,
    /// reciclagem de segmentos antigos, fundo infinito e placas de marco de distância (km).
    /// </summary>
    public class InfiniteTrackGenerator : MonoBehaviour
    {
        public class TrackSegment
        {
            public int index;
            public BiomeType biome;
            public float startX;
            public float endX;
            public GameObject root;
            public readonly List<ForestAnimal> animals = new List<ForestAnimal>();
            public readonly List<VillageResident> residents = new List<VillageResident>();
            public readonly List<TrailAdventureEvent> events = new List<TrailAdventureEvent>();
        }

        private const float SegmentLength = 48f;
        private const float SpawnAheadDistance = 130f;
        private const float DespawnBehindDistance = 95f;
        private const float MinObstacleSpacing = 11f;
        private const float DistanceUnitsPerMeter = 1f / 2.5f; // 1 unidade = 2.5m => 200 unidades = 500m (0.5 km)
        private const float MilestoneStepUnits = 200f;         // Marco visual a cada 0.5 km

        public readonly List<ForestAnimal> ActiveAnimals = new List<ForestAnimal>();
        public readonly List<VillageResident> ActiveResidents = new List<VillageResident>();
        public readonly List<TrailAdventureEvent> ActiveEvents = new List<TrailAdventureEvent>();
        public float MinSafeBacktrackX { get; private set; } = -4f;
        public ForestAnimal PrimarySquirrel { get; private set; }

        private readonly List<TrackSegment> activeSegments = new List<TrackSegment>();
        private readonly Dictionary<int, GameObject> bgTiles = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> roadTiles = new Dictionary<int, GameObject>();
        private readonly List<BiomeType> biomeBag = new List<BiomeType>();
        private readonly Queue<TrailAdventureEvent.EventKind> recentAdventureEvents = new Queue<TrailAdventureEvent.EventKind>();

        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite happySp;
        private Sprite crySp;
        private Sprite tearSp;
        private Sprite[] heartParticles;
        private Sprite bgSprite;
        private Sprite roadStripSprite;
        private float bgTileWidth = 20f;
        private float bgBottomWorldY = -4.22f;

        private int nextSegmentIndex = 0;
        private float nextSegmentStartX = 0f;
        private float nextMilestoneX = MilestoneStepUnits;
        private BiomeType lastBiome = BiomeType.CampoFlorido;

        // Regras de segurança de recursos (garante que o jogador nunca fique sem água ou cozinha)
        private int segmentsSinceLastWater = 0;
        private int segmentsSinceLastKitchen = 0;

        public void Initialize(
            float cameraWorldHeight,
            Dictionary<FoodType, Sprite> foodMap,
            Sprite happySprite,
            Sprite crySprite,
            Sprite tearSprite,
            Sprite[] heartParticleSprites)
        {
            foodSprites = foodMap;
            happySp = happySprite;
            crySp = crySprite;
            tearSp = tearSprite;
            heartParticles = heartParticleSprites;

            bgSprite = FlorestaSpriteFactory.LoadForestBackgroundSprite(cameraWorldHeight);
            roadStripSprite = FlorestaSpriteFactory.LoadForestRoadStripSprite(cameraWorldHeight);
            if (bgSprite != null)
            {
                // Subtrai apenas 0.008f (~0.5px) para eliminar fresta subpixel sem desalinhar a emenda espelhada (flipX) nem duplicar alfa!
                bgTileWidth = Mathf.Max(8f, bgSprite.bounds.size.x - 0.008f);
                bgBottomWorldY = 1.5f - cameraWorldHeight * 0.5f;
            }

            // Gera o fundo inicial, a pista física 1.0x e os primeiros 3 segmentos à frente
            UpdateWorld(0f, 2f);
        }

        public void UpdateWorld(float playerX, float cameraX)
        {
            UpdateBackgroundTiles(cameraX);
            UpdateSegments(playerX);
        }

        public BiomeType GetBiomeAtX(float x)
        {
            for (int i = 0; i < activeSegments.Count; i++)
            {
                TrackSegment seg = activeSegments[i];
                if (x >= seg.startX && x < seg.endX)
                    return seg.biome;
            }
            return activeSegments.Count > 0 ? activeSegments[0].biome : BiomeType.CampoFlorido;
        }

        /// <summary>
        /// Retorna a elevação vertical da trilha (em metros/unidades de mundo acima do nível base da estrada)
        /// e a inclinação visual da bicicleta (em graus) ao atravessar a Ponte de Madeira ou a Subida/Descida das Montanhas.
        /// </summary>
        public float GetTrackElevationAtX(float worldX, out float pitchAngleDeg)
        {
            pitchAngleDeg = 0f;
            for (int i = 0; i < activeSegments.Count; i++)
            {
                TrackSegment seg = activeSegments[i];
                if (worldX < seg.startX || worldX >= seg.endX) continue;

                if (seg.biome == BiomeType.RegiaoDoRio)
                {
                    float bridgeCenterX = seg.startX + 22f;
                    const float halfSpan = 4.25f;
                    float dx = worldX - bridgeCenterX;
                    if (Mathf.Abs(dx) <= halfSpan)
                    {
                        float t = (dx + halfSpan) / (2f * halfSpan);
                        pitchAngleDeg = Mathf.Cos(t * Mathf.PI) * 7.5f;
                        return Mathf.Sin(t * Mathf.PI) * 0.375f;
                    }
                }
                else if (seg.biome == BiomeType.Colinas)
                {
                    float hillStart = seg.startX + 6f;
                    float hillEnd = seg.startX + 30f;
                    if (worldX >= hillStart && worldX <= hillEnd)
                    {
                        float t = (worldX - hillStart) / (hillEnd - hillStart);
                        pitchAngleDeg = Mathf.Sin(t * 2f * Mathf.PI) * 4.5f;
                        return 0f;
                    }
                }
                break;
            }
            return 0f;
        }

        public static string GetBiomeDisplayName(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.CampoFlorido:     return "Vale das Flores";
                case BiomeType.Colinas:          return "Montanhas do Vento";
                case BiomeType.RegiaoDoRio:      return "Vale das Águas";
                case BiomeType.TrilhaDaFloresta: return "Floresta dos Segredos";
                case BiomeType.PequenaVila:      return "Vila dos Viajantes";
                case BiomeType.BosqueDaLua:      return "Bosque da Lua";
                default:                         return "Bosque";
            }
        }

        public static string GetBiomeArrivalTip(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.CampoFlorido:
                    return "Vale das Flores! Pétalas ao vento, flores coloridas e bichinhos amigáveis!";
                case BiomeType.Colinas:
                    return "Montanhas do Vento! Descidas embaladas, folhas voando e rampas com estrelas!";
                case BiomeType.RegiaoDoRio:
                    return "Vale das Águas! Atravesse as pontes de madeira e refresque o cachorrinho!";
                case BiomeType.TrilhaDaFloresta:
                    return "Floresta dos Segredos! Fique atento às raízes, troncos e animais misteriosos!";
                case BiomeType.PequenaVila:
                    return "Vila dos Viajantes! Ajude os moradores nas missões e abasteça na Cozinha!";
                case BiomeType.BosqueDaLua:
                    return "Bosque da Lua! Vaga-lumes, flores luminosas e a Coruja Noturna esperam por você!";
                default:
                    return "Continue pedalando pelo Pedal no Bosque!";
            }
        }

        public static Color GetBiomeAccentColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.CampoFlorido:     return new Color(0.68f, 1f, 0.72f, 1f);
                case BiomeType.Colinas:          return new Color(1f, 0.90f, 0.50f, 1f);
                case BiomeType.RegiaoDoRio:      return new Color(0.56f, 0.90f, 1f, 1f);
                case BiomeType.TrilhaDaFloresta: return new Color(0.74f, 0.96f, 0.62f, 1f);
                case BiomeType.PequenaVila:      return new Color(1f, 0.80f, 0.88f, 1f);
                case BiomeType.BosqueDaLua:      return new Color(0.74f, 0.84f, 1f, 1f);
                default:                         return Color.white;
            }
        }

        // ─── Fundo Infinito ──────────────────────────────────────────

        private const float BgParallaxFactor = 0.16f; // Plano 1 (Paisagem e Montanhas Distantes): move-se a 16% da câmera!

        private void UpdateBackgroundTiles(float cameraX)
        {
            if (bgSprite == null) return;

            // 1. Camada de Paisagem e Montanhas Distantes (Parallax 0.22x)
            float parallaxShiftX = cameraX * (1f - BgParallaxFactor);
            float effectiveX = cameraX - parallaxShiftX;
            int centerIdx = Mathf.FloorToInt(effectiveX / bgTileWidth);
            int minKeep = centerIdx - 2;
            int maxKeep = centerIdx + 3;

            for (int i = minKeep; i <= maxKeep; i++)
            {
                if (!bgTiles.TryGetValue(i, out GameObject bgObj) || bgObj == null)
                {
                    bgObj = new GameObject($"CenarioFloresta_{i}");
                    bgObj.transform.SetParent(transform, false);
                    SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.flipX = (Mathf.Abs(i) % 2 == 1);
                    sr.sortingOrder = -20;
                    bgTiles[i] = bgObj;
                }
                bgObj.transform.position = new Vector3(parallaxShiftX + i * bgTileWidth, 1.5f, 5f);
            }

            List<int> toRemove = null;
            foreach (var kv in bgTiles)
            {
                if (kv.Key < minKeep - 1 || kv.Key > maxKeep + 1)
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove != null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    int idx = toRemove[i];
                    if (bgTiles[idx] != null) Destroy(bgTiles[idx]);
                    bgTiles.Remove(idx);
                }
            }

            // 2. Camada da Estrada e Margens Físicas (1.00x fixas no mundo! Nunca desliza sob os pneus da bicicleta!)
            Sprite stripSp = roadStripSprite != null ? roadStripSprite : bgSprite;
            int roadCenterIdx = Mathf.FloorToInt(cameraX / bgTileWidth);
            int roadMinKeep = roadCenterIdx - 2;
            int roadMaxKeep = roadCenterIdx + 3;

            for (int i = roadMinKeep; i <= roadMaxKeep; i++)
            {
                if (!roadTiles.TryGetValue(i, out GameObject roadObj) || roadObj == null)
                {
                    roadObj = new GameObject($"EstradaFisica_{i}");
                    roadObj.transform.SetParent(transform, false);
                    SpriteRenderer rsr = roadObj.AddComponent<SpriteRenderer>();
                    rsr.sprite = stripSp;
                    rsr.flipX = (Mathf.Abs(i) % 2 == 1);
                    rsr.sortingOrder = -6; // Acima das montanhas (-20) e árvores intermediárias (-10), alinhada com a pista física!
                    roadTiles[i] = roadObj;
                }
                float roadY = (roadStripSprite != null) ? bgBottomWorldY : 1.5f;
                roadObj.transform.position = new Vector3(i * bgTileWidth, roadY, 0f);
            }

            List<int> roadRemove = null;
            foreach (var kv in roadTiles)
            {
                if (kv.Key < roadMinKeep - 1 || kv.Key > roadMaxKeep + 1)
                {
                    if (roadRemove == null) roadRemove = new List<int>();
                    roadRemove.Add(kv.Key);
                }
            }

            if (roadRemove != null)
            {
                for (int i = 0; i < roadRemove.Count; i++)
                {
                    int idx = roadRemove[i];
                    if (roadTiles[idx] != null) Destroy(roadTiles[idx]);
                    roadTiles.Remove(idx);
                }
            }
        }

        // ─── Segmentos Infinitos ─────────────────────────────────────

        private void UpdateSegments(float playerX)
        {
            // Gera segmentos à frente
            while (nextSegmentStartX < playerX + SpawnAheadDistance)
            {
                SpawnNextSegment();
            }

            // Recicla segmentos muito atrás do jogador
            while (activeSegments.Count > 0 && activeSegments[0].endX < playerX - DespawnBehindDistance)
            {
                TrackSegment old = activeSegments[0];

                // Nunca recicla um segmento se o esquilo estiver cavando ou o gato estiver acompanhando a bike
                bool keepSegment = false;
                for (int i = 0; i < old.animals.Count; i++)
                {
                    if (old.animals[i] != null &&
                        (old.animals[i].emotion == ForestAnimal.EmotionState.Cavando || old.animals[i].isCatFollowing))
                    {
                        keepSegment = true;
                        break;
                    }
                }
                if (keepSegment) break;

                activeSegments.RemoveAt(0);
                for (int i = 0; i < old.animals.Count; i++)
                {
                    ForestAnimal a = old.animals[i];
                    ActiveAnimals.Remove(a);
                    if (a == PrimarySquirrel) PrimarySquirrel = null;
                }
                for (int i = 0; i < old.residents.Count; i++)
                {
                    ActiveResidents.Remove(old.residents[i]);
                }
                for (int i = 0; i < old.events.Count; i++)
                {
                    ActiveEvents.Remove(old.events[i]);
                }

                MinSafeBacktrackX = Mathf.Max(MinSafeBacktrackX, old.endX + 2f);
                if (old.root != null) Destroy(old.root);
            }

            // Garante referência válida do Esquilo para o HUD
            if (PrimarySquirrel == null)
            {
                for (int i = 0; i < ActiveAnimals.Count; i++)
                {
                    if (ActiveAnimals[i] != null && ActiveAnimals[i].isSquirrelSpecial)
                    {
                        PrimarySquirrel = ActiveAnimals[i];
                        break;
                    }
                }
            }
        }

        private BiomeType PickNextBiome()
        {
            if (nextSegmentIndex == 0)
            {
                lastBiome = BiomeType.CampoFlorido;
                return BiomeType.CampoFlorido;
            }
            if (nextSegmentIndex == 1)
            {
                // Região demonstrativa de referência (Vale das Águas) logo após o início!
                lastBiome = BiomeType.RegiaoDoRio;
                return BiomeType.RegiaoDoRio;
            }

            if (biomeBag.Count == 0)
            {
                biomeBag.Add(BiomeType.CampoFlorido);
                biomeBag.Add(BiomeType.Colinas);
                biomeBag.Add(BiomeType.RegiaoDoRio);
                biomeBag.Add(BiomeType.TrilhaDaFloresta);
                biomeBag.Add(BiomeType.PequenaVila);
                biomeBag.Add(BiomeType.BosqueDaLua);

                // Embaralha (Fisher-Yates)
                for (int i = biomeBag.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    BiomeType tmp = biomeBag[i];
                    biomeBag[i] = biomeBag[j];
                    biomeBag[j] = tmp;
                }

                // Evita repetir exatamente o último bioma na virada da sacola
                if (biomeBag[0] == lastBiome && biomeBag.Count > 1)
                {
                    BiomeType tmp = biomeBag[0];
                    biomeBag[0] = biomeBag[1];
                    biomeBag[1] = tmp;
                }
            }

            BiomeType chosen = biomeBag[0];
            biomeBag.RemoveAt(0);
            lastBiome = chosen;
            return chosen;
        }

        private void SpawnNextSegment()
        {
            BiomeType biome = PickNextBiome();
            float startX = nextSegmentStartX;
            float endX = startX + SegmentLength;

            GameObject root = new GameObject($"Segmento_{nextSegmentIndex}_{biome}");
            root.transform.SetParent(transform, false);

            TrackSegment seg = new TrackSegment
            {
                index = nextSegmentIndex,
                biome = biome,
                startX = startX,
                endX = endX,
                root = root
            };

            // Lista de zonas bloqueadas onde obstáculos NÃO podem nascer (regra de segurança)
            List<(float min, float max)> blockedZones = new List<(float min, float max)>();
            List<float> placedObstacles = new List<float>();

            bool spawnedWater = false;
            bool spawnedKitchen = false;

            if (nextSegmentIndex == 0)
            {
                BuildStarterSegment(seg, blockedZones, placedObstacles, out spawnedWater, out spawnedKitchen);
            }
            else
            {
                switch (biome)
                {
                    case BiomeType.CampoFlorido:
                        BuildCampoFlorido(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                    case BiomeType.Colinas:
                        BuildColinas(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                    case BiomeType.RegiaoDoRio:
                        BuildRegiaoDoRio(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                    case BiomeType.TrilhaDaFloresta:
                        BuildTrilhaDaFloresta(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                    case BiomeType.PequenaVila:
                        BuildPequenaVila(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                    case BiomeType.BosqueDaLua:
                        BuildBosqueDaLua(seg, blockedZones, placedObstacles, ref spawnedWater, ref spawnedKitchen);
                        break;
                }

                // Regras de segurança globais: garante água e cozinha em intervalos seguros
                if (!spawnedWater && segmentsSinceLastWater >= 2)
                {
                    float wx = startX + 7f;
                    SpawnWaterColumn(root.transform, wx, blockedZones);
                    spawnedWater = true;
                }

                if (!spawnedKitchen && segmentsSinceLastKitchen >= 3)
                {
                    float kx = startX + SegmentLength - 7f;
                    SpawnKitchenStation(root.transform, kx, blockedZones);
                    spawnedKitchen = true;
                }
            }

            // Colecionável natural coerente com o bioma (Etapa 7)
            SpawnBiomeCollectible(root.transform, biome, startX, seg.index);

            // Evento contextual de aventura (regar flores, ajudar passarinho, consertar ponte, tesouro escondido)
            SpawnContextualAdventureEvent(seg, biome, startX, blockedZones);

            // Placas de marco de distância (a cada 0.5 km) que caírem dentro deste segmento
            while (nextMilestoneX < endX)
            {
                if (nextMilestoneX >= startX)
                {
                    float kmValue = (nextMilestoneX / DistanceUnitsPerMeter) / 1000f;
                    SpawnDistanceSign(root.transform, nextMilestoneX, kmValue);
                }
                nextMilestoneX += MilestoneStepUnits;
            }

            segmentsSinceLastWater = spawnedWater ? 0 : segmentsSinceLastWater + 1;
            segmentsSinceLastKitchen = spawnedKitchen ? 0 : segmentsSinceLastKitchen + 1;

            activeSegments.Add(seg);
            nextSegmentIndex++;
            nextSegmentStartX = endX;
        }

        // ─── Construtores dos 5 Biomas ───────────────────────────────

        private void BuildStarterSegment(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            out bool hasWater,
            out bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            SpawnMeadowFlowers(p, s, seg.index);

            // Riqueza ambiental e elementos de descoberta atrás do meio-fio da estrada (-2.38f)
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 3.2f, -2.38f, 0f), 0.92f, -1, 1.3f);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(0), new Vector3(s + 5.4f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreatePicnicBasketAndCrateSprite(), new Vector3(s + 10.6f, -2.40f, 0f), 0.90f, 2);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 15.6f, -2.38f, 0f), 0.95f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(s + 23.2f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateAnimalBurrowSprite(), new Vector3(s + 35.6f, -2.39f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyStonesClusterSprite(), new Vector3(s + 41.2f, -2.40f, 0f), 0.92f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 45.0f, -2.38f, 0f), 0.88f, -1, 1.2f);

            SpawnKitchenStation(p, s + 8f, blocked);
            SpawnBench(p, s + 13f);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 6, s + 18.0f, blocked);
            SpawnWaterColumn(p, s + 25f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho da Trilha", 2, s + 29.2f, blocked);
            SpawnRampWithStars(p, s + 33.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 3, s + 39.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Coelho, "Coelho", 2, s + 44.5f, blocked);

            hasWater = true;
            hasKitchen = true;
        }

        // Canteiros extras de flores silvestres, arbustos e samambaias variados na margem superior da estrada (-2.38f).
        private void SpawnMeadowFlowers(Transform parent, float start, int variant)
        {
            float[] offsets = { 2.0f, 16.4f, 26.8f, 36.6f, 44.2f };
            for (int i = 0; i < offsets.Length; i++)
            {
                float sc = 0.86f + ((i + variant) % 3) * 0.08f;
                SpawnSwayingProp(parent, BiomePropsFactory.CreateFlowerPatchSprite(variant + i),
                    new Vector3(start + offsets[i], -2.39f, 0f), sc, 1, 1.4f);
            }
        }

        private void BuildCampoFlorido(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            SpawnMeadowFlowers(p, s, seg.index);
            SpawnBench(p, s + 11.5f);

            // Vegetação extra, casinha de pássaros, placa direcional, tronco musgoso e toca de animais
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 9.2f, -2.28f, 0f), 0.70f, -4, 1.1f);
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 34.5f, -2.28f, 0f), 0.66f, -4, 1.0f);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(seg.index), new Vector3(s + 4.0f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreatePicnicBasketAndCrateSprite(), new Vector3(s + 13.4f, -2.40f, 0f), 0.90f, 2);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 17.4f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(s + 28.8f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateAnimalBurrowSprite(), new Vector3(s + 39.8f, -2.39f, 0f), 0.92f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 44.0f, -2.38f, 0f), 0.90f, -1, 1.2f);

            // Bastante animais variados no Campo Florido: Gato, Cachorrinho, Coelho, Esquilo, Passarinhos, Raposa e Família de Ursos!
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gatinho do Campo", 2, s + 6.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho do Bosque", 2, s + 15.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Coelho, "Coelho", 2, s + 19.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Passarinhos, "Passarinhos", 2, s + 21.8f, blocked);

            // Rampa suave no meio do campo
            SpawnRampWithStars(p, s + 24f, blocked);

            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 3, s + 31.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 3, s + 37.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Raposa, "Raposa", 3, s + 42.2f, blocked);

            if (seg.index % 2 == 1)
                SpawnResident(seg, VillageResident.ResidentKind.FamiliaPiquenique, s + 45.8f, blocked);
            else
                TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Pedra, s + 46f, blocked, obstacles);
        }

        private void BuildColinas(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            // Cerca rústica de madeira com pedras musgosas e flores na margem superior da estrada (-2.38f, sem cortar o chão!)
            SpawnProp(p, BiomePropsFactory.CreateMountainHillTrailSprite(), new Vector3(s + 18f, -2.38f, 0f), 1.0f, -2);

            // Carvalhos e decoração de trilha acompanhando a margem da estrada (sortOrder -4..-1)
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 10f, -2.28f, 0f), 0.70f, -4, 1.1f);
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 26f, -2.28f, 0f), 0.68f, -4, 1.0f);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(2), new Vector3(s + 2.4f, -2.38f, 0f), 0.95f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyStonesClusterSprite(), new Vector3(s + 7.5f, -2.39f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(s + 21.8f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 31.2f, -2.38f, 0f), 0.92f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 42.2f, -2.38f, 0f), 0.90f, -1, 1.2f);
            SpawnMeadowFlowers(p, s, seg.index + 1);

            // Subida longa (s+6..s+18) seguida de descida embalada (s+18..s+30) + rampa de salto!
            CreateSlope(p, SlopeZone.SlopeType.Subida, s + 12f, 12f);
            CreateSlope(p, SlopeZone.SlopeType.Descida, s + 24f, 12f);
            blocked.Add((s + 5f, s + 31f));

            SpawnRampWithStars(p, s + 34f, blocked);

            // Animais na base, subida, topo e descida da montanha (Família de Ursos, Cervo, Raposa, Esquilo, Gato e Cachorrinho!)
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho da Trilha", 2, s + 4.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Cervo, "Cervo", 4, s + 13.0f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 4, s + 18.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Raposa, "Raposa", 3, s + 25.0f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 3, s + 28.4f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gatinho da Colina", 2, s + 39.8f, blocked);

            // Água opcional no pé da colina
            if (segmentsSinceLastWater >= 1)
            {
                SpawnWaterColumn(p, s + 44.5f, blocked);
                hasWater = true;
            }
        }

        private void BuildRegiaoDoRio(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            // Conjunto integrado: Rio sob a ponte + Ponte de Madeira com piso superior no exato nível das rodas (-3.45f)
            float bridgeCenterX = s + 22f;
            GameObject riverGroup = new GameObject($"RioPonteIntegrada_{seg.index}");
            riverGroup.transform.SetParent(p, false);
            riverGroup.transform.position = new Vector3(bridgeCenterX, -3.45f, 0f);

            // Água cristalina contida sob o tablado da ponte
            GameObject waterObj = new GameObject("AguaRio");
            waterObj.transform.SetParent(riverGroup.transform, false);
            waterObj.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            SpriteRenderer waterSR = waterObj.AddComponent<SpriteRenderer>();
            waterSR.sprite = BiomePropsFactory.CreateRiverWaterSprite();
            waterSR.sortingOrder = 1;

            // Camada de correnteza cintilante animada sob os pilares
            GameObject shimmerObj = new GameObject("CorrentezaBrilho");
            shimmerObj.transform.SetParent(riverGroup.transform, false);
            shimmerObj.transform.localPosition = new Vector3(0f, -0.43f, 0f);
            SpriteRenderer shimmerSR = shimmerObj.AddComponent<SpriteRenderer>();
            shimmerSR.sprite = BiomePropsFactory.CreateRiverShimmerSprite();
            shimmerSR.sortingOrder = 2;

            // Ponte de madeira com piso transitável no topo (todas as vigas e pilares ficam ABAIXO dos pneus da bicicleta!)
            GameObject bridgeObj = new GameObject("PonteMadeira");
            bridgeObj.transform.SetParent(riverGroup.transform, false);
            bridgeObj.transform.localPosition = Vector3.zero;
            SpriteRenderer bridgeSR = bridgeObj.AddComponent<SpriteRenderer>();
            bridgeSR.sprite = BiomePropsFactory.CreateWoodenBridgeSprite();
            bridgeSR.sortingOrder = 3;

            // Carvalhos ilustrados, arbustos, pedras musgosas e placas nas margens atrás do meio-fio da estrada
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(bridgeCenterX - 8.2f, -2.28f, 0f), 0.72f, -4, 1.2f);
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(bridgeCenterX + 8.4f, -2.28f, 0f), 0.68f, -4, 1.1f);
            SpawnProp(p, BiomePropsFactory.CreateMossyStonesClusterSprite(), new Vector3(bridgeCenterX - 6.0f, -2.39f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(bridgeCenterX + 6.4f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(1), new Vector3(s + 2.4f, -2.38f, 0f), 0.94f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 34.8f, -2.38f, 0f), 0.90f, -1, 1.2f);
            SpawnMeadowFlowers(p, s, seg.index + 2);

            // Vida na água (peixes em pool saltando + correnteza + balanço leve da madeira)
            riverGroup.AddComponent<LivingRiverFeature>().Setup(shimmerObj.transform, bridgeObj.transform);

            // Bloqueia obstáculos sobre a ponte e nas cabeceiras (segurança)
            blocked.Add((bridgeCenterX - 8.5f, bridgeCenterX + 8.5f));

            // Coluna de água garantida na Região do Rio!
            SpawnWaterColumn(p, s + 9f, blocked);
            hasWater = true;

            // Bastante animais nas duas margens do rio: Quati, Castor, Sapo, Gato, Cachorrinho, Cervo e Família de Ursos!
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho do Rio", 2, s + 4.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Quati, "Quati", 3, s + 12.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Castor, "Castor", 3, s + 15.4f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Sapo, "Sapo", 2, s + 28.6f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gatinho", 2, s + 31.8f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 3, s + 37.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Cervo, "Cervo", 4, s + 43.5f, blocked);
        }

        private void BuildTrilhaDaFloresta(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            // Carvalhos imponentes da mata atrás do meio-fio da trilha (sortOrder -4)
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 12f, -2.28f, 0f), 0.72f, -4, 1.1f);
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 35f, -2.28f, 0f), 0.68f, -4, 1.0f);

            // Vegetação densa e elementos de descoberta da floresta
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 2.6f, -2.38f, 0f), 0.94f, -1, 1.2f);
            SpawnProp(p, BiomePropsFactory.CreateAnimalBurrowSprite(), new Vector3(s + 10.6f, -2.39f, 0f), 0.95f, 1);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(seg.index), new Vector3(s + 16.4f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(s + 21.8f, -2.39f, 0f), 0.95f, 1);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 31.2f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateGlowingMushroomSprite(), new Vector3(s + 37.8f, -2.40f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyStonesClusterSprite(), new Vector3(s + 43.4f, -2.39f, 0f), 0.92f, 1);
            SpawnMeadowFlowers(p, s, seg.index + 3);

            // Muitos animais na Trilha da Floresta: Família de Ursos, Lobo, Quati, Ouriço, Gato, Cachorrinho, Raposa e Esquilo!
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gatinho do Bosque", 2, s + 4.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Ourico, "Ouriço", 2, s + 7.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 4, s + 13.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Lobo, "Lobo", 4, s + 19.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho", 2, s + 24.0f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Quati, "Quati", 3, s + 34.0f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 4, s + 41.0f, blocked);

            // Obstáculos naturais bem espaçados com espaço seguro para saltar
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Raiz, s + 8.5f, blocked, obstacles);
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Tronco, s + 28.5f, blocked, obstacles);
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Pedra, s + 45.8f, blocked, obstacles);
        }

        private void BuildPequenaVila(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            // Chalés enxaimel ilustrados espaçados harmoniosamente ao longo do meio-fio (-2.36f) sem sobrepor bebedouro ou barraca
            SpawnProp(p, BiomePropsFactory.CreateVillageCottageSprite(seg.index), new Vector3(s + 7.5f, -2.36f, 0f), 1.0f, 1);
            SpawnProp(p, BiomePropsFactory.CreateVillageCottageSprite(seg.index + 1), new Vector3(s + 41.5f, -2.36f, 0f), 0.98f, 1);

            // Postes de ferro forjado com lanterna, guirlandas de bandeirolas e banco de praça
            SpawnProp(p, BiomePropsFactory.CreateVillageLampSprite(), new Vector3(s + 3.2f, -2.40f, 0f), 1.0f, 2);
            SpawnProp(p, BiomePropsFactory.CreateVillageLampSprite(), new Vector3(s + 21.0f, -2.40f, 0f), 1.0f, 2);
            SpawnProp(p, BiomePropsFactory.CreateVillageLampSprite(), new Vector3(s + 36.8f, -2.40f, 0f), 1.0f, 2);

            SpawnProp(p, BiomePropsFactory.CreateVillageBuntingSprite(), new Vector3(s + 12.1f, 0.12f, 0f), 1.0f, 2);
            SpawnProp(p, BiomePropsFactory.CreateVillageBuntingSprite(), new Vector3(s + 28.9f, 0.12f, 0f), 1.0f, 2);

            // Barraca da Cozinha garantida na Vila dos Viajantes (s + 15.5f)
            SpawnKitchenStation(p, s + 15.5f, blocked);
            hasKitchen = true;

            // Banco de praça na área central (s + 25.0f)
            SpawnBench(p, s + 25.0f);

            // Bebedouro na praça da vila (s + 31.8f, livre de qualquer chalé!)
            if (segmentsSinceLastWater >= 1)
            {
                SpawnWaterColumn(p, s + 31.8f, blocked);
                hasWater = true;
            }

            // Carvalho acolhedor e detalhes artesanais da praça da vila atrás do meio-fio
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 28.5f, -2.28f, 0f), 0.70f, -4, 1.1f);
            SpawnProp(p, BiomePropsFactory.CreateDirectionalTrailSignSprite(seg.index), new Vector3(s + 1.8f, -2.38f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreatePicnicBasketAndCrateSprite(), new Vector3(s + 18.2f, -2.40f, 0f), 0.92f, 2);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 22.8f, -2.38f, 0f), 0.94f, 1);
            SpawnMeadowFlowers(p, s, seg.index + 4);

            // Moradores da Vila com Missões Aleatórias posicionados ao lado do banco e na saída da praça (sem sobreposição!)
            VillageResident.ResidentKind[] residentPool =
            {
                VillageResident.ResidentKind.Crianca,
                VillageResident.ResidentKind.SenhorBanco,
                VillageResident.ResidentKind.FamiliaPiquenique,
                VillageResident.ResidentKind.ViajanteSede,
                VillageResident.ResidentKind.Musico,
                VillageResident.ResidentKind.Feirante
            };
            VillageResident.ResidentKind r1 = residentPool[seg.index % residentPool.Length];
            VillageResident.ResidentKind r2 = residentPool[(seg.index + 2) % residentPool.Length];
            SpawnResident(seg, r1, s + 20.2f, blocked);
            SpawnResident(seg, r2, s + 35.0f, blocked);

            // Animais variados e bem distribuídos na vila (sem duplicações agrupadas!)
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho da Vila", 2, s + 11.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo da Praça", 2, s + 28.4f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gato da Praça", 2, s + 44.5f, blocked);
        }

        private void BuildBosqueDaLua(
            TrackSegment seg,
            List<(float min, float max)> blocked,
            List<float> obstacles,
            ref bool hasWater,
            ref bool hasKitchen)
        {
            Transform p = seg.root.transform;
            float s = seg.startX;

            // Carvalhos, flores lunares, cogumelos encantados e elementos de descoberta ao longo da trilha noturna
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 11f, -2.28f, 0f), 0.72f, -4, 1.1f);
            SpawnSwayingProp(p, BiomePropsFactory.LoadOakTreeSprite(), new Vector3(s + 47f, -2.28f, 0f), 0.66f, -4, 1.0f);
            SpawnSwayingProp(p, BiomePropsFactory.CreateMoonFlowerSprite(), new Vector3(s + 3.4f, -2.39f, 0f), 0.95f, 1, 1.3f);
            SpawnProp(p, BiomePropsFactory.CreateGlowingMushroomSprite(), new Vector3(s + 9.4f, -2.40f, 0f), 0.94f, 1);
            SpawnProp(p, BiomePropsFactory.CreateAnimalBurrowSprite(), new Vector3(s + 15.0f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateMossyLogAndFernsSprite(), new Vector3(s + 20.6f, -2.39f, 0f), 0.92f, 1);
            SpawnProp(p, BiomePropsFactory.CreateBirdhousePostSprite(), new Vector3(s + 30.8f, -2.38f, 0f), 0.94f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateMoonFlowerSprite(), new Vector3(s + 36.4f, -2.39f, 0f), 0.95f, 1, 1.3f);
            SpawnProp(p, BiomePropsFactory.CreateGlowingMushroomSprite(), new Vector3(s + 41.2f, -2.40f, 0f), 0.92f, 1);
            SpawnSwayingProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 44.2f, -2.38f, 0f), 0.90f, -1, 1.2f);

            // Bastante animais no Bosque da Lua: Gato, Cachorrinho, Coruja, Família de Ursos, Cervo e Raposa!
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gato", 2, s + 6.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Cachorrinho", 2, s + 12.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.CorujaNoturna, "Coruja", 3, s + 17.5f, blocked);

            // Rampa iluminada com estrelas bônus
            SpawnRampWithStars(p, s + 25f, blocked);

            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.FilhoteUrso, "Família de Ursos", 3, s + 33.5f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Cervo, "Cervo", 4, s + 39.2f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Raposa, "Raposa", 3, s + 42.8f, blocked);

            // Obstáculo natural seguro no final do segmento
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Raiz, s + 45.8f, blocked, obstacles);
        }

        // ─── Helpers de Spawn + Regras de Segurança ──────────────────

        private void SpawnProp(Transform parent, Sprite sp, Vector3 pos, float scale, int sortOrder)
        {
            if (sp == null) return;
            GameObject go = new GameObject("PropBioma");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;
        }

        private void SpawnSwayingProp(Transform parent, Sprite sp, Vector3 pos, float scale, int sortOrder, float maxAngleDeg)
        {
            if (sp == null) return;
            GameObject go = new GameObject("PropVivo");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;
            GentleSway sway = go.AddComponent<GentleSway>();
            sway.maxAngleDeg = maxAngleDeg;
            sway.speed = Random.Range(1.3f, 2.0f);
        }

        private void CreateSlope(Transform parent, SlopeZone.SlopeType type, float xCenter, float length)
        {
            GameObject obj = new GameObject(type == SlopeZone.SlopeType.Subida ? "ZonaSubida" : "ZonaDescida");
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(xCenter, -2.95f, 0f);
            obj.AddComponent<SlopeZone>().SetupSideTrail(type, new Vector2(length, 3.8f));
        }

        private void SpawnRampWithStars(Transform parent, float rx, List<(float min, float max)> blocked)
        {
            GameObject rObj = new GameObject($"RampaSalto_{rx:F0}");
            rObj.transform.SetParent(parent, false);
            rObj.transform.position = new Vector3(rx, -3.55f, 0f);
            rObj.AddComponent<JumpRamp>().Setup();

            // Bloqueia obstáculos antes da rampa e em toda a área de aterrissagem
            blocked.Add((rx - 3.5f, rx + 11.5f));

            for (int i = 0; i < 3; i++)
            {
                GameObject stObj = new GameObject($"EstrelaAr_{rx:F0}_{i}");
                stObj.transform.SetParent(parent, false);
                float sx = rx + 2.2f + i * 1.6f;
                float sy = -0.9f + (i == 1 ? 0.55f : 0f);
                stObj.transform.position = new Vector3(sx, sy, 0f);
                stObj.AddComponent<AerialCollectible>().Setup(50);
            }
        }

        private void SpawnKitchenStation(Transform parent, float kx, List<(float min, float max)> blocked)
        {
            GameObject kObj = new GameObject($"BarracaCozinha_{kx:F0}");
            kObj.transform.SetParent(parent, false);
            kObj.transform.position = new Vector3(kx, -2.40f, 0f);
            kObj.AddComponent<KitchenStation>().Setup();
            blocked.Add((kx - 5f, kx + 5f));
        }

        private void SpawnBench(Transform parent, float xPos)
        {
            GameObject go = new GameObject($"BancoPraca_{xPos:F0}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(xPos, -2.42f, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BiomePropsFactory.LoadParkBenchSprite();
            sr.sortingOrder = 3;
            BiomePropsFactory.AddContactShadow(go.transform, new Vector3(0f, 0.01f, 0f), new Vector2(2.05f, 0.28f), 2);
        }

        private void SpawnWaterColumn(Transform parent, float wx, List<(float min, float max)> blocked)
        {
            GameObject wObj = new GameObject($"ColunaAgua_{wx:F0}");
            wObj.transform.SetParent(parent, false);
            wObj.transform.position = new Vector3(wx, -2.42f, 0f);
            wObj.AddComponent<WaterColumn>().Setup();
            blocked.Add((wx - 4.5f, wx + 4.5f));
        }

        private void SpawnAnimal(
            TrackSegment seg,
            ParkSpriteFactory.AnimalKind kind,
            string displayName,
            int meals,
            float xPos,
            List<(float min, float max)> blocked)
        {
            // Garante espaçamento orgânico mínimo de 4.2m de qualquer animal e 16m da MESMA espécie/aparência,
            // impedindo duas raposas, dois quatis/guaxinins ou dois grupos de ursos lado a lado na mesma tela!
            for (int i = 0; i < ActiveAnimals.Count; i++)
            {
                ForestAnimal existing = ActiveAnimals[i];
                if (existing == null) continue;
                float dist = Mathf.Abs(existing.transform.position.x - xPos);
                bool sameVisualSpecies = existing.kind == kind
                    || ((existing.kind == ParkSpriteFactory.AnimalKind.Quati || existing.kind == ParkSpriteFactory.AnimalKind.Guaxinim)
                        && (kind == ParkSpriteFactory.AnimalKind.Quati || kind == ParkSpriteFactory.AnimalKind.Guaxinim));
                if (sameVisualSpecies && dist < 16.0f)
                {
                    return;
                }
                if (dist < 4.2f)
                {
                    xPos += (xPos >= existing.transform.position.x ? 1f : -1f) * (4.3f - dist);
                }
            }

            GameObject obj = new GameObject($"Animal_{displayName}_{seg.index}");
            obj.transform.SetParent(seg.root.transform, false);
            obj.transform.position = new Vector3(xPos, -2.42f, 0f);

            // Variação natural de tamanho para diferenciar os animais ambientais
            float scaleVar = 0.90f + ((Mathf.Abs(Mathf.RoundToInt(xPos * 10f)) + seg.index) % 4) * 0.055f;
            obj.transform.localScale = new Vector3(scaleVar, scaleVar, 1f);

            ForestAnimal a = obj.AddComponent<ForestAnimal>();
            a.Setup(kind, displayName, meals, foodSprites, happySp, crySp, tearSp, heartParticles);
            seg.animals.Add(a);
            ActiveAnimals.Add(a);

            if (a.isSquirrelSpecial && PrimarySquirrel == null)
                PrimarySquirrel = a;

            blocked.Add((xPos - 4.5f, xPos + 4.5f));
        }

        private void SpawnResident(
            TrackSegment seg,
            VillageResident.ResidentKind kind,
            float xPos,
            List<(float min, float max)> blocked)
        {
            GameObject obj = new GameObject($"Morador_{kind}_{seg.index}");
            obj.transform.SetParent(seg.root.transform, false);
            obj.transform.position = new Vector3(xPos, -2.42f, 0f);

            VillageResident r = obj.AddComponent<VillageResident>();
            BiomePropsFactory.AddContactShadow(obj.transform, new Vector3(0f, 0.01f, 0f), new Vector2(1.1f, 0.24f), 2);
            r.Setup(kind, foodSprites, happySp, heartParticles);
            seg.residents.Add(r);
            ActiveResidents.Add(r);
            blocked.Add((xPos - 4.5f, xPos + 4.5f));
        }

        private bool TrySpawnSafeObstacle(
            Transform parent,
            TrailObstacle.ObstacleKind kind,
            float candidateX,
            List<(float min, float max)> blocked,
            List<float> placedObstacles)
        {
            for (int i = 0; i < blocked.Count; i++)
            {
                if (candidateX >= blocked[i].min && candidateX <= blocked[i].max)
                    return false;
            }

            for (int i = 0; i < placedObstacles.Count; i++)
            {
                if (Mathf.Abs(candidateX - placedObstacles[i]) < MinObstacleSpacing)
                    return false;
            }

            GameObject obObj = new GameObject($"Obstaculo_{kind}_{candidateX:F0}");
            obObj.transform.SetParent(parent, false);
            obObj.transform.position = new Vector3(candidateX, -3.55f, 0f);
            obObj.AddComponent<TrailObstacle>().Setup(kind);
            placedObstacles.Add(candidateX);
            return true;
        }

        private void SpawnBiomeCollectible(Transform parent, BiomeType biome, float startX, int segIndex)
        {
            CollectibleKind kind;
            switch (biome)
            {
                case BiomeType.CampoFlorido:
                    kind = (segIndex % 2 == 0) ? CollectibleKind.Flores : CollectibleKind.Sementes;
                    break;
                case BiomeType.Colinas:
                    kind = (segIndex % 2 == 0) ? CollectibleKind.FolhasEspeciais : CollectibleKind.PedrasBrilhantes;
                    break;
                case BiomeType.RegiaoDoRio:
                    kind = (segIndex % 2 == 0) ? CollectibleKind.PedrasBrilhantes : CollectibleKind.Flores;
                    break;
                case BiomeType.TrilhaDaFloresta:
                    kind = (segIndex % 3 == 0) ? CollectibleKind.TesourosSecretos : CollectibleKind.Cogumelos;
                    break;
                case BiomeType.PequenaVila:
                    kind = (segIndex % 2 == 0) ? CollectibleKind.Flores : CollectibleKind.Sementes;
                    break;
                case BiomeType.BosqueDaLua:
                    kind = (segIndex % 2 == 0) ? CollectibleKind.TesourosSecretos : CollectibleKind.Cogumelos;
                    break;
                default:
                    kind = CollectibleKind.Flores;
                    break;
            }

            GameObject colObj = new GameObject($"Colecionavel_{kind}_{segIndex}");
            colObj.transform.SetParent(parent, false);
            colObj.transform.position = new Vector3(startX + 29f, -2.15f, 0f);
            colObj.AddComponent<TrailCollectibleItem>().Setup(kind);
        }

        private void SpawnContextualAdventureEvent(TrackSegment seg, BiomeType biome, float startX, List<(float min, float max)> blocked)
        {
            if (seg.index != 0 && TrySpawnHedgehogStoryEvent(seg, biome, startX, blocked)) return;
            // Nem todo trecho precisa de uma missão: intervalos sem eventos deixam o passeio respirar.
            if (seg.index != 0 && Random.value > 0.72f) return;

            TrailAdventureEvent.EventKind[] candidates;
            switch (biome)
            {
                case BiomeType.CampoFlorido:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.FloresMurchas,
                        TrailAdventureEvent.EventKind.MusicaDaFloresta,
                        TrailAdventureEvent.EventKind.CuidadoraAbrigo,
                        TrailAdventureEvent.EventKind.PassaroCaido
                    };
                    break;
                case BiomeType.Colinas:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.PicapauArvore,
                        TrailAdventureEvent.EventKind.PegadasCachorro,
                        TrailAdventureEvent.EventKind.PassaroCaido
                    };
                    break;
                case BiomeType.RegiaoDoRio:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.LimparRiacho,
                        TrailAdventureEvent.EventKind.PonteQuebrada,
                        TrailAdventureEvent.EventKind.MusicaDaFloresta
                    };
                    PlayerProgressManager pm = PlayerProgressManager.Instance;
                    if (pm != null && pm.Data != null && pm.Data.castorBridgeRepaired
                        && pm.TryScheduleCastorBridgeFollowup())
                    {
                        candidates = new[] { TrailAdventureEvent.EventKind.PonteCastorPronta };
                    }
                    break;
                case BiomeType.TrilhaDaFloresta:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.ToupeiraSubterranea,
                        TrailAdventureEvent.EventKind.EnigmaCoruja,
                        TrailAdventureEvent.EventKind.TesouroEscondido,
                        TrailAdventureEvent.EventKind.PicapauArvore
                    };
                    break;
                case BiomeType.PequenaVila:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.RatinhoCarteiro,
                        TrailAdventureEvent.EventKind.TexugoOficina,
                        TrailAdventureEvent.EventKind.CuidadoraAbrigo,
                        TrailAdventureEvent.EventKind.FloresMurchas
                    };
                    break;
                case BiomeType.BosqueDaLua:
                    candidates = new[] {
                        TrailAdventureEvent.EventKind.EnigmaCoruja,
                        TrailAdventureEvent.EventKind.MusicaDaFloresta,
                        TrailAdventureEvent.EventKind.ToupeiraSubterranea,
                        TrailAdventureEvent.EventKind.TesouroEscondido
                    };
                    break;
                default:
                    candidates = new[] { TrailAdventureEvent.EventKind.FloresMurchas };
                    break;
            }

            TrailAdventureEvent.EventKind kind = seg.index == 0
                ? TrailAdventureEvent.EventKind.FloresMurchas
                : ChooseWeightedAdventureEvent(candidates);

            float ex = (seg.index == 0) ? startX + 27.5f : startX + 35f;
            GameObject evObj = new GameObject($"EventoAventura_{kind}_{seg.index}");
            evObj.transform.SetParent(seg.root.transform, false);
            evObj.transform.position = new Vector3(ex, -2.42f, 0f);
            TrailAdventureEvent ev = evObj.AddComponent<TrailAdventureEvent>();
            ev.Setup(kind);
            seg.events.Add(ev);
            ActiveEvents.Add(ev);
            blocked.Add((ex - 3.5f, ex + 3.5f));
        }

        private bool TrySpawnHedgehogStoryEvent(TrackSegment seg, BiomeType biome, float startX, List<(float min, float max)> blocked)
        {
            PlayerProgressManager pm = PlayerProgressManager.Instance;
            if (pm == null || pm.Data == null || pm.Data.hedgehogStoryStage >= 4) return false;
            if (biome != BiomeType.CampoFlorido && biome != BiomeType.TrilhaDaFloresta && biome != BiomeType.PequenaVila) return false;

            TrailAdventureEvent.EventKind storyKind;
            switch (pm.Data.hedgehogStoryStage)
            {
                case 0: storyKind = TrailAdventureEvent.EventKind.OuricoProcurandoAbrigo; break;
                case 1: storyKind = TrailAdventureEvent.EventKind.OuricoTocaBloqueada; break;
                case 2: storyKind = TrailAdventureEvent.EventKind.OuricoReconstruirAbrigo; break;
                default: storyKind = TrailAdventureEvent.EventKind.OuricoCelebraComAnimais; break;
            }

            for (int i = 0; i < ActiveEvents.Count; i++)
                if (ActiveEvents[i] != null && !ActiveEvents[i].isCompleted && ActiveEvents[i].kind == storyKind)
                    return false;
            if (Random.value > 0.28f) return false;

            float eventX = startX + 35f;
            GameObject eventObject = new GameObject($"HistoriaOurico_{pm.Data.hedgehogStoryStage + 1}_{seg.index}");
            eventObject.transform.SetParent(seg.root.transform, false);
            eventObject.transform.position = new Vector3(eventX, -2.42f, 0f);
            TrailAdventureEvent adventure = eventObject.AddComponent<TrailAdventureEvent>();
            adventure.Setup(storyKind);
            seg.events.Add(adventure);
            ActiveEvents.Add(adventure);
            blocked.Add((eventX - 3.5f, eventX + 3.5f));
            return true;
        }

        private TrailAdventureEvent.EventKind ChooseWeightedAdventureEvent(TrailAdventureEvent.EventKind[] candidates)
        {
            // A raridade é escolhida primeiro; a lista do bioma limita o resultado ao contexto local.
            float roll = Random.value;
            int targetRarity = roll < 0.62f ? 0 : (roll < 0.91f ? 1 : 2);
            List<TrailAdventureEvent.EventKind> pool = new List<TrailAdventureEvent.EventKind>();
            for (int pass = 0; pass < 3 && pool.Count == 0; pass++)
            {
                int rarity = (targetRarity + pass) % 3;
                for (int i = 0; i < candidates.Length; i++)
                    if (GetAdventureRarity(candidates[i]) == rarity && !recentAdventureEvents.Contains(candidates[i]))
                        pool.Add(candidates[i]);
                if (pool.Count == 0)
                {
                    for (int i = 0; i < candidates.Length; i++)
                        if (GetAdventureRarity(candidates[i]) == rarity) pool.Add(candidates[i]);
                }
            }

            TrailAdventureEvent.EventKind selected = pool.Count > 0
                ? pool[Random.Range(0, pool.Count)] : candidates[Random.Range(0, candidates.Length)];
            recentAdventureEvents.Enqueue(selected);
            while (recentAdventureEvents.Count > 3) recentAdventureEvents.Dequeue();
            return selected;
        }

        private static int GetAdventureRarity(TrailAdventureEvent.EventKind kind)
        {
            if (kind == TrailAdventureEvent.EventKind.TesouroEscondido
                || kind == TrailAdventureEvent.EventKind.EnigmaCoruja
                || kind == TrailAdventureEvent.EventKind.PonteCastorPronta) return 2;
            if (kind == TrailAdventureEvent.EventKind.PegadasCachorro
                || kind == TrailAdventureEvent.EventKind.ToupeiraSubterranea
                || kind == TrailAdventureEvent.EventKind.PonteQuebrada
                || kind == TrailAdventureEvent.EventKind.LimparRiacho) return 1;
            return 0;
        }

        private void SpawnDistanceSign(Transform parent, float xPos, float kmValue)
        {
            GameObject signObj = new GameObject($"PlacaKm_{kmValue:F1}");
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = new Vector3(xPos, -2.40f, 0f);

            SpriteRenderer sr = signObj.AddComponent<SpriteRenderer>();
            sr.sprite = BiomePropsFactory.CreateDistanceSignSprite();
            sr.sortingOrder = 3;
            BiomePropsFactory.AddContactShadow(signObj.transform, new Vector3(0f, 0.01f, 0f), new Vector2(0.85f, 0.18f), 2);

            GameObject txtObj = new GameObject("KmLabel");
            txtObj.transform.SetParent(signObj.transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.24f, -0.05f);
            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(2.2f, 0.8f);
            tmp.text = $"{kmValue:0.0} km";
            tmp.fontSize = 3.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.23f, 0.11f, 0.06f, 1f);
            tmp.sortingOrder = 4;
        }

        private static string GetAnimalName(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo:       return "Esquilo";
                case ParkSpriteFactory.AnimalKind.Coelho:        return "Coelho";
                case ParkSpriteFactory.AnimalKind.Gato:          return "Gato";
                case ParkSpriteFactory.AnimalKind.Lobo:          return "Lobo";
                case ParkSpriteFactory.AnimalKind.Quati:         return "Quati";
                case ParkSpriteFactory.AnimalKind.Raposa:        return "Raposa";
                case ParkSpriteFactory.AnimalKind.Cervo:         return "Cervo";
                case ParkSpriteFactory.AnimalKind.Ourico:        return "Ouriço";
                case ParkSpriteFactory.AnimalKind.Guaxinim:      return "Guaxinim";
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:   return "Filhote de Urso";
                case ParkSpriteFactory.AnimalKind.Passarinhos:   return "Passarinhos";
                case ParkSpriteFactory.AnimalKind.Castor:        return "Castor";
                case ParkSpriteFactory.AnimalKind.Sapo:          return "Sapo";
                case ParkSpriteFactory.AnimalKind.CorujaNoturna: return "Coruja";
                default:                                         return "Bichinho";
            }
        }

        private static int GetAnimalMeals(ParkSpriteFactory.AnimalKind kind)
        {
            switch (kind)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo:       return 6;
                case ParkSpriteFactory.AnimalKind.Coelho:        return 2;
                case ParkSpriteFactory.AnimalKind.Gato:          return 2;
                case ParkSpriteFactory.AnimalKind.Lobo:          return 4;
                case ParkSpriteFactory.AnimalKind.Quati:         return 4;
                case ParkSpriteFactory.AnimalKind.Raposa:        return 3;
                case ParkSpriteFactory.AnimalKind.Cervo:         return 5;
                case ParkSpriteFactory.AnimalKind.Ourico:        return 2;
                case ParkSpriteFactory.AnimalKind.Guaxinim:      return 3;
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:   return 4;
                case ParkSpriteFactory.AnimalKind.Passarinhos:   return 2;
                case ParkSpriteFactory.AnimalKind.Castor:        return 3;
                case ParkSpriteFactory.AnimalKind.Sapo:          return 2;
                case ParkSpriteFactory.AnimalKind.CorujaNoturna: return 3;
                default:                                         return 3;
            }
        }
    }
}
