using UnityEngine;

public class DokumenPickup : MonoBehaviour
{
    [Header("Data Dokumen")]
    public DokumenItem dataDokumen;

    [Header("Opsi Interaksi")]
    [Tooltip("Jika dicentang, game akan langsung di-pause dan membuka panel baca saat diambil")]
    public bool langsungBukaSaatDiambil = true;

    [Header("Audio (Opsional)")]
    public AudioClip sfxAmbilDokumen;

    public void AmbilDokumen()
    {
        if (DokumenManager.Instance != null)
        {
            // Tambahkan dokumen ke inventori dokumen (suara diatur satu pintu di DokumenManager agar tidak dobel)
            DokumenManager.Instance.TambahDokumen(dataDokumen, langsungBukaSaatDiambil, sfxAmbilDokumen);

            // Sembunyikan objek dokumen dari dunia 3D
            gameObject.SetActive(false);
        }
    }
}
