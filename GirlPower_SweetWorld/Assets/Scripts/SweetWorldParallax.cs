using UnityEngine;

/// <summary>
/// Sistema de paralaxe 2.5D em 4 camadas idêntico ao universo da capa Girl Power:
/// - Camada 0: Céu estrelado galáxia roxa com lua crescente e montanhas listradas de bengala doce
/// - Camada 1: Colinas doces, árvores de algodão-doce rosa/azul e cupcakes gigantes
/// - Camada 2: Estrada de creme dourado com balas de goma arco-íris e confeitos
/// - Camada 3: Pirulitos/cupcakes de primeiro plano passando rapidamente na borda inferior
/// </summary>
public sealed class SweetWorldParallax : MonoBehaviour
{
    [System.Serializable]
    public struct ParallaxBand
    {
        public Transform tileA;
        public Transform tileB;
        public float tileWidth;
        public float speedMultiplier;
    }

    public ParallaxBand[] bands;
    public Transform[] twinklingStars;
    public float baseScrollSpeed = 6.5f;

    private void Update()
    {
        float worldSpeed = SweetWorldGameManager.Instance != null
            ? SweetWorldGameManager.Instance.CurrentScrollSpeed
            : baseScrollSpeed;

        if (bands != null)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                var b = bands[i];
                if (b.tileA == null || b.tileB == null || b.tileWidth <= 0.1f) continue;

                float dx = worldSpeed * b.speedMultiplier * Time.deltaTime;
                b.tileA.position += Vector3.left * dx;
                b.tileB.position += Vector3.left * dx;

                if (b.tileA.position.x <= -b.tileWidth)
                    b.tileA.position = new Vector3(b.tileB.position.x + b.tileWidth, b.tileA.position.y, b.tileA.position.z);
                if (b.tileB.position.x <= -b.tileWidth)
                    b.tileB.position = new Vector3(b.tileA.position.x + b.tileWidth, b.tileB.position.y, b.tileB.position.z);
            }
        }

        if (twinklingStars != null)
        {
            float t = Time.time;
            for (int i = 0; i < twinklingStars.Length; i++)
            {
                Transform st = twinklingStars[i];
                if (st == null) continue;
                float s = 0.65f + 0.45f * Mathf.Sin(t * 2.8f + i * 1.3f);
                st.localScale = Vector3.one * (0.28f * s);
            }
        }
    }
}
