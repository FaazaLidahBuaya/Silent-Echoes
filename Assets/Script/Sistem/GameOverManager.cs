using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Referensi UI Game Over")]
    [Tooltip("Panel hitam penuh Game Over")]
    public GameObject panelGameOver;
    [Tooltip("Text tulisan GAME OVER (opsional jika ingin custom pesan)")]
    public TextMeshProUGUI teksGameOver;
    [Tooltip("Tombol Retry untuk mengulang dari checkpoint")]
    public Button tombolRetry;
    [Tooltip("Tombol Main Menu (opsional jika ada)")]
    public Button tombolMainMenu;

    [Header("Efek Flash Layar (Opsional)")]
    [Tooltip("Canvas putih untuk efek kilatan flash saat mati mendadak")]
    public CanvasGroup flashPutihCanvas;
    [Tooltip("AudioSource untuk memutar suara impact kematian")]
    public AudioSource audioSourceUI;
    public AudioClip sfxFlashImpact;

    [Header("Nama Scene Main Menu")]
    public string namaSceneMainMenu = "MainMenu";

    [HideInInspector]
    public bool sedangGameOver = false;

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
        // Pastikan saat game mulai, panel game over dan flash tersembunyi
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (flashPutihCanvas != null)
        {
            flashPutihCanvas.alpha = 0f;
            flashPutihCanvas.gameObject.SetActive(true);
        }

        if (tombolRetry != null)
        {
            tombolRetry.onClick.RemoveAllListeners();
            tombolRetry.onClick.AddListener(OnKlikRetry);
        }

        if (tombolMainMenu != null)
        {
            tombolMainMenu.onClick.RemoveAllListeners();
            tombolMainMenu.onClick.AddListener(OnKlikMainMenu);
        }
    }

    /// <summary>
    /// Panggil fungsi ini dari mana saja (Seloker, monster lain, jebakan, dll):
    /// GameOverManager.Instance.MemicuGameOver();
    /// </summary>
    /// <param name="pesanKematian">Pesan teks (opsional, default: GAME OVER)</param>
    /// <param name="pakaiFlashPutih">Jika true, layar menyala putih singkat sebelum hitam</param>
    public void MemicuGameOver(string pesanKematian = "GAME OVER", bool pakaiFlashPutih = true)
    {
        if (sedangGameOver) return;
        StartCoroutine(ProsesGameOverUniversal(pesanKematian, pakaiFlashPutih));
    }

    IEnumerator ProsesGameOverUniversal(string pesanKematian, bool pakaiFlashPutih)
    {
        sedangGameOver = true;

        // 1. Kunci Player Controller jika ada
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.sedangCutscene = true;
            if (player.controller != null) player.controller.enabled = false;
        }

        // 2. Efek White Screen (Layar Putih)
        if (pakaiFlashPutih)
        {
            if (audioSourceUI != null && sfxFlashImpact != null)
            {
                audioSourceUI.PlayOneShot(sfxFlashImpact);
            }

            // Layar langsung putih silau penuh di depan Game Over
            if (flashPutihCanvas != null)
            {
                flashPutihCanvas.alpha = 1f;
            }

            // Tunggu sepersekian detik (0.2 detik) saat layar masih putih pekat
            yield return new WaitForSeconds(0.2f);

            // Nyalakan panel Game Over di belakang white screen (tersembunyi di balik putih)
            if (panelGameOver != null)
            {
                panelGameOver.SetActive(true);
            }

            if (teksGameOver != null && !string.IsNullOrEmpty(pesanKematian))
            {
                teksGameOver.text = pesanKematian;
            }

            // Buka kursor mouse
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Tahan layar putih selama sisa durasi (total putih pekat ~1 detik)
            yield return new WaitForSeconds(0.8f);

            // Fade out layar putih secara mulus selama 0.4 detik (menyingkap panel Game Over di baliknya!)
            float tFadeFlash = 0f;
            while (tFadeFlash < 0.4f)
            {
                tFadeFlash += Time.deltaTime;
                if (flashPutihCanvas != null)
                {
                    flashPutihCanvas.alpha = Mathf.Lerp(1f, 0f, tFadeFlash / 0.4f);
                }
                yield return null;
            }

            if (flashPutihCanvas != null)
            {
                flashPutihCanvas.alpha = 0f;
            }
        }
        else
        {
            // Jika tanpa flash putih, langsung munculkan game over
            if (panelGameOver != null) panelGameOver.SetActive(true);
            if (teksGameOver != null && !string.IsNullOrEmpty(pesanKematian)) teksGameOver.text = pesanKematian;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnKlikRetry()
    {
        Time.timeScale = 1f;
        sedangGameOver = false;

        // Jika ada sistem checkpoint, manfaatkan untuk respawn
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TombolRetry();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnKlikMainMenu()
    {
        Time.timeScale = 1f;
        sedangGameOver = false;
        GameCheckpointManager.ResetAllCheckpointData();
        SceneManager.LoadScene(namaSceneMainMenu);
    }
}
