using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Dá vida ao conjunto Rio + Ponte de Madeira no VALE DAS ÁGUAS (sem alocações por quadro):
    /// - Correnteza cintilante animada na superfície da água;
    /// - Peixe em pool que salta em arco suavemente sobre o rio periodicamente;
    /// - Balanço sutil na madeira da ponte quando a bicicleta cruza.
    /// </summary>
    public class LivingRiverFeature : MonoBehaviour
    {
        private Transform shimmerTr;
        private Transform bridgeTr;
        private Vector3 shimmerBasePos;
        private Vector3 bridgeBasePos;

        // Pool de peixe (criado uma vez no Setup, reutilizado sem Instantiate/Destroy)
        private GameObject pooledFish;
        private SpriteRenderer fishSR;
        private bool fishJumping = false;
        private float fishElapsed = 0f;
        private float fishDuration = 0.90f;
        private float fishStartX = 0f;
        private float fishDir = 1f;
        private float fishCooldown;

        private float bridgeSway = 0f;

        public void Setup(Transform waterShimmerTransform, Transform bridgeTransform)
        {
            shimmerTr = waterShimmerTransform;
            bridgeTr = bridgeTransform;
            if (shimmerTr != null) shimmerBasePos = shimmerTr.localPosition;
            if (bridgeTr != null) bridgeBasePos = bridgeTr.localPosition;

            // Pré-aloca o peixe no pool
            pooledFish = new GameObject("PeixeRio_Pool");
            pooledFish.transform.SetParent(transform, false);
            fishSR = pooledFish.AddComponent<SpriteRenderer>();
            fishSR.sprite = BiomePropsFactory.CreateFishSprite();
            fishSR.sortingOrder = 5;
            pooledFish.SetActive(false);

            fishCooldown = Random.Range(1.4f, 3.2f);

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(8.5f, 2.2f);
            col.offset = new Vector2(0f, 0.2f);
        }

        private void Update()
        {
            // Movimento suave da correnteza e reflexos da água
            if (shimmerTr != null)
            {
                float drift = Mathf.Sin(Time.time * 1.9f) * 0.36f;
                float bob = Mathf.Cos(Time.time * 2.8f) * 0.035f;
                shimmerTr.localPosition = shimmerBasePos + new Vector3(drift, bob, 0f);
            }

            // Balanço sutil da ponte quando a bicicleta cruza
            if (bridgeTr != null && bridgeSway > 0.001f)
            {
                bridgeSway = Mathf.Lerp(bridgeSway, 0f, 4.5f * Time.deltaTime);
                float dip = Mathf.Sin(Time.time * 15f) * bridgeSway;
                bridgeTr.localPosition = bridgeBasePos + new Vector3(0f, dip, 0f);
            }

            // Ciclo do peixe em pool (zero GC)
            if (fishJumping)
            {
                fishElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fishElapsed / fishDuration);
                float x = fishStartX + fishDir * t * 1.95f;
                float y = -0.80f + Mathf.Sin(t * Mathf.PI) * 1.15f;
                pooledFish.transform.localPosition = new Vector3(x, y, 0f);
                pooledFish.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Cos(t * Mathf.PI) * 40f * fishDir);

                if (t >= 1f)
                {
                    fishJumping = false;
                    pooledFish.SetActive(false);
                    fishCooldown = Random.Range(3.2f, 5.8f);
                }
            }
            else
            {
                fishCooldown -= Time.deltaTime;
                if (fishCooldown <= 0f)
                {
                    fishJumping = true;
                    fishElapsed = 0f;
                    fishDir = Random.value > 0.5f ? 1f : -1f;
                    fishStartX = Random.Range(-3.0f, 3.0f);
                    fishSR.flipX = fishDir < 0f;
                    pooledFish.SetActive(true);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponent<BikeGirlController>() != null)
            {
                bridgeSway = 0.04f;
            }
        }
    }
}
