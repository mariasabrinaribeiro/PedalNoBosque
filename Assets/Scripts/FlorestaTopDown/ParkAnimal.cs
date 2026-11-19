using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Animal do parque com:
    /// - Apetite individual (uns ficam satisfeitos rápido, outros precisam de bastante comida);
    /// - Escolha própria do alimento (Refrigerante, Suco, Coxinha, Doces, Pipoca, Sanduíche, Hot Dog);
    /// - Animação de mastigar/comer feliz + vários corações subindo na cabeça;
    /// - Choro com lágrimas se for atropelado;
    /// - Ciclo longo: animais normais voltam a sentir fome depois de um tempo;
    /// - Segredo especial do Esquilo: ao completar todas as porções dele, ele cava a terra em busca de diamantes!
    /// </summary>
    public class ParkAnimal : MonoBehaviour
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

        private SpriteRenderer bodySR;
        private SpriteRenderer emotionIconSR;
        private Transform emotionIconT;

        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite iconHappy;
        private Sprite iconCry;
        private Sprite tearSprite;
        private Sprite[] heartParticles;

        private Vector3 basePos;
        private float lastRunOverTime = -5f;
        private float bobPhase;
        private float reHungryTimer = 0f;

        public void Setup(
            ParkSpriteFactory.AnimalKind animalKind,
            string displayName,
            int mealsNeeded,
            Dictionary<FoodType, Sprite> foodSpriteMap,
            Sprite happySp,
            Sprite crySp,
            Sprite tearSp,
            Sprite[] heartParticleSprites)
        {
            kind = animalKind;
            animalName = displayName;
            requiredMeals = mealsNeeded;
            foodSprites = foodSpriteMap;
            iconHappy = happySp;
            iconCry = crySp;
            tearSprite = tearSp;
            heartParticles = heartParticleSprites;

            basePos = transform.position;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            bodySR = gameObject.AddComponent<SpriteRenderer>();
            bodySR.sprite = ParkSpriteFactory.LoadAnimalSprite(kind);
            bodySR.sortingOrder = 8;

            GameObject iconObj = new GameObject("OrderBubble");
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            iconObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            emotionIconT = iconObj.transform;

            emotionIconSR = iconObj.AddComponent<SpriteRenderer>();
            emotionIconSR.sortingOrder = 14;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.95f;

            PickNextFoodOrder();
        }

        public void PickNextFoodOrder()
        {
            // Primeiro pedido costuma ser pronto; depois mistura todos incluindo Sanduíche e Hot Dog!
            int maxIdx = (mealsEaten == 0) ? 5 : 7;
            wantedFood = (FoodType)Random.Range(0, maxIdx);
            emotion = EmotionState.ComFome;
            UpdateBubbleSprite();
        }

        private void UpdateBubbleSprite()
        {
            if (emotionIconSR == null) return;
            if (emotion == EmotionState.Saciado || emotion == EmotionState.Comendo)
            {
                emotionIconSR.sprite = iconHappy;
            }
            else if (emotion == EmotionState.Chorando)
            {
                emotionIconSR.sprite = iconCry;
            }
            else if (foodSprites != null && foodSprites.TryGetValue(wantedFood, out Sprite sp))
            {
                emotionIconSR.sprite = sp;
            }
        }

        private void Update()
        {
            if (emotionIconT != null)
            {
                float speed = (emotion == EmotionState.Empolgado || emotion == EmotionState.Comendo) ? 7.5f : 3.2f;
                float amp = (emotion == EmotionState.Empolgado || emotion == EmotionState.Comendo) ? 0.16f : 0.10f;
                emotionIconT.localPosition = new Vector3(0f, 1.45f + Mathf.Sin(Time.time * speed + bobPhase) * amp, 0f);
            }

            // Animais normais (exceto o Esquilo especial) voltam a sentir fome após ~35s para o jogo continuar longo!
            if (emotion == EmotionState.Saciado && !isSquirrelSpecial)
            {
                reHungryTimer += Time.deltaTime;
                if (reHungryTimer >= 35f)
                {
                    reHungryTimer = 0f;
                    mealsEaten = Mathf.Max(0, requiredMeals - 1); // Volta a pedir mais 1 lanche gostoso
                    PickNextFoodOrder();
                }
            }
        }

        public void SetExcited(bool excited)
        {
            if (emotion == EmotionState.ComFome && excited)
                emotion = EmotionState.Empolgado;
            else if (emotion == EmotionState.Empolgado && !excited)
                emotion = EmotionState.ComFome;
        }

        public enum FeedResult { WrongFood, AteNeedsMore, FullySatisfied, TriggeredSquirrelSecret, AlreadyFull }

        public FeedResult ReceiveFood(FoodType givenFood)
        {
            if (emotion == EmotionState.Cavando) return FeedResult.AlreadyFull;
            if (emotion == EmotionState.Saciado)
            {
                // Mesmo saciado, adora um agrado com corações!
                StartCoroutine(EatAndHeartsRoutine(5, keepSaciado: true));
                return FeedResult.AlreadyFull;
            }

            if (givenFood != wantedFood)
            {
                StartCoroutine(ShakeNoRoutine());
                return FeedResult.WrongFood;
            }

            mealsEaten++;
            if (mealsEaten >= requiredMeals)
            {
                if (isSquirrelSpecial)
                {
                    emotion = EmotionState.Cavando;
                    StartCoroutine(EatAndHeartsRoutine(14, keepSaciado: true));
                    return FeedResult.TriggeredSquirrelSecret;
                }
                else
                {
                    emotion = EmotionState.Saciado;
                    reHungryTimer = 0f;
                    UpdateBubbleSprite();
                    StartCoroutine(EatAndHeartsRoutine(12, keepSaciado: true));
                    return FeedResult.FullySatisfied;
                }
            }
            else
            {
                emotion = EmotionState.Comendo;
                UpdateBubbleSprite();
                StartCoroutine(EatAndHeartsRoutine(8, keepSaciado: false));
                return FeedResult.AteNeedsMore;
            }
        }

        public bool OnRunOver()
        {
            if (emotion == EmotionState.Cavando) return false;
            if (Time.time - lastRunOverTime < 2.2f) return false;
            lastRunOverTime = Time.time;

            emotion = EmotionState.Chorando;
            UpdateBubbleSprite();
            StartCoroutine(CryReactionRoutine());
            return true;
        }

        public void MoveToDigSpot(Vector3 digPosition)
        {
            emotion = EmotionState.Cavando;
            basePos = digPosition;
            transform.position = digPosition;
            if (emotionIconSR != null) emotionIconSR.sprite = iconHappy;
        }

        private IEnumerator EatAndHeartsRoutine(int heartCount, bool keepSaciado)
        {
            // Solta vários corações coloridos subindo em volta da cabeça do animal!
            for (int i = 0; i < heartCount; i++)
            {
                SpawnRisingHeart(transform.position + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.5f, 1.1f), 0f));
                if (i % 3 == 0) yield return new WaitForSeconds(0.08f);
            }

            // Animação de mastigar / comer feliz (squash & stretch + pulinhos)
            for (int chew = 0; chew < 4; chew++)
            {
                float t = 0f;
                while (t < 0.20f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Sin((t / 0.20f) * Mathf.PI);
                    transform.localScale = new Vector3(1f + s * 0.16f, 1f - s * 0.12f, 1f);
                    transform.position = basePos + new Vector3(0f, s * 0.28f, 0f);
                    yield return null;
                }
            }

            transform.localScale = Vector3.one;
            transform.position = basePos;

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
                transform.position = basePos + new Vector3(Mathf.Sin(elapsed * 45f) * 0.14f, 0f, 0f);
                yield return null;
            }
            transform.position = basePos;
        }

        private IEnumerator CryReactionRoutine()
        {
            for (int i = 0; i < 6; i++)
            {
                SpawnTear(transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.4f, 0f));
            }

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                transform.position = basePos + new Vector3(Mathf.Sin(elapsed * 40f) * 0.12f, 0f, 0f);
                yield return null;
            }
            transform.position = basePos;

            for (int wave = 0; wave < 3; wave++)
            {
                yield return new WaitForSeconds(0.65f);
                if (emotion != EmotionState.Chorando) yield break;
                SpawnTear(transform.position + new Vector3(Random.Range(-0.35f, 0.35f), 0.35f, 0f));
                SpawnTear(transform.position + new Vector3(Random.Range(-0.35f, 0.35f), 0.35f, 0f));
            }

            if (emotion == EmotionState.Chorando)
            {
                emotion = isFullySatisfied ? EmotionState.Saciado : EmotionState.ComFome;
                UpdateBubbleSprite();
            }
        }

        private void SpawnRisingHeart(Vector3 startPos)
        {
            if (heartParticles == null || heartParticles.Length == 0) return;
            GameObject heart = new GameObject("CoracaoFeliz");
            heart.transform.position = startPos;
            float sc = Random.Range(0.55f, 0.95f);
            heart.transform.localScale = new Vector3(sc, sc, 1f);

            SpriteRenderer sr = heart.AddComponent<SpriteRenderer>();
            sr.sprite = heartParticles[Random.Range(0, heartParticles.Length)];
            sr.sortingOrder = 19;

            heart.AddComponent<FallingParticle>().Init(
                new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.4f, 2.5f), 0f),
                gravity: 0.4f, // Sobe suavemente para o céu!
                duration: Random.Range(0.9f, 1.35f)
            );
        }

        private void SpawnTear(Vector3 startPos)
        {
            GameObject tear = new GameObject("Lagrima");
            tear.transform.position = startPos;
            SpriteRenderer sr = tear.AddComponent<SpriteRenderer>();
            sr.sprite = tearSprite;
            sr.sortingOrder = 16;
            tear.AddComponent<FallingParticle>().Init(
                new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.4f, 1.1f), 0f),
                gravity: -4.2f,
                duration: 0.9f
            );
        }
    }

    /// <summary>
    /// Partícula 2D simples para corações subindo, lágrimas caindo, gotinhas de água, terra e diamantes.
    /// </summary>
    public class FallingParticle : MonoBehaviour
    {
        private Vector3 vel;
        private float grav;
        private float life;
        private float elapsed;
        private SpriteRenderer sr;

        public void Init(Vector3 initialVel, float gravity, float duration)
        {
            vel = initialVel;
            grav = gravity;
            life = duration;
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            vel.y += grav * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - Mathf.Clamp01(elapsed / life);
                sr.color = c;
            }
            if (elapsed >= life) Destroy(gameObject);
        }
    }
}
