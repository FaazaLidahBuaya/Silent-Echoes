using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class KorekTangan : MonoBehaviour
{
    [Header("Referensi Objek")]
    public Transform apiSprite; 
    [Tooltip("Jika ada api kedua / sprite api tambahan, masukkan ke sini")]
    public Transform[] apiTambahan; 
    public Light cahayaApi; // Masukkan Point Light korek tangan ke sini
    [Tooltip("Jika ada lampu tambahan / Point Light kedua, masukkan ke sini")]
    public Light[] cahayaTambahan;
    public Animator animJari; // Masukkan Animator tangan pemain ke sini

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxCetik; 
    public AudioClip sfxNyala; 

    [Header("Pengaturan Mekanik")]
    public float minWaktuMati = 15f; 
    public float maxWaktuMati = 40f; 
    public float kecepatanNyala = 0.5f; 
    public bool selaluMenyala = false; // Jika true, korek TIDAK AKAN PERNAH MATI (misal saat jumpscare)

    private bool sedangMenyala = true;
    private bool sedangProsesCetik = false;
    
    private int klikDibutuhkan = 0;
    private int jumlahKlik = 0;

    private Vector3 ukuranAsliApi;
    private float intensitasAsliCahaya;
    private System.Collections.Generic.Dictionary<Light, float> mapIntensitasAsli = new System.Collections.Generic.Dictionary<Light, float>();

    void Awake()
    {
        // Rekam intensitas asli masing-masing lampu langsung dari Inspector sebelum diubah
        Light[] allLights = GetComponentsInChildren<Light>(true);
        foreach (var l in allLights)
        {
            if (l != null && !mapIntensitasAsli.ContainsKey(l))
            {
                mapIntensitasAsli[l] = l.intensity;
            }
        }

        if (cahayaApi != null && mapIntensitasAsli.ContainsKey(cahayaApi))
        {
            intensitasAsliCahaya = mapIntensitasAsli[cahayaApi];
        }
    }

    void Start()
    {
        // Menyimpan ukuran asli api sprite
        if (apiSprite != null)
        {
            ukuranAsliApi = apiSprite.localScale;
        }

        // Pastikan semua lampu menyala sesuai intensitas aslinya
        foreach (var pair in mapIntensitasAsli)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.SetActive(true);
                pair.Key.intensity = pair.Value;
            }
        }

        // Mulai hitung mundur pertama kali dikumpulkan jika tidak terkunci selalu menyala
        if (!selaluMenyala)
        {
            MulaiHitungMundurMati();
        }

        // Pasang otomatis fisika ayunan nyala api jika belum ada
        if (apiSprite != null && apiSprite.GetComponent<FlamePhysicsSway>() == null)
        {
            apiSprite.gameObject.AddComponent<FlamePhysicsSway>();
        }
        if (apiTambahan != null)
        {
            foreach (var a in apiTambahan)
            {
                if (a != null && a.GetComponent<FlamePhysicsSway>() == null)
                {
                    a.gameObject.AddComponent<FlamePhysicsSway>();
                }
            }
        }
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
            
            // Lerp ukuran dan cahaya masing-masing lampu
            apiSprite.localScale = Vector3.Lerp(skalaAwal, skalaAkhir, efekMulus);
            foreach (var pair in mapIntensitasAsli)
            {
                if (pair.Key != null)
                {
                    pair.Key.intensity = Mathf.Lerp(0f, pair.Value, efekMulus);
                }
            }
            
            yield return null;
        }

        apiSprite.localScale = skalaAkhir;
        
        // Pulihkan semua lampu ke intensitas aslinya masing-masing
        foreach (var pair in mapIntensitasAsli)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.SetActive(true);
                pair.Key.intensity = pair.Value;
            }
        }

        // Nyalakan semua api tambahan jika ada
        if (apiTambahan != null)
        {
            foreach (var a in apiTambahan) if (a != null) a.gameObject.SetActive(true);
        }

        // --- JURUS CROSSFADE PULANG ---
        if (animJari != null)
        {
            animJari.CrossFade("Tangan", 0.2f);
        }

        sedangMenyala = true;
        sedangProsesCetik = false;

        if (!selaluMenyala)
        {
            MulaiHitungMundurMati();
        }
    }

    void MulaiHitungMundurMati()
    {
        if (selaluMenyala) return;
        float waktuTunggu = Random.Range(minWaktuMati, maxWaktuMati);
        Invoke("MatikanKorek", waktuTunggu);
    }

    void MatikanKorek()
    {
        // JIKA DIKUNCI SELALU MENYALA (MISAL SAAT JUMPSCARE/EVENT), JANGAN MATIKAN!
        if (selaluMenyala) return;

        sedangMenyala = false;
        if (apiSprite != null) apiSprite.gameObject.SetActive(false); 
        
        if (apiTambahan != null)
        {
            foreach (var a in apiTambahan) if (a != null) a.gameObject.SetActive(false);
        }

        // Matikan intensitas semua lampu
        foreach (var pair in mapIntensitasAsli)
        {
            if (pair.Key != null) pair.Key.intensity = 0f;
        }
        
        // Acak jumlah klik untuk percobaan berikutnya (2 sampai 7 klik)
        klikDibutuhkan = Random.Range(2, 8);
        jumlahKlik = 0;
    }

    /// <summary>
    /// Panggil fungsi ini agar korek menyala normal secara instan dan TIDAK BISA MATI (untuk sekuen jumpscare)
    /// </summary>
    public void PaksaNyalakanKorek(bool kunciSelamanya = true)
    {
        selaluMenyala = kunciSelamanya;
        CancelInvoke("MatikanKorek");
        StopAllCoroutines();

        sedangMenyala = true;
        sedangProsesCetik = false;

        // 1. Nyalakan Api Utama
        if (apiSprite != null)
        {
            apiSprite.gameObject.SetActive(true);
            if (ukuranAsliApi != Vector3.zero) apiSprite.localScale = ukuranAsliApi;
        }

        // 2. Nyalakan Api Tambahan (Api kedua dll)
        if (apiTambahan != null)
        {
            foreach (var a in apiTambahan) if (a != null) a.gameObject.SetActive(true);
        }

        // 3. Nyalakan semua lampu PERSIS pada intensitas aslinya (TIDAK LEBIH TERANG / BERBEDA)
        foreach (var pair in mapIntensitasAsli)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.SetActive(true);
                pair.Key.intensity = pair.Value;
            }
        }

        Transform[] trans = GetComponentsInChildren<Transform>(true);
        foreach (var t in trans)
        {
            if (t.name.ToLower().Contains("api") || t.name.ToLower().Contains("flame"))
            {
                t.gameObject.SetActive(true);
            }
        }

        if (animJari != null)
        {
            animJari.CrossFade("Tangan", 0.1f);
        }
    }
}