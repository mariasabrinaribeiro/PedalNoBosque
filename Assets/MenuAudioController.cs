using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuAudioController : MonoBehaviour
{
    [Header("Configurações de Transição")]
    [Tooltip("O nome da cena de gameplay a ser carregada após o fade out terminar.")]
    [SerializeField] private string gameplaySceneName = "aula 3";

    [Header("Configurações de Áudio")]
    [Tooltip("O nome do evento de música configurado no AudioManager.")]
    [SerializeField] private string menuMusicEventName = "MusicaMenu80s";

    [Tooltip("Duração do fade out da música em segundos.")]
    [Range(0.1f, 5f)]
    [SerializeField] private float fadeDuration = 1.2f;

    private bool isTransitioning = false;

    private void Start()
    {
        // Toca a música em loop assim que a cena do menu inicia
        if (AudioManager.Instance != null)
        {
            AudioManager.PlayMusic(menuMusicEventName);
        }
        else
        {
            Debug.LogWarning("[MenuAudioController] AudioManager.Instance não foi encontrado na cena.");
        }
    }

    // Essa função será chamada no evento do botão "Jogar" do seu Canvas
    public void AoClicarEmJogar()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (AudioManager.Instance != null)
        {
            // Inicia o fade out suave e carrega a cena de gameplay ao finalizar
            AudioManager.FadeOutMusic(fadeDuration, CarregarCenaGameplay);
        }
        else
        {
            // Caso o AudioManager não exista na cena, carrega a gameplay imediatamente
            CarregarCenaGameplay();
        }
    }

    private void CarregarCenaGameplay()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}
