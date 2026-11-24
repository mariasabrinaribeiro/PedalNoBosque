using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Transform targetTransform;
    private Vector3 originalScale = Vector3.one;
    private Vector3 pressedScale;
    private Vector3 hoverScale;

    private bool isLerpingBack = false;
    private bool isHovering = false;
    private float lerpTime = 0f;
    [SerializeField] private float returnSpeed = 12f;

    private void Awake()
    {
        Transform visualsChild = transform.Find("Visuals");
        targetTransform = (visualsChild != null) ? visualsChild : transform;
        originalScale = targetTransform.localScale;
        pressedScale = originalScale * 0.86f;
        hoverScale = originalScale * 1.05f;
    }

    private void OnEnable()
    {
        if (targetTransform != null) targetTransform.localScale = originalScale;
        isLerpingBack = false;
        isHovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!isLerpingBack && targetTransform != null)
            targetTransform.localScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (targetTransform != null)
        {
            isLerpingBack = true;
            lerpTime = 0f;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isLerpingBack = false;
        if (targetTransform != null)
            targetTransform.localScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isLerpingBack = true;
        lerpTime = 0f;
    }

    private void Update()
    {
        if (isLerpingBack && targetTransform != null)
        {
            lerpTime += Time.unscaledDeltaTime * returnSpeed;
            Vector3 goal = isHovering ? hoverScale : originalScale;
            targetTransform.localScale = Vector3.Lerp(targetTransform.localScale, goal, Mathf.Clamp01(lerpTime));

            if (lerpTime >= 1.0f)
            {
                isLerpingBack = false;
                targetTransform.localScale = goal;
            }
        }
    }
}
