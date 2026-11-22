using System.Collections;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Evento secreto final na cena lateral: quando o Esquilo recebe todas as 6 porções,
    /// ele corre para o monte de terra fofa e cava fundo; a menina ajuda a cavar e brotam vários diamantes!
    /// </summary>
    public class SideDiamondDigSite : MonoBehaviour
    {
        public bool isActive { get; private set; } = false;
        public bool isCompleted { get; private set; } = false;
        public bool playerNearby { get; private set; } = false;
        public float digProgress { get; private set; } = 0f;

        private Sprite dirtParticleSprite;
        private Sprite diamondSprite;
        private System.Action onDiamondsFound;

        public void Setup(System.Action onVictory)
        {
            onDiamondsFound = onVictory;
            dirtParticleSprite = ParkSpriteFactory.CreateDirtClodSprite();
            diamondSprite = ParkSpriteFactory.CreateDiamondSprite();

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ParkSpriteFactory.CreateDirtMoundSprite();
            sr.sortingOrder = 4;
            transform.localScale = new Vector3(1.1f, 0.7f, 1f);

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(4.5f, 3.5f);

            gameObject.SetActive(false);
        }

        public void ActivateDigging(ForestAnimal squirrel)
        {
            isActive = true;
            isCompleted = false;
            digProgress = 0f;

            if (squirrel != null)
            {
                transform.position = new Vector3(squirrel.transform.position.x + 2.4f, -2.45f, 0f);
                squirrel.MoveToDigSpot(transform.position + new Vector3(1.3f, -0.2f, 0f));
            }

            gameObject.SetActive(true);
        }

        public void HelpDig()
        {
            if (!isActive || isCompleted) return;

            digProgress = Mathf.Min(100f, digProgress + 18f);
            FlorestaSoundFX.Instance?.PlayDig();

            for (int i = 0; i < 6; i++)
            {
                GameObject clod = new GameObject("TerraVoando");
                clod.transform.position = transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.2f, 0f);
                SpriteRenderer sr = clod.AddComponent<SpriteRenderer>();
                sr.sprite = dirtParticleSprite;
                sr.sortingOrder = 17;
                clod.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(2.0f, 3.8f), 0f),
                    gravity: -6.8f,
                    duration: 0.75f
                );
            }

            float sc = 1.1f + (digProgress / 100f) * 0.45f;
            transform.localScale = new Vector3(sc, sc * 0.65f, 1f);

            if (digProgress >= 100f)
            {
                isCompleted = true;
                StartCoroutine(BurstDiamondsRoutine());
            }
        }

        private IEnumerator BurstDiamondsRoutine()
        {
            FlorestaSoundFX.Instance?.PlayDiamond();
            for (int wave = 0; wave < 3; wave++)
            {
                for (int i = 0; i < 6; i++)
                {
                    GameObject dia = new GameObject("Diamante");
                    dia.transform.position = transform.position + new Vector3(0f, 0.3f, 0f);
                    float sc = Random.Range(0.8f, 1.3f);
                    dia.transform.localScale = new Vector3(sc, sc, 1f);
                    SpriteRenderer sr = dia.AddComponent<SpriteRenderer>();
                    sr.sprite = diamondSprite;
                    sr.sortingOrder = 20;
                    dia.AddComponent<FallingParticle>().Init(
                        new Vector3(Random.Range(-3.2f, 3.2f), Random.Range(2.8f, 5.2f), 0f),
                        gravity: -4.4f,
                        duration: 1.45f
                    );
                }
                yield return new WaitForSeconds(0.28f);
            }
            onDiamondsFound?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<BikeGirlController>() != null) playerNearby = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<BikeGirlController>() != null) playerNearby = false;
        }
    }
}
