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

            int w = 96, h = 80;
            Texture2D tex = Blank(w, h);
            Color shadow = new Color(0.10f, 0.14f, 0.08f, 0.24f);
            RectPx(tex, 14, 2, 82, 8, shadow);

            switch (kind)
            {
                case EventKind.FloresMurchas:
                    if (!completed)
                    {
                        // Flores caidinhas e pálidas esperando água
                        RectPx(tex, 26, 8, 28, 28, new Color(0.46f, 0.50f, 0.30f, 1f));
                        RectPx(tex, 48, 8, 50, 32, new Color(0.46f, 0.50f, 0.30f, 1f));
                        RectPx(tex, 70, 8, 72, 26, new Color(0.46f, 0.50f, 0.30f, 1f));
                        CirclePx(tex, 32, 24, 6, new Color(0.76f, 0.68f, 0.58f, 1f));
                        CirclePx(tex, 54, 28, 7, new Color(0.76f, 0.68f, 0.58f, 1f));
                        CirclePx(tex, 74, 22, 6, new Color(0.76f, 0.68f, 0.58f, 1f));
                    }
                    else
                    {
                        // Jardim florido exuberante e colorido!
                        RectPx(tex, 26, 8, 29, 44, new Color(0.22f, 0.66f, 0.28f, 1f));
                        RectPx(tex, 48, 8, 51, 52, new Color(0.22f, 0.66f, 0.28f, 1f));
                        RectPx(tex, 70, 8, 73, 46, new Color(0.22f, 0.66f, 0.28f, 1f));
                        CirclePx(tex, 27, 48, 10, new Color(1f, 0.44f, 0.72f, 1f));
                        CirclePx(tex, 27, 48, 4, new Color(1f, 0.88f, 0.22f, 1f));
                        CirclePx(tex, 49, 56, 11, new Color(1f, 0.82f, 0.22f, 1f));
                        CirclePx(tex, 49, 56, 4, new Color(1f, 0.46f, 0.22f, 1f));
                        CirclePx(tex, 71, 50, 10, new Color(0.68f, 0.50f, 1f, 1f));
                        CirclePx(tex, 71, 50, 4, new Color(1f, 0.94f, 0.44f, 1f));
                    }
                    break;

                case EventKind.PassaroCaido:
                    // Tronco baixo com ninho de gravetos no topo
                    RectPx(tex, 54, 8, 70, 52, new Color(0.46f, 0.28f, 0.14f, 1f));
                    CirclePx(tex, 62, 54, 15, new Color(0.58f, 0.38f, 0.18f, 1f));
                    CirclePx(tex, 62, 58, 11, new Color(0.38f, 0.24f, 0.10f, 1f));
                    if (!completed)
                    {
                        // Passarinho azul no chão pedindo ajuda
                        CirclePx(tex, 28, 18, 9, new Color(0.30f, 0.74f, 0.98f, 1f));
                        CirclePx(tex, 34, 24, 5, new Color(0.30f, 0.74f, 0.98f, 1f));
                        CirclePx(tex, 38, 24, 2, new Color(1f, 0.76f, 0.16f, 1f));
                    }
                    else
                    {
                        // Passarinho feliz e seguro dentro do ninho!
                        CirclePx(tex, 62, 62, 9, new Color(0.30f, 0.74f, 0.98f, 1f));
                        CirclePx(tex, 68, 67, 5, new Color(0.30f, 0.74f, 0.98f, 1f));
                        CirclePx(tex, 72, 67, 2, new Color(1f, 0.76f, 0.16f, 1f));
                    }
                    break;

                case EventKind.PonteQuebrada:
                    Color wood = new Color(0.64f, 0.40f, 0.20f, 1f);
                    Color woodDark = new Color(0.42f, 0.24f, 0.12f, 1f);
                    RectPx(tex, 12, 8, 22, 34, woodDark);
                    RectPx(tex, 74, 8, 84, 34, woodDark);
                    if (!completed)
                    {
                        // Tábuas quebradas/faltando no meio
                        RectPx(tex, 10, 26, 36, 36, wood);
                        RectPx(tex, 62, 26, 86, 36, wood);
                        RectPx(tex, 38, 10, 56, 18, wood); // Tábua caída no chão
                    }
                    else
                    {
                        // Passarela inteira e firme com flores nas pontas!
                        RectPx(tex, 10, 26, 86, 37, wood);
                        RectPx(tex, 10, 35, 86, 38, new Color(0.82f, 0.60f, 0.34f, 1f));
                        CirclePx(tex, 16, 42, 5, new Color(1f, 0.84f, 0.24f, 1f));
                        CirclePx(tex, 80, 42, 5, new Color(1f, 0.48f, 0.72f, 1f));
                    }
                    break;

                case EventKind.PonteCastorPronta:
                    // A ponte concluída guarda um detalhe visual que distingue este reencontro.
                    Color finishedWood = new Color(0.64f, 0.40f, 0.20f, 1f);
                    RectPx(tex, 12, 8, 22, 34, new Color(0.42f, 0.24f, 0.12f, 1f));
                    RectPx(tex, 74, 8, 84, 34, new Color(0.42f, 0.24f, 0.12f, 1f));
                    RectPx(tex, 10, 26, 86, 37, finishedWood);
                    RectPx(tex, 10, 35, 86, 38, new Color(0.84f, 0.63f, 0.36f, 1f));
                    CirclePx(tex, 24, 48, 9, new Color(0.56f, 0.36f, 0.19f, 1f));
                    CirclePx(tex, 27, 52, 6, new Color(0.68f, 0.46f, 0.27f, 1f));
                    CirclePx(tex, 29, 54, 1, Color.white);
                    CirclePx(tex, 30, 54, 1, Color.black);
                    break;

                case EventKind.TesouroEscondido:
                    if (!completed)
                    {
                        // Baú/tesouro semi-escondido em arbusto com brilho sutil
                        CirclePx(tex, 48, 24, 22, new Color(0.18f, 0.50f, 0.24f, 1f));
                        RectPx(tex, 34, 12, 62, 30, new Color(0.62f, 0.36f, 0.16f, 1f));
                        RectPx(tex, 34, 22, 62, 26, new Color(0.98f, 0.82f, 0.22f, 1f));
                    }
                    else
                    {
                        // Baú aberto cheio de brilho dourado e cristais!
                        RectPx(tex, 30, 10, 66, 32, new Color(0.62f, 0.36f, 0.16f, 1f));
                        RectPx(tex, 30, 32, 66, 46, new Color(0.48f, 0.26f, 0.12f, 1f));
                        CirclePx(tex, 42, 34, 7, new Color(1f, 0.86f, 0.22f, 1f));
                        CirclePx(tex, 54, 35, 7, new Color(0.46f, 0.92f, 1f, 1f));
                        CirclePx(tex, 48, 42, 6, new Color(1f, 0.96f, 0.72f, 1f));
                    }
                    break;

                case EventKind.ToupeiraSubterranea:
                    // Monte de terra fofa + Toupeira simpática com focinho rosado
                    CirclePx(tex, 48, 18, 22, new Color(0.46f, 0.30f, 0.16f, 1f));
                    CirclePx(tex, 48, 36, 14, new Color(0.30f, 0.26f, 0.24f, 1f));
                    CirclePx(tex, 48, 34, 4, new Color(1f, 0.62f, 0.72f, 1f)); // Focinho rosado
                    if (completed) CirclePx(tex, 68, 38, 7, new Color(0.38f, 0.86f, 1f, 1f)); // Cristal achado
                    break;

                case EventKind.TexugoOficina:
                    // Bancada de madeira + Texugo listrado
                    RectPx(tex, 18, 8, 78, 28, new Color(0.62f, 0.40f, 0.20f, 1f));
                    CirclePx(tex, 48, 42, 16, new Color(0.28f, 0.28f, 0.30f, 1f));
                    RectPx(tex, 44, 30, 52, 56, Color.white); // Faixa branca clássica do texugo
                    if (completed) CirclePx(tex, 72, 46, 7, new Color(1f, 0.88f, 0.24f, 1f));
                    break;

                case EventKind.RatinhoCarteiro:
                    // Caixa de correio charmosa + Ratinho com orelhinhas redondas e carta
                    RectPx(tex, 58, 8, 66, 46, new Color(0.44f, 0.28f, 0.14f, 1f));
                    RectPx(tex, 50, 42, 76, 58, new Color(0.26f, 0.56f, 0.82f, 1f));
                    CirclePx(tex, 32, 24, 12, new Color(0.76f, 0.74f, 0.72f, 1f));
                    CirclePx(tex, 24, 34, 6, new Color(1f, 0.72f, 0.78f, 1f));
                    CirclePx(tex, 40, 34, 6, new Color(1f, 0.72f, 0.78f, 1f));
                    RectPx(tex, 34, 20, 48, 30, Color.white); // Cartinha!
                    break;

                case EventKind.PicapauArvore:
                    // Tronco especial + Pica-pau de topete vermelho
                    RectPx(tex, 46, 8, 68, 70, new Color(0.48f, 0.30f, 0.16f, 1f));
                    CirclePx(tex, 38, 46, 10, new Color(0.18f, 0.20f, 0.24f, 1f));
                    CirclePx(tex, 38, 56, 7, new Color(0.92f, 0.20f, 0.24f, 1f)); // Topete vermelho
                    if (completed) CirclePx(tex, 58, 48, 7, new Color(1f, 0.84f, 0.24f, 1f));
                    break;

                case EventKind.LimparRiacho:
                    CirclePx(tex, 48, 20, 26, new Color(0.24f, 0.70f, 0.92f, 0.92f));
                    if (!completed)
                    {
                        RectPx(tex, 24, 18, 72, 26, new Color(0.46f, 0.28f, 0.14f, 1f)); // Galhos bloqueando
                    }
                    else
                    {
                        CirclePx(tex, 40, 28, 7, new Color(0.28f, 0.78f, 0.30f, 1f)); // Sapinho feliz
                        CirclePx(tex, 60, 22, 6, new Color(1f, 0.64f, 0.22f, 1f));    // Peixinho
                    }
                    break;

                case EventKind.PegadasCachorro:
                    // Pegadas na terra levando a um pontinho brilhante
                    Color paw = new Color(0.42f, 0.26f, 0.14f, 0.92f);
                    CirclePx(tex, 24, 18, 6, paw);
                    CirclePx(tex, 44, 22, 6, paw);
                    CirclePx(tex, 64, 18, 6, paw);
                    if (completed) CirclePx(tex, 70, 34, 9, new Color(1f, 0.86f, 0.22f, 1f));
                    break;

                case EventKind.MusicaDaFloresta:
                    // Tronco musical com flauta de bambu e notas
                    RectPx(tex, 22, 12, 74, 32, new Color(0.60f, 0.38f, 0.18f, 1f));
                    RectPx(tex, 30, 34, 66, 42, new Color(0.42f, 0.74f, 0.34f, 1f)); // Flauta de bambu
                    CirclePx(tex, 48, 56, 7, completed ? new Color(1f, 0.84f, 0.22f, 1f) : new Color(0.64f, 0.50f, 0.96f, 1f));
                    break;

                case EventKind.CuidadoraAbrigo:
                    // Casinha/abrigo fofo de folhas com almofada
                    CirclePx(tex, 48, 28, 22, new Color(0.26f, 0.62f, 0.32f, 1f));
                    CirclePx(tex, 48, 22, 14, new Color(0.96f, 0.86f, 0.68f, 1f));
                    if (completed) CirclePx(tex, 48, 52, 8, new Color(1f, 0.42f, 0.66f, 1f)); // Coração!
                    break;

                case EventKind.EnigmaCoruja:
                    // Poleiro iluminado da Coruja Sábia
                    RectPx(tex, 44, 8, 52, 42, new Color(0.46f, 0.28f, 0.14f, 1f));
                    CirclePx(tex, 48, 50, 14, new Color(0.46f, 0.34f, 0.26f, 1f));
                    CirclePx(tex, 43, 52, 4, new Color(1f, 0.84f, 0.22f, 1f));
                    CirclePx(tex, 53, 52, 4, new Color(1f, 0.84f, 0.22f, 1f));
                    break;

                case EventKind.OuricoProcurandoAbrigo:
                case EventKind.OuricoTocaBloqueada:
                case EventKind.OuricoReconstruirAbrigo:
                case EventKind.OuricoCelebraComAnimais:
                    Color burrow = new Color(0.24f, 0.16f, 0.10f, 1f);
                    Color leaf = new Color(0.22f, 0.54f, 0.26f, 1f);
                    EllipsePx(tex, 53, 18, 34, 13, new Color(0.38f, 0.24f, 0.14f, 1f));
                    CirclePx(tex, 51, 20, 14, burrow);
                    CirclePx(tex, 27, 21, 13, leaf);
                    CirclePx(tex, 76, 21, 13, leaf);
                    // Ouriço em pixel art: corpo, focinho e pequenas pontas.
                    CirclePx(tex, 52, 29, 13, new Color(0.62f, 0.38f, 0.22f, 1f));
                    for (int i = 0; i < 7; i++)
                        RectPx(tex, 38 + i * 4, 38 + (i % 2) * 2, 41 + i * 4, 48 + (i % 2) * 2, new Color(0.34f, 0.24f, 0.18f, 1f));
                    CirclePx(tex, 63, 29, 7, new Color(0.82f, 0.58f, 0.38f, 1f));
                    CirclePx(tex, 66, 31, 2, Color.black);
                    if (kind == EventKind.OuricoTocaBloqueada)
                    {
                        RectPx(tex, 29, 8, 72, 15, new Color(0.42f, 0.27f, 0.15f, 1f));
                        RectPx(tex, 34, 15, 78, 20, new Color(0.52f, 0.34f, 0.18f, 1f));
                    }
                    else if (kind == EventKind.OuricoReconstruirAbrigo)
                    {
                        RectPx(tex, 16, 8, 31, 12, new Color(0.50f, 0.32f, 0.16f, 1f));
                        RectPx(tex, 73, 8, 88, 12, new Color(0.50f, 0.32f, 0.16f, 1f));
                        CirclePx(tex, 20, 45, 4, new Color(0.90f, 0.54f, 0.26f, 1f));
                        CirclePx(tex, 82, 44, 4, new Color(0.90f, 0.54f, 0.26f, 1f));
                    }
                    else if (kind == EventKind.OuricoCelebraComAnimais)
                    {
                        CirclePx(tex, 23, 31, 6, new Color(0.70f, 0.46f, 0.28f, 1f));
                        CirclePx(tex, 82, 31, 6, new Color(0.86f, 0.72f, 0.46f, 1f));
                        CirclePx(tex, 52, 55, 4, new Color(1f, 0.84f, 0.26f, 1f));
                    }
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 34f);
            spriteCache[key] = sp;
            return sp;
        }
    }
}
