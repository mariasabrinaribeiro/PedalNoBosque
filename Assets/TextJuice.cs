using UnityEngine;
using TMPro;

public class TextJuice : MonoBehaviour
{
    private TMP_Text textComponent;
    private string lastText;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isAnimating = false;
    private float lerpTime = 0f;
    [SerializeField] private float animationSpeed = 5.0f; // how fast it shrinks back

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        originalScale = transform.localScale;
        targetScale = originalScale * 1.35f; // bump by 35%

        if (textComponent != null)
        {
            lastText = textComponent.text;
        }
    }

    void Update()
    {
        if (textComponent == null) return;

        // Automatically trigger scale bump when text changes
        if (textComponent.text != lastText)
        {
            lastText = textComponent.text;
            TriggerBump();
        }

        if (isAnimating)
        {
            lerpTime += Time.deltaTime * animationSpeed;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, lerpTime);

            if (lerpTime >= 1.0f)
            {
                isAnimating = false;
                transform.localScale = originalScale;
            }
        }
    }

    public void TriggerBump()
    {
        isAnimating = true;
        lerpTime = 0f;
        // Start from large scale immediately
        transform.localScale = targetScale;
    }
}
