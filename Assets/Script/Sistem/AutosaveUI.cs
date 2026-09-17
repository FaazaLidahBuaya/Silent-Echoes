using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AutosaveUI : MonoBehaviour
{
    public static AutosaveUI Instance;

    [Header("Referensi UI Image")]
    [Tooltip("Tarik UI Image logo kamu ke sini (atau pasang script ini langsung di GameObject Image-nya)")]
    public Image iconAutosave;

    [Header("Pengaturan Efek Fade (Pulsing)")]
    [Tooltip("Kecepatan kedip/fade")]
    public float fadeSpeed = 4f;

    [Tooltip("Tingkat transparansi terendah saat pudar (0 = transparan total)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.15f;

    [Tooltip("Tingkat transparansi tertinggi saat terang")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.9f;

    [Header("Durasi Tampil")]
    [Tooltip("Berapa detik animasi fade berlangsung saat autosave")]
    public float defaultDurasi = 3.5f;

    [Header("Fitur Pengujian")]
    [Tooltip("Tekan F5 saat Playmode untuk menguji logo autosave")]
    public bool aktifkanTombolTest = true;

    private Coroutine autosaveCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Jika belum ditarik manual di Inspector, ambil dari objek ini sendiri
        if (iconAutosave == null)
        {
            iconAutosave = GetComponent<Image>();
        }
    }

    void Start()
    {
        // Pastikan logo transparan/tersembunyi saat game baru mulai
        SetAlpha(0f);
    }

    void Update()
    {
        // Uji coba manual dengan menekan tombol F5
        if (aktifkanTombolTest && Keyboard.current != null)
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                TriggerAutosave();
            }
        }
    }

    /// <summary>
    /// Memanggil animasi fade-full-fade logo autosave
    /// </summary>
    public void TriggerAutosave(float durasi = -1f)
    {
        if (iconAutosave == null) return;

        float durasiAktif = durasi > 0f ? durasi : defaultDurasi;

        if (autosaveCoroutine != null)
        {
            StopCoroutine(autosaveCoroutine);
        }

        autosaveCoroutine = StartCoroutine(ProsesAnimasiAutosave(durasiAktif));
    }

    private IEnumerator ProsesAnimasiAutosave(float durasi)
    {
        float waktuBerjalan = 0f;

        // Fase 1: Animasi berkedip (fade in - full - fade out) berulang
        while (waktuBerjalan < durasi)
        {
            waktuBerjalan += Time.deltaTime;

            float lerpFactor = (Mathf.Sin(Time.time * fadeSpeed) + 1f) / 2f;
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpFactor);

            SetAlpha(currentAlpha);
            yield return null;
        }

        // Fase 2: Fade Out halus sampai hilang total
        float startAlpha = iconAutosave.color.a;
        float fadeOutTime = 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutTime;
            SetAlpha(Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        SetAlpha(0f);
        autosaveCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (iconAutosave != null)
        {
            Color c = iconAutosave.color;
            c.a = alpha;
            iconAutosave.color = c;
        }
    }
}
