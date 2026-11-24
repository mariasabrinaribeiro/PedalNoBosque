using UnityEngine;

/// <summary>
/// Pirulito Gigante Jogável de "O Avesso das Horas - A Corrida da Meia-Noite":
/// - Objeto independente com posição fixa no mundo e colisão própria (fora do parallax!)
/// - Na 3ª passagem, gira sozinho antes do carrinho chegar como se já conhecesse a rota.
/// - Nara pousa na engrenagem da base para baixar a haste e transformá-la em rampa brilhante.
/// - Suri grava o movimento de descida da haste e pode repeti-lo quando Íria se aproxima!
/// </summary>
public sealed class LollipopRampMechanism : MonoBehaviour
{
    public Transform stemPivot;
    public Transform swirlHead;
    public Transform baseGear;
    public SpriteRenderer rampGlow;
    public SpriteRenderer gearHighlight;

    [Header("Ângulos da Haste")]
    public float uprightAngle = 0f;
    public float loweredRampAngle = 68f; // Inclina para a esquerda criando rampa de subida para a pista elevada!

    public float LowerProgress { get; private set; } // 0 = em pé, 1 = rampa abaixada pronta para salto!
    public bool IsRampReady => LowerProgress >= 0.72f;
    public bool HasPreSpun { get; private set; }
    public bool NaraWorkingHere { get; private set; }

    private float targetLower;
    private float suriReplayTimer;
    private float mysterySpinSpeed = 28f;

    public Vector3 GearWorldPosition => baseGear != null ? baseGear.position : (transform.position + new Vector3(0.45f, -0.85f, 0f));

    public void TriggerMysteryPreSpin()
    {
        if (HasPreSpun) return;
        HasPreSpun = true;
        mysterySpinSpeed = 280f;
        SweetConfettiBurst.Spawn(swirlHead != null ? swirlHead.position : transform.position, 14, new Color(1f, 0.48f, 0.92f, 1f));
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
    }

    public void SetNaraWorking(bool working)
    {
        NaraWorkingHere = working;
        if (working)
        {
            targetLower = 1f;
            if (SweetAudio.Instance != null) SweetAudio.Instance.PlayPickup();
            SweetConfettiBurst.Spawn(GearWorldPosition, 10, new Color(0.40f, 0.94f, 0.90f, 1f));
        }
    }

    public void TriggerSuriReplay(float holdDuration = 5.5f)
    {
        suriReplayTimer = holdDuration;
        targetLower = 1f;
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
        SweetConfettiBurst.Spawn(transform.position + Vector3.up * 0.5f, 12, new Color(0.76f, 0.48f, 1f, 1f));
    }

    public void ResetMechanism()
    {
        LowerProgress = 0f;
        targetLower = 0f;
        suriReplayTimer = 0f;
        NaraWorkingHere = false;
        HasPreSpun = false;
        mysterySpinSpeed = 28f;
    }

    private void Update()
    {
        if (suriReplayTimer > 0f)
        {
            suriReplayTimer -= Time.deltaTime;
            targetLower = 1f;
        }
        else if (!NaraWorkingHere)
        {
            // Se ninguém estiver segurando nem reproduzindo, volta devagar para a posição vertical
            targetLower = Mathf.MoveTowards(targetLower, 0f, Time.deltaTime * 0.14f);
        }

        LowerProgress = Mathf.MoveTowards(LowerProgress, targetLower, Time.deltaTime * 1.45f);

        if (stemPivot != null)
        {
            float ang = Mathf.Lerp(uprightAngle, loweredRampAngle, LowerProgress);
            stemPivot.localRotation = Quaternion.Euler(0f, 0f, ang);
        }

        if (swirlHead != null)
        {
            float spin = HasPreSpun ? Mathf.Lerp(mysterySpinSpeed, 95f, Time.deltaTime * 0.6f) : 22f;
            mysterySpinSpeed = spin;
            swirlHead.Rotate(0f, 0f, spin * Time.deltaTime);
        }

        if (baseGear != null)
        {
            float gSpin = (NaraWorkingHere || suriReplayTimer > 0f) ? 180f : 20f;
            baseGear.Rotate(0f, 0f, gSpin * Time.deltaTime);
        }

        if (rampGlow != null)
        {
            float a = IsRampReady ? (0.48f + 0.25f * Mathf.Sin(Time.time * 8f)) : 0.06f;
            rampGlow.color = new Color(1f, 0.86f, 0.34f, a);
        }

        if (gearHighlight != null)
        {
            float ga = NaraWorkingHere ? 0.65f : (0.28f + 0.20f * Mathf.Sin(Time.time * 5f));
            gearHighlight.color = NaraWorkingHere
                ? new Color(0.35f, 0.96f, 0.86f, ga)
                : new Color(1f, 0.82f, 0.35f, ga);
        }
    }
}
