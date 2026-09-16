using System.Collections;
using UnityEngine;

public class AmbientManager : MonoBehaviour
{
    public static AmbientManager Instance;

    [Header("Audio Sources (Sistem Dual Player untuk Transisi Mulus)")]
    [Tooltip("Sumber audio utama")]
    public AudioSource audioSourceA;
    [Tooltip("Sumber audio cadangan untuk crossfade mulus")]
    public AudioSource audioSourceB;

    [Header("Pengaturan Default")]
    [Range(0f, 1f)] public float masterAmbientVolume = 0.8f;
    [Tooltip("Durasi transisi fade out lalu fade in ke ambient baru (detik)")]
    public float durasiFade = 2.0f;

    private AudioSource activeSource;
    private AudioClip currentClip;
    private Coroutine transisiCoroutine;

    void Awake()
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

        // Setup 2 AudioSource jika belum dipasang di Inspector
        if (audioSourceA == null)
        {
            audioSourceA = gameObject.AddComponent<AudioSource>();
            audioSourceA.loop = true;
            audioSourceA.playOnAwake = false;
        }

        if (audioSourceB == null)
        {
            audioSourceB = gameObject.AddComponent<AudioSource>();
            audioSourceB.loop = true;
            audioSourceB.playOnAwake = false;
        }

        activeSource = audioSourceA;
    }

    /// <summary>
    /// Panggil fungsi ini dari AmbientZone saat player memasuki area
    /// </summary>
    public void GantiAmbient(AudioClip clipBaru, float volumeTarget = 1.0f, float customFade = -1f)
    {
        if (clipBaru == null || clipBaru == currentClip) return;

        currentClip = clipBaru;

        if (transisiCoroutine != null)
        {
            StopCoroutine(transisiCoroutine);
        }

        float fadeTime = (customFade > 0) ? customFade : durasiFade;
        transisiCoroutine = StartCoroutine(ProsesFadeGantiAmbient(clipBaru, volumeTarget * masterAmbientVolume, fadeTime));
    }

    /// <summary>
    /// Matikan ambient jika player keluar ke area hening
    /// </summary>
    public void HentikanAmbient(float customFade = -1f)
    {
        currentClip = null;

        if (transisiCoroutine != null)
        {
            StopCoroutine(transisiCoroutine);
        }

        float fadeTime = (customFade > 0) ? customFade : durasiFade;
        transisiCoroutine = StartCoroutine(ProsesFadeOutTotal(fadeTime));
    }

    // Alur: Fade Out hingga benar-benar senyap (0), baru Fade In ambient baru
    IEnumerator ProsesFadeGantiAmbient(AudioClip clipBaru, float targetVol, float fadeTime)
    {
        // 1. FADE OUT audio yang sedang berjalan sampai benar-benar senyap (Volume 0)
        if (activeSource != null && activeSource.isPlaying)
        {
            float volAwal = activeSource.volume;
            float waktuOut = 0f;
            float durasiOut = fadeTime * 0.5f;

            while (waktuOut < durasiOut)
            {
                waktuOut += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(volAwal, 0f, waktuOut / durasiOut);
                yield return null;
            }

            activeSource.volume = 0f;
            activeSource.Stop();
        }

        // Beri jeda sepersekian detik hening agar pergantian terasa natural
        yield return new WaitForSeconds(0.2f);

        // 2. TUKAR AUDIO SOURCE (Alternating Source)
        activeSource = (activeSource == audioSourceA) ? audioSourceB : audioSourceA;

        activeSource.clip = clipBaru;
        activeSource.volume = 0f;
        activeSource.loop = true;
        activeSource.Play();

        // 3. FADE IN audio baru secara perlahan naik ke volume target
        float waktuIn = 0f;
        float durasiIn = fadeTime * 0.5f;

        while (waktuIn < durasiIn)
        {
            waktuIn += Time.deltaTime;
            activeSource.volume = Mathf.Lerp(0f, targetVol, waktuIn / durasiIn);
            yield return null;
        }

        activeSource.volume = targetVol;
    }

    IEnumerator ProsesFadeOutTotal(float fadeTime)
    {
        if (activeSource != null && activeSource.isPlaying)
        {
            float volAwal = activeSource.volume;
            float t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                activeSource.volume = Mathf.Lerp(volAwal, 0f, t / fadeTime);
                yield return null;
            }

            activeSource.volume = 0f;
            activeSource.Stop();
        }
    }
}
