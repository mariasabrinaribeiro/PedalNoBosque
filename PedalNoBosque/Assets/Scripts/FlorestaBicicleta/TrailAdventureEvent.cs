using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Eventos e Pequenas Missões Contextuais na Trilha (PEDAL NO BOSQUE):
    /// Transforma o passeio em uma aventura variada além de entregar comida:
    /// - FloresMurchas: regar plantas para vê-las florescer com brilho;
    /// - PassaroCaido: ajudar um passarinho filhote a voltar ao ninho;
    /// - PonteQuebrada: encaixar tábuas de madeira para consertar a passarela;
    /// - TesouroEscondido: o cachorrinho fareja e late ao detectar o segredo escondido!
    /// </summary>
    public class TrailAdventureEvent : MonoBehaviour
    {
        public enum EventKind
        {
            FloresMurchas,
            PassaroCaido,
            PonteQuebrada,
            TesouroEscondido,
            // Novos personagens e atividades da floresta viva!
            ToupeiraSubterranea,
            TexugoOficina,
            RatinhoCarteiro,
            PicapauArvore,
            LimparRiacho,
            PegadasCachorro,
            MusicaDaFloresta,
            CuidadoraAbrigo,
            EnigmaCoruja,
            PonteCastorPronta,
            OuricoProcurandoAbrigo,
            OuricoTocaBloqueada,
            OuricoReconstruirAbrigo,
            OuricoCelebraComAnimais
        }

        public bool AllowsOnFootExploration => kind == EventKind.TesouroEscondido
            || kind == EventKind.PegadasCachorro
            || kind == EventKind.ToupeiraSubterranea
            || kind == EventKind.PonteQuebrada
            || kind == EventKind.PonteCastorPronta
            || kind == EventKind.OuricoProcurandoAbrigo
            || kind == EventKind.OuricoTocaBloqueada
            || kind == EventKind.OuricoReconstruirAbrigo
            || kind == EventKind.OuricoCelebraComAnimais
            || kind == EventKind.EnigmaCoruja;

        public EventKind kind { get; private set; }
        public bool isCompleted { get; private set; } = false;
        public bool puppyAlerted { get; private set; } = false;

        public string ActionButtonLabel
        {
            get
            {
                switch (kind)
                {
                    case EventKind.FloresMurchas:       return "Regar Flores (Espaco)";
                    case EventKind.PassaroCaido:        return "Ajudar Passarinho (Espaco)";
                    case EventKind.PonteQuebrada:       return "Ajudar o castor na ponte (Espaço)";
                    case EventKind.TesouroEscondido:    return "Abrir Tesouro (Espaco)";
                    case EventKind.ToupeiraSubterranea: return "Ajudar Toupeira (Espaco)";
                    case EventKind.TexugoOficina:       return "Oficina do Texugo (Espaco)";
                    case EventKind.RatinhoCarteiro:     return "Entregar Carta (Espaco)";
                    case EventKind.PicapauArvore:       return "Seguir Pica-pau (Espaco)";
                    case EventKind.LimparRiacho:        return "Limpar Riacho (Espaco)";
                    case EventKind.PegadasCachorro:     return "Seguir Pegadas (Espaco)";
                    case EventKind.MusicaDaFloresta:    return "Tocar Musica (Espaco)";
                    case EventKind.CuidadoraAbrigo:     return "Cuidar do Bichinho (Espaco)";
                    case EventKind.EnigmaCoruja:        return "Ouvir Coruja (Espaco)";
                    case EventKind.PonteCastorPronta:   return "Explorar ponte do castor (Espaco)";
                    case EventKind.OuricoProcurandoAbrigo: return "Ajudar o ouriço (Espaço)";
                    case EventKind.OuricoTocaBloqueada: return "Investigar a toca (Espaço)";
                    case EventKind.OuricoReconstruirAbrigo: return "Liberar o abrigo (Espaço)";
                    case EventKind.OuricoCelebraComAnimais: return "Participar do reencontro (Espaço)";
                    default: return "Interagir (Espaco)";
                }
            }
        }

        public string NearbyPrompt
        {
            get
            {
                switch (kind)
                {
                    case EventKind.FloresMurchas:       return "Flores murchas na beira da trilha! Aperte ESPACO para regar!";
                    case EventKind.PassaroCaido:        return "Um passarinho caiu do ninho! Aperte ESPACO para ajuda-lo!";
                    case EventKind.PonteQuebrada:       return "O castor precisa de ajuda com as tábuas da ponte!";
                    case EventKind.TesouroEscondido:    return "Algo brilha escondido nos arbustos! Aperte ESPACO para investigar!";
                    case EventKind.ToupeiraSubterranea: return "A Toupeira encontrou algo subterraneo! Aperte ESPACO para ajudar!";
                    case EventKind.TexugoOficina:       return "O Texugo esta na pequena oficina! Aperte ESPACO para revisar a bike!";
                    case EventKind.RatinhoCarteiro:     return "O Ratinho Carteiro tem uma carta da vila! Aperte ESPACO para ajudar!";
                    case EventKind.PicapauArvore:       return "O Pica-pau aponta uma arvore especial! Aperte ESPACO para descobrir!";
                    case EventKind.LimparRiacho:        return "Galhos cairam no riacho! Aperte ESPACO para limpar e trazer peixes!";
                    case EventKind.PegadasCachorro:     return "Pegadas misteriosas na trilha! O cachorrinho quer cavar (ESPACO)!";
                    case EventKind.MusicaDaFloresta:    return "Instrumentos naturais da floresta! Aperte ESPACO para tocar musica!";
                    case EventKind.CuidadoraAbrigo:     return "Um bichinho precisa de escovacao e abrigo! Aperte ESPACO para cuidar!";
                    case EventKind.EnigmaCoruja:        return "A Coruja Sabia quer contar um enigma! Aperte ESPACO para ouvir!";
                    case EventKind.PonteCastorPronta:   return "O castor terminou a ponte! Desça para ver a construção.";
                    case EventKind.OuricoProcurandoAbrigo: return "Um ouriço procura um cantinho seguro entre as folhas.";
                    case EventKind.OuricoTocaBloqueada: return "Você encontrou o ouriço outra vez. Galhos bloqueiam a toca.";
                    case EventKind.OuricoReconstruirAbrigo: return "Ajude a retirar os galhos e arrumar o abrigo do ouriço.";
                    case EventKind.OuricoCelebraComAnimais: return "O ouriço voltou com amigos para mostrar o abrigo pronto!";
                    default: return "";
                }
            }
        }

        private SpriteRenderer mainSR;
        private SpriteRenderer sparkleSR;
        private Vector3 basePos;
        private float bobPhase;

        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public void Setup(EventKind eventKind)
        {
            kind = eventKind;
            basePos = transform.position;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            mainSR = gameObject.AddComponent<SpriteRenderer>();
            mainSR.sprite = GetEventSprite(kind, completed: false);
            mainSR.sortingOrder = 5;

            // Brilho discreto convidativo para a criança notar o ponto de interesse
            GameObject spObj = new GameObject("BrilhoEvento");
            spObj.transform.SetParent(transform, false);
            spObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            sparkleSR = spObj.AddComponent<SpriteRenderer>();
            sparkleSR.sprite = StorybookUISpriteFactory.GetIcon(StorybookUISpriteFactory.UIIconKind.Star);
            sparkleSR.sortingOrder = 12;
            spObj.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        }

        private void Update()
        {
            if (sparkleSR != null && !isCompleted)
            {
                float bob = Mathf.Sin(Time.time * 3.4f + bobPhase) * 0.14f;
                sparkleSR.transform.localPosition = new Vector3(0f, 1.55f + bob, 0f);
                float a = 0.55f + 0.40f * Mathf.Abs(Mathf.Sin(Time.time * 2.5f + bobPhase));
                sparkleSR.color = new Color(1f, 0.96f, 0.65f, a);
            }
        }

        public bool CheckPuppyAlert(float distanceToPlayer)
        {
            if (isCompleted || puppyAlerted) return false;
            bool isSecretSniff = (kind == EventKind.TesouroEscondido || kind == EventKind.PegadasCachorro || kind == EventKind.ToupeiraSubterranea);
            if (isSecretSniff && distanceToPlayer < 9.5f)
            {
                puppyAlerted = true;
                FlorestaSoundFX.Instance?.PlayDogBark();
                return true;
            }
            return false;
        }

        public bool TryInteract(BikeGirlController player, out string completionMessage)
        {
            completionMessage = "";
            if (isCompleted || player == null) return false;

            // Atividades interativas ricas com UI dedicada (Música, Cuidadora, Enigma da Coruja)
            if (kind == EventKind.MusicaDaFloresta && ForestActivitiesUI.Instance != null)
            {
                ForestActivitiesUI.Instance.OpenMusicActivity(() =>
                    CompleteEventNow(player, 3, 180, "Melodia linda! Os passaros e flores celebraram sua musica! +3 Estrelas!"));
                completionMessage = "Escute os instrumentos naturais da floresta!";
                return true;
            }
            if (kind == EventKind.CuidadoraAbrigo && ForestActivitiesUI.Instance != null)
            {
                ForestActivitiesUI.Instance.OpenCareActivity(() =>
                    CompleteEventNow(player, 3, 170, "Voce escovou, montou o abrigo e deu carinho! +3 Estrelas!"));
                completionMessage = "Hora de cuidar do bichinho com carinho!";
                return true;
            }
            if (kind == EventKind.EnigmaCoruja && ForestActivitiesUI.Instance != null)
            {
                ForestActivitiesUI.Instance.OpenOwlRiddle(() =>
                    CompleteEventNow(player, 3, 175, "A Coruja Sabia aplaudiu sua resposta! +3 Estrelas!"));
                completionMessage = "A Coruja Sabia tem um enigma para voce!";
                return true;
            }

            int stars = 2;
            int pts = 140;

            if (kind == EventKind.PonteCastorPronta)
            {
                stars = 3; pts = 190;
                completionMessage = "O castor reconheceu sua ajuda e terminou a ponte! Agora os animais atravessam em segurança. +3 Estrelas!";
                CompleteEventNow(player, stars, pts, completionMessage);
                return true;
            }

            switch (kind)
            {
                case EventKind.FloresMurchas:
                    stars = 2; pts = 130;
                    FlorestaSoundFX.Instance?.PlayWater();
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.Flores, 1);
                    completionMessage = "Voce regou as flores e o jardim voltou a brilhar! +2 Estrelas e +130 pts!";
                    break;

                case EventKind.PassaroCaido:
                    stars = 2; pts = 150;
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(ParkSpriteFactory.AnimalKind.Passarinhos);
                    PlayerProgressManager.Instance?.RecordAnimalDiscovery(ParkSpriteFactory.AnimalKind.Passarinhos);
                    completionMessage = "O passarinho voltou feliz para o ninho e cantou para voce! +2 Estrelas e +150 pts!";
                    break;

                case EventKind.PonteQuebrada:
                    stars = 3; pts = 180;
                    FlorestaSoundFX.Instance?.PlayVillagerHappy();
                    completionMessage = "Você ajudou o castor a reconstruir a ponte! Mais tarde, passe por aqui para ver como ficou. +3 Estrelas e +180 pts!";
                    break;


                case EventKind.TesouroEscondido:
                    stars = 3; pts = 200;
                    FlorestaSoundFX.Instance?.PlayDiamond();
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.TesourosSecretos, 1);
                    completionMessage = "Tesouro Secreto descoberto com a ajuda do cachorrinho! +3 Estrelas e +200 pts!";
                    break;

                case EventKind.ToupeiraSubterranea:
                    stars = 2; pts = 160;
                    FlorestaSoundFX.Instance?.PlayDig();
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.PedrasBrilhantes, 1);
                    completionMessage = "A Toupeira encontrou uma Pedra Brilhante subterranea para voce! +2 Estrelas!";
                    break;

                case EventKind.TexugoOficina:
                    stars = 2; pts = 160;
                    FlorestaSoundFX.Instance?.PlayBell();
                    player.waterLevel = 100f;
                    completionMessage = "O Texugo revisou sua bicicleta e encheu a agua! +2 Estrelas!";
                    break;

                case EventKind.RatinhoCarteiro:
                    stars = 2; pts = 155;
                    FlorestaSoundFX.Instance?.PlayVillagerHappy();
                    completionMessage = "Voce ajudou o Ratinho Carteiro a entregar as cartas da vila! +2 Estrelas!";
                    break;

                case EventKind.PicapauArvore:
                    stars = 2; pts = 165;
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(ParkSpriteFactory.AnimalKind.Passarinhos);
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.Sementes, 2);
                    completionMessage = "O Pica-pau revelou sementes especiais na arvore antiga! +2 Estrelas!";
                    break;

                case EventKind.LimparRiacho:
                    stars = 3; pts = 180;
                    FlorestaSoundFX.Instance?.PlayWater();
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(ParkSpriteFactory.AnimalKind.Sapo);
                    completionMessage = "Riacho limpinho! Peixes e sapinhos voltaram felizes para a agua! +3 Estrelas!";
                    break;

                case EventKind.PegadasCachorro:
                    stars = 3; pts = 190;
                    FlorestaSoundFX.Instance?.PlayDig();
                    FlorestaSoundFX.Instance?.PlayDogBark();
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.TesourosSecretos, 1);
                    completionMessage = "O cachorrinho seguiu as pegadas e cavou um tesouro escondido! +3 Estrelas!";
                    break;

                case EventKind.OuricoProcurandoAbrigo:
                    stars = 2; pts = 150;
                    completionMessage = "Você ajudou o ouriço a encontrar um lugar protegido. Ele seguiu para sua toca.";
                    break;
                case EventKind.OuricoTocaBloqueada:
                    stars = 2; pts = 160;
                    completionMessage = "A toca estava bloqueada por galhos. O ouriço precisa de ajuda para desobstruí-la.";
                    break;
                case EventKind.OuricoReconstruirAbrigo:
                    stars = 3; pts = 190;
                    completionMessage = "Galhos retirados e abrigo reorganizado! O ouriço ficou em segurança.";
                    break;
                case EventKind.OuricoCelebraComAnimais:
                    stars = 3; pts = 210;
                    completionMessage = "O ouriço reuniu seus amigos para agradecer. A pequena história terminou feliz!";
                    break;
            }

            CompleteEventNow(player, stars, pts, completionMessage);
            return true;
        }

        private void CompleteEventNow(BikeGirlController player, int stars, int pts, string bannerMsg)
        {
            if (isCompleted) return;
            isCompleted = true;
            if (mainSR != null) mainSR.sprite = GetEventSprite(kind, completed: true);
            if (sparkleSR != null) sparkleSR.gameObject.SetActive(false);

            player.score += pts;
            PlayerProgressManager.Instance?.AddStars(stars);
            PlayerProgressManager.Instance?.RecordAdventureAction(kind);
            if (player.isOnFoot && AllowsOnFootExploration)
                PlayerProgressManager.Instance?.RecordSkillProgress(PlayerSkillKind.Exploracao);
            FlorestaSoundFX.Instance?.PlayDiamond();
            StartCoroutine(BloomPopRoutine());
        }

        private IEnumerator BloomPopRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.35f;
                float sc = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                transform.localScale = new Vector3(sc, sc, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        // ─── Sprites Ilustrados dos 4 Eventos (Antes e Depois!) ──────

        private static Texture2D Blank(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
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
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) tex.SetPixel(x, y, c);
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

        private static Sprite GetEventSprite(EventKind kind, bool completed)
        {
            string key = $"{kind}_{completed}";
            if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            // Para eventos protagonizados por animais que possuem arte 1024x1024 de estúdio, usa o sprite oficial!
            switch (kind)
            {
                case EventKind.PonteQuebrada:
                case EventKind.PonteCastorPronta:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Castor);
                case EventKind.EnigmaCoruja:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.CorujaNoturna);
                case EventKind.OuricoProcurandoAbrigo:
                case EventKind.OuricoTocaBloqueada:
                case EventKind.OuricoReconstruirAbrigo:
                case EventKind.OuricoCelebraComAnimais:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Ourico);
                case EventKind.PassaroCaido:
                case EventKind.PicapauArvore:
                    return EcosystemSpriteFactory.GetAnimalSprite(ParkSpriteFactory.AnimalKind.Passarinhos);
            }

            int w = 144, h = 120;
            Texture2D tex = Blank(w, h);
            Color ink = new Color(0.23f, 0.11f, 0.08f, 1f);
            Color shadow = new Color(0.06f, 0.14f, 0.08f, 0.32f);
            Color grassDark = new Color(0.24f, 0.48f, 0.16f, 1f);
            Color grassLight = new Color(0.46f, 0.72f, 0.24f, 1f);
            Color woodDark = new Color(0.48f, 0.26f, 0.13f, 1f);
            Color woodMid = new Color(0.72f, 0.44f, 0.21f, 1f);
            Color woodLight = new Color(0.88f, 0.62f, 0.34f, 1f);

            EllipsePx(tex, 72, 12, 48, 8, shadow);

            switch (kind)
            {
                case EventKind.FloresMurchas:
                    // Canteiro de madeira rústica com flores silvestres (antes/depois de regar)
                    RectPx(tex, 26, 10, 118, 28, ink);
                    RectPx(tex, 29, 13, 115, 25, woodMid);
                    RectPx(tex, 29, 21, 115, 25, woodLight);
                    if (!completed)
                    {
                        int[] fx = { 44, 72, 100 };
                        for (int i = 0; i < fx.Length; i++)
                        {
                            RectPx(tex, fx[i] - 2, 26, fx[i] + 2, 54, ink);
                            RectPx(tex, fx[i] - 1, 27, fx[i] + 1, 53, new Color(0.48f, 0.56f, 0.32f, 1f));
                            CirclePx(tex, fx[i] + 4, 52, 10, ink);
                            CirclePx(tex, fx[i] + 4, 52, 8, new Color(0.84f, 0.76f, 0.66f, 1f));
                            CirclePx(tex, fx[i] + 4, 52, 3, new Color(0.92f, 0.80f, 0.42f, 1f));
                        }
                    }
                    else
                    {
                        int[] fx = { 44, 72, 100 };
                        Color[] pCols =
                        {
                            new Color(1f, 0.46f, 0.68f, 1f),
                            new Color(1f, 0.86f, 0.24f, 1f),
                            new Color(0.56f, 0.78f, 1f, 1f)
                        };
                        for (int i = 0; i < fx.Length; i++)
                        {
                            RectPx(tex, fx[i] - 2, 26, fx[i] + 2, 68, ink);
                            RectPx(tex, fx[i] - 1, 27, fx[i] + 1, 67, grassLight);
                            EllipsePx(tex, fx[i] - 6, 44, 7, 4, ink);
                            EllipsePx(tex, fx[i] - 6, 44, 5, 3, grassLight);
                            for (int a = 0; a < 5; a++)
                            {
                                float ang = a * Mathf.PI * 2f / 5f;
                                int px = fx[i] + Mathf.RoundToInt(Mathf.Cos(ang) * 9f);
                                int py = 72 + Mathf.RoundToInt(Mathf.Sin(ang) * 9f);
                                CirclePx(tex, px, py, 8, ink);
                                CirclePx(tex, px, py, 6, pCols[i]);
                            }
                            CirclePx(tex, fx[i], 72, 6, ink);
                            CirclePx(tex, fx[i], 72, 4, new Color(1f, 0.92f, 0.30f, 1f));
                        }
                    }
                    break;

                case EventKind.TesouroEscondido:
                case EventKind.PegadasCachorro:
                    // Baú de madeira e ferragens douradas entre tufos de grama florida
                    EllipsePx(tex, 72, 20, 44, 14, ink);
                    EllipsePx(tex, 72, 20, 41, 11, grassDark);
                    EllipsePx(tex, 68, 22, 30, 8, grassLight);
                    RectPx(tex, 38, 18, 106, 56, ink);
                    RectPx(tex, 41, 21, 103, 53, woodMid);
                    RectPx(tex, 41, 36, 103, 40, ink);
                    RectPx(tex, 41, 37, 103, 39, new Color(0.98f, 0.80f, 0.24f, 1f));
                    CirclePx(tex, 72, 38, 6, ink);
                    CirclePx(tex, 72, 38, 4, new Color(1f, 0.88f, 0.30f, 1f));
                    if (completed)
                    {
                        CirclePx(tex, 60, 62, 10, ink);
                        CirclePx(tex, 60, 62, 8, new Color(0.46f, 0.92f, 1f, 1f));
                        CirclePx(tex, 84, 62, 10, ink);
                        CirclePx(tex, 84, 62, 8, new Color(1f, 0.88f, 0.26f, 1f));
                    }
                    break;

                case EventKind.ToupeiraSubterranea:
                    // Canteiro de terra fofa contornado + Toupeira ilustrada com capacete de exploradora e cristal
                    EllipsePx(tex, 72, 20, 44, 14, ink);
                    EllipsePx(tex, 72, 20, 41, 11, woodDark);
                    EllipsePx(tex, 72, 23, 34, 8, woodMid);
                    // Corpo da toupeira com contorno #3B1C14
                    EllipsePx(tex, 72, 42, 22, 24, ink);
                    EllipsePx(tex, 72, 42, 19, 21, new Color(0.38f, 0.32f, 0.30f, 1f));
                    EllipsePx(tex, 72, 36, 13, 12, new Color(0.86f, 0.76f, 0.68f, 1f));
                    CirclePx(tex, 65, 48, 3, ink);
                    CirclePx(tex, 79, 48, 3, ink);
                    CirclePx(tex, 72, 43, 5, ink);
                    CirclePx(tex, 72, 43, 3, new Color(1f, 0.62f, 0.72f, 1f));
                    // Capacete amarelo de mineiro/explorador
                    EllipsePx(tex, 72, 60, 20, 10, ink);
                    EllipsePx(tex, 72, 60, 17, 7, new Color(1f, 0.84f, 0.22f, 1f));
                    CirclePx(tex, 72, 62, 4, Color.white);
                    break;

                case EventKind.CuidadoraAbrigo:
                case EventKind.TexugoOficina:
                case EventKind.RatinhoCarteiro:
                    // Casinha/abrigo de madeira esculpido na beira da trilha com telhadinho e flores
                    EllipsePx(tex, 72, 18, 42, 12, ink);
                    EllipsePx(tex, 72, 18, 39, 9, grassDark);
                    RectPx(tex, 42, 18, 102, 64, ink);
                    RectPx(tex, 45, 21, 99, 61, woodLight);
                    CirclePx(tex, 72, 34, 15, ink);
                    CirclePx(tex, 72, 34, 12, woodDark);
                    // Telhadinho em duas águas
                    for (int y = 60; y <= 92; y++)
                    {
                        int halfW = Mathf.RoundToInt((1f - (y - 60f) / 32f) * 40f);
                        RectPx(tex, 72 - halfW - 3, y, 72 + halfW + 3, y, ink);
                        RectPx(tex, 72 - halfW, y, 72 + halfW, y, new Color(0.84f, 0.36f, 0.28f, 1f));
                    }
                    break;

                case EventKind.MusicaDaFloresta:
                case EventKind.LimparRiacho:
                default:
                    // Tronco musical / canteiro florido do bosque com contorno escuro
                    EllipsePx(tex, 72, 18, 44, 12, ink);
                    EllipsePx(tex, 72, 18, 41, 9, grassDark);
                    RectPx(tex, 36, 18, 108, 48, ink);
                    RectPx(tex, 39, 21, 105, 45, woodMid);
                    RectPx(tex, 39, 38, 105, 45, woodLight);
                    CirclePx(tex, 54, 56, 8, ink);
                    CirclePx(tex, 54, 56, 6, new Color(1f, 0.86f, 0.24f, 1f));
                    CirclePx(tex, 90, 56, 8, ink);
                    CirclePx(tex, 90, 56, 6, new Color(1f, 0.48f, 0.68f, 1f));
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 10f / (float)h), 84f);
            spriteCache[key] = sp;
            return sp;
        }
    }
}
