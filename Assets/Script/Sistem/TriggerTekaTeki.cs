using UnityEngine;
using UnityEngine.Events;

public class TriggerTekaTeki : MonoBehaviour
{
    [Header("Identitas Teka-Teki")]
    [Tooltip("ID unik teka-teki ini (misal: 'puzzle_jam', 'puzzle_lukisan')")]
    public string idTekaTeki = "teka_teki_01";

    [Tooltip("Teks prompt yang muncul saat kursor diarahkan ke objek ini")]
    public string promptInteraksi = "Periksa Teka-Teki";

    [Header("Status")]
    public bool sudahSelesai = false;

    [Header("Audio & Efek")]
    public AudioClip sfxBerhasil;
    public UnityEvent onBerhasilDipecahkan;

    /// <summary>
    /// Panggil fungsi ini saat teka-teki berhasil dipecahkan (bisa dari script puzzle khusus atau interaksi langsung)
    /// </summary>
    public void SelesaikanTekaTekiIni()
    {
        if (sudahSelesai) return;
        sudahSelesai = true;

        Debug.Log($"<color=green>[TriggerTekaTeki]</color> Teka-teki '{idTekaTeki}' berhasil dipecahkan!");

        // Mainkan suara jika ada
        if (sfxBerhasil != null)
        {
            AudioSource.PlayClipAtPoint(sfxBerhasil, transform.position);
        }

        // Jalankan event lokal (misal: menyalakan lampu / membuka laci)
        onBerhasilDipecahkan?.Invoke();

        // Laporkan ke Manager
        if (TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.LaporkanTekaTekiSelesai(idTekaTeki);
        }
    }

    /// <summary>
    /// Dipanggil saat player menekan tombol E
    /// </summary>
    public void InteraksiPlayer()
    {
        if (sudahSelesai) return;
        SelesaikanTekaTekiIni();
    }
}
