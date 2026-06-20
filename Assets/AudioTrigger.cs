using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    public enum TriggerEvent { None, OnStart, OnEnable, OnTriggerEnter2D, OnCollisionEnter2D }
    
    [Header("Event Configuration")]
    public TriggerEvent triggerType = TriggerEvent.None;
    public string soundName;
    
    [Header("Tag Filtering (2D Collision only)")]
    public string tagFilter = "Carro";

    private void Start()
    {
        if (triggerType == TriggerEvent.OnStart)
        {
            PlaySound();
        }
    }

    private void OnEnable()
    {
        if (triggerType == TriggerEvent.OnEnable)
        {
            PlaySound();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (triggerType == TriggerEvent.OnTriggerEnter2D)
        {
            if (string.IsNullOrEmpty(tagFilter) || collision.CompareTag(tagFilter))
            {
                PlaySound();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggerType == TriggerEvent.OnCollisionEnter2D)
        {
            if (string.IsNullOrEmpty(tagFilter) || collision.gameObject.CompareTag(tagFilter))
            {
                PlaySound();
            }
        }
    }

    public void PlaySound()
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            AudioManager.PlaySFX(soundName);
        }
    }
}
