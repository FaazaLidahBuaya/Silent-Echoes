using System.Collections;
using UnityEngine;
using TMPro; // Wajib ditambahkan untuk menggunakan TextMeshPro

public class SubtitleManager : MonoBehaviour
{
    // Ini adalah pola Singleton. Memungkinkan script lain memanggil sistem ini dengan mudah
    public static SubtitleManager Instance; 

    [Header("Referensi UI Text")]
    public TextMeshProUGUI subtitleText;
    
    private Coroutine subtitleCoroutine;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (subtitleText != null)
        {
            subtitleText.text = ""; // Kosongkan teks saat game baru dimulai
        }
    }

    // Fungsi ini yang akan dipanggil oleh script lain
    public void TampilkanSubtitle(string teks, float durasi)
    {
        // Jika ada subtitle yang sedang berjalan, hentikan dulu
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
        }
        
        // Jalankan subtitle baru
        subtitleCoroutine = StartCoroutine(ProsesSubtitle(teks, durasi));
    }

    private IEnumerator ProsesSubtitle(string teks, float durasi)
    {
        subtitleText.text = teks;
        yield return new WaitForSeconds(durasi);
        subtitleText.text = ""; // Hilangkan teks setelah durasinya habis
    }
}