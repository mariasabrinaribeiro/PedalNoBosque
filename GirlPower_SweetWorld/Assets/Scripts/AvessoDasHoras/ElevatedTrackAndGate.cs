using UnityEngine;

/// <summary>
/// Segundo nível da cena "O Avesso das Horas - A Corrida da Meia-Noite":
/// - Pista Elevada Estrelada atrás das casas
/// - Cancela Mágica compatível com o movimento gravado por Suri
/// - Vagão Doce que corre sozinho na pista elevada (contido pela cancela!)
/// - Eco do Tempo: o próprio carrinho das amigas passando lá embaixo acenando para cima!
/// </summary>
public sealed class ElevatedTrackAndGate : MonoBehaviour
{
    [Header("Cancela Mágica & Vagão")]
    public Transform barrierArmPivot;
    public SpriteRenderer barrierGlow;
    public Transform runawayWagon;

    [Header("Eco do Tempo (Pista Inferior)")]
    public Transform timeEchoCart;
    public SpriteRenderer timeEchoWaveIcon;

    public bool GateClosed { get; private set; }
    public bool WagonStopped { get; private set; }
    public bool TimeEchoTriggered { get; private set; }

    private float gateProgress;
    private float wagonSpeed = 2.6f;
    private float echoX = 11.5f;

    public void LowerBarrierBySuri()
    {
        if (GateClosed) return;
        GateClosed = true;
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
        if (barrierArmPivot != null)
            SweetConfettiBurst.Spawn(barrierArmPivot.position, 14, new Color(0.78f, 0.48f, 1f, 1f));
    }

    public void TriggerTimeEchoPass()
    {
        if (TimeEchoTriggered) return;
        TimeEchoTriggered = true;
        echoX = 10.5f;
        if (timeEchoCart != null)
        {
            timeEchoCart.gameObject.SetActive(true);
            timeEchoCart.position = new Vector3(echoX, -2.65f, 0f);
        }
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayPickup();
    }

    public void ResetElevated()
    {
        GateClosed = false;
        WagonStopped = false;
        TimeEchoTriggered = false;
        gateProgress = 0f;
        if (barrierArmPivot != null) barrierArmPivot.localRotation = Quaternion.Euler(0f, 0f, 76f);
        if (runawayWagon != null) runawayWagon.position = new Vector3(-8.5f, 2.55f, 0f);
        if (timeEchoCart != null) timeEchoCart.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Anima a cancela baixando com o mesmo movimento gravado por Suri!
        float target = GateClosed ? 1f : 0f;
        gateProgress = Mathf.MoveTowards(gateProgress, target, Time.deltaTime * 2.2f);
        if (barrierArmPivot != null)
            barrierArmPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(76f, 0f, gateProgress));

        if (barrierGlow != null)
        {
            float a = GateClosed ? 0.65f : (0.24f + 0.16f * Mathf.Sin(Time.time * 5f));
            barrierGlow.color = GateClosed ? new Color(0.46f, 0.94f, 0.66f, a) : new Color(0.80f, 0.50f, 1f, a);
        }

        // Vagão Doce correndo sozinho na pista elevada (para com segurança quando a cancela baixa!)
        if (runawayWagon != null && !WagonStopped)
        {
            Vector3 wp = runawayWagon.position;
            wp.x += wagonSpeed * Time.deltaTime;
            float barrierX = barrierArmPivot != null ? barrierArmPivot.position.x - 1.35f : 1.4f;

            if (GateClosed && gateProgress > 0.65f && wp.x >= barrierX && wp.x <= barrierX + 1.2f)
            {
                wp.x = barrierX;
                WagonStopped = true;
                if (SweetAudio.Instance != null) SweetAudio.Instance.PlayDelivery();
                SweetConfettiBurst.Spawn(runawayWagon.position + Vector3.up * 0.5f, 22, new Color(1f, 0.86f, 0.34f, 1f));
                if (AvessoTrioController.Instance != null)
                    AvessoTrioController.Instance.OnWagonSafelyStopped();
            }
            else if (wp.x > 11.2f)
            {
                wp.x = -11.0f; // Loop suave na pista elevada até a criança baixar a cancela!
            }

            wp.y = 2.55f + Mathf.Sin(Time.time * 10f) * 0.04f;
            runawayWagon.position = wp;
        }

        // Eco do Tempo: o próprio carrinho das amigas passa lá embaixo acenando para cima!
        if (TimeEchoTriggered && timeEchoCart != null && timeEchoCart.gameObject.activeSelf)
        {
            echoX -= 3.2f * Time.deltaTime;
            timeEchoCart.position = new Vector3(echoX, -2.65f + Mathf.Sin(Time.time * 11f) * 0.05f, 0f);
            if (timeEchoWaveIcon != null)
                timeEchoWaveIcon.transform.localPosition = new Vector3(0.35f, 1.85f + Mathf.Sin(Time.time * 7f) * 0.16f, 0f);
            if (echoX < -12.5f)
                echoX = 12.0f; // Continua o eco misterioso em ciclo calmo lá embaixo!
        }
    }
}
