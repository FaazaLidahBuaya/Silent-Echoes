using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ToiletValve : MonoBehaviour
{
    [Header("Pengaturan Identitas Katup")]
    public string namaKatup = "Katup Air Toilet";
    public bool butuhGagang = false; // Jika true (Toilet 2), harus punya item gagang dulu
    public bool sudahAdaGagang = true;
    public GameObject visualGagang;  // Model katup yang akan muncul saat dipasang
    public bool bisaDiinteraksi = false; // HANYA BISA DIINTERAKSI SAAT QUEST SELOKER AKTIF!

    [Header("Status Putaran")]
    [Tooltip("0 = Terbuka penuh (bocor), 1 = Tertutup penuh (aman)")]
    [Range(0f, 1f)]
    public float tightness = 0.6f; // Mulai dari 60% yang terus turun/bocor jika tidak dijaga
    public bool isMaxTight = false;

    [Header("Pengaturan Putar (Hold E)")]
    public float durasiTutupPenuh = 7f; // Butuh 7 detik menahan E sampai tertutup penuh (2x lebih lama!)
    public Transform wheelTransform;      // Objek roda katup yang berputar
    public Vector3 sumbuPutar = new Vector3(0, 0, 1);
    public float totalRotasiDerajat = 720f; // Berputar 2 kali putaran penuh

    [Header("Pengaturan Melonggar Otomatis")]
    [Tooltip("JANGAN centang ini di Inspector! Hanya diaktifkan oleh SelokerQuestManager lewat script.")]
    public bool bisaMelonggarSendiri = false;
    public float delaySebelumMelonggar = 12f; // Jeda aman sebelum mulai longgar sendiri
    public float kecepatanMelonggar = 0.008f; // Kecepatan berkurang per detik (SANGAT PELAN = ~2 mnt dari 100% ke 0%)
    [HideInInspector] public float timerLonggar = 0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxPutarKatup;
    public AudioClip sfxKatupMentok;
    public AudioClip sfxPasangGagang;

    [Header("Events")]
    public UnityEvent onTightenedMax;
    public UnityEvent onHandleInstalled;

    private Quaternion rotasiAwalWheel;
    private bool sedangDiputarPlayer = false;

    void Start()
    {
        // KEAMANAN: selalu false di start, hanya diaktifkan lewat script oleh SelokerQuestManager
        bisaMelonggarSendiri = false;
        timerLonggar = 0f;

        if (wheelTransform != null)
        {
            rotasiAwalWheel = wheelTransform.localRotation;
        }

        if (butuhGagang)
        {
            sudahAdaGagang = false;
            if (visualGagang != null) visualGagang.SetActive(false);
        }
        else
        {
            sudahAdaGagang = true;
            if (visualGagang != null) visualGagang.SetActive(true);
        }

        UpdateVisualRotasi();
    }

    void Update()
    {
        // Logika melonggar sendiri secara perlahan seiring berjalannya waktu
        // Jika katup bisa melonggar dan sedang bocor, persentase terus turun (meskipun belum ada gagang!)
        if (bisaMelonggarSendiri && !sedangDiputarPlayer && tightness > 0f)
        {
            if (timerLonggar > 0f)
            {
                timerLonggar -= Time.deltaTime;
            }
            else
            {
                tightness -= kecepatanMelonggar * Time.deltaTime;
                tightness = Mathf.Clamp01(tightness);
                if (tightness < 0.98f)
                {
                    isMaxTight = false;
                }
                UpdateVisualRotasi();
            }
        }
    }

    // Dipanggil setiap frame saat player menahan tombol E
    public void PutarKatup(float deltaTime)
    {
        if (!sudahAdaGagang) return;
        if (isMaxTight) return;

        sedangDiputarPlayer = true;
        tightness += (deltaTime / durasiTutupPenuh);
        tightness = Mathf.Clamp01(tightness);

        UpdateVisualRotasi();

        // Audio memutar
        if (audioSource != null && sfxPutarKatup != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = sfxPutarKatup;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        if (tightness >= 1f && !isMaxTight)
        {
            isMaxTight = true;
            timerLonggar = delaySebelumMelonggar; // Set timer jeda aman
            HentikanAudioPutar();

            if (audioSource != null && sfxKatupMentok != null)
            {
                audioSource.PlayOneShot(sfxKatupMentok);
            }

            onTightenedMax?.Invoke();
        }
    }

    public void LepasPutar()
    {
        sedangDiputarPlayer = false;
        HentikanAudioPutar();
    }

    void HentikanAudioPutar()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == sfxPutarKatup)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }
    }

    public void PasangGagang()
    {
        sudahAdaGagang = true;
        if (visualGagang != null) visualGagang.SetActive(true);
        GameCheckpointManager.gagangKatupSudahTerpasang = true;

        if (audioSource != null && sfxPasangGagang != null)
        {
            audioSource.PlayOneShot(sfxPasangGagang);
        }

        onHandleInstalled?.Invoke();
    }

    void UpdateVisualRotasi()
    {
        if (wheelTransform != null)
        {
            wheelTransform.localRotation = rotasiAwalWheel * Quaternion.Euler(sumbuPutar * (tightness * totalRotasiDerajat));
        }
    }
}
