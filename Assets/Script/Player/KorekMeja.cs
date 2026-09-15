using System.Collections;
using UnityEngine;

public class KorekMeja : MonoBehaviour
{
    [Header("Referensi Objek")]
    public Transform apiSprite; 
    public GameObject korekTanganPlayer; 
    
    [Header("Referensi Cahaya")]
    public Light cahayaApi; // BARU: Masukkan objek Point Light ke sini

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxNyalaPerlahan;

    private bool bisaDiambil = false;
    private Vector3 ukuranAsliApi; 
    private float intensitasAsliCahaya; // BARU: Menyimpan intensitas cahaya aslimu

    void Start()
    {
        // Setup Api Sprite
        if (apiSprite != null)
        {
            ukuranAsliApi = apiSprite.localScale; 
            apiSprite.localScale = new Vector3(ukuranAsliApi.x, 0, ukuranAsliApi.z);
            apiSprite.gameObject.SetActive(false);
        }

        // Setup Cahaya
        if (cahayaApi != null)
        {
            // Simpan intensitas yang kamu atur di Inspector, lalu matikan (set 0)
            intensitasAsliCahaya = cahayaApi.intensity;
            cahayaApi.intensity = 0f;
        }

        // Hapus baris ini nanti jika sudah selesai tes langsung
        // NyalakanPerlahan();
    }

    public void NyalakanPerlahan()
    {
        StartCoroutine(ProsesNyala());
    }

    IEnumerator ProsesNyala()
    {
        apiSprite.gameObject.SetActive(true);
        if (audioSource != null && sfxNyalaPerlahan != null)
        {
            audioSource.PlayOneShot(sfxNyalaPerlahan);
        }

        Vector3 skalaAwal = new Vector3(ukuranAsliApi.x, 0, ukuranAsliApi.z);
        Vector3 skalaAkhir = ukuranAsliApi; 
        
        float durasi = 2.0f; 
        float waktu = 0f;

        while (waktu < durasi)
        {
            waktu += Time.deltaTime;
            float persentase = waktu / durasi;
            float efekMulus = Mathf.SmoothStep(0f, 1f, persentase);

            // Perbesar sprite api
            apiSprite.localScale = Vector3.Lerp(skalaAwal, skalaAkhir, efekMulus);

            // Terangkan cahaya secara perlahan
            if (cahayaApi != null)
            {
                cahayaApi.intensity = Mathf.Lerp(0f, intensitasAsliCahaya, efekMulus);
            }

            yield return null;
        }

        // Pastikan nilai akhirnya pas
        apiSprite.localScale = skalaAkhir;
        if (cahayaApi != null)
        {
            cahayaApi.intensity = intensitasAsliCahaya;
        }

        bisaDiambil = true;
    }

    public void AmbilKorek()
    {
        if (!bisaDiambil) return;

        if (korekTanganPlayer != null)
        {
            korekTanganPlayer.SetActive(true);
        }

        // --- YANG DIUBAH ADA DI BARIS INI ---
        // Ganti FindObjectOfType menjadi FindAnyObjectByType
        PlayerController scriptPlayer = FindAnyObjectByType<PlayerController>();
        
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangBawaBarang = true;
        }
        // ------------------------------------

        Destroy(gameObject);
    }
}