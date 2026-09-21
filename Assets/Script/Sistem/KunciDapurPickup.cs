using UnityEngine;

public class KunciDapurPickup : ItemPickup
{
    [Header("Pengaturan Quest Lanjutan")]
    [Tooltip("Subtitle yang muncul saat pemain memungut kunci ini")]
    public string subtitleSaatDiambil = "(Kunci kuno... Untuk apa kunci ini? Sepertinya berhubungan dengan teka-teki di rumah ini.)";

    [Tooltip("Judul quest baru setelah mengambil kunci")]
    public string judulQuestTekaTeki = "Selesaikan Seluruh Teka-Teki yang Ada";

    [Tooltip("Deskripsi quest baru")]
    [TextArea(2, 4)]
    public string deskripsiQuestTekaTeki = "Cari dan selesaikan seluruh teka-teki yang tersembunyi di dalam mansion.";

    void Reset()
    {
        namaItem = "Kunci Misterius";
    }

    public override void AmbilItem()
    {
        Debug.Log("<color=green>[KunciDapurPickup]</color> Kunci dapur berhasil diambil oleh player!");

        // 1. Jalankan proses ambil item standar (masuk inventory & sembunyikan objek)
        base.AmbilItem();

        // 2. Tampilkan subtitle dialog player
        if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleSaatDiambil))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitleSaatDiambil, 4.5f);
        }

        // 3. Update quest utama menjadi 'Selesaikan seluruh teka-teki'
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestTekaTeki))
        {
            QuestManager.Instance.SetQuest(judulQuestTekaTeki, deskripsiQuestTekaTeki);
        }

        // 4. Aktifkan sistem pelacak teka-teki jika ada di scene
        if (TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.MulaiQuestTekaTeki();
        }
    }

    [ContextMenu("TEST AMBIL KUNCI SEKARANG")]
    public void TestAmbilKunci()
    {
        AmbilItem();
    }
}
