using System.Collections;
using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Evento secreto final: quando o Esquilo recebe todo o alimento que queria,
    /// ele cava a terra fundo, a menina ajuda a cavar e encontram vários diamantes para vencer o jogo!
    /// </summary>
    public class DiamondDigSite : MonoBehaviour
    {
        public bool isActive { get; private set; } = false;
        public bool isCompleted { get; private set; } = false;
        public bool playerNearby { get; private set; } = false;
        public float digProgress { get; private set; } = 0f; // 0 a 100

        private SpriteRenderer moundSR;
        private Sprite dirtParticleSprite;
        private Sprite diamondSprite;
        private System.Action onDiamondsFound;

        public void Setup(System.Action onVictory)
        {
            onDiamondsFound = onVictory;
            dirtParticleSprite = ParkSpriteFactory.CreateDirtClodSprite();
            diamondSprite = ParkSpriteFactory.CreateDiamondSprite();

            moundSR = gameObject.AddComponent<SpriteRenderer>();
            moundSR.sprite = ParkSpriteFactory.CreateDirtMoundSprite();
            moundSR.sortingOrder = 3;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 3.5f;

            gameObject.SetActive(false);
        }

        public void ActivateDigging(ParkAnimal squirrel)
        {
            isActive = true;
            isCompleted = false;
            digProgress = 0f;
            gameObject.SetActive(true);

            if (squirrel != null)
            {
                squirrel.MoveToDigSpot(transform.position + new Vector3(-1.3f, 0.2f, 0f));
            }
        }

        public void HelpDig()
        {
            if (!isActive || isCompleted) return;

            digProgress = Mathf.Min(100f, digProgress + 18f);
            ParkSoundFX.Instance?.PlayDig();

            // Joga terra voando para o alto
            for (int i = 0; i < 5; i++)
            {
                GameObject clod = new GameObject("TerraVoando");
                clod.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f), 0f);
                SpriteRenderer sr = clod.AddComponent<SpriteRenderer>();
                sr.sprite = dirtParticleSprite;
                sr.sortingOrder = 17;
                clod.AddComponent<FallingParticle>().Init(
                    new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(1.8f, 3.6f), 0f),
                    gravity: -6.5f,
                    duration: 0.7f
                );
            }

            // Aumenta o buraco conforme cavam mais fundo
            float scale = 1f + (digProgress / 100f) * 0.45f;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (digProgress >= 100f)
            {
                isCompleted = true;
                StartCoroutine(BurstDiamondsRoutine());
            }
        }

        private IEnumerator BurstDiamondsRoutine()
        {
            ParkSoundFX.Instance?.PlayDiamond();

            // Vários diamantes brilhantes saltam do fundo da terra!
            for (int wave = 0; wave < 3; wave++)
            {
                for (int i = 0; i < 6; i++)
                {
                    GameObject dia = new GameObject("Diamante");
                    dia.transform.position = transform.position;
                    float sc = Random.Range(0.75f, 1.25f);
                    dia.transform.localScale = new Vector3(sc, sc, 1f);
                    SpriteRenderer sr = dia.AddComponent<SpriteRenderer>();
                    sr.sprite = diamondSprite;
                    sr.sortingOrder = 20;
                    dia.AddComponent<FallingParticle>().Init(
                        new Vector3(Random.Range(-3.2f, 3.2f), Random.Range(2.5f, 5.0f), 0f),
                        gravity: -4.2f,
                        duration: 1.4f
                    );
                }
                yield return new WaitForSeconds(0.28f);
            }

            onDiamondsFound?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<TopDownBikeController>() != null)
                playerNearby = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<TopDownBikeController>() != null)
                playerNearby = false;
        }
    }
}
