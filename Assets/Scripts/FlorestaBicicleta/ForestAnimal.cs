using System.Collections;
using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Representa um animal da floresta que espera ser alimentado pela menina da bicicleta.
    /// </summary>
    public class ForestAnimal : MonoBehaviour
    {
        public string animalName;
        public FlorestaSpriteFactory.AnimalKind kind;
        public bool isFed { get; private set; } = false;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer statusIconRenderer;
        private Transform statusIconTransform;
        private Vector3 basePosition;
        private float bobPhase;

        public void Setup(FlorestaSpriteFactory.AnimalKind animalKind, string displayName, Sprite foodSprite)
        {
            kind = animalKind;
            animalName = displayName;
            basePosition = transform.position;
            bobPhase = Random.Range(0f, Mathf.PI * 2f);

            bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = FlorestaSpriteFactory.CreateAnimalSprite(kind);
            bodyRenderer.sortingOrder = 6;

            // Virado para a esquerda (olhando na direção da menina chegando)
            bodyRenderer.flipX = true;
            transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            // Balãozinho de fome acima do animal
            GameObject iconObj = new GameObject("StatusIcon");
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            statusIconTransform = iconObj.transform;

            statusIconRenderer = iconObj.AddComponent<SpriteRenderer>();
            statusIconRenderer.sprite = foodSprite;
            statusIconRenderer.sortingOrder = 8;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.2f, 2.5f);
            col.offset = new Vector2(0f, 0.8f);
        }

        private void Update()
        {
            if (statusIconTransform != null)
            {
                float hover = Mathf.Sin(Time.time * 3.5f + bobPhase) * 0.1f;
                statusIconTransform.localPosition = new Vector3(0f, 1.95f + hover, 0f);
            }
        }

        public void Feed(Sprite heartSprite)
        {
            if (isFed) return;
            isFed = true;

            if (statusIconRenderer != null && heartSprite != null)
            {
                statusIconRenderer.sprite = heartSprite;
            }

            StartCoroutine(HappyHopRoutine());
        }

        private IEnumerator HappyHopRoutine()
        {
            for (int hop = 0; hop < 2; hop++)
            {
                float elapsed = 0f;
                float duration = 0.28f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float yOffset = Mathf.Sin(t * Mathf.PI) * 0.45f;
                    transform.position = basePosition + new Vector3(0f, yOffset, 0f);
                    yield return null;
                }
            }
            transform.position = basePosition;
        }
    }
}
