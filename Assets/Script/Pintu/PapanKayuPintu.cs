using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PapanKayuPintu : MonoBehaviour
{
    [Header("Item yang Dibutuhkan")]
    [Tooltip("Nama item di inventory yang dibutuhkan untuk mencopot papan kayu")]
    public string namaItemDibutuhkan = "Linggis";

    [Tooltip("Jika dicentang, linggis akan dihapus dari inventory setelah papan terbuka. Jika tidak dicentang, linggis tetap disimpan.")]
    public bool konsumsiLinggis = false;

    [Header("Objek Papan Kayu")]
    [Tooltip("Daftar objek papan kayu yang akan dihilangkan. Jika kosong, objek script ini sendiri yang akan dihilangkan.")]
    public GameObject[] objekPapanKayu;

    [Header("Pengaturan Audio")]
    public AudioSource audioSource;
    [Tooltip("Suara kayu patah / congkelan linggis saat berhasil dilepas")]
    public AudioClip sfxLepasPapan;
    [Tooltip("Suara kayu dipukul/diketuk saat gagal (belum punya linggis)")]
    public AudioClip sfxGagal;

    [Header("Pengaturan Subtitle (Opsional)")]
    public bool tampilkanSubtitle = true;
    [TextArea] public string teksButuhLinggis = "(Papan kayu ini dipaku kuat menghalangi pintu. Aku butuh linggis untuk membukanya.)";
    [TextArea] public string teksBerhasil = "(Papan kayu berhasil dilepas dengan linggis.)";
    public float durasiSubtitle = 2.5f;

    [Header("Event Tambahan (Opsional)")]
    public UnityEvent onPapanDilepas;

    [Header("Status")]
    public bool sudahDilepas = false;

    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Mengecek apakah player saat ini memiliki item Linggis di inventory
    /// </summary>
    public bool CekPunyaLinggis()
    {
        if (InventoryManager.Instance == null) return false;
        return InventoryManager.Instance.CekItem(namaItemDibutuhkan);
    }

    /// <summary>
    /// Dipanggil oleh PlayerInteract saat player menekan tombol E pada papan kayu atau pintu yang terhalang
    /// </summary>
    public void InteraksiPapan()
    {
        if (sudahDilepas) return;

        // 1. Cek apakah pemain memiliki linggis di inventory
        if (CekPunyaLinggis())
        {
            LepasPapanKayu();
        }
        else
        {
            GagalLepasPapan();
        }
    }

    private void LepasPapanKayu()
    {
        sudahDilepas = true;

        // Mainkan SFX congkel/lepas papan
        if (audioSource != null && sfxLepasPapan != null)
        {
            audioSource.PlayOneShot(sfxLepasPapan);
        }

        // Tampilkan subtitle keberhasilan
        if (tampilkanSubtitle && SubtitleManager.Instance != null && !string.IsNullOrEmpty(teksBerhasil))
        {
            SubtitleManager.Instance.TampilkanSubtitle(teksBerhasil, durasiSubtitle);
        }

        // Hapus linggis jika diatur sebagai item sekali pakai
        if (konsumsiLinggis && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.HapusItem(namaItemDibutuhkan);
        }

        // Matikan collider agar tidak menghalangi raycast ke pintu lagi
        if (col != null) col.enabled = false;

        // Hilangkan objek papan kayu
        StartCoroutine(ProsesHilangkanPapan());

        // Panggil event jika ada
        onPapanDilepas?.Invoke();
    }

    private void GagalLepasPapan()
    {
        // Mainkan SFX gagal / ketukan kayu
        if (audioSource != null && sfxGagal != null)
        {
            audioSource.PlayOneShot(sfxGagal);
        }

        // Tampilkan subtitle petunjuk butuh linggis
        if (tampilkanSubtitle && SubtitleManager.Instance != null && !string.IsNullOrEmpty(teksButuhLinggis))
        {
            SubtitleManager.Instance.TampilkanSubtitle(teksButuhLinggis, durasiSubtitle);
        }
    }

    private IEnumerator ProsesHilangkanPapan()
    {
        // Beri jeda sangat singkat (0.05s) agar suara congkelan mulai berbunyi
        yield return new WaitForSeconds(0.05f);

        // Jika ada list objek papan spesifik yang di-assign di Inspector
        if (objekPapanKayu != null && objekPapanKayu.Length > 0)
        {
            foreach (GameObject papan in objekPapanKayu)
            {
                if (papan != null)
                {
                    papan.SetActive(false);
                }
            }
        }
        else
        {
            // Jika tidak ada array papan khusus, sembunyikan gameObject ini sendiri
            // Tapi jika ada audio yang sedang berbunyi, sembunyikan visual & col-nya saja
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers) r.enabled = false;

            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;

            // Tunggu sampai audio selesai lalu matikan gameobject
            if (audioSource != null && sfxLepasPapan != null)
            {
                yield return new WaitForSeconds(sfxLepasPapan.length);
            }
            gameObject.SetActive(false);
        }
    }
}
