using UnityEngine;

public class AmbientGlitterManager : MonoBehaviour
{
    // O ambiente glitter foi desativado para deixar o jogo mais leve no mobile.
    void Start()
    {
        // Todo o código de criação do ParticleSystem foi removido/comentado.
        Debug.Log("AmbientGlitterManager: Sistema de partículas desativado para ganho de desempenho.");
    }
}
