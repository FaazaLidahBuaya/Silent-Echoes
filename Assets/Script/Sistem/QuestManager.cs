using System.Collections;
using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Referensi UI Text")]
    [Tooltip("Text TMP untuk menampilkan judul / instruksi quest")]
    public TextMeshProUGUI teksJudulQuest;
    [Tooltip("Text TMP untuk deskripsi atau instruksi (opsional)")]
    public TextMeshProUGUI teksDeskripsiQuest;

    [Header("Pengaturan Animasi Fade / Transisi")]
    public CanvasGroup canvasGroup;
    public float durasiFade = 0.5f;

    [Header("Audio (Opsional)")]
    public AudioSource audioSource;
    public AudioClip sfxQuestUpdate;

    [Header("Quest Awal")]
    public string questAwal = "Masuk ke dalam rumah";
    [TextArea] public string deskripsiAwal = "Periksa bagian dalam rumah kakek";

    private Coroutine transisiCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Tampilkan quest awal secara otomatis saat game dimulai
        if (!string.IsNullOrEmpty(questAwal))
        {
            SetQuest(questAwal, deskripsiAwal);
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari script mana saja untuk mengganti quest aktif:
    /// QuestManager.Instance.SetQuest("Tunggu kakek di ruang tamu");
    /// </summary>
    public void SetQuest(string judulBaru, string deskripsiBaru = "")
    {
        if (transisiCoroutine != null)
        {
            StopCoroutine(transisiCoroutine);
        }

        transisiCoroutine = StartCoroutine(ProsesGantiQuest(judulBaru, deskripsiBaru));
    }

    private IEnumerator ProsesGantiQuest(string judulBaru, string deskripsiBaru)
    {
        // Mainkan SFX notifikasi quest jika ada
        if (audioSource != null && sfxQuestUpdate != null)
        {
            audioSource.PlayOneShot(sfxQuestUpdate);
        }

        // Efek fade out jika ada CanvasGroup
        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < durasiFade)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / durasiFade);
                yield return null;
            }
        }

        // Update teks
        if (teksJudulQuest != null)
        {
            teksJudulQuest.text = "• " + judulBaru;
        }

        if (teksDeskripsiQuest != null)
        {
            teksDeskripsiQuest.text = deskripsiBaru;
            teksDeskripsiQuest.gameObject.SetActive(!string.IsNullOrEmpty(deskripsiBaru));
        }

        // Efek fade in
        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < durasiFade)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / durasiFade);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Sembunyikan quest (misalnya saat cutscene atau tamat)
    /// </summary>
    public void SembunyikanQuest()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        else if (teksJudulQuest != null)
        {
            teksJudulQuest.text = "";
        }
    }
}
