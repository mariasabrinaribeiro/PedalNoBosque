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
        }

        private const float SegmentLength = 48f;
        private const float SpawnAheadDistance = 130f;
        private const float DespawnBehindDistance = 95f;
        private const float MinObstacleSpacing = 11f;
        private const float DistanceUnitsPerMeter = 1f / 2.5f; // 1 unidade = 2.5m => 200 unidades = 500m (0.5 km)
        private const float MilestoneStepUnits = 200f;         // Marco visual a cada 0.5 km

        public readonly List<ForestAnimal> ActiveAnimals = new List<ForestAnimal>();
        public readonly List<VillageResident> ActiveResidents = new List<VillageResident>();
        public float MinSafeBacktrackX { get; private set; } = -4f;
        public ForestAnimal PrimarySquirrel { get; private set; }

        private readonly List<TrackSegment> activeSegments = new List<TrackSegment>();
        private readonly Dictionary<int, GameObject> bgTiles = new Dictionary<int, GameObject>();
        private readonly List<BiomeType> biomeBag = new List<BiomeType>();

        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite happySp;
        private Sprite crySp;
        private Sprite tearSp;
        private Sprite[] heartParticles;
        private Sprite bgSprite;
        private float bgTileWidth = 20f;

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
            if (bgSprite != null) bgTileWidth = Mathf.Max(8f, bgSprite.bounds.size.x);

            // Gera o fundo inicial e os primeiros 3 segmentos à frente
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

        private const float BgParallaxFactor = 0.25f; // Plano 1 (Fundo Distante): montanhas se movem a 25% da pista!

        private void UpdateBackgroundTiles(float cameraX)
        {
            if (bgSprite == null) return;

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

            // Flores acolhedoras no Campo Florido inicial
            for (int i = 0; i < 6; i++)
                SpawnProp(p, BiomePropsFactory.CreateFlowerPatchSprite(i), new Vector3(s + 5f + i * 7.5f, -2.75f, 0f), 1.05f, 2);

            SpawnProp(p, BiomePropsFactory.LoadParkBenchSprite(), new Vector3(s + 13f, -2.55f, 0f), 1f, 3);
            SpawnKitchenStation(p, s + 8f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 6, s + 18f, blocked);
            SpawnWaterColumn(p, s + 24f, blocked);
            SpawnRampWithStars(p, s + 31f, blocked);
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Coelho, "Coelho", 2, s + 43f, blocked);

            hasWater = true;
            hasKitchen = true;
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

            for (int i = 0; i < 7; i++)
                SpawnProp(p, BiomePropsFactory.CreateFlowerPatchSprite(i + seg.index), new Vector3(s + 4f + i * 6.5f, -2.78f, 0f), Random.Range(0.95f, 1.2f), 2);

            SpawnProp(p, BiomePropsFactory.LoadParkBenchSprite(), new Vector3(s + 12f, -2.55f, 0f), 1f, 3);

            // Animais amigáveis do campo (Coelho, Esquilo, Passarinhos e Ouriço)
            ParkSpriteFactory.AnimalKind[] campoPool =
            {
                ParkSpriteFactory.AnimalKind.Coelho,
                ParkSpriteFactory.AnimalKind.Passarinhos,
                ParkSpriteFactory.AnimalKind.Ourico,
                ParkSpriteFactory.AnimalKind.Esquilo
            };
            ParkSpriteFactory.AnimalKind a1 = campoPool[seg.index % campoPool.Length];
            ParkSpriteFactory.AnimalKind a2 = campoPool[(seg.index + 2) % campoPool.Length];
            SpawnAnimal(seg, a1, GetAnimalName(a1), GetAnimalMeals(a1), s + 15f, blocked);
            SpawnAnimal(seg, a2, GetAnimalName(a2), GetAnimalMeals(a2), s + 39f, blocked);

            // Rampa suave no meio do campo
            SpawnRampWithStars(p, s + 23f, blocked);

            // 1 obstáculo leve em local seguro + morador eventual fazendo piquenique ou viajando!
            if (seg.index % 2 == 1)
                SpawnResident(seg, VillageResident.ResidentKind.FamiliaPiquenique, s + 44f, blocked);
            else
                TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Pedra, s + 45f, blocked, obstacles);
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

            // Colinas verdes ao fundo
            SpawnProp(p, BiomePropsFactory.CreateRollingHillSprite(), new Vector3(s + 12f, -2.3f, 0f), 1.35f, -5);
            SpawnProp(p, BiomePropsFactory.CreateRollingHillSprite(), new Vector3(s + 34f, -2.3f, 0f), 1.55f, -5);

            // Subida longa seguida de descida embalada + rampa de salto!
            CreateSlope(p, SlopeZone.SlopeType.Subida, s + 10f, 10f);
            CreateSlope(p, SlopeZone.SlopeType.Descida, s + 26f, 10f);

            SpawnRampWithStars(p, s + 33f, blocked);

            ParkSpriteFactory.AnimalKind[] colinasPool =
            {
                ParkSpriteFactory.AnimalKind.Cervo,
                ParkSpriteFactory.AnimalKind.Raposa,
                ParkSpriteFactory.AnimalKind.FilhoteUrso,
                ParkSpriteFactory.AnimalKind.Passarinhos
            };
            ParkSpriteFactory.AnimalKind a1 = colinasPool[seg.index % colinasPool.Length];
            SpawnAnimal(seg, a1, GetAnimalName(a1), GetAnimalMeals(a1), s + 19f, blocked);

            // Água opcional no pé da colina
            if (segmentsSinceLastWater >= 1)
            {
                SpawnWaterColumn(p, s + 44f, blocked);
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

            // Conjunto integrado: Rio com correnteza + Margens com pedras/juncos + Ponte de Madeira proporcional + Peixes!
            float bridgeCenterX = s + 22f;
            GameObject riverGroup = new GameObject($"RioPonteIntegrada_{seg.index}");
            riverGroup.transform.SetParent(p, false);
            riverGroup.transform.position = new Vector3(bridgeCenterX, -3.55f, 0f);

            // Água azul-turquesa abaixo da pista
            GameObject waterObj = new GameObject("AguaRio");
            waterObj.transform.SetParent(riverGroup.transform, false);
            waterObj.transform.localPosition = new Vector3(0f, -0.82f, 0f);
            SpriteRenderer waterSR = waterObj.AddComponent<SpriteRenderer>();
            waterSR.sprite = BiomePropsFactory.CreateRiverWaterSprite();
            waterSR.sortingOrder = 1;

            // Camada de correnteza cintilante animada
            GameObject shimmerObj = new GameObject("CorrentezaBrilho");
            shimmerObj.transform.SetParent(riverGroup.transform, false);
            shimmerObj.transform.localPosition = new Vector3(0f, -0.78f, 0f);
            SpriteRenderer shimmerSR = shimmerObj.AddComponent<SpriteRenderer>();
            shimmerSR.sprite = BiomePropsFactory.CreateRiverShimmerSprite();
            shimmerSR.sortingOrder = 2;

            // Ponte de madeira proporcional, integrada ao nível da trilha (sem tampar a paisagem!)
            GameObject bridgeObj = new GameObject("PonteMadeira");
            bridgeObj.transform.SetParent(riverGroup.transform, false);
            bridgeObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            SpriteRenderer bridgeSR = bridgeObj.AddComponent<SpriteRenderer>();
            bridgeSR.sprite = BiomePropsFactory.CreateWoodenBridgeSprite();
            bridgeSR.sortingOrder = 4;

            // Salgueiro ribeirinho com passarinho pousado no galho + pequena ilha no rio!
            SpawnSwayingProp(p, BiomePropsFactory.CreateWillowTreeWithBirdSprite(), new Vector3(bridgeCenterX - 7.8f, -2.65f, 0f), 1.05f, -2, 1.5f);
            SpawnProp(p, BiomePropsFactory.CreateRiverIsletSprite(), new Vector3(bridgeCenterX + 3.2f, -4.45f, 0f), 0.92f, 3);

            // Margens naturais com pedras, juncos aquáticos e flores nas cabeceiras da ponte
            SpawnSwayingProp(p, BiomePropsFactory.CreateRiverBankRocksAndReedsSprite(), new Vector3(bridgeCenterX - 4.8f, -3.95f, 0f), 1.0f, 5, 2.5f);
            SpawnSwayingProp(p, BiomePropsFactory.CreateRiverBankRocksAndReedsSprite(), new Vector3(bridgeCenterX + 4.8f, -3.95f, 0f), 1.0f, 5, 2.5f);

            // Vida na água (peixes em pool saltando + correnteza + balanço leve da madeira)
            riverGroup.AddComponent<LivingRiverFeature>().Setup(shimmerObj.transform, bridgeObj.transform);

            // Bloqueia obstáculos sobre a ponte e nas cabeceiras (segurança)
            blocked.Add((bridgeCenterX - 8.5f, bridgeCenterX + 8.5f));

            // Flores delicadas balançando ao vento nas margens
            SpawnSwayingProp(p, BiomePropsFactory.CreateFlowerPatchSprite(1), new Vector3(s + 11f, -2.75f, 0f), 0.92f, 2, 3.6f);
            SpawnSwayingProp(p, BiomePropsFactory.CreateFlowerPatchSprite(2), new Vector3(s + 33f, -2.75f, 0f), 0.92f, 2, 3.6f);

            // Coluna de água garantida na Região do Rio!
            SpawnWaterColumn(p, s + 9f, blocked);
            hasWater = true;

            // Animais típicos da Região do Rio: Castor, Sapo, Quati e Cervo
            ParkSpriteFactory.AnimalKind[] rioPool =
            {
                ParkSpriteFactory.AnimalKind.Castor,
                ParkSpriteFactory.AnimalKind.Sapo,
                ParkSpriteFactory.AnimalKind.Quati,
                ParkSpriteFactory.AnimalKind.Cervo
            };
            ParkSpriteFactory.AnimalKind a1 = rioPool[seg.index % rioPool.Length];
            ParkSpriteFactory.AnimalKind a2 = rioPool[(seg.index + 1) % rioPool.Length];
            SpawnAnimal(seg, a1, GetAnimalName(a1), GetAnimalMeals(a1), s + 13f, blocked);
            SpawnAnimal(seg, a2, GetAnimalName(a2), GetAnimalMeals(a2), s + 38f, blocked);

            // Estrelas flutuando acima da ponte para premiar pulo sobre a ponte
            for (int i = 0; i < 3; i++)
            {
                GameObject stObj = new GameObject($"EstrelaPonte_{seg.index}_{i}");
                stObj.transform.SetParent(p, false);
                stObj.transform.position = new Vector3(bridgeCenterX - 2.6f + i * 2.6f, -1.25f + (i == 1 ? 0.45f : 0f), 0f);
                stObj.AddComponent<AerialCollectible>().Setup(50);
            }
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

            // Arbustos densos de floresta
            for (int i = 0; i < 5; i++)
                SpawnProp(p, BiomePropsFactory.CreateForestBushSprite(), new Vector3(s + 5f + i * 9.5f, -2.65f, 0f), Random.Range(0.95f, 1.25f), 2);

            // Animais da mata fechada: Lobo, Filhote de Urso, Guaxinim, Ouriço, Coruja Noturna e Esquilo
            ParkSpriteFactory.AnimalKind[] mataPool =
            {
                ParkSpriteFactory.AnimalKind.Lobo,
                ParkSpriteFactory.AnimalKind.FilhoteUrso,
                ParkSpriteFactory.AnimalKind.Guaxinim,
                ParkSpriteFactory.AnimalKind.CorujaNoturna,
                ParkSpriteFactory.AnimalKind.Ourico
            };
            ParkSpriteFactory.AnimalKind a1 = mataPool[seg.index % mataPool.Length];
            ParkSpriteFactory.AnimalKind a2 = (seg.index % 2 == 0) ? ParkSpriteFactory.AnimalKind.Esquilo : mataPool[(seg.index + 2) % mataPool.Length];
            SpawnAnimal(seg, a1, GetAnimalName(a1), GetAnimalMeals(a1), s + 18f, blocked);
            SpawnAnimal(seg, a2, GetAnimalName(a2), GetAnimalMeals(a2), s + 41f, blocked);

            // Obstáculos naturais bem espaçados com espaço seguro para saltar
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Raiz, s + 8f, blocked, obstacles);
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Tronco, s + 28f, blocked, obstacles);
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Pedra, s + 46f, blocked, obstacles);
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

            // Casinhas charmosas da vila ao fundo
            SpawnProp(p, BiomePropsFactory.CreateVillageCottageSprite(seg.index), new Vector3(s + 11f, -2.50f, 0f), 1.1f, 1);
            SpawnProp(p, BiomePropsFactory.CreateVillageCottageSprite(seg.index + 1), new Vector3(s + 29f, -2.50f, 0f), 1.05f, 1);
            SpawnProp(p, BiomePropsFactory.CreateVillageCottageSprite(seg.index + 2), new Vector3(s + 42f, -2.50f, 0f), 1.0f, 1);

            // Postes com lanterna, varal de bandeirinhas festivas e bancos de praça
            SpawnProp(p, BiomePropsFactory.CreateVillageLampSprite(), new Vector3(s + 6f, -2.55f, 0f), 0.95f, 3);
            SpawnProp(p, BiomePropsFactory.CreateVillageBuntingSprite(), new Vector3(s + 16f, 0.35f, 0f), 1.15f, 4);
            SpawnProp(p, BiomePropsFactory.CreateVillageBuntingSprite(), new Vector3(s + 32f, 0.35f, 0f), 1.15f, 4);
            SpawnProp(p, BiomePropsFactory.LoadParkBenchSprite(), new Vector3(s + 23f, -2.55f, 0f), 1f, 3);
            SpawnProp(p, BiomePropsFactory.CreateVillageLampSprite(), new Vector3(s + 38f, -2.55f, 0f), 0.95f, 3);

            // Barraca da Cozinha garantida na Vila dos Viajantes!
            SpawnKitchenStation(p, s + 14f, blocked);
            hasKitchen = true;

            // Bebedouro na praça da vila
            if (segmentsSinceLastWater >= 1)
            {
                SpawnWaterColumn(p, s + 31f, blocked);
                hasWater = true;
            }

            // Moradores da Vila com Missões Aleatórias (Etapa 5)
            VillageResident.ResidentKind[] residentPool =
            {
                VillageResident.ResidentKind.Crianca,
                VillageResident.ResidentKind.SenhorBanco,
                VillageResident.ResidentKind.FamiliaPiquenique,
                VillageResident.ResidentKind.ViajanteSede,
                VillageResident.ResidentKind.Musico
            };
            VillageResident.ResidentKind r1 = residentPool[seg.index % residentPool.Length];
            VillageResident.ResidentKind r2 = residentPool[(seg.index + 2) % residentPool.Length];
            SpawnResident(seg, r1, s + 22f, blocked);
            SpawnResident(seg, r2, s + 36f, blocked);

            // Gato companheiro da vila
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.Gato, "Gato", 2, s + 43f, blocked);
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

            // Flores luminosas e cogumelos encantados azul-violeta sob o luar!
            for (int i = 0; i < 5; i++)
            {
                if (i % 2 == 0)
                    SpawnSwayingProp(p, BiomePropsFactory.CreateMoonFlowerSprite(), new Vector3(s + 5f + i * 9f, -2.72f, 0f), Random.Range(0.95f, 1.18f), 2, 2.8f);
                else
                    SpawnProp(p, BiomePropsFactory.CreateGlowingMushroomSprite(), new Vector3(s + 6f + i * 9f, -2.70f, 0f), Random.Range(0.92f, 1.15f), 2);
            }

            // Coruja Noturna garantida no Bosque da Lua + encontro raro (Raposa / Cervo / Filhote de Urso)
            SpawnAnimal(seg, ParkSpriteFactory.AnimalKind.CorujaNoturna, "Coruja", 3, s + 17f, blocked);
            ParkSpriteFactory.AnimalKind nightFriend = (seg.index % 3 == 0)
                ? ParkSpriteFactory.AnimalKind.Cervo
                : ((seg.index % 3 == 1) ? ParkSpriteFactory.AnimalKind.Raposa : ParkSpriteFactory.AnimalKind.FilhoteUrso);
            SpawnAnimal(seg, nightFriend, GetAnimalName(nightFriend), GetAnimalMeals(nightFriend), s + 39f, blocked);

            // Rampa iluminada com estrelas bônus
            SpawnRampWithStars(p, s + 25f, blocked);

            // Obstáculo natural seguro no final do segmento
            TrySpawnSafeObstacle(p, TrailObstacle.ObstacleKind.Raiz, s + 45f, blocked, obstacles);
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
            obj.transform.position = new Vector3(xCenter, -3.15f, 0f);
            float angle = type == SlopeZone.SlopeType.Subida ? -72f : -108f;
            obj.AddComponent<SlopeZone>().Setup(type, new Vector2(length, 2.2f), angle);
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
            kObj.transform.position = new Vector3(kx, -2.55f, 0f);
            kObj.AddComponent<KitchenStation>().Setup();
            blocked.Add((kx - 5f, kx + 5f));
        }

        private void SpawnWaterColumn(Transform parent, float wx, List<(float min, float max)> blocked)
        {
            GameObject wObj = new GameObject($"ColunaAgua_{wx:F0}");
            wObj.transform.SetParent(parent, false);
            wObj.transform.position = new Vector3(wx, -2.55f, 0f);
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
            GameObject obj = new GameObject($"Animal_{displayName}_{seg.index}");
            obj.transform.SetParent(seg.root.transform, false);
            obj.transform.position = new Vector3(xPos, -2.55f, 0f);

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
            obj.transform.position = new Vector3(xPos, -2.55f, 0f);

            VillageResident r = obj.AddComponent<VillageResident>();
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

        private void SpawnDistanceSign(Transform parent, float xPos, float kmValue)
        {
            GameObject signObj = new GameObject($"PlacaKm_{kmValue:F1}");
            signObj.transform.SetParent(parent, false);
            signObj.transform.position = new Vector3(xPos, -2.55f, 0f);

            SpriteRenderer sr = signObj.AddComponent<SpriteRenderer>();
            sr.sprite = BiomePropsFactory.CreateDistanceSignSprite();
            sr.sortingOrder = 4;

            GameObject txtObj = new GameObject("KmLabel");
            txtObj.transform.SetParent(signObj.transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 1.62f, -0.05f);
            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(2.4f, 0.9f);
            tmp.text = $"{kmValue:0.0} km";
            tmp.fontSize = 3.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.22f, 0.12f, 0.04f, 1f);
            tmp.sortingOrder = 5;
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
