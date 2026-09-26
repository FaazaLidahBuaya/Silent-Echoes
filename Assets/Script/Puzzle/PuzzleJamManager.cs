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

    [Header("Titik Suara Ruang Tengah")]
    [Tooltip("Transform posisi sumber suara benturan di ruang tengah")]
    public Transform posisiRuangTengah;

    [Header("Pengaturan Audio Benturan (Brak)")]
    public AudioSource audioSourceBrak;
    public AudioClip sfxBrakRuangTengah;
    [Range(0f, 1f)] public float volumeBrak = 1.0f;
    public float jarakDengarBrak = 35f;

    [Header("Subtitle")]
    public bool tampilkanSubtitle = true;
    public string subtitleBrak = "*BRAKKK!!* (Ada suara benturan keras dari arah ruang tengah!)";
    public float durasiSubtitle = 4.0f;

    [Header("Sinkronisasi Quest")]
    public bool perbaruiQuest = true;
    public string judulQuestBerikutnya = "Periksa Ruang Tengah";
    [TextArea(2, 3)]
    public string deskripsiQuestBerikutnya = "Ada suara benturan keras dari arah ruang tengah. Cari tahu apa yang terjadi.";

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

        // 2. Cari titik ruang tengah jika belum ada
        if (posisiRuangTengah == null)
        {
            GameObject blokir = GameObject.Find("Cutscene Blokir tengah");
            if (blokir != null)
            {
                posisiRuangTengah = blokir.transform;
            }
            else
            {
                GameObject objTengah = GameObject.Find("RuangTengah");
                if (objTengah != null) posisiRuangTengah = objTengah.transform;
            }
        }

        // 3. Setup AudioSource 3D di titik ruang tengah
        if (audioSourceBrak == null)
        {
            GameObject wadahAudio = posisiRuangTengah != null ? posisiRuangTengah.gameObject : gameObject;
            audioSourceBrak = wadahAudio.GetComponent<AudioSource>();
            if (audioSourceBrak == null)
            {
                audioSourceBrak = wadahAudio.AddComponent<AudioSource>();
            }
            audioSourceBrak.playOnAwake = false;
            audioSourceBrak.spatialBlend = 1f; // 3D Audio
            audioSourceBrak.minDistance = 3f;
            audioSourceBrak.maxDistance = jarakDengarBrak;
            audioSourceBrak.rolloffMode = AudioRolloffMode.Linear;
        }

        // 4. Editor fallback untuk SFX Brak
#if UNITY_EDITOR
        if (sfxBrakRuangTengah == null)
        {
            sfxBrakRuangTengah = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Cutscene/distract.mp3");
            if (sfxBrakRuangTengah == null)
            {
                sfxBrakRuangTengah = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/woodHit.mp3");
            }
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

        Debug.Log("<color=green>[PuzzleJamManager]</color> SELURUH 5 JAM BERHASIL DISINKRONKAN! Memainkan suara BRAK di ruang tengah...");

        // 1. Kunci semua jam agar tidak berubah lagi
        foreach (JamDindingPuzzle jam in daftarJam)
        {
            if (jam != null) jam.terkunci = true;
        }

        // 2. Mainkan suara BRAK dari arah ruang tengah
        if (audioSourceBrak != null && sfxBrakRuangTengah != null)
        {
            audioSourceBrak.PlayOneShot(sfxBrakRuangTengah, volumeBrak);
        }
        else if (sfxBrakRuangTengah != null)
        {
            Vector3 pos = posisiRuangTengah != null ? posisiRuangTengah.position : transform.position;
            AudioSource.PlayClipAtPoint(sfxBrakRuangTengah, pos, volumeBrak);
        }

        // 3. Tampilkan Subtitle
        if (tampilkanSubtitle && SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleBrak))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitleBrak, durasiSubtitle);
        }

        // 4. Sinkronisasi dengan TekaTekiManager (jika aktif)
        if (laporkanKeTekaTekiManager && TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.LaporkanTekaTekiSelesai(idTekaTekiManager);
        }

        // 5. Perbarui Quest aktif
        if (perbaruiQuest && QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestBerikutnya))
        {
            QuestManager.Instance.SetQuest(judulQuestBerikutnya, deskripsiQuestBerikutnya);
        }

        // 6. Jalankan UnityEvent untuk kebutuhan level designer (buka pintu, trigger monster, dll)
        onSemuaJamSelesai?.Invoke();
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
