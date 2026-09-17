using System.Collections;
using UnityEngine;

public class TuasListrik : MonoBehaviour
{
    [Header("Referensi Lampu (Ruangan)")]
    [Tooltip("Pastikan Mode lampu di Inspector adalah REALTIME")]
    public Light[] lampuRuangan;

    [Header("Pengaturan Animasi Tuas")]
    public Transform engselTuas;
    public Vector3 sudutDitarik = new Vector3(90, 0, 0); 
    public float kecepatanTarik = 5f;

    [Header("Pengaturan Kedip Lampu")]
    [Tooltip("Berapa lama (dalam detik) lampu akan berkedip sebelum mati total")]
    public float durasiKedipTotal = 3.0f; // Kamu bisa ubah angkanya di Inspector

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxTuasDitarik;
    public AudioClip sfxListrikMati;
    public AudioClip sfxBukaBautObeng;
    public AudioClip sfxPasangFuse;
    public AudioClip sfxListrikNyalaStabil;

    [Header("Item Quest Dibutuhkan")]
    public string namaObeng = "Obeng";
    public string namaFuse = "Fuse"; // Sesuaikan dengan namaItem di ItemPickup

    [Header("Status Saklar & Perbaikan")]
    public bool sudahDitarik { get; private set; } = false;
    public bool sedangProses { get; private set; } = false;
    public bool coverTerbuka { get; private set; } = false;
    public bool fuseTerpasang { get; private set; } = false;
    public bool sudahDiperbaiki { get; private set; } = false;
    public bool listrikMenyalaStabil { get; private set; } = false;

    // Objek 3D visual jika ada (opsional)
    [Header("Objek Visual Opsional")]
    public GameObject objekCoverSaklar;
    public GameObject objekFuseDalamSaklar;

    void Awake()
    {
        // Jika sedang respawn setelah Game Over di telepon, saklar sudah pernah diperbaiki dan listrik menyala
        if (GameCheckpointManager.respawnDiTelepon)
        {
            SetKondisiListrikSudahMenyala();
        }
    }

    /// <summary>
    /// Mengatur tuas dan lampu gudang langsung dalam status menyala stabil (untuk checkpoint respawn)
    /// </summary>
    public void SetKondisiListrikSudahMenyala()
    {
        sudahDitarik = true;
        sedangProses = false;
        coverTerbuka = true;
        fuseTerpasang = true;
        sudahDiperbaiki = true;
        listrikMenyalaStabil = true;

        if (objekCoverSaklar != null) objekCoverSaklar.SetActive(false);
        if (objekFuseDalamSaklar != null) objekFuseDalamSaklar.SetActive(true);

        AturStatusLampu(true);
        AturIntensitasLampu(2.5f);
    }

    public void InteraksiTuas()
    {
        // Jika sedang animasi berjalan ATAU listrik sudah berhasil dinyalakan permanen, tolak interaksi!
        if (sedangProses || listrikMenyalaStabil) return;

        // FASE 1: Tarik pertama kali (mati lampu)
        if (!sudahDitarik)
        {
            StartCoroutine(ProsesLampuMati());
            return;
        }

        // FASE 2: Saklar rusak, butuh perbaikan
        if (!sudahDiperbaiki)
        {
            CobaPerbaikiSaklar();
            return;
        }

        // FASE 3: Sudah diperbaiki, nyalakan listrik stabil
        StartCoroutine(ProsesLampuNyalaStabil());
    }

    IEnumerator ProsesLampuMati()
    {
        sedangProses = true;
        sudahDitarik = true;

        // 1. SUARA & ANIMASI TUAS DITARIK
        if (audioSource != null && sfxTuasDitarik != null)
        {
            audioSource.PlayOneShot(sfxTuasDitarik);
        }
        
        if (engselTuas != null)
        {
            Quaternion rotasiAwal = engselTuas.localRotation;
            Quaternion rotasiAkhir = rotasiAwal * Quaternion.Euler(sudutDitarik);
            float waktu = 0;
            while (waktu < 1f)
            {
                waktu += Time.deltaTime * kecepatanTarik;
                engselTuas.localRotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, waktu);
                yield return null;
            }
            engselTuas.localRotation = rotasiAkhir; // Pastikan posisi akhirnya pas
        }

        // 2. NYALAKAN OBJEK LAMPU & MAINKAN SUARA LISTRIK MATI
        AturStatusLampu(true);
        if (audioSource != null && sfxListrikMati != null)
        {
            // Ubah dari PlayOneShot menjadi clip & Play, agar mudah di-Stop nanti
            audioSource.clip = sfxListrikMati;
            audioSource.Play();
        }

        // 3. EFEK KEDIP NAIK TURUN BERDASARKAN DURASI WAKTU
        float waktuMulaiKedip = Time.time;
        // Terus berkedip selama waktu saat ini belum melewati target durasi
        while (Time.time < waktuMulaiKedip + durasiKedipTotal)
        {
            // Acak intensitas cahaya antara 0 sampai 6
            float intensitasAcak = Random.Range(0f, 6f);
            AturIntensitasLampu(intensitasAcak);
            
            // Jeda acak antar kedipan agar terlihat tidak stabil (konslet)
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f)); 
        }

        // 4. LAMPU MATI TOTAL, HENTIKAN AUDIO PAKSA, & NONAKTIFKAN KOMPONEN
        AturIntensitasLampu(0f);
        AturStatusLampu(false);
        
        // Hentikan paksa sfx listrik matinya!
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Beri jeda sebentar (gelap) sebelum pemain ngomong
        yield return new WaitForSeconds(0.5f);

        // 5. MUNCULKAN SUBTITLE
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Sepertinya saklar nya rusak, aku harus memperbaikinya)", 3f);
        }

        // 6. UPDATE QUEST PETUNJUK
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Perbaiki saklar listrik", "Cari obeng dan sekring cadangan");
        }

        sedangProses = false;
    }

    void CobaPerbaikiSaklar()
    {
        if (InventoryManager.Instance == null) return;

        bool punyaObeng = InventoryManager.Instance.CekItem(namaObeng);
        bool punyaFuse = InventoryManager.Instance.CekItem(namaFuse);

        // Kasus 1: Belum buka cover dan belum bawa obeng
        if (!coverTerbuka && !punyaObeng)
        {
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.TampilkanSubtitle("(Penutup saklar ini terkunci baut. Aku butuh obeng)", 2.5f);
            }
            return;
        }

        // Kasus 2: Punya obeng, buka cover saklar
        if (!coverTerbuka && punyaObeng)
        {
            coverTerbuka = true;
            if (audioSource != null && sfxBukaBautObeng != null)
            {
                audioSource.PlayOneShot(sfxBukaBautObeng);
            }

            if (objekCoverSaklar != null)
            {
                objekCoverSaklar.SetActive(false); // Buka penutupnya
            }

            if (!punyaFuse)
            {
                if (SubtitleManager.Instance != null)
                {
                    SubtitleManager.Instance.TampilkanSubtitle("(Baut terbuka. Sekring di dalamnya terbakar, aku butuh sekring cadangan)", 3f);
                }
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.SetQuest("Pasang sekring cadangan", "Cari sekring (fuse) di sekitar rumah");
                }
            }
            else
            {
                // Jika sudah bawa sekring sekaligus
                PasangSekring();
            }
            return;
        }

        // Kasus 3: Cover sudah terbuka, tapi belum bawa sekring
        if (coverTerbuka && !punyaFuse)
        {
            if (SubtitleManager.Instance != null)
            {
                SubtitleManager.Instance.TampilkanSubtitle("(Sekring masih kosong. Aku harus memasukkan sekring cadangan)", 2.5f);
            }
            return;
        }

        // Kasus 4: Cover sudah terbuka dan punya sekring -> Pasang sekring
        if (coverTerbuka && punyaFuse)
        {
            PasangSekring();
        }
    }

    void PasangSekring()
    {
        fuseTerpasang = true;
        sudahDiperbaiki = true;

        if (audioSource != null && sfxPasangFuse != null)
        {
            audioSource.PlayOneShot(sfxPasangFuse);
        }

        if (objekFuseDalamSaklar != null)
        {
            objekFuseDalamSaklar.SetActive(true);
        }

        // Hapus sekring dari inventory karena sudah digunakan
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HapusItem(namaFuse);
        }

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Sekring baru terpasang! Sekarang saklar siap dinyalakan kembali)", 3f);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Nyalakan kembali saklar", "Tarik tuas saklar listrik");
        }
    }

    IEnumerator ProsesLampuNyalaStabil()
    {
        sedangProses = true;

        if (audioSource != null && sfxTuasDitarik != null)
        {
            audioSource.PlayOneShot(sfxTuasDitarik);
        }

        // Kembalikan tuas ke posisi awal (atau putar balik)
        if (engselTuas != null)
        {
            Quaternion rotasiAwal = engselTuas.localRotation;
            Quaternion rotasiAkhir = rotasiAwal * Quaternion.Euler(-sudutDitarik);
            float waktu = 0;
            while (waktu < 1f)
            {
                waktu += Time.deltaTime * kecepatanTarik;
                engselTuas.localRotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, waktu);
                yield return null;
            }
            engselTuas.localRotation = rotasiAkhir;
        }

        yield return new WaitForSeconds(0.3f);

        // Nyalakan lampu (hanya lampu gudang / ruangan saklar yang menyala)
        AturStatusLampu(true);
        AturIntensitasLampu(2.5f); 

        if (audioSource != null && sfxListrikNyalaStabil != null)
        {
            audioSource.PlayOneShot(sfxListrikNyalaStabil);
        }

        listrikMenyalaStabil = true; // Kunci permanen: listrik sudah stabil, tidak bisa ditarik lagi
        sedangProses = false;

        // Jeda 6 detik setelah lampu menyala sebelum player menyadari hanya lampu gudang yang menyala
        yield return new WaitForSeconds(6.0f);

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Kenapa hanya lampu gudang yang menyala...?)", 3.5f);
        }

        // Jeda 5 detik setelah subtitle muncul kemudian telepon berdering
        yield return new WaitForSeconds(5.0f);

        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.MulaiBerdering();
        }
    }

    // Fungsi bantuan untuk menghidupkan/mematikan objek & komponen Light
    private void AturStatusLampu(bool status)
    {
        foreach (Light lampu in lampuRuangan)
        {
            if (lampu != null) 
            {
                lampu.gameObject.SetActive(status); // Nyala/matikan objeknya
                lampu.enabled = status;             // Nyala/matikan komponennya
            }
        }
    }

    // Fungsi bantuan untuk mengatur nilai intensitas cahaya
    private void AturIntensitasLampu(float intensitas)
    {
        foreach (Light lampu in lampuRuangan)
        {
            if (lampu != null) lampu.intensity = intensitas;
        }
    }
}