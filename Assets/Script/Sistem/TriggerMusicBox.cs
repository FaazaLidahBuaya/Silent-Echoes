using System.Collections;
using UnityEngine;

public class TriggerMusicBox : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip musikClip;
    [Range(0f, 1f)] public float targetVolume = 0.8f;
    public bool loop = true;

    [Header("Pengaturan Transisi Fade")]
    public bool gunakanFadeIn = true;
    public float durasiFadeIn = 2.0f;
    public bool hentikanSaatKeluar = false;
    public float durasiFadeOut = 1.5f;

    [Header("Pengaturan Trigger")]
    [Tooltip("Jika dicentang, musik hanya terpicu 1x saja seumur permainan")]
    public bool hanyaSekali = true;

    private bool sudahTerpicu = false;
    private Coroutine fadeCoroutine;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (hanyaSekali && sudahTerpicu) return;

            sudahTerpicu = true;
            MulaiPutarMusik();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hentikanSaatKeluar)
        {
            HentikanMusik();
        }
    }

    public void MulaiPutarMusik()
    {
        if (audioSource == null || musikClip == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        audioSource.clip = musikClip;
        audioSource.loop = loop;

        if (gunakanFadeIn)
        {
            fadeCoroutine = StartCoroutine(ProsesFadeIn());
        }
        else
        {
            audioSource.volume = targetVolume;
            audioSource.Play();
        }
    }

    public void HentikanMusik()
    {
        if (audioSource == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(ProsesFadeOut());
    }

    IEnumerator ProsesFadeIn()
    {
        audioSource.volume = 0f;
        audioSource.Play();

        float t = 0f;
        while (t < durasiFadeIn)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / durasiFadeIn);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    IEnumerator ProsesFadeOut()
    {
        float volumeAwal = audioSource.volume;
        float t = 0f;
        while (t < durasiFadeOut)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(volumeAwal, 0f, t / durasiFadeOut);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}
