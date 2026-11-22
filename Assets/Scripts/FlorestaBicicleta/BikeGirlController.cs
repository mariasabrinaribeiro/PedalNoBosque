using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Controla a menina com o cachorrinho na cesta na cena lateral da floresta:
    /// - Pedala devagar, rápido e volta pela estrada;
    /// - Sente Subidas (mais pesada, bicicleta inclina pra cima) e Descidas (embala, inclina pra baixo);
    /// - Carrega cesta de até 5 lanches e arremessa o alimento certo em arco para os animais;
    /// - Se passar rápido em cima de um animal, atropela (-50 pts + lágrimas);
    /// - Cachorrinho sente sede (som ofegante < 25%) e bebe nas Colunas de Água;
    /// - Ajuda o Esquilo a cavar a terra para encontrar os diamantes!
    /// </summary>
    public class BikeGirlController : MonoBehaviour
    {
        [Header("Velocidades")]
        public float slowSpeed = 3.6f;
        public float fastSpeed = 7.8f;
        public float reverseSpeed = -3.8f;
        public float acceleration = 11f;

        [Header("Limites da Estrada")]
        public bool infiniteRoad = true;
        public float minX = -4f;
        public float maxX = 160f;

        [Header("Status")]
        public int score = 0;
        public int happyCombo = 0;
        public float waterLevel = 100f;
        public float waterDrainPerSecond = 2.0f;
        public int maxBasketSize = 6;

        public readonly List<FoodType> foodBasket = new List<FoodType>();
        public float currentSpeed { get; private set; } = 0f;
        public bool isFaroMode { get; private set; }
        public bool isOnFoot { get; private set; }
        public bool canDismountAtNearbyEvent { get; set; }
        public string currentSlopeLabel { get; private set; } = "Plano";
        public bool isNearKitchen { get; private set; } = false;

        [Header("Física de Salto (Etapa 1)")]
        public float manualJumpVelocity = 7.8f;
        public float jumpGravity = -19.5f;
        public float jumpHeight { get; private set; } = 0f;
        private float verticalVelocity = 0f;
        private float baseGroundY = -3.55f;
        private float stumbleTimer = 0f;
        public int starsCollected { get; private set; } = 0;
        public int gourmetBonusCharges { get; set; } = 0;
        public float distanceMeters => Mathf.Max(0f, transform.position.x * 2.5f);
        public float distanceKm => distanceMeters / 1000f;
        public float maxDistanceKm { get; private set; } = 0f;

        public void SetMinX(float newMinX)
        {
            if (newMinX > minX) minX = newMinX;
        }

        private Transform visualRoot;
        private SpriteRenderer bikeSR;
        private SpriteRenderer walkingGirlSR;
        private SpriteRenderer parkedBikeSR;
        private Sprite walkingGirlSprite;
        private Sprite parkedBikeSprite;
        private float parkedBikeX;
        private SpriteRenderer customAccentBadgeSR;
        private float slopeMultiplier = 1f;
        private float slopeTiltDeg = 0f;
        private float facingDir = 1f;
        private float lastThrowTime = -1f;
        private float lastThirstSoundTime = -10f;

        private Dictionary<FoodType, Sprite> foodSprites;
        private Sprite tearSprite;
        private SideDiamondDigSite digSiteRef;
        private InfiniteTrackGenerator trackGeneratorRef;

        // Inputs de botões na tela
        private bool uiSlowPressed = false;
        private bool uiFastPressed = false;
        private bool uiReversePressed = false;

        private System.Action<ForestAnimal, FoodType, ForestAnimal.FeedResult> onFoodDelivered;
        private System.Action<ForestAnimal> onAnimalRunOver;
        private System.Action onWaterRefilled;
        private System.Action<string> onInfoMessage;

        public void Setup(
            Dictionary<FoodType, Sprite> foodMap,
            SideDiamondDigSite digSite,
            System.Action<ForestAnimal, FoodType, ForestAnimal.FeedResult> deliveredCb,
            System.Action<ForestAnimal> runOverCb,
            System.Action waterCb,
            System.Action<string> infoCb)
        {
            foodSprites = foodMap;
            digSiteRef = digSite;
            onFoodDelivered = deliveredCb;
            onAnimalRunOver = runOverCb;
            onWaterRefilled = waterCb;
            onInfoMessage = infoCb;

            tearSprite = ParkSpriteFactory.CreateTearDropSprite();

            // Começa com uma cesta variada de alimentos naturais + suco para alimentar logo de início
            foodBasket.Clear();
            foodBasket.Add(FoodType.Nozes);
            foodBasket.Add(FoodType.Cenoura);
            foodBasket.Add(FoodType.Maca);
            foodBasket.Add(FoodType.Mel);
            foodBasket.Add(FoodType.Sementes);
            foodBasket.Add(FoodType.Suco);

            visualRoot = new GameObject("BikeVisual").transform;
            visualRoot.SetParent(transform);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localScale = Vector3.one;
            baseGroundY = transform.position.y;

            bikeSR = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            bikeSR.sprite = FlorestaSpriteFactory.LoadGirlBikeSprite();
            bikeSR.sortingOrder = 10;

            // Adorno delicado para acessórios/enfeites equipados (Etapa 7)
            GameObject badgeObj = new GameObject("CustomAccent");
            badgeObj.transform.SetParent(visualRoot, false);
            badgeObj.transform.localPosition = new Vector3(0.55f, 1.95f, 0f);
            badgeObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            customAccentBadgeSR = badgeObj.AddComponent<SpriteRenderer>();
            customAccentBadgeSR.sprite = TrailCollectibleItem.GetOrCreateSprite(CollectibleKind.Flores);
            customAccentBadgeSR.sortingOrder = 11;
            customAccentBadgeSR.enabled = false;

            ApplyCustomizationVisuals();

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.2f, 2.4f);
            col.offset = new Vector2(0f, 1.2f);

            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        public void AddFoodToBasket(FoodType type)
        {
            if (foodBasket.Count >= maxBasketSize) foodBasket.RemoveAt(0);
            foodBasket.Add(type);
        }

        public bool TryAddFoodToBasket(FoodType type)
        {
            if (foodBasket.Count >= maxBasketSize) return false;
            foodBasket.Add(type);
            return true;
        }

        public bool TryConsumeFood(FoodType type)
        {
            return foodBasket.Remove(type);
        }

        public string GetBasketSummary()
        {
            if (foodBasket.Count == 0) return "Cesta vazia (passe na Barraca da Cozinha!)";
            List<string> names = new List<string>();
            foreach (var f in foodBasket) names.Add(FoodInfo.GetDisplayName(f));
            return $"Cesta ({foodBasket.Count}/{maxBasketSize}): " + string.Join(", ", names);
        }

        public void SetTrackGenerator(InfiniteTrackGenerator generator) => trackGeneratorRef = generator;

        public void SetFaroMode(bool enabled)
        {
            if (isFaroMode == enabled) return;
            isFaroMode = enabled;
            if (enabled)
                onInfoMessage?.Invoke("Modo Faro ativo: pare a bicicleta e siga o sinal do cachorrinho.");
            else
                onInfoMessage?.Invoke("Modo Faro encerrado. Vamos continuar pedalando!");
        }

        public void ApplyCustomizationVisuals()
        {
            var pm = PlayerProgressManager.Instance;
            if (pm == null || bikeSR == null) return;

            var bikeDef = pm.GetEquippedDef(CustomizationCategory.Bicicletas);
            bikeSR.color = bikeDef != null ? bikeDef.tintColor : Color.white;

            if (customAccentBadgeSR != null)
            {
                var puppyDef = pm.GetEquippedDef(CustomizationCategory.Cachorrinho);
                var accDef = pm.GetEquippedDef(CustomizationCategory.Acessorios);
                bool hasCustomDecor = (puppyDef != null && puppyDef.id != "puppy_default")
                                   || (accDef != null && accDef.id != "acc_default");
                customAccentBadgeSR.enabled = hasCustomDecor;
                if (hasCustomDecor)
                {
                    Color badgeCol = (puppyDef != null && puppyDef.id != "puppy_default")
                        ? puppyDef.tintColor
                        : accDef.tintColor;
                    customAccentBadgeSR.color = badgeCol;
                }
            }
        }

        public void SetUISlow(bool pressed) => uiSlowPressed = pressed;
        public void SetUIFast(bool pressed) => uiFastPressed = pressed;
        public void SetUIReverse(bool pressed) => uiReversePressed = pressed;

        public bool ToggleExploreMode()
        {
            if (!isOnFoot)
            {
                if (!canDismountAtNearbyEvent || Mathf.Abs(currentSpeed) > 0.25f)
                {
                    onInfoMessage?.Invoke("Pare ao lado de um ponto de exploração para descer da bicicleta.");
                    return false;
                }

                isOnFoot = true;
                parkedBikeX = transform.position.x;
                if (bikeSR != null) bikeSR.enabled = false;
                if (customAccentBadgeSR != null) customAccentBadgeSR.enabled = false;
                CreateFootVisuals();
                onInfoMessage?.Invoke("Você estacionou a bicicleta. Explore a área com A/D ou os controles laterais.");
                return true;
            }

            if (Mathf.Abs(transform.position.x - parkedBikeX) > 7f)
            {
                onInfoMessage?.Invoke("Volte até a bicicleta para continuar o passeio.");
                return false;
            }

            isOnFoot = false;
            if (walkingGirlSR != null) Destroy(walkingGirlSR.gameObject);
            if (parkedBikeSR != null) Destroy(parkedBikeSR.gameObject);
            if (bikeSR != null) bikeSR.enabled = true;
            ApplyCustomizationVisuals();
            onInfoMessage?.Invoke("Você voltou à bicicleta.");
            return true;
        }

        private void CreateFootVisuals()
        {
            if (parkedBikeSprite == null) parkedBikeSprite = BuildParkedBikeSprite();
            if (walkingGirlSprite == null) walkingGirlSprite = BuildWalkingGirlSprite();

            GameObject bike = new GameObject("BicicletaEstacionada");
            bike.transform.position = new Vector3(parkedBikeX, baseGroundY, 0f);
            parkedBikeSR = bike.AddComponent<SpriteRenderer>();
            parkedBikeSR.sprite = parkedBikeSprite;
            parkedBikeSR.sortingOrder = 9;

            GameObject girl = new GameObject("MeninaExplorandoAPe");
            girl.transform.SetParent(transform, false);
            girl.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            walkingGirlSR = girl.AddComponent<SpriteRenderer>();
            walkingGirlSR.sprite = walkingGirlSprite;
            walkingGirlSR.sortingOrder = 10;
        }

        private static Sprite BuildWalkingGirlSprite()
        {
            Texture2D tex = new Texture2D(32, 48, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color skin = new Color(0.88f, 0.62f, 0.42f, 1f);
            Color hair = new Color(0.28f, 0.16f, 0.10f, 1f);
            Color shirt = new Color(0.90f, 0.40f, 0.55f, 1f);
            Color shorts = new Color(0.22f, 0.40f, 0.58f, 1f);
            Color shoe = new Color(0.32f, 0.22f, 0.18f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < 48; y++) for (int x = 0; x < 32; x++) tex.SetPixel(x, y, clear);
            FillSpriteRect(tex, 11, 34, 21, 44, hair);
            FillSpriteRect(tex, 12, 35, 20, 43, skin);
            FillSpriteRect(tex, 11, 41, 21, 45, hair);
            FillSpriteRect(tex, 9, 22, 23, 35, shirt);
            FillSpriteRect(tex, 6, 25, 10, 34, skin);
            FillSpriteRect(tex, 22, 24, 26, 33, skin);
            FillSpriteRect(tex, 10, 13, 22, 23, shorts);
            FillSpriteRect(tex, 11, 4, 15, 14, shorts);
            FillSpriteRect(tex, 17, 4, 21, 14, shorts);
            FillSpriteRect(tex, 9, 1, 16, 4, shoe);
            FillSpriteRect(tex, 16, 1, 23, 4, shoe);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0f), 24f);
        }

        private static Sprite BuildParkedBikeSprite()
        {
            Texture2D tex = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color frame = new Color(0.30f, 0.58f, 0.42f, 1f);
            Color tire = new Color(0.16f, 0.17f, 0.15f, 1f);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++) tex.SetPixel(x, y, clear);
            DrawSpriteCircle(tex, 13, 10, 8, tire);
            DrawSpriteCircle(tex, 51, 10, 8, tire);
            FillSpriteRect(tex, 12, 9, 14, 11, frame);
            FillSpriteRect(tex, 13, 10, 33, 12, frame);
            FillSpriteRect(tex, 24, 10, 27, 24, frame);
            FillSpriteRect(tex, 26, 21, 38, 23, frame);
            FillSpriteRect(tex, 36, 10, 52, 12, frame);
            FillSpriteRect(tex, 33, 21, 36, 23, frame);
            FillSpriteRect(tex, 24, 24, 32, 26, tire);
            FillSpriteRect(tex, 37, 21, 42, 23, tire);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0f), 24f);
        }

        private static void FillSpriteRect(Texture2D tex, int x1, int y1, int x2, int y2, Color color)
        {
            for (int x = x1; x < x2; x++) for (int y = y1; y < y2; y++) tex.SetPixel(x, y, color);
        }

        private static void DrawSpriteCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int x = cx - radius; x <= cx + radius; x++) for (int y = cy - radius; y <= cy + radius; y++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius
                    && x >= 0 && y >= 0 && x < tex.width && y < tex.height) tex.SetPixel(x, y, color);
        }

        private void Update()
        {
            float baseTargetSpeed = 0f;
            bool actionPressed = false;
            bool bellPressed = false;
            bool jumpPressed = false;
            bool faroPressed = false;
            bool explorePressed = false;

            if (Keyboard.current != null)
            {
                bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

                if (uiFastPressed || (right && shift)) baseTargetSpeed = fastSpeed;
                else if (uiSlowPressed || right) baseTargetSpeed = slowSpeed;
                else if (uiReversePressed || left) baseTargetSpeed = reverseSpeed;

                actionPressed = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
                jumpPressed = Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame;
                bellPressed = Keyboard.current.bKey.wasPressedThisFrame;
                faroPressed = Keyboard.current.fKey.wasPressedThisFrame;
                explorePressed = Keyboard.current.qKey.wasPressedThisFrame;
            }
            else
            {
                if (uiFastPressed) baseTargetSpeed = fastSpeed;
                else if (uiSlowPressed) baseTargetSpeed = slowSpeed;
                else if (uiReversePressed) baseTargetSpeed = reverseSpeed;
            }

            if (faroPressed) SetFaroMode(!isFaroMode);
            if (explorePressed) ToggleExploreMode();
            if (bellPressed) RingBell();
            if (jumpPressed) TriggerJump();

            if (stumbleTimer > 0f) stumbleTimer -= Time.deltaTime;

            float comboBoost = happyCombo >= 3 ? 1.18f : 1f;
            float thirstPenalty = waterLevel <= 1f ? 0.55f : 1f;
            float stumblePenalty = stumbleTimer > 0f ? 0.35f : 1f;
            float targetSpeed = baseTargetSpeed * slopeMultiplier * comboBoost * thirstPenalty * stumblePenalty;
            int pedalingLevel = PlayerProgressManager.Instance != null
                ? PlayerProgressManager.Instance.GetSkillLevel(PlayerSkillKind.Pedalada) : 0;
            if (slopeTiltDeg > 0f) targetSpeed *= 1f + pedalingLevel * 0.06f;
            if (isFaroMode || isOnFoot) targetSpeed = 0f;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            // Física vertical do salto (rampas ou pulo manual)
            if (jumpHeight > 0f || verticalVelocity > 0f)
            {
                verticalVelocity += jumpGravity * Time.deltaTime;
                jumpHeight += verticalVelocity * Time.deltaTime;
                if (jumpHeight <= 0f)
                {
                    jumpHeight = 0f;
                    verticalVelocity = 0f;
                }
            }

            Vector3 pos = transform.position;
            bool footRight = uiFastPressed || uiSlowPressed || (Keyboard.current != null
                && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed));
            bool footLeft = uiReversePressed || (Keyboard.current != null
                && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed));
            float moveX = isOnFoot ? ((footRight ? 1f : 0f) - (footLeft ? 1f : 0f)) * 2f : currentSpeed;
            float nextX = pos.x + moveX * Time.deltaTime;
            int explorationLevel = PlayerProgressManager.Instance != null
                ? PlayerProgressManager.Instance.GetSkillLevel(PlayerSkillKind.Exploracao) : 0;
            float walkRange = 7f + explorationLevel;
            pos.x = isOnFoot ? Mathf.Clamp(nextX, parkedBikeX - walkRange, parkedBikeX + walkRange)
                : (infiniteRoad ? Mathf.Max(minX, nextX) : Mathf.Clamp(nextX, minX, maxX));
            pos.y = baseGroundY + jumpHeight;
            if ((pos.x <= minX && currentSpeed < 0f) || (!infiniteRoad && pos.x >= maxX && currentSpeed > 0f))
                currentSpeed = 0f;
            transform.position = pos;

            if (distanceKm > maxDistanceKm) maxDistanceKm = distanceKm;

            // Vira o sprite se estiver voltando pela estrada
            if (currentSpeed > 0.25f || moveX > 0.1f) { facingDir = 1f; if (bikeSR != null) bikeSR.flipX = false; }
            else if (currentSpeed < -0.25f || moveX < -0.1f) { facingDir = -1f; if (bikeSR != null) bikeSR.flipX = true; }

            // Sede do cachorrinho + som ofegante quando água está baixa
            float drain = Mathf.Abs(currentSpeed) > 0.2f ? (Mathf.Abs(currentSpeed) > 5f ? 1.45f : 0.95f) : 0.25f;
            waterLevel = Mathf.Max(0f, waterLevel - waterDrainPerSecond * drain * Time.deltaTime);
            if (waterLevel < 25f && Time.time - lastThirstSoundTime > 7.5f)
            {
                lastThirstSoundTime = Time.time;
                FlorestaSoundFX.Instance?.PlayThirst();
            }

            UpdateVisualAnimation();
            if (walkingGirlSR != null)
            {
                walkingGirlSR.flipX = facingDir < 0f;
                walkingGirlSR.transform.localPosition = new Vector3(0f, 0.72f + (Mathf.Abs(moveX) > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.06f : 0f), 0f);
            }

            if (actionPressed)
            {
                if (digSiteRef != null && digSiteRef.isActive && digSiteRef.playerNearby && !digSiteRef.isCompleted)
                    digSiteRef.HelpDig();
                else if (!TryInteractNearbyEvent())
                    ThrowFood();
            }
        }

        public void RingBell()
        {
            FlorestaSoundFX.Instance?.PlayBell();
            if (trackGeneratorRef != null)
            {
                var animals = trackGeneratorRef.ActiveAnimals;
                for (int i = 0; i < animals.Count; i++)
                {
                    ForestAnimal a = animals[i];
                    if (a != null && Mathf.Abs(a.transform.position.x - transform.position.x) < 10f)
                        a.OnBellHeard();
                }
            }
            FlorestaSoundFX.Instance?.PlayDogBark();
            onInfoMessage?.Invoke("Trim-trim! A campainha chamou a atencao dos bichinhos por perto!");
        }

        public bool TryInteractNearbyEvent()
        {
            if (trackGeneratorRef == null) return false;
            var events = trackGeneratorRef.ActiveEvents;
            TrailAdventureEvent best = null;
            float bestD = 4.5f;
            for (int i = 0; i < events.Count; i++)
            {
                TrailAdventureEvent ev = events[i];
                if (ev == null || ev.isCompleted) continue;
                float d = Mathf.Abs(ev.transform.position.x - transform.position.x);
                if (d < bestD)
                {
                    bestD = d;
                    best = ev;
                }
            }

            if (best != null && best.TryInteract(this, out string msg))
            {
                onInfoMessage?.Invoke(msg);
                return true;
            }
            return false;
        }

        public void TriggerJump(float customVelocity = -1f)
        {
            if (jumpHeight > 0.12f) return; // Só pula estando no chão (ou quase tocando)
            verticalVelocity = customVelocity > 0f ? customVelocity : manualJumpVelocity;
            FlorestaSoundFX.Instance?.PlayJump();
        }

        private void UpdateVisualAnimation()
        {
            if (visualRoot == null) return;
            float absSpd = Mathf.Abs(currentSpeed);
            if (jumpHeight > 0.05f)
            {
                // Inclina o nariz da bicicleta suavemente no ar durante o salto
                float airTilt = Mathf.Clamp(verticalVelocity * 1.9f, -14f, 16f) * facingDir;
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.Euler(0f, 0f, airTilt), 10f * Time.deltaTime);
            }
            else if (absSpd > 0.15f)
            {
                float freq = absSpd > 4.5f ? 14f : 7.5f;
                float bob = Mathf.Abs(Mathf.Sin(Time.time * freq)) * 0.08f;
                float tilt = slopeTiltDeg * facingDir - Mathf.Clamp(currentSpeed / fastSpeed, -1f, 1f) * 2.5f;
                visualRoot.localPosition = new Vector3(0f, bob, 0f);
                visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.Euler(0f, 0f, tilt), 8f * Time.deltaTime);
            }
            else
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, Vector3.zero, 10f * Time.deltaTime);
                visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.Euler(0f, 0f, slopeTiltDeg * facingDir), 8f * Time.deltaTime);
            }
        }

        public void ThrowFood()
        {
            if (Time.time - lastThrowTime < 0.35f) return;

            if (foodBasket.Count == 0)
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                onInfoMessage?.Invoke("Cesta vazia! Passe na Barraca da Cozinha (rosa e branca) na estrada para pegar ou preparar lanches!");
                return;
            }

            lastThrowTime = Time.time;

            // Encontra o animal ou morador mais próximo dentro de 11 unidades (sem FindObjectsOfType!)
            ForestAnimal targetAnimal = null;
            float bestAnimalDist = 11f;
            if (trackGeneratorRef != null)
            {
                var animals = trackGeneratorRef.ActiveAnimals;
                for (int i = 0; i < animals.Count; i++)
                {
                    ForestAnimal a = animals[i];
                    if (a == null) continue;
                    float d = Mathf.Abs(a.transform.position.x - transform.position.x);
                    if (d < bestAnimalDist)
                    {
                        bestAnimalDist = d;
                        targetAnimal = a;
                    }
                }
            }

            VillageResident targetResident = null;
            float bestResidentDist = 11f;
            if (trackGeneratorRef != null)
            {
                var residents = trackGeneratorRef.ActiveResidents;
                for (int i = 0; i < residents.Count; i++)
                {
                    VillageResident r = residents[i];
                    if (r == null || r.isHelped) continue;
                    float d = Mathf.Abs(r.transform.position.x - transform.position.x);
                    if (d < bestResidentDist)
                    {
                        bestResidentDist = d;
                        targetResident = r;
                    }
                }
            }

            bool deliverToResident = targetResident != null && (targetAnimal == null || bestResidentDist < bestAnimalDist);

            int basketIdx = 0;
            if (deliverToResident)
            {
                int match = foodBasket.IndexOf(targetResident.wantedFood);
                if (match >= 0) basketIdx = match;
            }
            else if (targetAnimal != null && !targetAnimal.isFullySatisfied)
            {
                int match = foodBasket.IndexOf(targetAnimal.wantedFood);
                if (match >= 0) basketIdx = match;
            }

            FoodType chosen = foodBasket[basketIdx];
            foodBasket.RemoveAt(basketIdx);

            FlorestaSoundFX.Instance?.PlayThrow();
            Vector3 startPos = transform.position + new Vector3(facingDir * 0.9f, 1.8f, 0f);
            GameObject projObj = new GameObject("AlimentoArremessado");

            if (deliverToResident)
            {
                Vector3 endPos = targetResident.transform.position + new Vector3(0f, 1.2f, 0f);
                projObj.AddComponent<SideFoodProjectile>().LaunchToResident(chosen, startPos, endPos, targetResident, foodSprites[chosen], HandleResidentDelivery);
            }
            else
            {
                Vector3 endPos = targetAnimal != null
                    ? targetAnimal.transform.position + new Vector3(0f, 1.0f, 0f)
                    : transform.position + new Vector3(facingDir * 5.5f, 0.8f, 0f);
                projObj.AddComponent<SideFoodProjectile>().Launch(chosen, startPos, endPos, targetAnimal, foodSprites[chosen], HandleFoodArrived);
            }
        }

        private void HandleResidentDelivery(VillageResident resident, FoodType given)
        {
            VillageResident.DeliveryResult res = resident.ReceiveItem(given);
            if (res == VillageResident.DeliveryResult.Completed)
            {
                score += resident.pointReward;
                starsCollected += resident.starReward;
                PlayerProgressManager.Instance?.AddStars(resident.starReward);
                PlayerProgressManager.Instance?.RecordDelivery();
                happyCombo++;
                if (FoodInfo.NeedsPreparation(given) && gourmetBonusCharges > 0)
                {
                    gourmetBonusCharges--;
                    score += 50;
                }
                string musicExtra = resident.kind == VillageResident.ResidentKind.Musico ? " Ele tocou uma serenata especial!" : "";
                onInfoMessage?.Invoke($"Missao '{resident.missionTitle}' completa! {resident.residentName} agradeceu! +{resident.starReward} Estrelas e +{resident.pointReward} pts!{musicExtra}");
            }
            else if (res == VillageResident.DeliveryResult.WrongItem)
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                onInfoMessage?.Invoke($"{resident.residentName} queria {FoodInfo.GetDisplayName(resident.wantedFood)}, não {FoodInfo.GetDisplayName(given)}!");
            }
        }

        private void HandleFoodArrived(ForestAnimal animal, FoodType given)
        {
            ForestAnimal.FeedResult res = animal.ReceiveFood(given);
            switch (res)
            {
                case ForestAnimal.FeedResult.AteNeedsMore:
                    score += (animal.kind == ParkSpriteFactory.AnimalKind.Lobo) ? 120 : 80;
                    if (FoodInfo.NeedsPreparation(given) && gourmetBonusCharges > 0)
                    {
                        gourmetBonusCharges--;
                        score += 50;
                        onInfoMessage?.Invoke($"Bônus Gourmet! {FoodInfo.GetDisplayName(given)} artesanal! +50 pts extras!");
                    }
                    happyCombo++;
                    PlayerProgressManager.Instance?.RecordDelivery();
                    FlorestaSoundFX.Instance?.PlayHappy();
                    FlorestaSoundFX.Instance?.PlayDogBark();
                    break;
                case ForestAnimal.FeedResult.FullySatisfied:
                    score += (animal.kind == ParkSpriteFactory.AnimalKind.Lobo) ? 220
                           : (animal.kind == ParkSpriteFactory.AnimalKind.Gato ? 175 : 150);
                    if (FoodInfo.NeedsPreparation(given) && gourmetBonusCharges > 0)
                    {
                        gourmetBonusCharges--;
                        score += 50;
                        onInfoMessage?.Invoke($"Bônus Gourmet! {FoodInfo.GetDisplayName(given)} artesanal! +50 pts extras!");
                    }
                    happyCombo++;
                    PlayerProgressManager.Instance?.RecordDelivery();
                    PlayerProgressManager.Instance?.AddStars(1);
                    FlorestaSoundFX.Instance?.PlayHappy();
                    FlorestaSoundFX.Instance?.PlayDogBark();
                    break;
                case ForestAnimal.FeedResult.TriggeredSquirrelSecret:
                    score += 300;
                    happyCombo++;
                    PlayerProgressManager.Instance?.RecordDelivery(isSquirrelSecret: true);
                    FlorestaSoundFX.Instance?.PlayDiamond();
                    FlorestaSoundFX.Instance?.PlayDogBark();
                    break;
                case ForestAnimal.FeedResult.AlreadyFull:
                    score += 20;
                    FlorestaSoundFX.Instance?.PlayHappy();
                    break;
                case ForestAnimal.FeedResult.WolfStartled:
                    FlorestaSoundFX.Instance?.PlayWrong();
                    break;
                case ForestAnimal.FeedResult.WrongFood:
                    FlorestaSoundFX.Instance?.PlayWrong();
                    break;
            }
            onFoodDelivered?.Invoke(animal, given, res);
        }

        private void SpawnGirlTears()
        {
            for (int i = 0; i < 4; i++)
            {
                GameObject tear = new GameObject("LagrimaMenina");
                tear.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 2.3f, 0f);
                SpriteRenderer sr = tear.AddComponent<SpriteRenderer>();
                sr.sprite = tearSprite;
                sr.sortingOrder = 16;
                tear.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.3f, 0.9f), 0f),
                    gravity: -4.2f,
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
                slopeTiltDeg = slope.slopeType == SlopeZone.SlopeType.Subida ? 9f : -9f;
                if (slope.slopeType == SlopeZone.SlopeType.Descida)
                {
                    FlorestaSoundFX.Instance?.PlayDogBark();
                    onInfoMessage?.Invoke("Au-au! O cachorrinho esta adorando embalar na descida!");
                }
                return;
            }

            WaterColumn water = other.GetComponent<WaterColumn>();
            if (water != null)
            {
                RefillWater(water.transform.position);
                return;
            }

            if (other.GetComponent<KitchenStation>() != null)
            {
                isNearKitchen = true;
                return;
            }

            // Rampa / Lombada de salto!
            JumpRamp ramp = other.GetComponent<JumpRamp>();
            if (ramp != null && Mathf.Abs(currentSpeed) > 2.2f && ramp.CanTrigger())
            {
                float boostVel = ramp.launchVelocity + Mathf.Clamp(Mathf.Abs(currentSpeed) * 0.25f, 0f, 2.2f);
                TriggerJump(boostVel);
                onInfoMessage?.Invoke("Uhuu! Salto na rampa!");
                return;
            }

            // Estrela aérea coletável
            AerialCollectible star = other.GetComponent<AerialCollectible>();
            if (star != null && star.Collect())
            {
                starsCollected++;
                score += star.bonusPoints;
                PlayerProgressManager.Instance?.AddStars(1);
                FlorestaSoundFX.Instance?.PlayStar();
                onInfoMessage?.Invoke($"Estrela no ar coletada! +1 Estrela e +{star.bonusPoints} pts!");
                return;
            }

            // Colecionável natural da trilha (Etapa 7)
            TrailCollectibleItem natureItem = other.GetComponent<TrailCollectibleItem>();
            if (natureItem != null && natureItem.TryCollect())
            {
                score += natureItem.bonusPoints;
                PlayerProgressManager.Instance?.AddCollectible(natureItem.kind, 1);
                FlorestaSoundFX.Instance?.PlayStar();
                onInfoMessage?.Invoke($"Colecionável encontrado: {PlayerProgressManager.GetCollectibleDisplayName(natureItem.kind)}! +{natureItem.bonusPoints} pts");
                return;
            }

            // Obstáculo na trilha (Pedra, Tronco, Raiz)
            TrailObstacle obstacle = other.GetComponent<TrailObstacle>();
            if (obstacle != null)
            {
                if (jumpHeight >= 0.55f)
                {
                    if (obstacle.TryRewardJumpOver())
                    {
                        score += 15;
                        PlayerProgressManager.Instance?.RecordSkillProgress(PlayerSkillKind.Equilibrio);
                        FlorestaSoundFX.Instance?.PlayStar();
                        onInfoMessage?.Invoke($"Belo salto sobre o(a) {obstacle.DisplayName}! +15 pts");
                    }
                }
                else if (obstacle.TryHit())
                {
                    int balanceLevel = PlayerProgressManager.Instance != null
                        ? PlayerProgressManager.Instance.GetSkillLevel(PlayerSkillKind.Equilibrio) : 0;
                    stumbleTimer = 0.85f - balanceLevel * 0.12f;
                    currentSpeed *= 0.25f;
                    FlorestaSoundFX.Instance?.PlayBump();
                    if (balanceLevel < 3 && Mathf.Abs(currentSpeed) > 1.2f && foodBasket.Count > 1)
                    {
                        FoodType dropped = foodBasket[foodBasket.Count - 1];
                        foodBasket.RemoveAt(foodBasket.Count - 1);
                        onInfoMessage?.Invoke($"Ops! Tropeçou na(o) {obstacle.DisplayName} e caiu {FoodInfo.GetDisplayName(dropped)}! Aperte W / PULAR!");
                    }
                    else
                    {
                        onInfoMessage?.Invoke($"Ops! Tropeçou na(o) {obstacle.DisplayName}! Use W ou PULAR para saltar!");
                    }
                }
                return;
            }

            ForestAnimal animal = other.GetComponent<ForestAnimal>();
            if (animal != null)
            {
                // Só atropela se passar RÁPIDO por cima do animal! Passando devagar, respeita o bichinho.
                if (Mathf.Abs(currentSpeed) > 4.2f && animal.OnRunOver())
                {
                    score = Mathf.Max(0, score - 50);
                    happyCombo = 0;
                    FlorestaSoundFX.Instance?.PlayCry();
                    SpawnGirlTears();
                    onAnimalRunOver?.Invoke(animal);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            WaterColumn water = other.GetComponent<WaterColumn>();
            if (water != null && waterLevel < 98f) RefillWater(water.transform.position);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<SlopeZone>() != null)
            {
                slopeMultiplier = 1f;
                currentSlopeLabel = "Plano";
                slopeTiltDeg = 0f;
            }
            if (other.GetComponent<KitchenStation>() != null)
            {
                isNearKitchen = false;
            }
        }

        private void RefillWater(Vector3 pos)
        {
            if (waterLevel >= 99f) return;
            waterLevel = 100f;
            FlorestaSoundFX.Instance?.PlayWater();
            FlorestaSoundFX.Instance?.PlayDogBark();

            for (int i = 0; i < 6; i++)
            {
                GameObject drop = new GameObject("GotaAgua");
                drop.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), 1.0f, 0f);
                SpriteRenderer sr = drop.AddComponent<SpriteRenderer>();
                sr.sprite = tearSprite;
                sr.sortingOrder = 16;
                drop.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(1.0f, 2.2f), 0f),
                    gravity: -4.8f,
                    duration: 0.75f
                );
            }
            onWaterRefilled?.Invoke();
        }
    }
}
