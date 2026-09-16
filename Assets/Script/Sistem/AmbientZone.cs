using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmbientZone : MonoBehaviour
{
    [Header("Pengaturan Audio Ruangan")]
    [Tooltip("Clip audio ambient untuk ruangan ini (outdoor, kamar, dapur, WC)")]
    public AudioClip ambientClip;

    [Range(0f, 1f)]
    [Tooltip("Volume relatif untuk ruangan ini")]
    public float volumeRuangan = 1.0f;

    [Tooltip("Durasi transisi fade (biarkan -1 untuk menggunakan settingan default di manager)")]
    public float customDurasiFade = -1f;

    [Header("Pengaturan Saat Keluar")]
    [Tooltip("Jika dicentang, audio akan fade-out sampai mati saat keluar jika tidak ada trigger lain")]
    public bool hentikanSaatKeluar = false;

    void Reset()
    {
        // Otomatis centang Is Trigger pada collider saat script dipasang
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (AmbientManager.Instance != null && ambientClip != null)
            {
                AmbientManager.Instance.GantiAmbient(ambientClip, volumeRuangan, customDurasiFade);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hentikanSaatKeluar)
        {
            if (AmbientManager.Instance != null)
            {
                AmbientManager.Instance.HentikanAmbient(customDurasiFade);
            }
        }
    }
}
