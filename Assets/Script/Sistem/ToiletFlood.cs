using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToiletFlood : MonoBehaviour
{
    [Header("Referensi Visual Air Utama")]
    public Renderer waterRenderer; // Renderer untuk mengubah warna material air utama
    public string namaPropertiWarna = "_BaseColor"; // URP "_BaseColor", Standard "_Color"

    [Header("Mesh Rembesan Air Tambahan (2 Mesh Tambahan)")]
    [Tooltip("Masukkan 2 mesh rembesan tambahan di sekitar toilet (lantai/dinding)")]
    public Renderer[] meshRembesanTambahan;

    [Header("Warna Fase Air Horor")]
    public Color warnaPutih = new Color(0.9f, 0.95f, 1f, 0.85f); // Air keran awal
    public Color warnaKuning = new Color(0.85f, 0.75f, 0.2f, 0.9f); // Keruh kotor
    public Color warnaMerahDarah = new Color(0.6f, 0.02f, 0.02f, 0.98f); // Darah kental Seloker

    [Header("Status Luapan")]
    [Tooltip("0 = Aman, 1 = Kritis (Game Over)")]
    [Range(0f, 1f)]
    public float floodLevel = 0f;
    public bool isFlooding = false;
    [Tooltip("Kecepatan dasar meluap saat katup terbuka (dikalibrasi pas untuk tensi horor)")]
    public float kecepatanMeluap = 0.033f;
    [Tooltip("Kecepatan air surut perlahan saat katup tertutup")]
    public float kecepatanSurut = 0.028f;
    [Tooltip("Batas putaran katup di mana air mulai surut (95% ke atas)")]
    public float batasSurutTightness = 0.95f;

    [Header("Referensi Katup")]
    public ToiletValve katupToilet;

    [Header("Audio Air Meluap (3D Spatial Audio)")]
    public AudioSource audioSourceAir;
    [Tooltip("SFX semburan/desisan air meluap di toilet (Looping 3D)")]
    public AudioClip sfxAirMeluap;
    [Range(0f, 1f)] public float volumeMaksimalAir = 0.85f;
    public float jarakMaksimalDengar = 14f;

    private Material instancedMaterial;
    private List<Material> instancedRembesanMaterials = new List<Material>();
    private Coroutine coroutineFade;
    private Coroutine coroutineFadeAudio;

    void Start()
    {
        InisialisasiAudioAir();

        if (waterRenderer != null)
        {
            instancedMaterial = waterRenderer.material;
        }

        // Inisialisasi material unik untuk mesh rembesan tambahan
        if (meshRembesanTambahan != null)
        {
            foreach (var r in meshRembesanTambahan)
            {
                if (r != null)
                {
                    instancedRembesanMaterials.Add(r.material);
                    // Matikan di awal game sebelum event dimulai
                    if (!isFlooding)
                    {
                        r.gameObject.SetActive(false);
                    }
                }
            }
        }

        UpdateVisualAir();
    }

    void Update()
    {
        if (!isFlooding) return;

        // Jika katup ditutup rapat (tightness >= 95%), air surut pelan
        if (katupToilet != null && katupToilet.tightness >= batasSurutTightness)
        {
            floodLevel -= kecepatanSurut * Time.deltaTime;
        }
        else
        {
            // Hitung faktor kebocoran berdasarkan tightness katup
            float t = (katupToilet != null) ? katupToilet.tightness : 0f;

            float bocorFactor;
            if (t >= 0.60f)
            {
                // Katup masih di atas 60%: rembesan kecil tapi terasa jika dibiarkan
                bocorFactor = (1f - t) * 0.6f;
            }
            else
            {
                // Katup di bawah 60%: tensi naik signifikan namun tetap punya ruang gerak (sweet spot)
                // Pada t = 0.60 -> bocorFactor = 0.24
                // Pada t = 0.00 -> bocorFactor = 0.90
                float progressBawah60 = (0.60f - t) / 0.60f;
                bocorFactor = Mathf.Lerp(0.24f, 0.90f, progressBawah60);
            }

            floodLevel += (kecepatanMeluap * bocorFactor) * Time.deltaTime;
        }

        floodLevel = Mathf.Clamp01(floodLevel);
        UpdateVisualAir();
        UpdateAudioAir();
    }

    private void InisialisasiAudioAir()
    {
        if (audioSourceAir == null)
        {
            audioSourceAir = GetComponent<AudioSource>();
            if (audioSourceAir == null)
            {
                audioSourceAir = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSourceAir.spatialBlend = 1f; // 100% 3D Audio!
        audioSourceAir.rolloffMode = AudioRolloffMode.Linear;
        audioSourceAir.minDistance = 1.5f;
        audioSourceAir.maxDistance = jarakMaksimalDengar;
        audioSourceAir.loop = true;
        audioSourceAir.playOnAwake = false;

#if UNITY_EDITOR
        if (sfxAirMeluap == null)
        {
            sfxAirMeluap = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/Bocorair.mp3");
        }
#endif
        if (sfxAirMeluap != null)
        {
            audioSourceAir.clip = sfxAirMeluap;
        }
    }

    private void UpdateAudioAir()
    {
        if (audioSourceAir == null) return;
        if (audioSourceAir.clip == null && sfxAirMeluap != null) audioSourceAir.clip = sfxAirMeluap;
        if (audioSourceAir.clip == null) return;

        bool bocorAktif = isFlooding && (katupToilet == null || katupToilet.tightness < batasSurutTightness);

        if (bocorAktif)
        {
            if (!audioSourceAir.isPlaying)
            {
                audioSourceAir.volume = 0f;
                audioSourceAir.Play();
            }

            // Volume berbanding lurus dengan kelonggaran katup & luapan
            float faktorLonggar = (katupToilet != null) ? Mathf.Clamp01(1f - katupToilet.tightness) : 1f;
            float targetVolume = Mathf.Lerp(0.35f, volumeMaksimalAir, faktorLonggar);
            audioSourceAir.volume = Mathf.MoveTowards(audioSourceAir.volume, targetVolume, Time.deltaTime * 0.8f);
        }
        else
        {
            if (audioSourceAir.isPlaying)
            {
                audioSourceAir.volume = Mathf.MoveTowards(audioSourceAir.volume, 0f, Time.deltaTime * 1.5f);
                if (audioSourceAir.volume <= 0.01f)
                {
                    audioSourceAir.Stop();
                }
            }
        }
    }

    public void HentikanAudioAir(float durasiFade = 1.5f)
    {
        if (audioSourceAir != null && audioSourceAir.isPlaying)
        {
            if (coroutineFadeAudio != null) StopCoroutine(coroutineFadeAudio);
            coroutineFadeAudio = StartCoroutine(ProsesFadeAudioAir(durasiFade));
        }
    }

    private IEnumerator ProsesFadeAudioAir(float durasi)
    {
        if (audioSourceAir == null) yield break;
        float startVol = audioSourceAir.volume;
        float t = 0f;
        while (t < durasi)
        {
            t += Time.deltaTime;
            audioSourceAir.volume = Mathf.Lerp(startVol, 0f, t / durasi);
            yield return null;
        }
        audioSourceAir.volume = 0f;
        audioSourceAir.Stop();
        coroutineFadeAudio = null;
    }

    /// <summary>
    /// Panggil saat toilet mulai bocor: nyalakan mesh rembesan dan update warna awal
    /// </summary>
    public void MulaiBanjir(float initialFloodLevel)
    {
        if (coroutineFade != null) StopCoroutine(coroutineFade);
        isFlooding = true;
        floodLevel = initialFloodLevel;

        AktifkanMeshRembesan(true);
        UpdateVisualAir(1f);

        // Langsung mulai semburan audio air 3D
        if (audioSourceAir != null && audioSourceAir.clip != null && !audioSourceAir.isPlaying)
        {
            audioSourceAir.volume = volumeMaksimalAir * 0.5f;
            audioSourceAir.Play();
        }
    }

    /// <summary>
    /// Panggil saat quest selesai: hilangkan mesh rembesan secara fade out mulus
    /// </summary>
    public void SelesaikanBanjirFade(float durasiFade = 2.5f)
    {
        isFlooding = false;
        if (coroutineFade != null) StopCoroutine(coroutineFade);
        coroutineFade = StartCoroutine(ProsesFadeOutRembesan(durasiFade));
        HentikanAudioAir(durasiFade);
    }

    public void MatikanRembesanLangsung()
    {
        isFlooding = false;
        if (coroutineFade != null) StopCoroutine(coroutineFade);
        floodLevel = 0f;
        AktifkanMeshRembesan(false);
        UpdateVisualAir(0f);
        if (audioSourceAir != null) audioSourceAir.Stop();
    }

    public void AktifkanMeshRembesan(bool aktif)
    {
        if (meshRembesanTambahan != null)
        {
            foreach (var r in meshRembesanTambahan)
            {
                if (r != null) r.gameObject.SetActive(aktif);
            }
        }
    }

    private IEnumerator ProsesFadeOutRembesan(float durasi)
    {
        float t = 0f;
        float awalFlood = floodLevel;

        while (t < durasi)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / durasi);
            float alpha = Mathf.Lerp(1f, 0f, progress);

            floodLevel = Mathf.Lerp(awalFlood, 0f, progress);
            UpdateVisualAir(alpha);

            yield return null;
        }

        // Matikan mesh rembesan setelah selesai fade
        AktifkanMeshRembesan(false);
        coroutineFade = null;
    }

    public void UpdateVisualAir(float pengaliAlpha = 1f)
    {
        // 0–45% = Putih, 45–70% = Kuning, 70–100% = Merah Darah
        Color targetColor;
        if (floodLevel < 0.45f)
        {
            targetColor = Color.Lerp(warnaPutih, warnaKuning, floodLevel / 0.45f);
        }
        else if (floodLevel < 0.70f)
        {
            targetColor = Color.Lerp(warnaKuning, warnaMerahDarah, (floodLevel - 0.45f) / 0.25f);
        }
        else
        {
            targetColor = warnaMerahDarah;
        }

        // Terapkan transparansi fade jika sedang proses menghilang
        Color warnaDenganAlpha = targetColor;
        warnaDenganAlpha.a *= pengaliAlpha;

        // 1. Terapkan pada material air utama
        if (instancedMaterial != null)
        {
            SetWarnaMaterial(instancedMaterial, warnaDenganAlpha);
        }

        // 2. Terapkan pada semua mesh rembesan tambahan
        foreach (var mat in instancedRembesanMaterials)
        {
            if (mat != null)
            {
                SetWarnaMaterial(mat, warnaDenganAlpha);
            }
        }
    }

    private void SetWarnaMaterial(Material mat, Color c)
    {
        if (mat.HasProperty(namaPropertiWarna))
        {
            mat.SetColor(namaPropertiWarna, c);
        }
        else
        {
            mat.color = c;
        }
    }
}
