using UnityEngine;

public class ControladorJogo : MonoBehaviour
{
    // Singleton para acesso fácil de qualquer script
    public static ControladorJogo Instance { get; private set; }

    [Header("Canais de Áudio")]
    [SerializeField] private AudioSource audioFundo;       // Arraste 'Musica_Menu' aqui
    [SerializeField] private AudioSource audioCarro;       // Arraste 'Som do carro' aqui
    [SerializeField] private AudioSource audioGameOver;    // Arraste 'som game over' aqui

    // ─── Inicialização ───────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inicia a música de fundo automaticamente ao entrar na cena
        IniciarMusicaFundo();
    }

    // ─── Música de Fundo ─────────────────────────────────────────

    /// <summary>Toca a música de fundo em loop assim que a cena começa.</summary>
    public void IniciarMusicaFundo()
    {
        if (audioFundo != null)
        {
            audioFundo.loop = true;
            audioFundo.Play();
        }
    }

    /// <summary>Para a música de fundo (ex: ao pausar ou entrar em game over).</summary>
    public void PararMusicaFundo()
    {
        if (audioFundo != null && audioFundo.isPlaying)
            audioFundo.Stop();
    }

    // ─── Som do Carro ────────────────────────────────────────────

    /// <summary>Liga o som do carro (chame quando o carro começa a andar).</summary>
    public void TocarSomCarro()
    {
        if (audioCarro != null && !audioCarro.isPlaying)
        {
            audioCarro.loop = true;
            audioCarro.Play();
        }
    }

    /// <summary>Desliga o som do carro (chame quando parar de andar).</summary>
    public void PararSomCarro()
    {
        if (audioCarro != null && audioCarro.isPlaying)
            audioCarro.Stop();
    }

    // ─── Game Over ───────────────────────────────────────────────

    /// <summary>
    /// Chame este método quando o jogador perder/bater.
    /// Para toda a música e toca o som de game over.
    /// </summary>
    public void GameOver()
    {
        // Para a música de fundo
        PararMusicaFundo();

        // Para o som do carro
        PararSomCarro();

        // Toca o game over uma única vez
        if (audioGameOver != null)
        {
            audioGameOver.loop = false;
            audioGameOver.Play();
        }

        Debug.Log("[ControladorJogo] Game Over! Sons controlados com sucesso.");
    }

    // ─── Parar Tudo ──────────────────────────────────────────────

    /// <summary>Para todos os sons (útil ao pausar o jogo).</summary>
    public void PararTudo()
    {
        PararMusicaFundo();
        PararSomCarro();
        if (audioGameOver != null && audioGameOver.isPlaying)
            audioGameOver.Stop();
    }
}
