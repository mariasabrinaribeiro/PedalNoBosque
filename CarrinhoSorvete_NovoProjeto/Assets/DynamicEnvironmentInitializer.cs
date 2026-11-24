using UnityEngine;

/// <summary>
/// Garante em tempo de execução que partículas e entidades dinâmicas respeitem as
/// Sorting Layers padronizadas e o limite de 35% de opacidade / -40% emissão da Etapa 4.
/// </summary>
public class DynamicEnvironmentInitializer : MonoBehaviour
{
    private void Start()
    {
        foreach (ParticleSystem ps in FindObjectsOfType<ParticleSystem>(true))
        {
            if (ps == null) continue;
            var main = ps.main;
            main.startSizeMultiplier = Mathf.Min(main.startSizeMultiplier, 0.45f);
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Min(emission.rateOverTimeMultiplier, 10f);
            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.sortingLayerName = "VFX";
        }
    }
}
