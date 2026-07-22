using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public struct SoundEvent
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Sound List")]
    public List<SoundEvent> soundEvents = new List<SoundEvent>();

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
    }

    public static void PlaySFX(string name)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AudioManager] Instância não encontrada na cena. Som não reproduzido: " + name);
            return;
        }

        AudioClip clip = Instance.FindClip(name);
        if (clip != null)
        {
            Instance.sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("[AudioManager] AudioClip não cadastrado para o evento: " + name);
        }
    }

    public static void PlayMusic(string name)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[AudioManager] Instância não encontrada na cena. Música não reproduzida: " + name);
            return;
        }

        AudioClip clip = Instance.FindClip(name);
        if (clip != null)
        {
            if (Instance.fadeCoroutine != null)
            {
                Instance.StopCoroutine(Instance.fadeCoroutine);
                Instance.fadeCoroutine = null;
            }
            Instance.musicSource.volume = 1f;

            if (Instance.musicSource.clip == clip && Instance.musicSource.isPlaying)
            {
                return; // already playing this music
            }

            Instance.musicSource.clip = clip;
            Instance.musicSource.loop = true;
            Instance.musicSource.Play();
        }
        else
        {
            Debug.LogWarning("[AudioManager] AudioClip não cadastrado para a música: " + name);
        }
    }

    public static void StopMusic()
    {
        if (Instance != null)
        {
            Instance.musicSource.Stop();
        }
    }

    private AudioClip FindClip(string name)
    {
        foreach (var sEvent in soundEvents)
        {
            if (sEvent.name == name)
            {
                return sEvent.clip;
            }
        }
        return null;
    }

    public static void FadeOutMusic(float duration, System.Action onComplete = null)
    {
        if (Instance == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (Instance.fadeCoroutine != null)
        {
            Instance.StopCoroutine(Instance.fadeCoroutine);
        }
        Instance.fadeCoroutine = Instance.StartCoroutine(Instance.FadeOutCoroutine(duration, onComplete));
    }

    private IEnumerator FadeOutCoroutine(float duration, System.Action onComplete)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.volume = 1f; // Reseta o volume para as próximas músicas
        fadeCoroutine = null;

        onComplete?.Invoke();
    }
}
