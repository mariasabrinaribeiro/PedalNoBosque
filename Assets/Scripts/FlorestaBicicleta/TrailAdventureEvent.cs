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
            TesouroEscondido
        }

        public EventKind kind { get; private set; }
        public bool isCompleted { get; private set; } = false;
        public bool puppyAlerted { get; private set; } = false;

        public string ActionButtonLabel
        {
            get
            {
                switch (kind)
                {
                    case EventKind.FloresMurchas:    return "Regar Flores (Espaco)";
                    case EventKind.PassaroCaido:     return "Ajudar Passarinho (Espaco)";
                    case EventKind.PonteQuebrada:    return "Consertar Ponte (Espaco)";
                    case EventKind.TesouroEscondido: return "Abrir Tesouro (Espaco)";
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
                    case EventKind.FloresMurchas:    return "Flores murchas na beira da trilha! Aperte ESPACO para regar!";
                    case EventKind.PassaroCaido:     return "Um passarinho caiu do ninho! Aperte ESPACO para ajuda-lo!";
                    case EventKind.PonteQuebrada:    return "Passarela com tabuas soltas! Aperte ESPACO para consertar!";
                    case EventKind.TesouroEscondido: return "Algo brilha escondido nos arbustos! Aperte ESPACO para investigar!";
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
            if (kind == EventKind.TesouroEscondido && distanceToPlayer < 9.5f)
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

            isCompleted = true;
            if (mainSR != null) mainSR.sprite = GetEventSprite(kind, completed: true);
            if (sparkleSR != null) sparkleSR.gameObject.SetActive(false);

            int stars = 2;
            int pts = 140;

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
                    completionMessage = "Passarela consertada! Agora todos os animais podem atravessar! +3 Estrelas e +180 pts!";
                    break;

                case EventKind.TesouroEscondido:
                    stars = 3; pts = 200;
                    FlorestaSoundFX.Instance?.PlayDiamond();
                    PlayerProgressManager.Instance?.AddCollectible(CollectibleKind.TesourosSecretos, 1);
                    completionMessage = "Tesouro Secreto descoberto com a ajuda do cachorrinho! +3 Estrelas e +200 pts!";
                    break;
            }

            player.score += pts;
            PlayerProgressManager.Instance?.AddStars(stars);
            PlayerProgressManager.Instance?.RecordAdventureAction(kind);
            StartCoroutine(BloomPopRoutine());
            return true;
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
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 34f);
            spriteCache[key] = sp;
            return sp;
        }
    }
}
