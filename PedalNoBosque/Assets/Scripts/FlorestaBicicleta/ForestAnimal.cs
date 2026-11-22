using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Animal ilustrado na cena lateral da floresta:
    /// - Voz própria (miado, uivo, chiado, latidinho, etc.);
    /// - Apetite individual e pedido próprio de comida (7 alimentos);
    /// - Animação de mastigar feliz + chuva de corações subindo na cabeça;
    /// - Choro com lágrimas azuis se atropelado em alta velocidade;
    /// - Volta a sentir fome depois de ~35s para o jogo render bastante;
    /// - Segredo especial do Esquilo (6 porções -> cava a terra para achar diamantes!).
    /// </summary>
    public class ForestAnimal : MonoBehaviour
    {
        public enum EmotionState { ComFome, Empolgado, Comendo, Saciado, Chorando, Cavando }

        public string animalName;
        public ParkSpriteFactory.AnimalKind kind;
        public EmotionState emotion { get; private set; } = EmotionState.ComFome;

        public int requiredMeals = 3;
        public int mealsEaten { get; private set; } = 0;
        public FoodType wantedFood { get; private set; }
        public bool isSquirrelSpecial => kind == ParkSpriteFactory.AnimalKind.Esquilo;
        public bool isFullySatisfied => mealsEaten >= requiredMeals;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer statusIconRenderer;
        private Transform statusIconTransform;

        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite iconHappy;
        private Sprite iconCry;
        private Sprite tearSprite;
        private Sprite[] heartParticles;

        private Vector3 basePosition;
        private float bobPhase;
        private float lastRunOverTime = -5f;
        private float lastVoiceTime = -10f;
        private float reHungryTimer = 0f;

        // Comportamentos especiais (Etapa 3: Lobo cauteloso e Gato companheiro)
        public bool isWolfCautious { get; private set; } = false;
        public bool isCatFollowing { get; private set; } = false;
        private float wolfCalmTimer = 0f;
        private float catFollowTimer = 0f;
        private float catHeartTimer = 0f;
        private Transform trackedPlayer;
        private bool isRestingBySchedule = false;
        private bool picnicAttracted = false;
        private float picnicAttractionTime = 0f;
        private float picnicTargetX = 0f;

        public void Setup(
            ParkSpriteFactory.AnimalKind animalKind,
            string displayName,
            int mealsNeeded,
            Dictionary<FoodType, Sprite> foodMap,
            Sprite happySp,
            Sprite crySp,
            Sprite tearSp,
            Sprite[] heartParticleSprites)
        {
            kind = animalKind;
            animalName = displayName;
            requiredMeals = mealsNeeded;
            foodSprites = foodMap;
            iconHappy = happySp;
            iconCry = crySp;
            tearSprite = tearSp;
            heartParticles = heartParticleSprites;

            basePosition = transform.position;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = LoadSideAnimalSprite(kind);
            bodyRenderer.sortingOrder = 6;

            // Sombra de contato suave para ancoragem realista no solo
            BiomePropsFactory.AddContactShadow(transform, new Vector3(0f, 0.04f, 0f), new Vector2(0.85f, 0.22f), 5);

            GameObject iconObj = new GameObject("OrderBubble");
            iconObj.transform.SetParent(transform);
            float bubbleY = (kind == ParkSpriteFactory.AnimalKind.Esquilo || kind == ParkSpriteFactory.AnimalKind.Coelho
                || kind == ParkSpriteFactory.AnimalKind.Ourico || kind == ParkSpriteFactory.AnimalKind.Sapo
                || kind == ParkSpriteFactory.AnimalKind.Passarinhos) ? 1.42f : 2.1f;
            iconObj.transform.localPosition = new Vector3(0f, bubbleY, 0f);
            iconObj.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            statusIconTransform = iconObj.transform;

            statusIconRenderer = iconObj.AddComponent<SpriteRenderer>();
            statusIconRenderer.sortingOrder = 12;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.2f, 2.5f);
            col.offset = new Vector2(0f, 1.1f);

            PickNextFoodOrder();
        }

        private Sprite LoadSideAnimalSprite(ParkSpriteFactory.AnimalKind k)
        {
            return EcosystemSpriteFactory.LoadOrCreateAnimalSprite(k);
        }

        public void PickNextFoodOrder()
        {
            FoodType[] preferred = GetSpeciesPreferredFoods(kind);
            wantedFood = preferred[Random.Range(0, preferred.Length)];
            emotion = EmotionState.ComFome;
            UpdateBubble();
        }

        private static FoodType[] GetSpeciesPreferredFoods(ParkSpriteFactory.AnimalKind k)
        {
            switch (k)
            {
                case ParkSpriteFactory.AnimalKind.Esquilo:
                    return new[] { FoodType.Nozes, FoodType.Sementes, FoodType.Pipoca };
                case ParkSpriteFactory.AnimalKind.Coelho:
                    return new[] { FoodType.Cenoura, FoodType.Maca, FoodType.FrutasSilvestres };
                case ParkSpriteFactory.AnimalKind.Cervo:
                    return new[] { FoodType.Maca, FoodType.Cenoura };
                case ParkSpriteFactory.AnimalKind.FilhoteUrso:
                    return new[] { FoodType.Mel, FoodType.Maca };
                case ParkSpriteFactory.AnimalKind.Passarinhos:
                    return new[] { FoodType.Sementes, FoodType.Maca };
                case ParkSpriteFactory.AnimalKind.Ourico:
                    return new[] { FoodType.Maca, FoodType.Nozes, FoodType.Sementes };
                case ParkSpriteFactory.AnimalKind.Guaxinim:
                    return new[] { FoodType.Maca, FoodType.Nozes, FoodType.Coxinha, FoodType.Sanduiche };
                case ParkSpriteFactory.AnimalKind.Castor:
                    return new[] { FoodType.Cenoura, FoodType.Maca, FoodType.Nozes };
                case ParkSpriteFactory.AnimalKind.Sapo:
                    return new[] { FoodType.Sementes, FoodType.Doces, FoodType.Suco };
                case ParkSpriteFactory.AnimalKind.CorujaNoturna:
                    return new[] { FoodType.Nozes, FoodType.Sementes, FoodType.Mel };
                case ParkSpriteFactory.AnimalKind.Raposa:
                    return new[] { FoodType.Maca, FoodType.Sanduiche, FoodType.Coxinha };
                case ParkSpriteFactory.AnimalKind.Quati:
                    return new[] { FoodType.Maca, FoodType.Mel, FoodType.Pipoca, FoodType.FrutasSilvestres };
                case ParkSpriteFactory.AnimalKind.Gato:
                    return new[] { FoodType.Suco, FoodType.Sanduiche, FoodType.HotDog, FoodType.Doces };
                case ParkSpriteFactory.AnimalKind.Lobo:
                    return new[] { FoodType.HotDog, FoodType.Sanduiche, FoodType.Coxinha, FoodType.Mel };
                default:
                    return new[] { FoodType.Maca, FoodType.Nozes, FoodType.Pipoca };
            }
        }

        private void UpdateBubble()
        {
            if (statusIconRenderer == null) return;
            if (emotion == EmotionState.Saciado || emotion == EmotionState.Comendo || emotion == EmotionState.Cavando)
                statusIconRenderer.sprite = iconHappy;
            else if (emotion == EmotionState.Chorando)
                statusIconRenderer.sprite = iconCry;
            else if (foodSprites != null && foodSprites.TryGetValue(wantedFood, out Sprite sp))
                statusIconRenderer.sprite = sp;
        }

        private void Update()
        {
            if (statusIconTransform != null)
            {
                float spd = (emotion == EmotionState.Empolgado || emotion == EmotionState.Comendo) ? 7.5f : 3.4f;
                float amp = (emotion == EmotionState.Empolgado || emotion == EmotionState.Comendo) ? 0.16f : 0.10f;
                statusIconTransform.localPosition = new Vector3(0f, 2.55f + Mathf.Sin(Time.time * spd + bobPhase) * amp, 0f);
            }

            // Lobo se acalma depois de alguns segundos quando a menina desacelera
            if (isWolfCautious)
            {
                wolfCalmTimer -= Time.deltaTime;
                if (wolfCalmTimer <= 0f)
                {
                    isWolfCautious = false;
                    if (bodyRenderer != null) bodyRenderer.color = Color.white;
                }
            }

            // Comportamento especial do Gato: acompanha a bicicleta alegremente após ser alimentado!
            if (picnicAttracted)
            {
                if (picnicAttractionTime > 0f)
                {
                    picnicAttractionTime -= Time.deltaTime;
                    float dx = picnicTargetX - transform.position.x;
                    float step = Mathf.Clamp(dx * 2.8f, -3.8f, 3.8f) * Time.deltaTime;
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * 5.4f + bobPhase)) * (Mathf.Abs(dx) > 0.3f ? 0.12f : 0.04f);
                    transform.position = new Vector3(transform.position.x + step, basePosition.y + bob, 0f);
                    if (bodyRenderer != null && Mathf.Abs(dx) > 0.2f) bodyRenderer.flipX = dx < 0f;
                }
                else
                {
                    float dx = basePosition.x - transform.position.x;
                    float step = Mathf.Clamp(dx * 2.2f, -3f, 3f) * Time.deltaTime;
                    transform.position = new Vector3(transform.position.x + step, basePosition.y, 0f);
                    if (Mathf.Abs(dx) < 0.12f)
                    {
                        transform.position = basePosition;
                        picnicAttracted = false;
                        emotion = mealsEaten >= requiredMeals ? EmotionState.Saciado : EmotionState.ComFome;
                        UpdateBubble();
                    }
                }
            }
            else if (isCatFollowing && trackedPlayer != null)
            {
                catFollowTimer -= Time.deltaTime;
                float targetX = trackedPlayer.position.x - 2.6f;
                float dx = targetX - transform.position.x;
                float step = Mathf.Clamp(dx * 3.6f, -6.5f, 6.5f) * Time.deltaTime;
                float trotBounce = Mathf.Abs(Mathf.Sin(Time.time * 11f)) * (Mathf.Abs(dx) > 0.35f ? 0.22f : 0.04f);
                transform.position = new Vector3(transform.position.x + step, basePosition.y + trotBounce, 0f);
                if (bodyRenderer != null && Mathf.Abs(dx) > 0.2f) bodyRenderer.flipX = dx < 0f;

                catHeartTimer += Time.deltaTime;
                if (catHeartTimer >= 2.2f)
                {
                    catHeartTimer = 0f;
                    SpawnHeart(transform.position + new Vector3(0f, 1.8f, 0f));
                }

                if (catFollowTimer <= 0f)
                {
                    isCatFollowing = false;
                    basePosition = new Vector3(transform.position.x, basePosition.y, 0f);
                    transform.position = basePosition;
                }
            }
            else if (emotion != EmotionState.Comendo && emotion != EmotionState.Chorando && emotion != EmotionState.Cavando)
            {
                // Animações naturais por espécie (pulinhos do sapo, bater de asas de passarinhos/coruja)
                if (kind == ParkSpriteFactory.AnimalKind.Sapo)
                {
                    float hop = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 2.1f + bobPhase)), 6f) * 0.42f;
                    transform.position = basePosition + new Vector3(0f, hop, 0f);
                }
                else if (kind == ParkSpriteFactory.AnimalKind.Passarinhos || kind == ParkSpriteFactory.AnimalKind.CorujaNoturna)
                {
                    float flutter = Mathf.Sin(Time.time * 4.5f + bobPhase) * 0.09f;
                    transform.position = basePosition + new Vector3(0f, flutter, 0f);
                }
            }

            // Animais normais voltam a sentir fome depois de ~35s para manter o jogo longo
            if (emotion == EmotionState.Saciado && !isSquirrelSpecial && !isCatFollowing)
            {
                reHungryTimer += Time.deltaTime;
                if (reHungryTimer >= 35f)
                {
                    reHungryTimer = 0f;
                    mealsEaten = Mathf.Max(0, requiredMeals - 1);
                    PickNextFoodOrder();
                }
            }
        }

        public void AttractToPicnic(Vector3 picnicPosition, float duration)
        {
            if (emotion == EmotionState.Chorando || isCatFollowing) return;
            picnicAttracted = true;
            picnicAttractionTime = duration;
            picnicTargetX = picnicPosition.x + Random.Range(-2.5f, 2.5f);
            emotion = EmotionState.Empolgado;
            UpdateBubble();
        }

        public bool OnPlayerApproaching(bool isClose, float playerSpeed = 0f, Transform playerTransform = null)
        {
            if (playerTransform != null) trackedPlayer = playerTransform;

            // Comportamento especial do Lobo: assusta-se se a bicicleta chegar rápido demais!
            if (kind == ParkSpriteFactory.AnimalKind.Lobo && isClose && !isFullySatisfied && Mathf.Abs(playerSpeed) > 4.6f)
            {
                bool justStartled = !isWolfCautious;
                isWolfCautious = true;
                wolfCalmTimer = 2.6f;
                if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 0.86f, 0.82f, 1f);
                if (justStartled) FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
                return justStartled;
            }

            if (emotion == EmotionState.ComFome && isClose)
            {
                emotion = EmotionState.Empolgado;
                if (Time.time - lastVoiceTime > 6f)
                {
                    lastVoiceTime = Time.time;
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
                }
            }
            else if (emotion == EmotionState.Empolgado && !isClose)
            {
                emotion = EmotionState.ComFome;
            }
            return false;
        }

        public enum FeedResult { WrongFood, AteNeedsMore, FullySatisfied, TriggeredSquirrelSecret, AlreadyFull, WolfStartled }

        public FeedResult ReceiveFood(FoodType givenFood)
        {
            if (emotion == EmotionState.Cavando) return FeedResult.AlreadyFull;
            if (kind == ParkSpriteFactory.AnimalKind.Lobo && isWolfCautious)
            {
                StartCoroutine(ShakeNoRoutine());
                FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
                return FeedResult.WolfStartled;
            }
            if (emotion == EmotionState.Saciado)
            {
                StartCoroutine(EatAndHeartsRoutine(6, true));
                FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
                return FeedResult.AlreadyFull;
            }

            if (givenFood != wantedFood)
            {
                StartCoroutine(ShakeNoRoutine());
                return FeedResult.WrongFood;
            }

            mealsEaten++;
            FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);

            if (mealsEaten >= requiredMeals)
            {
                if (isSquirrelSpecial)
                {
                    emotion = EmotionState.Cavando;
                    UpdateBubble();
                    StartCoroutine(EatAndHeartsRoutine(14, true));
                    return FeedResult.TriggeredSquirrelSecret;
                }
                else
                {
                    emotion = EmotionState.Saciado;
                    reHungryTimer = 0f;
                    UpdateBubble();
                    StartCoroutine(EatAndHeartsRoutine(12, true));

                    // Gato saciado começa a acompanhar a bicicleta!
                    if (kind == ParkSpriteFactory.AnimalKind.Gato && trackedPlayer != null)
                    {
                        isCatFollowing = true;
                        catFollowTimer = 10f;
                        catHeartTimer = 0.5f;
                    }
                    return FeedResult.FullySatisfied;
                }
            }
            else
            {
                emotion = EmotionState.Comendo;
                UpdateBubble();
                StartCoroutine(EatAndHeartsRoutine(9, false));
                return FeedResult.AteNeedsMore;
            }
        }

        public bool OnRunOver()
        {
            if (emotion == EmotionState.Cavando) return false;
            if (Time.time - lastRunOverTime < 2.4f) return false;
            lastRunOverTime = Time.time;

            emotion = EmotionState.Chorando;
            UpdateBubble();
            StartCoroutine(CryRoutine());
            return true;
        }

        public void MoveToDigSpot(Vector3 digPos)
        {
            emotion = EmotionState.Cavando;
            basePosition = digPos;
            transform.position = digPos;
            UpdateBubble();
        }

        /// <summary>Rotina natural dos animais por horário do dia (Manhã, Entardecer, Noite e Madrugada).</summary>
        public void ApplyDayPeriodRoutine(DayPeriod period)
        {
            bool isNocturnal = (kind == ParkSpriteFactory.AnimalKind.CorujaNoturna
                             || kind == ParkSpriteFactory.AnimalKind.Lobo
                             || kind == ParkSpriteFactory.AnimalKind.Raposa
                             || kind == ParkSpriteFactory.AnimalKind.Guaxinim);

            if (period == DayPeriod.Manha)
            {
                isRestingBySchedule = false;
                if (bodyRenderer != null && !isWolfCautious) bodyRenderer.color = Color.white;
                if (kind == ParkSpriteFactory.AnimalKind.Passarinhos)
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
            }
            else if (period == DayPeriod.PorDoSol || period == DayPeriod.Noite)
            {
                isRestingBySchedule = !isNocturnal;
                if (bodyRenderer != null && !isWolfCautious)
                {
                    bodyRenderer.color = isRestingBySchedule
                        ? new Color(0.86f, 0.88f, 0.94f, 0.94f) // Descansando calmamente
                        : Color.white;                           // Noturnos despertos!
                }
                if (period == DayPeriod.Noite && kind == ParkSpriteFactory.AnimalKind.CorujaNoturna)
                    FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
            }
            else
            {
                isRestingBySchedule = false;
                if (bodyRenderer != null && !isWolfCautious) bodyRenderer.color = Color.white;
            }
        }

        /// <summary>Reação gentil à campainha da bicicleta: acalma o Lobo e alegra os bichinhos próximos!</summary>
        public void OnBellHeard()
        {
            isRestingBySchedule = false;
            if (isWolfCautious)
            {
                isWolfCautious = false;
                wolfCalmTimer = 0f;
                if (bodyRenderer != null) bodyRenderer.color = Color.white;
            }
            if (Time.time - lastVoiceTime > 3.5f)
            {
                lastVoiceTime = Time.time;
                FlorestaSoundFX.Instance?.PlayAnimalVoice(kind);
                SpawnHeart(transform.position + new Vector3(0f, 2.0f, 0f));
            }
        }

        private IEnumerator EatAndHeartsRoutine(int heartCount, bool keepSaciado)
        {
            for (int i = 0; i < heartCount; i++)
            {
                SpawnHeart(transform.position + new Vector3(Random.Range(-0.85f, 0.85f), Random.Range(1.5f, 2.4f), 0f));
                if (i % 3 == 0) yield return new WaitForSeconds(0.07f);
            }

            // Animação de mastigar e pular feliz!
            for (int chew = 0; chew < 4; chew++)
            {
                float t = 0f;
                while (t < 0.20f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Sin((t / 0.20f) * Mathf.PI);
                    transform.localScale = new Vector3(1f + s * 0.16f, 1f - s * 0.12f, 1f);
                    transform.position = basePosition + new Vector3(0f, s * 0.35f, 0f);
                    yield return null;
                }
            }

            transform.localScale = Vector3.one;
            transform.position = basePosition;

            if (!keepSaciado && emotion == EmotionState.Comendo)
            {
                PickNextFoodOrder();
            }
        }

        private IEnumerator ShakeNoRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                transform.position = basePosition + new Vector3(Mathf.Sin(elapsed * 45f) * 0.15f, 0f, 0f);
                yield return null;
            }
            transform.position = basePosition;
        }

        private IEnumerator CryRoutine()
        {
            for (int i = 0; i < 6; i++)
                SpawnTear(transform.position + new Vector3(Random.Range(-0.45f, 0.45f), 1.4f, 0f));

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                transform.position = basePosition + new Vector3(Mathf.Sin(elapsed * 40f) * 0.14f, 0f, 0f);
                yield return null;
            }
            transform.position = basePosition;

            for (int wave = 0; wave < 3; wave++)
            {
                yield return new WaitForSeconds(0.65f);
                if (emotion != EmotionState.Chorando) yield break;
                SpawnTear(transform.position + new Vector3(Random.Range(-0.35f, 0.35f), 1.4f, 0f));
                SpawnTear(transform.position + new Vector3(Random.Range(-0.35f, 0.35f), 1.4f, 0f));
            }

            if (emotion == EmotionState.Chorando)
            {
                emotion = isFullySatisfied ? EmotionState.Saciado : EmotionState.ComFome;
                UpdateBubble();
            }
        }

        private void SpawnHeart(Vector3 pos)
        {
            if (heartParticles == null || heartParticles.Length == 0) return;
            GameObject go = new GameObject("CoracaoFeliz");
            go.transform.position = pos;
            float sc = Random.Range(0.6f, 1.0f);
            go.transform.localScale = new Vector3(sc, sc, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = heartParticles[Random.Range(0, heartParticles.Length)];
            sr.sortingOrder = 19;
            go.AddComponent<FallingParticle>().Init(
                new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.5f, 2.7f), 0f),
                gravity: 0.4f,
                duration: Random.Range(0.95f, 1.4f)
            );
        }

        private void SpawnTear(Vector3 pos)
        {
            GameObject go = new GameObject("Lagrima");
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tearSprite;
            sr.sortingOrder = 16;
            go.AddComponent<FallingParticle>().Init(
                new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.5f, 1.2f), 0f),
                gravity: -4.5f,
                duration: 0.9f
            );
        }
    }
}
