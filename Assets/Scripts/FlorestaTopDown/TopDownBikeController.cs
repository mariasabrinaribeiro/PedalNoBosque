using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlorestaTopDown
{
    /// <summary>
    /// Controla a menina na bicicleta vista de cima com o cachorrinho na cesta:
    /// - Pedalada devagar/rápida afetada por Subidas, Descidas, Combo de Felicidade e Sede;
    /// - Cesta de alimentos (até 5 lanches) e arremesso com mira assistida;
    /// - Ajuda o Esquilo a cavar a terra para encontrar diamantes;
    /// - Atropelamento perde pontos, zera combo e solta lágrimas;
    /// - Colunas de Água recarregam a sede do cachorrinho.
    /// </summary>
    public class TopDownBikeController : MonoBehaviour
    {
        [Header("Movimento")]
        public float slowSpeed = 4.6f;
        public float fastSpeed = 8.4f;
        public float steerSpeed = 168f;
        public float parkHalfSize = 26f;

        [Header("Status")]
        public int score = 0;
        public int happyCombo = 0;
        public float waterLevel = 100f;
        public float waterDrainPerSecond = 2.0f;
        public int maxBasketSize = 5;

        public readonly List<FoodType> foodBasket = new List<FoodType>();
        public string currentSlopeLabel { get; private set; } = "Plano";

        private float slopeMultiplier = 1f;
        private float lastThrowTime = -1f;
        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite tearSprite;
        private DiamondDigSite digSiteRef;

        // Inputs mobile/UI
        private bool uiLeft, uiRight, uiSlow, uiFast, uiReverse;

        private System.Action<ParkAnimal, FoodType, ParkAnimal.FeedResult> onFoodDelivered;
        private System.Action<ParkAnimal> onAnimalRunOver;
        private System.Action onWaterRefilled;
        private System.Action<string> onInfoMessage;

        public void Setup(
            Dictionary<FoodType, Sprite> foodSpriteMap,
            DiamondDigSite digSite,
            System.Action<ParkAnimal, FoodType, ParkAnimal.FeedResult> deliveredCb,
            System.Action<ParkAnimal> runOverCb,
            System.Action waterCb,
            System.Action<string> infoCb)
        {
            foodSprites = foodSpriteMap;
            digSiteRef = digSite;
            onFoodDelivered = deliveredCb;
            onAnimalRunOver = runOverCb;
            onWaterRefilled = waterCb;
            onInfoMessage = infoCb;

            tearSprite = ParkSpriteFactory.CreateTearDropSprite();

            // Começa com alguns lanches na cesta para já poder jogar comida logo no início
            foodBasket.Clear();
            foodBasket.Add(FoodType.Pipoca);
            foodBasket.Add(FoodType.Coxinha);
            foodBasket.Add(FoodType.Suco);

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ParkSpriteFactory.LoadTopDownBikeSprite();
            sr.sortingOrder = 12;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;

            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        public void AddFoodToBasket(FoodType type)
        {
            if (foodBasket.Count >= maxBasketSize)
            {
                foodBasket.RemoveAt(0);
            }
            foodBasket.Add(type);
        }

        public string GetBasketSummary()
        {
            if (foodBasket.Count == 0) return "Cesta vazia (vá à Cozinha!)";
            List<string> names = new List<string>();
            foreach (var f in foodBasket) names.Add(FoodInfo.GetDisplayName(f));
            return $"Cesta ({foodBasket.Count}/{maxBasketSize}): " + string.Join(", ", names);
        }

        public void SetUILeft(bool v) => uiLeft = v;
        public void SetUIRight(bool v) => uiRight = v;
        public void SetUISlow(bool v) => uiSlow = v;
        public void SetUIFast(bool v) => uiFast = v;
        public void SetUIReverse(bool v) => uiReverse = v;

        private void Update()
        {
            float moveInput = 0f;
            float steerInput = 0f;
            bool fastPressed = uiFast;
            bool actionPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput = 1f;
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput = -0.6f;

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) steerInput = 1f;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) steerInput = -1f;

                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) fastPressed = true;
                actionPressed = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
            }

            if (uiFast) { moveInput = 1f; fastPressed = true; }
            else if (uiSlow) moveInput = 1f;
            else if (uiReverse) moveInput = -0.6f;

            if (uiLeft) steerInput = 1f;
            else if (uiRight) steerInput = -1f;

            transform.Rotate(0f, 0f, steerInput * steerSpeed * Time.deltaTime);

            float baseSpd = fastPressed ? fastSpeed : slowSpeed;
            float comboBoost = happyCombo >= 3 ? 1.20f : 1f;
            float thirstPenalty = waterLevel <= 1f ? 0.55f : 1f;
            float finalSpeed = moveInput * baseSpd * slopeMultiplier * comboBoost * thirstPenalty;

            Vector3 nextPos = transform.position + transform.up * (finalSpeed * Time.deltaTime);
            nextPos.x = Mathf.Clamp(nextPos.x, -parkHalfSize, parkHalfSize);
            nextPos.y = Mathf.Clamp(nextPos.y, -parkHalfSize, parkHalfSize);
            transform.position = nextPos;

            float drainFactor = Mathf.Abs(moveInput) > 0.1f ? (fastPressed ? 1.45f : 0.95f) : 0.3f;
            waterLevel = Mathf.Max(0f, waterLevel - waterDrainPerSecond * drainFactor * Time.deltaTime);

            if (actionPressed)
            {
                if (digSiteRef != null && digSiteRef.isActive && digSiteRef.playerNearby && !digSiteRef.isCompleted)
                {
                    digSiteRef.HelpDig();
                }
                else
                {
                    ThrowFood();
                }
            }
        }

        public void ThrowFood()
        {
            if (Time.time - lastThrowTime < 0.35f) return;

            if (foodBasket.Count == 0)
            {
                ParkSoundFX.Instance?.PlayWrong();
                onInfoMessage?.Invoke("Cesta vazia! Vá até a Barraca da Cozinha (rosa e branca) para pegar ou preparar lanches!");
                return;
            }

            lastThrowTime = Time.time;

            // Encontra o animal mais próximo à frente para mira assistida e seleção inteligente do alimento certo!
            Vector3 throwDir = transform.up;
            ParkAnimal targetAnimal = null;
            ParkAnimal[] allAnimals = FindObjectsOfType<ParkAnimal>();
            float bestDist = 11f;
            foreach (var a in allAnimals)
            {
                Vector3 toAnimal = a.transform.position - transform.position;
                float dist = toAnimal.magnitude;
                if (dist > 0.5f && dist < bestDist)
                {
                    float dot = Vector3.Dot(transform.up, toAnimal.normalized);
                    if (dot > 0.35f)
                    {
                        bestDist = dist;
                        throwDir = toAnimal.normalized;
                        targetAnimal = a;
                    }
                }
            }

            // Se a cesta contém exatamente o que o animal próximo pediu, arremessa esse alimento!
            int basketIndex = 0;
            if (targetAnimal != null && !targetAnimal.isFullySatisfied)
            {
                int matchIdx = foodBasket.IndexOf(targetAnimal.wantedFood);
                if (matchIdx >= 0) basketIndex = matchIdx;
            }

            FoodType chosenFood = foodBasket[basketIndex];
            foodBasket.RemoveAt(basketIndex);

            ParkSoundFX.Instance?.PlayThrow();
            GameObject projObj = new GameObject("AlimentoArremessado");
            projObj.transform.position = transform.position + transform.up * 0.9f;
            FoodProjectile proj = projObj.AddComponent<FoodProjectile>();
            Sprite sp = foodSprites[chosenFood];
            proj.Launch(chosenFood, throwDir, 12.5f, sp, HandleFoodHitAnimal);
        }

        private void HandleFoodHitAnimal(ParkAnimal animal, FoodType givenFood)
        {
            ParkAnimal.FeedResult result = animal.ReceiveFood(givenFood);
            switch (result)
            {
                case ParkAnimal.FeedResult.AteNeedsMore:
                    score += 80;
                    happyCombo++;
                    ParkSoundFX.Instance?.PlayHappy();
                    break;
                case ParkAnimal.FeedResult.FullySatisfied:
                    score += 150;
                    happyCombo++;
                    ParkSoundFX.Instance?.PlayHappy();
                    break;
                case ParkAnimal.FeedResult.TriggeredSquirrelSecret:
                    score += 300;
                    happyCombo++;
                    ParkSoundFX.Instance?.PlayDiamond();
                    break;
                case ParkAnimal.FeedResult.AlreadyFull:
                    score += 20;
                    ParkSoundFX.Instance?.PlayHappy();
                    break;
                case ParkAnimal.FeedResult.WrongFood:
                    ParkSoundFX.Instance?.PlayWrong();
                    break;
            }
            onFoodDelivered?.Invoke(animal, givenFood, result);
        }

        private void SpawnGirlTears()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject tear = new GameObject("LagrimaMenina");
                tear.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, 0f);
                SpriteRenderer sr = tear.AddComponent<SpriteRenderer>();
                sr.sprite = tearSprite;
                sr.sortingOrder = 16;
                tear.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.3f, 0.9f), 0f),
                    gravity: -4f,
                    duration: 0.85f
                );
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SlopeZone slope = other.GetComponent<SlopeZone>();
            if (slope != null)
            {
                slopeMultiplier = slope.SpeedMultiplier;
                currentSlopeLabel = slope.DisplayLabel;
                return;
            }

            WaterColumn water = other.GetComponent<WaterColumn>();
            if (water != null)
            {
                RefillWater(water.transform.position);
                return;
            }

            ParkAnimal animal = other.GetComponent<ParkAnimal>();
            if (animal != null)
            {
                if (animal.OnRunOver())
                {
                    score = Mathf.Max(0, score - 50);
                    happyCombo = 0;
                    ParkSoundFX.Instance?.PlayCry();
                    SpawnGirlTears();
                    onAnimalRunOver?.Invoke(animal);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            WaterColumn water = other.GetComponent<WaterColumn>();
            if (water != null && waterLevel < 98f)
            {
                RefillWater(water.transform.position);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            SlopeZone slope = other.GetComponent<SlopeZone>();
            if (slope != null)
            {
                slopeMultiplier = 1f;
                currentSlopeLabel = "Plano";
            }
        }

        private void RefillWater(Vector3 fountainPos)
        {
            if (waterLevel >= 99f) return;
            waterLevel = 100f;
            ParkSoundFX.Instance?.PlayWater();

            for (int i = 0; i < 6; i++)
            {
                GameObject drop = new GameObject("GotaAgua");
                drop.transform.position = fountainPos + new Vector3(Random.Range(-0.4f, 0.4f), 0.2f, 0f);
                SpriteRenderer sr = drop.AddComponent<SpriteRenderer>();
                sr.sprite = tearSprite;
                sr.sortingOrder = 16;
                drop.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0.8f, 1.8f), 0f),
                    gravity: -4.5f,
                    duration: 0.7f
                );
            }

            onWaterRefilled?.Invoke();
        }
    }
}
