using UnityEngine;

public class TriggerSubtitleBox : MonoBehaviour
{
    [Header("Pengaturan Subtitle")]
    [TextArea(2, 5)]
    [Tooltip("Teks subtitle yang akan muncul di layar")]
    public string teksSubtitle = "(Sepertinya ada yang aneh dengan tempat ini...)";

    [Tooltip("Berapa detik teks akan tampil di layar sebelum hilang")]
    public float durasiSubtitle = 3.0f;

    [Header("Pengaturan Trigger")]
    [Tooltip("Jika dicentang, hanya muncul 1x saja seumur permainan")]
    public bool hanyaSekali = true;

    [Header("Audio Suara Karakter / Bisikan (Opsional)")]
    public AudioSource audioSource;
    public AudioClip suaraVoiceOver;

    private bool sudahTerpicu = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (hanyaSekali && sudahTerpicu) return;

            sudahTerpicu = true;

            // 1. Munculkan teks lewat SubtitleManager Singleton bawaan project
            if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(teksSubtitle))
            {
                SubtitleManager.Instance.TampilkanSubtitle(teksSubtitle, durasiSubtitle);
            }

            // 2. Putar suara voice over/bisikan jika dipasang
            if (audioSource != null && suaraVoiceOver != null)
            {
                audioSource.PlayOneShot(suaraVoiceOver);
            }

            // 3. Jika hanya sekali dan tidak butuh trigger lagi, bisa matikan collider-nya
            if (hanyaSekali)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}
