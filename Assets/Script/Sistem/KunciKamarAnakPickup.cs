using UnityEngine;

public class KunciKamarAnakPickup : ItemPickup
{
    [Header("Pengaturan Quest (Setelah Diambil)")]
    public string judulQuestBaru = "Cari Jalan ke Lantai 2";
    [TextArea(2, 4)]
    public string deskripsiQuestBaru = "Kamu telah mendapatkan kunci kamar anak. Cari cara untuk naik ke lantai 2.";

    [Header("Subtitle (Opsional)")]
    public string subtitleSaatDiambil = "(Kunci kamar anak... Sekarang aku bisa mengecek ke atas.)";

    private void Reset()
    {
        namaItem = "Kunci Kamar Anak";
    }

    public override void AmbilItem()
    {
        Debug.Log("<color=green>[KunciKamarAnakPickup]</color> Kunci kamar anak diambil!");

        // 1. Masuk inventory & sembunyikan objek
        base.AmbilItem();

        // 2. Tampilkan subtitle
        if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleSaatDiambil))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitleSaatDiambil, 3.5f);
        }

        // 3. Update quest
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestBaru))
        {
            QuestManager.Instance.SetQuest(judulQuestBaru, deskripsiQuestBaru);
        }
    }
}
