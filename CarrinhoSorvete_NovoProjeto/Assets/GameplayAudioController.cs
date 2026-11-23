using UnityEngine;

public class GameplayAudioController : MonoBehaviour
{
    [Header("Configurações de Áudio de Gameplay")]
    [Tooltip("O nome do evento de música de fundo configurado no AudioManager.")]
    [SerializeField] private string gameplayMusicEventName = "MusicaGameplay";

    private void Start()
    {
        // Toca a música de fundo em loop assim que a cena inicia
        if (AudioManager.Instance != null)
        {
            AudioManager.PlayMusic(gameplayMusicEventName);
        }
        else
        {
            Debug.LogWarning("[GameplayAudioController] AudioManager.Instance não foi encontrado na cena.");
        }
    }
}
