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
            // Tambahkan dokumen ke inventori dokumen
            DokumenManager.Instance.TambahDokumen(dataDokumen, langsungBukaSaatDiambil);

            // Putar suara ambil dokumen
            if (sfxAmbilDokumen != null && DokumenManager.Instance.audioSource != null)
            {
                DokumenManager.Instance.audioSource.PlayOneShot(sfxAmbilDokumen);
            }

            // Sembunyikan objek dokumen dari dunia 3D
            gameObject.SetActive(false);
        }
    }
}
