using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SelokerQuestManager : MonoBehaviour
{
    public static SelokerQuestManager Instance;

    [Header("Referensi Toilet 1 & 2")]
    public ToiletFlood toilet1;
    public ToiletFlood toilet2;
    public ToiletValve katup1;
    public ToiletValve katup2;

    [Header("Pemberitahuan & Trigger")]
    [Tooltip("Posisi lubang/pecahan WC 1 asal Seloker keluar jika WC 1 meluap")]
    public Transform titikWCPecah1; 
    [Tooltip("Posisi lubang/pecahan WC 2 asal Seloker keluar jika WC 2 meluap")]
    public Transform titikWCPecah2; 
    public Transform playerCamera;
    public PlayerController scriptPlayer;

    [Header("Jumpscare & Cutscene Kalah (FNAF Style)")]
    public GameObject modelSelokerJumpscare; // Model Seloker di depan kamera
    public Animator animatorSeloker;         // Animasi jumpscare FNAF
    public string namaStateJumpscare = "Jumpscare_FNAF"; // Nama state di Animator controller
    public Transform transformTanganPlayer;  // Objek tangan player (akan otomatis dicari jika kosong)
    public Light lampuJumpscareSeloker;      // Lampu sorot/penerang Seloker (opsional, jika ada)
    public float durasiMenoleh = 1.3f;       // Durasi menoleh ke arah toilet
    [Tooltip("Kurva animasi menoleh kepala ke arah toilet jebol (Mulus dan sinematik)")]
    public AnimationCurve kurvaMenoleh = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public CanvasGroup blackoutCanvas;       // Layar hitam (kedip-mati-nyala)
    public CanvasGroup flashPutihCanvas;     // Panel UI putih untuk efek flash kilat
    public GameObject panelGameOver;         // Panel UI Game Over dengan tulisan GAME OVER dan tombol RETRY
    public Button tombolRetry;               // Tombol Retry di panel Game Over
    public AudioSource audioSourceGlobal;
    public AudioClip sfxAirBocorToilet2;     // Suara air mendesak bocor dari arah toilet 2
    public AudioClip sfxLayarBergetar;       // Suara gemuruh / getaran kamera (rumble)
    public AudioClip sfxFnafScreamer;        // Suara teriakan screamer FNAF
    public AudioClip sfxFlashPutih;          // Suara hantaman / sting saat flash putih menyala singkat (impact)

    [Header("Audio Ambient Khusus Event Seloker")]
    [Tooltip("Audio ambient/tensi mencekam yang berputar selama event Seloker berlangsung")]
    public AudioClip ambientSeloker;
    public AudioSource audioSourceAmbient;
    [Range(0f, 1f)] public float volumeAmbient = 0.65f;
    public float durasiFadeAmbient = 2f;
    private Coroutine coroutineFadeAmbient;

    [Header("Status Quest")]
    public bool questAktif = false;
    public bool toilet2BocorDimulai = false;
    public bool questSelesai = false;
    private bool sedangProsesGameOver = false;

    [Header("Fase Maintenance (Bertahan Bersama)")]
    [Tooltip("Durasi bertahan setelah katup 2 pertama kali ditutup (detik). 150 = 2.5 menit.")]
    public float durasiMaintenance = 150f;
    [Tooltip("Kecepatan melonggar katup di fase maintenance: 1 / 35 detik = 0.0286f")]
    public float kecepatanMelonggarMaintenance = 0.0286f; // Tepat 35 detik dari 100% ke 0%!
    [Tooltip("Jeda aman setelah putar katup di fase maintenance")]
    public float delayMelonggarMaintenance = 4f; // 4 detik jeda aman setelah mentok
    private bool faseMaintenance = false;
    private float timerMaintenance = 0f;

    [Header("Event Lanjutan: Suara & Kunci Dapur")]
    [Tooltip("Waktu tunggu setelah Seloker kalah sebelum suara di dapur terdengar")]
    public float delaySuaraDapur = 4.5f;
    [Tooltip("Titik lokasi suara benda jatuh di dapur (3D audio)")]
    public Transform titikSuaraDapur;
    [Tooltip("Efek suara benda jatuh / gaduh di dapur")]
    public AudioClip sfxSuaraDapur;
    [Tooltip("Objek Kunci di dapur yang akan dimunculkan")]
    public GameObject objekKunciDapur;
    [Tooltip("Subtitle yang muncul saat mendengar suara dapur")]
    public string subtitleDapur = "(Suara apa itu dari arah dapur...?)";
    [Tooltip("Judul quest untuk memeriksa dapur")]
    public string judulQuestDapur = "Periksa Suara di Dapur";
    [Tooltip("Deskripsi quest untuk memeriksa dapur")]
    public string deskripsiQuestDapur = "Ada suara aneh terdengar dari arah dapur. Cari tahu apa yang terjadi.";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (modelSelokerJumpscare != null) modelSelokerJumpscare.SetActive(false);
        if (blackoutCanvas != null)
        {
            blackoutCanvas.alpha = 0f;
            blackoutCanvas.gameObject.SetActive(true);
        }

        if (flashPutihCanvas != null)
        {
            flashPutihCanvas.alpha = 0f;
            flashPutihCanvas.gameObject.SetActive(true);
        }

        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (tombolRetry != null)
        {
            tombolRetry.onClick.AddListener(OnKlikTombolRetry);
        }

        // Kunci di dapur disembunyikan terlebih dahulu di awal game
        if (objekKunciDapur != null)
        {
            objekKunciDapur.SetActive(false);
        }

        // Listener saat katup 1 berhasil ditutup pertama kali
        if (katup1 != null)
        {
            katup1.onTightenedMax.AddListener(OnKatup1DitutupPertamaKali);
        }

        // Listener saat katup 2 berhasil ditutup
        if (katup2 != null)
        {
            katup2.onTightenedMax.AddListener(OnKatup2Ditutup);
        }

        // Siapkan AudioSource Ambient 2D khusus event Seloker
        if (audioSourceAmbient == null)
        {
            audioSourceAmbient = gameObject.AddComponent<AudioSource>();
        }
        audioSourceAmbient.loop = true;
        audioSourceAmbient.playOnAwake = false;
        audioSourceAmbient.spatialBlend = 0f; // 2D background atmosphere
    }

    /// <summary>
    /// Panggil fungsi ini untuk memulai quest kebocoran toilet (misal setelah telepon selesai)
    /// </summary>
    public void MulaiQuestToilet()
    {
        if (questAktif) return;
        questAktif = true;
        Debug.Log("<color=cyan>[SelokerQuest]</color> MulaiQuestToilet() dipanggil! questAktif = true");

        // Mulai audio ambient tensi horor khusus event Seloker
        MulaiAmbientSeloker();

        // Inisialisasi paksa katup 1: pastikan state awal bersih
        if (katup1 != null)
        {
            katup1.bisaMelonggarSendiri = false;
            katup1.timerLonggar = 0f;
            katup1.isMaxTight = false;
            katup1.tightness = 0.5f; // Mulai dari 50%
            Debug.Log("<color=cyan>[SelokerQuest]</color> katup1 direset. tightness=" + katup1.tightness);
        }
        else
        {
            Debug.LogError("<color=red>[SelokerQuest]</color> katup1 NULL! Belum di-assign di Inspector SelokerQuestManager!");
        }

        // Aktifkan luapan toilet 1 & mesh rembesannya — mulai dari 0.25f
        if (toilet1 != null)
        {
            toilet1.MulaiBanjir(0.25f);
            Debug.Log("<color=cyan>[SelokerQuest]</color> toilet1 mulai flooding dengan initial 0.25f.");
        }
        else
        {
            Debug.LogError("<color=red>[SelokerQuest]</color> toilet1 NULL! Belum di-assign di Inspector!");
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Periksa Toilet Yang Meluap", "Putar katup pipa toilet sebelum air membanjiri rumah");
        }

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Suara air meluap dari arah toilet! Aku harus mematikan krannya!)", 4f);
        }
    }

    void Update()
    {
        // =========================================================================
        // TOMBOL CEPAT DEBUG TEST (Play Mode):
        // - Huruf 'K' : Uji Coba Jumpscare Instan
        // - Huruf 'L' : Selesaikan Event Seloker Seketika (Instant Win)
        // =========================================================================
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
            {
                TestJumpscareInstan();
            }
            if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
            {
                SelesaikanQuestInstan();
            }
        }

        if (!questAktif || questSelesai || sedangProsesGameOver) return;

        // === CEK KATUP 1 SUDAH MAKS → PICU TOILET 2 ===
        if (!toilet2BocorDimulai && katup1 != null && katup1.isMaxTight)
        {
            Debug.Log("<color=yellow>[SelokerQuest]</color> Katup1 isMaxTight=true! Memicu ProsesToilet2Bocor...");
            StartCoroutine(ProsesToilet2Bocor());
        }

        // === FASE MAINTENANCE: Countdown tersembunyi (Hanya tampil di Console Debug.Log) ===
        if (faseMaintenance)
        {
            int detikSebelum = Mathf.FloorToInt(timerMaintenance);
            timerMaintenance -= Time.deltaTime;
            int detikSekarang = Mathf.FloorToInt(timerMaintenance);

            // Tampilkan sisa waktu di Console setiap 15 detik agar developer bisa pantau
            if (detikSekarang != detikSebelum && detikSekarang > 0 && detikSekarang % 15 == 0)
            {
                int menit = detikSekarang / 60;
                int detik = detikSekarang % 60;
                Debug.Log(string.Format("<color=yellow>[SelokerQuest Timer]</color> Sisa waktu bertahan: {0}:{1:00} ({2} detik)", menit, detik, detikSekarang));
            }

            // Waktu habis → MENANG!
            if (timerMaintenance <= 0f)
            {
                Debug.Log("<color=green>[SelokerQuest]</color> 2.5 Menit Berlalu! Player berhasil selamat!");
                SelesaikanQuest();
                return;
            }
        }

        // Cek jika ada toilet yang sudah penuh 100% (Game Over)
        float level1 = (toilet1 != null) ? toilet1.floodLevel : 0f;
        float level2 = (toilet2 != null && toilet2BocorDimulai) ? toilet2.floodLevel : 0f;

        if (level1 >= 1f)
        {
            StartCoroutine(ProsesGameOverFNAF(1));
        }
        else if (level2 >= 1f)
        {
            StartCoroutine(ProsesGameOverFNAF(2));
        }
    }

    [ContextMenu("TEST JUMPSCARE SEKARANG")]
    public void TestJumpscareInstan()
    {
        if (!sedangProsesGameOver)
        {
            Debug.Log("<color=red>[DEBUG]</color> Memicu Test Jumpscare Seloker!");
            StartCoroutine(ProsesGameOverFNAF(1));
        }
    }

    [ContextMenu("SELESAIKAN EVENT SELOKER SEKARANG (INSTANT WIN)")]
    public void SelesaikanQuestInstan()
    {
        Debug.Log("<color=green>[DEBUG/CHEAT]</color> Memaksa Event Seloker Selesai Seketika!");
        questAktif = true;
        SelesaikanQuest();
    }

    void OnKatup1DitutupPertamaKali()
    {
        if (!toilet2BocorDimulai)
        {
            StartCoroutine(ProsesToilet2Bocor());
        }
    }

    IEnumerator ProsesToilet2Bocor()
    {
        toilet2BocorDimulai = true; // Set SEKARANG agar Update tidak memicu coroutine ini lagi
        yield return new WaitForSeconds(2.5f);


        // Suara air mendesak bocor dari arah toilet 2
        if (sfxAirBocorToilet2 != null)
        {
            Vector3 posToilet2 = (toilet2 != null) ? toilet2.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(sfxAirBocorToilet2, posToilet2);
        }

        // Katup 1 sekarang bisa melonggar perlahan (dibuat lebih tegang)
        if (katup1 != null)
        {
            katup1.bisaMelonggarSendiri = true;
            katup1.kecepatanMelonggar = 0.018f; // ~55 detik
            katup1.delaySebelumMelonggar = 8f;
            katup1.timerLonggar = 8f;
        }

        // Toilet 2 mulai meluap & nyalakan mesh rembesan toilet 2
        if (toilet2 != null)
        {
            toilet2.MulaiBanjir(0.12f);
        }

        // Katup 2 sebelum dipasang gagang: dibuat lebih tegang!
        if (katup2 != null)
        {
            katup2.tightness = 1.0f;
            katup2.isMaxTight = false;
            katup2.timerLonggar = 10f;          // Jeda 10 detik sebelum mulai turun (tegang!)
            katup2.bisaMelonggarSendiri = true;
            katup2.kecepatanMelonggar = 0.018f; // Cukup cepat merosot (~55 detik dari 100% ke 0%)
        }

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Suara air lagi?! Sekarang dari toilet kedua!)", 4f);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Periksa Toilet Kedua", "Toilet kedua bocor! Cari gagang katup dan perbaiki sebelum meluap!");
        }
    }

    void OnKatup2Ditutup()
    {
        // Hanya aktifkan fase maintenance SEKALI (saat katup 2 pertama kali ditutup penuh)
        if (!faseMaintenance && toilet2BocorDimulai)
        {
            MulaiFaseMaintenance();
        }
    }

    void MulaiFaseMaintenance()
    {
        faseMaintenance = true;
        timerMaintenance = durasiMaintenance;

        // Tepat 35 detik dari 100% ke 0%: 1 / 35 = 0.0286f
        kecepatanMelonggarMaintenance = 0.0286f; 
        delayMelonggarMaintenance = 4f; // 4 detik jeda aman

        Debug.Log("<color=green>[SelokerQuest]</color> FASE MAINTENANCE dimulai! Bertahan " + durasiMaintenance + " detik! Kecepatan Melonggar=" + kecepatanMelonggarMaintenance + " (35 detik dari 100% ke 0%)");

        // Kedua katup sekarang masuk ke mode tantangan 35 detik
        if (katup1 != null)
        {
            katup1.bisaMelonggarSendiri = true;
            katup1.kecepatanMelonggar = kecepatanMelonggarMaintenance;
            katup1.delaySebelumMelonggar = delayMelonggarMaintenance;
            katup1.timerLonggar = delayMelonggarMaintenance; // jeda aman sebelum mulai longgar lagi
        }

        if (katup2 != null)
        {
            katup2.kecepatanMelonggar = kecepatanMelonggarMaintenance;
            katup2.delaySebelumMelonggar = delayMelonggarMaintenance;
            katup2.timerLonggar = delayMelonggarMaintenance;
        }

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle(
                "(Pipa semakin bergetar keras... Aku harus menjaga kedua katup tetap tertutup!)", 5f);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest(
                "Jaga Kedua Katup!",
                "Pipa terus bergetar hebat! Jaga kedua katup toilet jangan sampai meluap!"
            );
        }
    }


    public void SelesaikanQuest()
    {
        if (questSelesai) return;
        questSelesai = true;

        if (katup1 != null) katup1.bisaMelonggarSendiri = false;
        if (katup2 != null) katup2.bisaMelonggarSendiri = false;

        // Hilangkan mesh rembesan dan surutkan air secara fade out halus
        if (toilet1 != null) toilet1.SelesaikanBanjirFade(2.5f);
        if (toilet2 != null) toilet2.SelesaikanBanjirFade(2.5f);

        // Hentikan suara ambient tensi Seloker secara halus
        HentikanAmbientSeloker(2.5f);

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Syukurlah... airnya berhenti keluar dan mulai surut kembali.)", 4f);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Selamat", "Kedua toilet sudah berhasil dikendalikan");
        }

        // Picu Autosave lengkap dengan animasi logo di pojok layar
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TriggerAutosave();
        }
        else if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave();
        }

        // Jalankan event lanjutan: Suara mencurigakan dari arah dapur & kemunculan kunci
        StartCoroutine(ProsesEventDapurSetelahMenang());
    }

    public void MulaiAmbientSeloker()
    {
        if (audioSourceAmbient == null)
        {
            audioSourceAmbient = gameObject.AddComponent<AudioSource>();
            audioSourceAmbient.loop = true;
            audioSourceAmbient.playOnAwake = false;
            audioSourceAmbient.spatialBlend = 0f;
        }

#if UNITY_EDITOR
        if (ambientSeloker == null)
        {
            ambientSeloker = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/BGM/WC.mp3");
        }
#endif

        if (ambientSeloker != null)
        {
            audioSourceAmbient.clip = ambientSeloker;
            audioSourceAmbient.volume = 0f;
            audioSourceAmbient.Play();
            if (coroutineFadeAmbient != null) StopCoroutine(coroutineFadeAmbient);
            coroutineFadeAmbient = StartCoroutine(ProsesFadeAmbient(volumeAmbient, durasiFadeAmbient, false));
        }
    }

    public void HentikanAmbientSeloker(float durasi = 2f)
    {
        if (audioSourceAmbient != null && audioSourceAmbient.isPlaying)
        {
            if (coroutineFadeAmbient != null) StopCoroutine(coroutineFadeAmbient);
            coroutineFadeAmbient = StartCoroutine(ProsesFadeAmbient(0f, durasi, true));
        }
    }

    private IEnumerator ProsesFadeAmbient(float targetVolume, float durasi, bool stopAtEnd)
    {
        if (audioSourceAmbient == null) yield break;
        float startVol = audioSourceAmbient.volume;
        float elapsed = 0f;
        while (elapsed < durasi)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSourceAmbient.volume = Mathf.Lerp(startVol, targetVolume, elapsed / durasi);
            yield return null;
        }
        audioSourceAmbient.volume = targetVolume;
        if (stopAtEnd)
        {
            audioSourceAmbient.Stop();
        }
        coroutineFadeAmbient = null;
    }

    private IEnumerator ProsesEventDapurSetelahMenang()
    {
        // Tunggu beberapa detik setelah rasa aman menang melawan Seloker
        yield return new WaitForSeconds(delaySuaraDapur);

        // 1. Putar suara mencurigakan (benda jatuh/gaduh) dari arah dapur (3D audio)
        if (sfxSuaraDapur != null)
        {
            Vector3 posDapur = (titikSuaraDapur != null) ? titikSuaraDapur.position : transform.position;
            AudioSource.PlayClipAtPoint(sfxSuaraDapur, posDapur);
        }

        // 2. Tampilkan subtitle player
        if (SubtitleManager.Instance != null && !string.IsNullOrEmpty(subtitleDapur))
        {
            SubtitleManager.Instance.TampilkanSubtitle(subtitleDapur, 4f);
        }

        // 3. Munculkan kunci di dapur yang sebelumnya tidak ada di sana
        if (objekKunciDapur != null)
        {
            objekKunciDapur.SetActive(true);
            Debug.Log("<color=cyan>[SelokerQuestManager]</color> Kunci di dapur berhasil dimunculkan!");
        }

        // 4. Update quest pemain agar memeriksa ke dapur
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(judulQuestDapur))
        {
            QuestManager.Instance.SetQuest(judulQuestDapur, deskripsiQuestDapur);
        }
    }

    [ContextMenu("TEST EVENT DAPUR SEKARANG")]
    public void TestEventDapurInstan()
    {
        Debug.Log("<color=yellow>[DEBUG]</color> Memicu Test Event Suara Dapur & Kemunculan Kunci!");
        StartCoroutine(ProsesEventDapurSetelahMenang());
    }

    // =========================================================================
    // SEKUEN GAME OVER + JUMPSCARE FNAF STYLE
    // =========================================================================
    IEnumerator ProsesGameOverFNAF(int toiletPecahIndex)
    {
        sedangProsesGameOver = true;

        // Matikan suara ambient & suara air seketika agar jumpscare & impact menggelegar
        if (audioSourceAmbient != null) audioSourceAmbient.Stop();
        if (toilet1 != null) toilet1.MatikanRembesanLangsung();
        if (toilet2 != null) toilet2.MatikanRembesanLangsung();

        // 0. PASTIKAN KOREK API MENYALA TERANG & TIDAK BISA MATI (AGAR MONSTER SELALU TERLIHAT!)
        KorekTangan korek = FindAnyObjectByType<KorekTangan>();
        if (korek != null)
        {
            korek.PaksaNyalakanKorek(true);
        }

        if (scriptPlayer != null)
        {
            if (scriptPlayer.visualTangan != null) scriptPlayer.visualTangan.SetActive(true);
            if (scriptPlayer.visualKorek != null) scriptPlayer.visualKorek.SetActive(true);
            if (scriptPlayer.tanganKanan != null) scriptPlayer.tanganKanan.SetActive(true);
        }

        // Tentukan transform tangan player untuk digetarkan
        Transform targetTangan = transformTanganPlayer;
        if (targetTangan == null && scriptPlayer != null)
        {
            if (scriptPlayer.tanganKanan != null) targetTangan = scriptPlayer.tanganKanan.transform;
            else if (scriptPlayer.visualTangan != null) targetTangan = scriptPlayer.visualTangan.transform;
        }
        Vector3 posAsliTangan = (targetTangan != null) ? targetTangan.localPosition : Vector3.zero;

        // Tentukan titik toilet mana yang pecah
        Transform targetWCPecah = (toiletPecahIndex == 2) ? titikWCPecah2 : titikWCPecah1;
        if (targetWCPecah == null) targetWCPecah = titikWCPecah1 != null ? titikWCPecah1 : titikWCPecah2;

        // 1. Kunci player & nonaktifkan kontrol
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
            if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = false;
        }

        // 2. Kamera otomatis menoleh ke arah lubang/pecahan WC yang jebol dengan kurva mulus (tidak kaku)
        if (targetWCPecah != null && playerCamera != null)
        {
            Quaternion rotAwal = playerCamera.rotation;
            Vector3 arahWC = (targetWCPecah.position - playerCamera.position).normalized;
            if (arahWC.sqrMagnitude > 0.001f)
            {
                Quaternion rotTarget = Quaternion.LookRotation(arahWC);

                float tLook = 0f;
                float durasi = (durasiMenoleh > 0.1f) ? durasiMenoleh : 1.3f;
                while (tLook < durasi)
                {
                    tLook += Time.deltaTime;
                    float progress = Mathf.Clamp01(tLook / durasi);

                    // Evaluasi kurva (Ease-In-Out) agar kepala menoleh dengan inersia alami
                    float tKurva = (kurvaMenoleh != null && kurvaMenoleh.length > 0)
                        ? kurvaMenoleh.Evaluate(progress)
                        : Mathf.SmoothStep(0f, 1f, progress);

                    playerCamera.rotation = Quaternion.Slerp(rotAwal, rotTarget, tKurva);
                    yield return null;
                }
                playerCamera.rotation = rotTarget;
            }
        }

        // 3. Layar bergetar (Camera Shake) heboh tersinkronisasi DENGAN TANGAN PLAYER IKUT BERGETAR
        float durasiGetar = 1.8f;
        if (sfxLayarBergetar != null)
        {
            durasiGetar = sfxLayarBergetar.length;
            if (audioSourceGlobal != null)
            {
                audioSourceGlobal.PlayOneShot(sfxLayarBergetar);
            }
        }

        Vector3 posAsliKamera = (playerCamera != null) ? playerCamera.localPosition : Vector3.zero;
        float waktuGetar = 0f;
        while (waktuGetar < durasiGetar)
        {
            waktuGetar += Time.deltaTime;
            float progress = waktuGetar / durasiGetar;
            float kekuatan = Mathf.Lerp(0.02f, 0.09f, progress);
            
            if (playerCamera != null)
                playerCamera.localPosition = posAsliKamera + (Random.insideUnitSphere * kekuatan);

            // Tangan pemain gemetar hebat karena panik/takut!
            if (targetTangan != null)
            {
                float tremorTangan = Mathf.Lerp(0.015f, 0.065f, progress);
                targetTangan.localPosition = posAsliTangan + (Random.insideUnitSphere * tremorTangan);
            }

            yield return null;
        }
        if (playerCamera != null) playerCamera.localPosition = posAsliKamera;
        if (targetTangan != null) targetTangan.localPosition = posAsliTangan;

        // 4. KETIKA SFX SELESAI -> LANGSUNG CUT KE LAYAR HITAM (Instant Blackout)
        if (blackoutCanvas != null) blackoutCanvas.alpha = 1f;

        yield return new WaitForSeconds(2.0f); // Sunyi senyap dalam gelap gulita

        // 5. LAYAR NYALA KEMBALI SECARA SMOOTH FADE (Fade in pelan -> Player Merasa Aman)
        // Pastikan korek tetap menyala saat layar kembali terang
        if (korek != null) korek.PaksaNyalakanKorek(true);
        if (scriptPlayer != null)
        {
            if (scriptPlayer.visualTangan != null) scriptPlayer.visualTangan.SetActive(true);
            if (scriptPlayer.visualKorek != null) scriptPlayer.visualKorek.SetActive(true);
        }

        float tFadeNyala = 0f;
        while (tFadeNyala < 0.9f)
        {
            tFadeNyala += Time.deltaTime;
            if (blackoutCanvas != null) blackoutCanvas.alpha = Mathf.Lerp(1f, 0f, tFadeNyala / 0.9f);
            yield return null;
        }
        if (blackoutCanvas != null) blackoutCanvas.alpha = 0f;

        yield return new WaitForSeconds(1.6f); // Player mengira sudah aman... hening...

        // 6. TEPAT DI DEPAN MATA: JUMPSCARE SELOKER MENYAMBAR (FNAF STYLE)
        if (korek != null) korek.PaksaNyalakanKorek(true);

        if (lampuJumpscareSeloker != null)
            lampuJumpscareSeloker.gameObject.SetActive(true);

        if (modelSelokerJumpscare != null)
        {
            modelSelokerJumpscare.SetActive(true);

            // Jika Seloker punya lampu anak, ikut nyalakan
            Light selokerLight = modelSelokerJumpscare.GetComponentInChildren<Light>(true);
            if (selokerLight != null) selokerLight.gameObject.SetActive(true);

            if (animatorSeloker != null && animatorSeloker.runtimeAnimatorController != null)
            {
                if (animatorSeloker.HasState(0, Animator.StringToHash(namaStateJumpscare)))
                {
                    animatorSeloker.Play(namaStateJumpscare, 0, 0f);
                }
                else
                {
                    animatorSeloker.Play(0, 0, 0f);
                }
            }
        }

        if (audioSourceGlobal != null && sfxFnafScreamer != null)
        {
            audioSourceGlobal.PlayOneShot(sfxFnafScreamer);
        }

        // Goncang kamera dan tangan cepat saat jumpscare
        float tJump = 0f;
        while (tJump < 1.0f)
        {
            tJump += Time.deltaTime;
            if (playerCamera != null) playerCamera.localPosition = posAsliKamera + (Random.insideUnitSphere * 0.12f);
            if (targetTangan != null) targetTangan.localPosition = posAsliTangan + (Random.insideUnitSphere * 0.08f);
            yield return null;
        }
        if (playerCamera != null) playerCamera.localPosition = posAsliKamera;
        if (targetTangan != null) targetTangan.localPosition = posAsliTangan;

        // 7. FLASH PUTIH & GAME OVER UNIVERSAL
        if (modelSelokerJumpscare != null)
        {
            modelSelokerJumpscare.SetActive(false);
        }
        if (lampuJumpscareSeloker != null)
        {
            lampuJumpscareSeloker.gameObject.SetActive(false);
        }

        // Panggil sistem Game Over Universal
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.MemicuGameOver("GAME OVER", true);
        }
        else
        {
            // Fallback jika belum ada GameOverManager di scene
            if (sfxFlashPutih != null && audioSourceGlobal != null) audioSourceGlobal.PlayOneShot(sfxFlashPutih);
            if (flashPutihCanvas != null) flashPutihCanvas.alpha = 1f;
            yield return new WaitForSeconds(0.15f);
            if (blackoutCanvas != null) blackoutCanvas.alpha = 1f;
            if (flashPutihCanvas != null) flashPutihCanvas.alpha = 0f;
            yield return new WaitForSeconds(1.0f);
            if (panelGameOver != null) panelGameOver.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void OnKlikTombolRetry()
    {
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TombolRetry();
        }
        else
        {
            GameCheckpointManager.respawnDiTelepon = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ResetQuestState()
    {
        questAktif = false;
        toilet2BocorDimulai = false;
        questSelesai = false;
        sedangProsesGameOver = false;

        if (modelSelokerJumpscare != null) modelSelokerJumpscare.SetActive(false);
        if (lampuJumpscareSeloker != null) lampuJumpscareSeloker.gameObject.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (blackoutCanvas != null) blackoutCanvas.alpha = 0f;
        if (audioSourceAmbient != null) audioSourceAmbient.Stop();

        if (toilet1 != null)
        {
            toilet1.MatikanRembesanLangsung();
        }
        if (toilet2 != null)
        {
            toilet2.MatikanRembesanLangsung();
        }

        if (katup1 != null)
        {
            katup1.tightness = 0f;
            katup1.isMaxTight = false;
            katup1.bisaMelonggarSendiri = false;
        }
        if (katup2 != null)
        {
            katup2.tightness = 0f;
            katup2.isMaxTight = false;
            katup2.bisaMelonggarSendiri = false;

            // Jika sebelum Game Over gagang katup 2 sudah terpasang, pertahankan statusnya
            if (GameCheckpointManager.gagangKatupSudahTerpasang)
            {
                katup2.sudahAdaGagang = true;
                if (katup2.visualGagang != null) katup2.visualGagang.SetActive(true);
            }
        }
    }
}
