using UnityEngine;
using UnityEngine.InputSystem;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Controla a menina com o cachorrinho na cesta pedalando devagar e rápido pela pista da floresta,
    /// parando para alimentar os animais.
    /// </summary>
    public class BikeGirlController : MonoBehaviour
    {
        [Header("Velocidades")]
        public float slowSpeed = 3.2f;
        public float fastSpeed = 7.2f;
        public float reverseSpeed = -2.4f;
        public float acceleration = 10f;

        [Header("Limites da Pista")]
        public float minX = -4f;
        public float maxX = 115f;

        public float currentSpeed { get; private set; } = 0f;
        public string speedStateLabel { get; private set; } = "Parada";
        public ForestAnimal nearbyHungryAnimal { get; private set; }

        private Transform visualRoot;
        private Transform frontWheel;
        private Transform rearWheel;
        private float stopNearAnimalTimer = 0f;

        // Inputs de botões na tela (Mobile / Mouse)
        private bool uiSlowPressed = false;
        private bool uiFastPressed = false;
        private bool uiReversePressed = false;

        private System.Action<ForestAnimal> onAnimalFedCallback;

        public void Setup(System.Action<ForestAnimal> onAnimalFed)
        {
            onAnimalFedCallback = onAnimalFed;

            visualRoot = new GameObject("BikeVisual").transform;
            visualRoot.SetParent(transform);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localScale = new Vector3(1.3f, 1.3f, 1f);

            SpriteRenderer bikeSR = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            bikeSR.sprite = FlorestaSpriteFactory.CreateBikeGirlWithPuppySprite();
            bikeSR.sortingOrder = 10;

            // Rodas separadas para girar conforme a pedalada
            Sprite wheelSprite = FlorestaSpriteFactory.CreateWheelSprite();

            // Na textura 96x88 com PPU=32 e pivot (0.5, 0):
            // Centro X = 48 -> roda traseira x=24 => -24/32 = -0.75; roda dianteira x=68 => +20/32 = +0.625; y=16 => 16/32 = +0.5
            rearWheel = CreateWheel("RearWheel", wheelSprite, new Vector3(-0.75f, 0.5f, 0f));
            frontWheel = CreateWheel("FrontWheel", wheelSprite, new Vector3(0.625f, 0.5f, 0f));

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2.2f);
            col.offset = new Vector2(0f, 1.1f);

            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        private Transform CreateWheel(string objName, Sprite sprite, Vector3 localPos)
        {
            GameObject w = new GameObject(objName);
            w.transform.SetParent(visualRoot);
            w.transform.localPosition = localPos;
            SpriteRenderer sr = w.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 11;
            return w.transform;
        }

        public void SetUISlow(bool pressed) => uiSlowPressed = pressed;
        public void SetUIFast(bool pressed) => uiFastPressed = pressed;
        public void SetUIReverse(bool pressed) => uiReversePressed = pressed;

        private void Update()
        {
            float targetSpeed = 0f;
            bool feedKeyPressed = false;

            if (Keyboard.current != null)
            {
                bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                bool up = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
                bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;

                if (uiFastPressed || up || (right && shift))
                {
                    targetSpeed = fastSpeed;
                }
                else if (uiSlowPressed || right)
                {
                    targetSpeed = slowSpeed;
                }
                else if (uiReversePressed || left)
                {
                    targetSpeed = reverseSpeed;
                }

                feedKeyPressed = Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
            }
            else
            {
                if (uiFastPressed) targetSpeed = fastSpeed;
                else if (uiSlowPressed) targetSpeed = slowSpeed;
                else if (uiReversePressed) targetSpeed = reverseSpeed;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + currentSpeed * Time.deltaTime, minX, maxX);
            if ((pos.x <= minX && currentSpeed < 0f) || (pos.x >= maxX && currentSpeed > 0f))
            {
                currentSpeed = 0f;
            }
            transform.position = pos;

            UpdateVisualAnimation();
            UpdateSpeedLabel();
            HandleFeeding(feedKeyPressed);
        }

        private void UpdateVisualAnimation()
        {
            // Gira as rodas de acordo com a velocidade
            float wheelDeg = -currentSpeed * 180f * Time.deltaTime;
            if (frontWheel != null) frontWheel.Rotate(0f, 0f, wheelDeg);
            if (rearWheel != null) rearWheel.Rotate(0f, 0f, wheelDeg);

            // Leve balanço de pedalada no quadro + menina + cachorrinho
            if (visualRoot != null)
            {
                if (Mathf.Abs(currentSpeed) > 0.15f)
                {
                    float freq = Mathf.Abs(currentSpeed) > 4.5f ? 14f : 7f;
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * freq)) * 0.06f;
                    float tilt = -Mathf.Clamp(currentSpeed / fastSpeed, -1f, 1f) * 2.5f;
                    visualRoot.localPosition = new Vector3(0f, bob, 0f);
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
                }
                else
                {
                    visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, Vector3.zero, 10f * Time.deltaTime);
                    visualRoot.localRotation = Quaternion.Lerp(visualRoot.localRotation, Quaternion.identity, 10f * Time.deltaTime);
                }
            }
        }

        private void UpdateSpeedLabel()
        {
            float absSpeed = Mathf.Abs(currentSpeed);
            if (absSpeed < 0.25f)
                speedStateLabel = "Parada";
            else if (currentSpeed < 0f)
                speedStateLabel = "Freando / Ré";
            else if (absSpeed > 4.8f)
                speedStateLabel = "Pedalando Rápido!";
            else
                speedStateLabel = "Pedalando Devagar";
        }

        private void HandleFeeding(bool feedKeyPressed)
        {
            if (nearbyHungryAnimal == null || nearbyHungryAnimal.isFed)
            {
                nearbyHungryAnimal = null;
                stopNearAnimalTimer = 0f;
                return;
            }

            if (feedKeyPressed)
            {
                TriggerFeed();
                return;
            }

            // Se parar completamente perto do animal por 0.6s, alimenta automaticamente também!
            if (Mathf.Abs(currentSpeed) < 0.2f)
            {
                stopNearAnimalTimer += Time.deltaTime;
                if (stopNearAnimalTimer >= 0.6f)
                {
                    TriggerFeed();
                }
            }
            else
            {
                stopNearAnimalTimer = 0f;
            }
        }

        public void TriggerFeed()
        {
            if (nearbyHungryAnimal == null || nearbyHungryAnimal.isFed) return;
            ForestAnimal fed = nearbyHungryAnimal;
            nearbyHungryAnimal = null;
            stopNearAnimalTimer = 0f;
            onAnimalFedCallback?.Invoke(fed);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ForestAnimal animal = other.GetComponent<ForestAnimal>();
            if (animal != null && !animal.isFed)
            {
                nearbyHungryAnimal = animal;
                stopNearAnimalTimer = 0f;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            ForestAnimal animal = other.GetComponent<ForestAnimal>();
            if (animal != null && animal == nearbyHungryAnimal)
            {
                nearbyHungryAnimal = null;
                stopNearAnimalTimer = 0f;
            }
        }
    }
}
