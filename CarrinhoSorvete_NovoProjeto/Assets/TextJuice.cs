using UnityEngine;
using TMPro;

public class TextJuice : MonoBehaviour
{
    private TMP_Text textComponent;
    private string lastText;

    private Vector3 originalScale = Vector3.one;
    private Vector3 targetScale;

    private bool isAnimating = false;
    private float lerpTime = 0f;
    [SerializeField] private float animationSpeed = 6.0f;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        originalScale = transform.localScale;
        targetScale = originalScale * 1.28f;
    }

    private void Start()
    {
        if (textComponent != null) lastText = textComponent.text;
    }

    private void Update()
    {
        if (textComponent != null && textComponent.text != lastText)
        {
            lastText = textComponent.text;
            if (!string.IsNullOrEmpty(lastText)) TriggerBump();
        }

        if (isAnimating)
        {
            lerpTime += Time.unscaledDeltaTime * animationSpeed;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, Mathf.Clamp01(lerpTime));

            if (lerpTime >= 1.0f)
            {
                isAnimating = false;
                transform.localScale = originalScale;
            }
        }
    }

    public void TriggerBump()
    {
        if (originalScale.sqrMagnitude < 0.001f)
        {
            originalScale = Vector3.one;
            targetScale = originalScale * 1.28f;
        }
        isAnimating = true;
        lerpTime = 0f;
        transform.localScale = targetScale;
    }
}
