using System.Collections;
using UnityEngine;

public class TuasListrik : MonoBehaviour
{
    [Header("Referensi Lampu (Ruangan)")]
    [Tooltip("Pastikan Mode lampu di Inspector adalah REALTIME")]
    public Light[] lampuRuangan;

    [Header("Pengaturan Animasi Tuas")]
    public Transform engselTuas;
    public Vector3 sudutDitarik = new Vector3(90, 0, 0); 
    public float kecepatanTarik = 5f;

    [Header("Pengaturan Kedip Lampu")]
    [Tooltip("Berapa lama (dalam detik) lampu akan berkedip sebelum mati total")]
    public float durasiKedipTotal = 3.0f; // Kamu bisa ubah angkanya di Inspector

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxTuasDitarik;
    public AudioClip sfxListrikMati;

    private bool sudahDitarik = false;

    public void InteraksiTuas()
    {
        if (!sudahDitarik)
        {
            StartCoroutine(ProsesLampuMati());
        }
    }

    IEnumerator ProsesLampuMati()
    {
        sudahDitarik = true;

        // 1. SUARA & ANIMASI TUAS DITARIK
        if (audioSource != null && sfxTuasDitarik != null)
        {
            audioSource.PlayOneShot(sfxTuasDitarik);
        }
        
        if (engselTuas != null)
        {
            Quaternion rotasiAwal = engselTuas.localRotation;
            Quaternion rotasiAkhir = rotasiAwal * Quaternion.Euler(sudutDitarik);
            float waktu = 0;
            while (waktu < 1f)
            {
                waktu += Time.deltaTime * kecepatanTarik;
                engselTuas.localRotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, waktu);
                yield return null;
            }
            engselTuas.localRotation = rotasiAkhir; // Pastikan posisi akhirnya pas
        }

        // 2. NYALAKAN OBJEK LAMPU & MAINKAN SUARA LISTRIK MATI
        AturStatusLampu(true);
        if (audioSource != null && sfxListrikMati != null)
        {
            // Ubah dari PlayOneShot menjadi clip & Play, agar mudah di-Stop nanti
            audioSource.clip = sfxListrikMati;
            audioSource.Play();
        }

        // 3. EFEK KEDIP NAIK TURUN BERDASARKAN DURASI WAKTU
        float waktuMulaiKedip = Time.time;
        // Terus berkedip selama waktu saat ini belum melewati target durasi
        while (Time.time < waktuMulaiKedip + durasiKedipTotal)
        {
            // Acak intensitas cahaya antara 0 sampai 6
            float intensitasAcak = Random.Range(0f, 6f);
            AturIntensitasLampu(intensitasAcak);
            
            // Jeda acak antar kedipan agar terlihat tidak stabil (konslet)
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f)); 
        }

        // 4. LAMPU MATI TOTAL, HENTIKAN AUDIO PAKSA, & NONAKTIFKAN KOMPONEN
        AturIntensitasLampu(0f);
        AturStatusLampu(false);
        
        // Hentikan paksa sfx listrik matinya!
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Beri jeda sebentar (gelap) sebelum pemain ngomong
        yield return new WaitForSeconds(0.5f);

        // 5. MUNCULKAN SUBTITLE
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Sepertinya saklar nya rusak, aku harus memperbaikinya)", 3f);
        }
    }

    // Fungsi bantuan untuk menghidupkan/mematikan objek & komponen Light
    private void AturStatusLampu(bool status)
    {
        foreach (Light lampu in lampuRuangan)
        {
            if (lampu != null) 
            {
                lampu.gameObject.SetActive(status); // Nyala/matikan objeknya
                lampu.enabled = status;             // Nyala/matikan komponennya
            }
        }
    }

    // Fungsi bantuan untuk mengatur nilai intensitas cahaya
    private void AturIntensitasLampu(float intensitas)
    {
        foreach (Light lampu in lampuRuangan)
        {
            if (lampu != null) lampu.intensity = intensitas;
        }
    }
}