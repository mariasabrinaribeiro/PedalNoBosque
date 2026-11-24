using UnityEngine;

public class ControladorJogo : MonoBehaviour
{
    public static ControladorJogo Instance { get; private set; }

    [Header("Canais de Áudio")]
    [SerializeField] private AudioSource audioFundo;
    [SerializeField] private AudioSource audioCarro;
    [SerializeField] private AudioSource audioGameOver;

    [Header("Balanceamento de Pontuação (Etapa 4)")]
    public const int PontosPorColeta = 25;
    public const int PontosPorEntrega = 100;
    public const int PontosPorInimigo = 50;
    public const int PontosPorPortaoVelocidade = 150;
    public const int PontosPorMissao = 250;
    public const int PontosPorChefe = 500;

    public int PontuacaoAtual { get; private set; }

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
        IniciarMusicaFundo();
    }

    public void AdicionarPontos(int valor)
    {
        if (valor <= 0) return;
        PontuacaoAtual += valor;
        if (DriverPro.Instance != null)
        {
            DriverPro.Instance.score = PontuacaoAtual;
        }
    }

    public void ResetarEstadoJogo()
    {
        PontuacaoAtual = 0;
        if (DriverPro.Instance != null)
        {
            DriverPro.Instance.score = 0;
            DriverPro.Instance.health = DriverPro.Instance.maxHealth;
            DriverPro.Instance.cargo = 0;
            DriverPro.Instance.magnetTimer = 0f;
            DriverPro.Instance.tripleShotTimer = 0f;
            DriverPro.Instance.girlPowerTimer = 0f;
            DriverPro.Instance.hasShield = false;
        }
    }

    public void IniciarMusicaFundo()
    {
        if (audioFundo != null)
        {
            audioFundo.loop = true;
            audioFundo.Play();
        }
    }

    public void PararMusicaFundo()
    {
        if (audioFundo != null && audioFundo.isPlaying)
            audioFundo.Stop();
    }

    public void TocarSomCarro()
    {
        if (audioCarro != null && !audioCarro.isPlaying)
        {
            audioCarro.loop = true;
            audioCarro.Play();
        }
    }

    public void PararSomCarro()
    {
        if (audioCarro != null && audioCarro.isPlaying)
            audioCarro.Stop();
    }

    public void GameOver()
    {
        PararMusicaFundo();
        PararSomCarro();
        if (audioGameOver != null)
        {
            audioGameOver.loop = false;
            audioGameOver.Play();
        }
        Debug.Log("[ControladorJogo] Game Over! Sons controlados com sucesso.");
    }

    public void PararTudo()
    {
        PararMusicaFundo();
        PararSomCarro();
        if (audioGameOver != null && audioGameOver.isPlaying)
            audioGameOver.Stop();
    }
}
