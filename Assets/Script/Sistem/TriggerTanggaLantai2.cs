using UnityEngine;

public class TriggerTanggaLantai2 : MonoBehaviour
{
    [Header("Pengaturan Quest (Setelah Dekat Tangga)")]
    public string judulQuestBaru = "Selidiki Kamar Anak";
    [TextArea(2, 4)]
    public string deskripsiQuestBaru = "Rintangan di tangga ternyata sudah hilang. Naik ke lantai 2 dan periksa kamar anak menggunakan kunci yang ditemukan.";
    
    [Header("Subtitle Player Sadar")]
    public string subtitleSadar = "(Tunggu... Benda yang menghalangi tangga tadi sudah hilang?!)";

    [Header("Prasyarat")]
    [Tooltip("Apakah trigger ini hanya aktif jika puzzle jam sudah selesai?")]
    public bool butuhPuzzleSelesai = true;

    private bool sudahDitritigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if (sudahDitritigger) return;

        if (other.CompareTag("Player"))
        {
            // Cek prasyarat: puzzle jam sudah selesai DAN kunci sudah diambil
            if (butuhPuzzleSelesai)
            {
                bool puzzleSelesai = PuzzleJamManager.Instance != null && PuzzleJamManager.Instance.puzzleSelesai;
                bool kunciDiambil = PuzzleJamManager.Instance != null && PuzzleJamManager.Instance.kunciKamarAnak != null && !PuzzleJamManager.Instance.kunciKamarAnak.activeInHierarchy;
                
                if (!puzzleSelesai || !kunciDiambil)
                {
                    // Jangan trigger jika puzzle belum selesai atau kunci belum diambil
                    return;
                }
            }

            // Bisa juga cek apakah pemain punya item kunci di inventory jika inventory system mendukung,
            // tapi cek puzzleSelesai sudah cukup karena peti terbuka.
            
            sudahDitritigger = true;
            Debug.Log("<color=cyan>[TriggerTanggaLantai2]</color> Player mendekati tangga, mengupdate quest...");

            // Tampilkan subtitle
            if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleSadar))
            {
                SubtitleManager.Instance.TampilkanSubtitle(subtitleSadar, 4.0f);
            }

            // Ganti quest
            if (QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestBaru))
            {
                QuestManager.Instance.SetQuest(judulQuestBaru, deskripsiQuestBaru);
            }

            // Matikan trigger agar tidak kepanggil berkali-kali
            gameObject.SetActive(false);
        }
    }
}
