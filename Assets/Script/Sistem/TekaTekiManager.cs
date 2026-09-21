using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TekaTekiManager : MonoBehaviour
{
    public static TekaTekiManager Instance;

    [Header("Status Quest Teka-Teki")]
    [Tooltip("Apakah fase quest teka-teki sedang aktif")]
    public bool questTekaTekiAktif = false;

    [Tooltip("Total jumlah teka-teki yang harus diselesaikan di map")]
    public int totalTekaTeki = 3;

    [Tooltip("Jumlah teka-teki yang sudah berhasil diselesaikan")]
    public int tekaTekiTerselesaikan = 0;

    [Header("Tampilan Teks Quest")]
    public string judulQuestTekaTeki = "Selesaikan Seluruh Teka-Teki yang Ada";
    [TextArea(2, 3)]
    public string deskripsiQuestTekaTeki = "Cari dan selesaikan semua teka-teki yang tersembunyi di dalam mansion.";
    [Tooltip("Tampilkan jumlah progres (misal: 'Selesaikan Seluruh Teka-Teki (1/3)')")]
    public bool tampilkanCounterDiJudul = true;

    [Header("Quest Berikutnya (Setelah Seluruh Teka-Teki Selesai)")]
    public string judulQuestBerikutnya = "Buka Pintu Rahasia";
    [TextArea(2, 3)]
    public string deskripsiQuestBerikutnya = "Seluruh teka-teki telah terpecahkan! Temukan jalan keluar dari mansion.";
    public string subtitleSemuaSelesai = "(Semua teka-teki sudah terpecahkan! Ada suara mekanisme pintu terbuka di suatu tempat...)";

    [Header("Audio (Opsional)")]
    public AudioSource audioSource;
    public AudioClip sfxTekaTekiSelesai;       // Suara saat 1 puzzle selesai (chime/ding)
    public AudioClip sfxSemuaTekaTekiSelesai;  // Suara megah saat seluruh puzzle tuntas (mystery solved)

    [Header("Event Tambahan (Bisa hubungkan pintu/lampu di Inspector)")]
    public UnityEvent<string> onSatuTekaTekiSelesai;
    public UnityEvent onSemuaTekaTekiSelesai;

    // Catat ID teka-teki yang sudah selesai agar tidak bisa diklaim berulang kali
    private HashSet<string> daftarIdTekaTekiSelesai = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Dipanggil otomatis saat pemain mengambil kunci di dapur
    /// </summary>
    public void MulaiQuestTekaTeki()
    {
        questTekaTekiAktif = true;
        PerbaruiTeksQuest();
        Debug.Log("<color=cyan>[TekaTekiManager]</color> Quest Teka-Teki Dimulai! Total target: " + totalTekaTeki);
    }

    /// <summary>
    /// Panggil fungsi ini dari puzzle / teka-teki manapun di map saat pemain berhasil memecahkannya:
    /// TekaTekiManager.Instance.LaporkanTekaTekiSelesai("puzzle_jam_dinding");
    /// </summary>
    public void LaporkanTekaTekiSelesai(string idTekaTeki = "")
    {
        // Cegah klaim ganda untuk puzzle yang sama
        if (!string.IsNullOrEmpty(idTekaTeki))
        {
            if (daftarIdTekaTekiSelesai.Contains(idTekaTeki))
            {
                Debug.LogWarning($"[TekaTekiManager] Teka-teki '{idTekaTeki}' sudah pernah diselesaikan sebelumnya!");
                return;
            }
            daftarIdTekaTekiSelesai.Add(idTekaTeki);
        }

        tekaTekiTerselesaikan++;
        Debug.Log($"<color=green>[TekaTekiManager]</color> Teka-teki terpecahkan! Progres: {tekaTekiTerselesaikan}/{totalTekaTeki}");

        // Mainkan suara chime teka-teki
        if (audioSource != null && sfxTekaTekiSelesai != null)
        {
            audioSource.PlayOneShot(sfxTekaTekiSelesai);
        }

        // Panggil event per-puzzle
        onSatuTekaTekiSelesai?.Invoke(idTekaTeki);

        // Cek apakah seluruh teka-teki sudah selesai
        if (tekaTekiTerselesaikan >= totalTekaTeki)
        {
            SelesaikanSeluruhTekaTeki();
        }
        else
        {
            // Perbarui counter di teks quest
            PerbaruiTeksQuest();
        }
    }

    private void PerbaruiTeksQuest()
    {
        if (QuestManager.Instance == null) return;

        string judulTampil = judulQuestTekaTeki;
        if (tampilkanCounterDiJudul && totalTekaTeki > 0)
        {
            judulTampil = $"{judulQuestTekaTeki} ({tekaTekiTerselesaikan}/{totalTekaTeki})";
        }

        QuestManager.Instance.SetQuest(judulTampil, deskripsiQuestTekaTeki);
    }

    private void SelesaikanSeluruhTekaTeki()
    {
        questTekaTekiAktif = false;
        Debug.Log("<color=yellow>[TekaTekiManager]</color> SELURUH TEKA-TEKI SELESAI! Mengupdate ke quest berikutnya...");

        // 1. Suara penyelesaian seluruh teka-teki
        if (audioSource != null && sfxSemuaTekaTekiSelesai != null)
        {
            audioSource.PlayOneShot(sfxSemuaTekaTekiSelesai);
        }

        // 2. Subtitle dialog
        if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleSemuaSelesai))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitleSemuaSelesai, 5f);
        }

        // 3. Update quest ke tahap selanjutnya
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestBerikutnya))
        {
            QuestManager.Instance.SetQuest(judulQuestBerikutnya, deskripsiQuestBerikutnya);
        }

        // 4. Jalankan event Unity (misal buka pintu rahasia / nyalakan tangga)
        onSemuaTekaTekiSelesai?.Invoke();
    }

    [ContextMenu("TEST SELESAIKAN 1 TEKA-TEKI")]
    public void TestSelesaikanSatu()
    {
        LaporkanTekaTekiSelesai("test_puzzle_" + tekaTekiTerselesaikan);
    }

    [ContextMenu("TEST SELESAIKAN SEMUA TEKA-TEKI")]
    public void TestSelesaikanSemua()
    {
        tekaTekiTerselesaikan = totalTekaTeki;
        SelesaikanSeluruhTekaTeki();
    }
}
