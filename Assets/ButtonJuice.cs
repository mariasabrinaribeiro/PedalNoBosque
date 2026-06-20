using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Transform targetTransform;
    private Vector3 originalScale;
    private Vector3 pressedScale;

    private bool isLerpingBack = false;
    private float lerpTime = 0f;
    [SerializeField] private float returnSpeed = 10f;

    void Start()
    {
        // If there's a nested child named "Visuals" (used for larger hitboxes), animate that instead
        Transform visualsChild = transform.Find("Visuals");
        targetTransform = (visualsChild != null) ? visualsChild : transform;

        originalScale = targetTransform.localScale;
        pressedScale = originalScale * 0.82f; // squash to 82%
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isLerpingBack = false;
        targetTransform.localScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isLerpingBack = true;
        lerpTime = 0f;
    }

    void Update()
    {
        if (isLerpingBack)
        {
            lerpTime += Time.deltaTime * returnSpeed;
            targetTransform.localScale = Vector3.Lerp(pressedScale, originalScale, lerpTime);

            if (lerpTime >= 1.0f)
            {
                isLerpingBack = false;
                targetTransform.localScale = originalScale;
            }
        }
    }
}
