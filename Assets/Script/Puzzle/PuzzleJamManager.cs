using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PuzzleJamManager : MonoBehaviour
{
    public static PuzzleJamManager Instance;

    [Header("Daftar Jam (Otomatis jika kosong)")]
    [Tooltip("Daftar 5 jam dinding yang harus diselesaikan pemain")]
    public List<JamDindingPuzzle> daftarJam = new List<JamDindingPuzzle>();

    [Header("Pengaturan Chest (Peti)")]
    [Tooltip("Transform untuk tutup peti yang akan terbuka")]
    public Transform tutupChest;
    [Tooltip("GameObject kunci kamar anak yang ada di dalam peti")]
    public GameObject kunciKamarAnak;
    
    [Header("Pengaturan Pertukaran Objek (Tangga)")]
    public GameObject objekPenghalangLama;
    public GameObject objekPenghalangBaru;

    [Header("Pengaturan Audio (Opsional)")]
    public AudioSource audioSourceChest;
    public AudioClip sfxChestBuka;
    [Range(0f, 1f)] public float volumeSfx = 1.0f;

    [Header("Subtitle")]
    public bool tampilkanSubtitle = true;
    public string subtitlePeti = "(Terdengar suara peti yang terbuka di dekat sini)";
    public float durasiSubtitle = 4.0f;

    [Header("Sinkronisasi Quest")]
    public bool perbaruiQuest = true;
    public string judulQuestBerikutnya = "Periksa Peti";
    [TextArea(2, 3)]
    public string deskripsiQuestBerikutnya = "Peti di ruangan ini telah terbuka. Ambil kunci di dalamnya untuk membuka kamar anak di lantai atas.";

    [Header("Sinkronisasi TekaTekiManager")]
    public bool laporkanKeTekaTekiManager = true;
    public string idTekaTekiManager = "puzzle_jam_dinding";

    [Header("Status & Event")]
    public bool puzzleSelesai = false;
    public UnityEvent onSemuaJamSelesai;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InisialisasiManager();
    }

    void Start()
    {
        // Cek kondisi awal (jika misalnya diatur tepat dari awal)
        CekStatusPuzzle();
    }

    public void InisialisasiManager()
    {
        // 1. Temukan seluruh jam dinding jika daftar masih kosong
        if (daftarJam == null || daftarJam.Count == 0)
        {
            daftarJam = new List<JamDindingPuzzle>(FindObjectsByType<JamDindingPuzzle>());
            // Urutkan berdasarkan nama agar rapi
            daftarJam.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        }

        // 2. Setup Kunci agar tidak aktif saat game dimulai (kecuali puzzle sudah selesai)
        if (kunciKamarAnak != null && !puzzleSelesai)
        {
            kunciKamarAnak.SetActive(false);
        }

        if (!puzzleSelesai)
        {
            if (objekPenghalangLama != null) objekPenghalangLama.SetActive(true);
            if (objekPenghalangBaru != null) objekPenghalangBaru.SetActive(false);
        }

        // 3. Setup AudioSource
        if (audioSourceChest == null && tutupChest != null)
        {
            audioSourceChest = tutupChest.gameObject.GetComponent<AudioSource>();
            if (audioSourceChest == null)
            {
                audioSourceChest = tutupChest.gameObject.AddComponent<AudioSource>();
            }
            audioSourceChest.playOnAwake = false;
            audioSourceChest.spatialBlend = 1f; // 3D Audio
        }

        // 4. Editor fallback untuk SFX Buka Peti
#if UNITY_EDITOR
        if (sfxChestBuka == null)
        {
            sfxChestBuka = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/door_open.mp3");
            // Bisa diganti dengan sfx yang pas nantinya
        }
#endif
    }

    /// <summary>
    /// Memeriksa apakah seluruh 5 jam sudah disetel ke waktu target masing-masing
    /// </summary>
    public void CekStatusPuzzle()
    {
        if (puzzleSelesai) return;
        if (daftarJam == null || daftarJam.Count == 0) return;

        int jumlahTepat = 0;
        foreach (JamDindingPuzzle jam in daftarJam)
        {
            if (jam != null && jam.CekApakahTepat())
            {
                jumlahTepat++;
            }
        }

        Debug.Log($"<color=cyan>[PuzzleJamManager]</color> Progres Jam: {jumlahTepat}/{daftarJam.Count}");

        // Jika seluruh jam sudah tepat
        if (jumlahTepat >= daftarJam.Count)
        {
            SelesaikanPuzzle();
        }
    }

    private void SelesaikanPuzzle()
    {
        if (puzzleSelesai) return;
        puzzleSelesai = true;

        Debug.Log("<color=green>[PuzzleJamManager]</color> SELURUH 5 JAM BERHASIL DISINKRONKAN! Membuka peti...");

        // 1. Kunci semua jam agar tidak berubah lagi
        foreach (JamDindingPuzzle jam in daftarJam)
        {
            if (jam != null) jam.terkunci = true;
        }

        // 2. Mainkan suara buka peti
        if (audioSourceChest != null && sfxChestBuka != null)
        {
            audioSourceChest.PlayOneShot(sfxChestBuka, volumeSfx);
        }
        else if (sfxChestBuka != null)
        {
            Vector3 pos = tutupChest != null ? tutupChest.position : transform.position;
            AudioSource.PlayClipAtPoint(sfxChestBuka, pos, volumeSfx);
        }

        // 3. Tampilkan Subtitle
        if (tampilkanSubtitle && SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitlePeti))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitlePeti, durasiSubtitle);
        }

        // 4. Buka peti dan munculkan kunci
        if (tutupChest != null)
        {
            StartCoroutine(AnimasiBukaPeti());
        }
        
        if (kunciKamarAnak != null)
        {
            kunciKamarAnak.SetActive(true);
            Debug.Log("[PuzzleJamManager] Memunculkan kunci kamar anak di dalam peti.");
        }

        // Tukar objek penghalang
        if (objekPenghalangLama != null) objekPenghalangLama.SetActive(false);
        if (objekPenghalangBaru != null) objekPenghalangBaru.SetActive(true);

        // 5. Sinkronisasi dengan TekaTekiManager (jika aktif)
        if (laporkanKeTekaTekiManager && TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.LaporkanTekaTekiSelesai(idTekaTekiManager);
        }

        // 6. Perbarui Quest aktif
        if (perbaruiQuest && QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestBerikutnya))
        {
            QuestManager.Instance.SetQuest(judulQuestBerikutnya, deskripsiQuestBerikutnya);
        }

        // 7. Picu Autosave
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TriggerAutosave();
        }
        else if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave();
        }

        // 8. Jalankan UnityEvent untuk kebutuhan level designer (buka pintu, trigger monster, dll)
        onSemuaJamSelesai?.Invoke();
    }

    private IEnumerator AnimasiBukaPeti()
    {
        float durasi = 1.5f;
        float waktuBerjalan = 0f;
        
        Quaternion rotasiAwal = tutupChest.localRotation;
        // Asumsi peti dibuka pada sumbu X lokal (biasa untuk engsel peti)
        // Buka sekitar -50 derajat agar tidak terlalu keatas
        Quaternion rotasiTarget = rotasiAwal * Quaternion.Euler(-50f, 0f, 0f);

        while (waktuBerjalan < durasi)
        {
            tutupChest.localRotation = Quaternion.Slerp(rotasiAwal, rotasiTarget, waktuBerjalan / durasi);
            waktuBerjalan += Time.deltaTime;
            yield return null;
        }

        tutupChest.localRotation = rotasiTarget;
    }

    [ContextMenu("Tes Selesaikan Puzzle Jam")]
    public void TestSelesaikanSemuaJam()
    {
        InisialisasiManager();
        foreach (JamDindingPuzzle jam in daftarJam)
        {
            if (jam != null)
            {
                jam.ContextSetelKeTarget();
            }
        }
        SelesaikanPuzzle();
    }
}
