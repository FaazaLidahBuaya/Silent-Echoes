using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KorekTangan : MonoBehaviour
{
    [Header("Referensi Objek")]
    public Transform apiSprite; 
    public Light cahayaApi; // Masukkan Point Light korek tangan ke sini
    public Animator animJari; // Masukkan Animator tangan pemain ke sini

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxCetik; 
    public AudioClip sfxNyala; 

    [Header("Pengaturan Mekanik")]
    public float minWaktuMati = 15f; 
    public float maxWaktuMati = 40f; 
    public float kecepatanNyala = 0.5f; 

    private bool sedangMenyala = true;
    private bool sedangProsesCetik = false;
    
    private int klikDibutuhkan = 0;
    private int jumlahKlik = 0;

    private Vector3 ukuranAsliApi;
    private float intensitasAsliCahaya;

    void Start()
    {
        // Menyimpan ukuran dan cahaya asli seperti trik di meja
        if (apiSprite != null)
        {
            ukuranAsliApi = apiSprite.localScale;
        }

        if (cahayaApi != null)
        {
            cahayaApi.gameObject.SetActive(true);
            intensitasAsliCahaya = cahayaApi.intensity;
        }

        // Mulai hitung mundur pertama kali dikumpulkan
        MulaiHitungMundurMati();
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Cek input Klik Kanan jika api mati dan tidak sedang animasi cetik
        if (Mouse.current.rightButton.wasPressedThisFrame && !sedangMenyala && !sedangProsesCetik)
        {
            ProsesCetik();
        }
    }

    void ProsesCetik()
    {
        // 1. LANGSUNG GEMBOK agar pemain tidak bisa klik lagi!
        sedangProsesCetik = true; 
        
        jumlahKlik++;
        
        if (animJari != null) 
        {
            // 2. Hapus sisa memori klik dari Unity agar tidak numpuk
            animJari.ResetTrigger("Cetik"); 
            
            // 3. Panggil animasinya
            animJari.SetTrigger("Cetik");
        }

        if (audioSource != null && sfxCetik != null)
            audioSource.PlayOneShot(sfxCetik);

        if (jumlahKlik >= klikDibutuhkan)
        {
            StartCoroutine(NyalakanApi());
        }
        else
        {
            StartCoroutine(JedaCetik());
        }
    }

    IEnumerator JedaCetik()
    {
        // KUNCI UTAMA: Waktu ini HARUS LEBIH LAMA dari durasi klip Cetik.anim milikmu.
        // Jika animasimu butuh 1 detik untuk selesai, ganti angka ini jadi 1.1f atau 1.2f.
        yield return new WaitForSeconds(1.0f); 
        
        // Gembok baru dibuka setelah tangan benar-benar diam kembali
        sedangProsesCetik = false; 
    }

    IEnumerator NyalakanApi()
    {
        sedangProsesCetik = true;
        
        if (audioSource != null && sfxNyala != null)
            audioSource.PlayOneShot(sfxNyala);

        apiSprite.gameObject.SetActive(true);
        Vector3 skalaAwal = new Vector3(ukuranAsliApi.x, 0, ukuranAsliApi.z);
        Vector3 skalaAkhir = ukuranAsliApi;
        
        float waktu = 0f;
        while (waktu < 1f)
        {
            waktu += Time.deltaTime / kecepatanNyala;
            float efekMulus = Mathf.SmoothStep(0f, 1f, waktu);
            
            // Lerp ukuran dan cahaya
            apiSprite.localScale = Vector3.Lerp(skalaAwal, skalaAkhir, efekMulus);
            if (cahayaApi != null)
            {
                cahayaApi.intensity = Mathf.Lerp(0f, intensitasAsliCahaya, efekMulus);
            }
            
            yield return null;
        }

        apiSprite.localScale = skalaAkhir;
        if (cahayaApi != null) cahayaApi.intensity = intensitasAsliCahaya;

        // --- JURUS CROSSFADE PULANG ---
        if (animJari != null)
        {
            animJari.CrossFade("Tangan", 0.2f);
        }

        sedangMenyala = true;
        sedangProsesCetik = false;

        MulaiHitungMundurMati();
    }

    void MulaiHitungMundurMati()
    {
        float waktuTunggu = Random.Range(minWaktuMati, maxWaktuMati);
        Invoke("MatikanKorek", waktuTunggu);
    }

    void MatikanKorek()
    {
        sedangMenyala = false;
        apiSprite.gameObject.SetActive(false); 
        
        if (cahayaApi != null) cahayaApi.intensity = 0f;
        
        // Acak jumlah klik untuk percobaan berikutnya (2 sampai 7 klik)
        klikDibutuhkan = Random.Range(2, 8);
        jumlahKlik = 0;
    }
}