using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DevMissionSkipUI : MonoBehaviour
{
    public static DevMissionSkipUI Instance;

    [Header("Referensi Player")]
    public PlayerController scriptPlayer;
    public Transform playerCamera;

    [Header("Font UI")]
    public TMP_FontAsset fontInterMedium;

    [Header("Audio Notifikasi")]
    public AudioClip sfxKlik;
    public AudioClip sfxSuksesLompat;
    private AudioSource audioSource;

    [Header("Status Panel")]
    public bool panelAktif = false;
    private GameObject canvasDevObj;
    private GameObject panelUtama;

    // Cache posisi awal game
    private Vector3 posisiAwalGame;
    private Quaternion rotasiAwalGame;
    private bool posisiAwalTercatat = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitDevPanel()
    {
        if (Instance == null && FindAnyObjectByType<DevMissionSkipUI>() == null)
        {
            GameObject devObj = new GameObject("[DEV] Mission Skip UI");
            devObj.AddComponent<DevMissionSkipUI>();
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        CariReferensiPlayer();
        SetupAudio();
        MuatFontInter();
    }

    void Start()
    {
        // Catat posisi awal player saat game pertama kali dijalankan
        if (scriptPlayer != null && !posisiAwalTercatat)
        {
            posisiAwalGame = scriptPlayer.transform.position;
            rotasiAwalGame = scriptPlayer.transform.rotation;
            posisiAwalTercatat = true;
        }

        // Buat UI Canvas Dev otomatis jika belum ada
        BangunUIDevOtomatis();
    }

    void Update()
    {
        bool ditekan = false;
        bool tekanEsc = false;
        bool tekanP = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                ditekan = true;
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                tekanEsc = true;
            }
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                tekanP = true;
            }
        }
        else
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.BackQuote)) ditekan = true;
                if (Input.GetKeyDown(KeyCode.Escape)) tekanEsc = true;
                if (Input.GetKeyDown(KeyCode.P)) tekanP = true;
            }
            catch {}
        }

        if (ditekan)
        {
            TogglePanel();
        }

        // Tombol Escape menutup panel dev jika sedang aktif
        if (panelAktif && tekanEsc)
        {
            TutupPanel();
        }

        // Dev: tekan P untuk langsung munculkan korek di tangan
        if (tekanP)
        {
            SiapkanPlayerKorek();
            MainkanSFXLompat();
            TampilkanNotifDev("Korek api di tangan");
        }
    }

    private void CariReferensiPlayer()
    {
        if (scriptPlayer == null) scriptPlayer = FindAnyObjectByType<PlayerController>();
        if (playerCamera == null && scriptPlayer != null) playerCamera = scriptPlayer.playerCamera;
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D Sound
        }

#if UNITY_EDITOR
        if (sfxKlik == null)
        {
            sfxKlik = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/Key pick.mp3");
        }
        if (sfxSuksesLompat == null)
        {
            sfxSuksesLompat = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/Key pick.mp3");
        }
#endif
    }

    private void MuatFontInter()
    {
        if (fontInterMedium != null) return;

#if UNITY_EDITOR
        fontInterMedium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter_18pt-Medium SDF.asset");
#endif
    }

    public void TogglePanel()
    {
        if (panelAktif) TutupPanel();
        else BukaPanel();
    }

    public void BukaPanel()
    {
        panelAktif = true;

        if (canvasDevObj != null) canvasDevObj.SetActive(true);
        if (panelUtama != null) panelUtama.SetActive(true);

        // Buka kursor mouse untuk klik tombol
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Kunci input pergerakan player
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
        }

        if (audioSource != null && sfxKlik != null)
        {
            audioSource.PlayOneShot(sfxKlik, 0.7f);
        }
    }

    public void TutupPanel()
    {
        panelAktif = false;

        if (panelUtama != null) panelUtama.SetActive(false);
        if (canvasDevObj != null) canvasDevObj.SetActive(false);

        // Kunci kursor kembali ke gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Kembalikan input pergerakan player
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = false;
            if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = true;
        }
    }

    // =========================================================================
    // IMPLEMENTASI LOMPAT CHECKPOINT MISI
    // =========================================================================

    /// <summary>
    /// Checkpoint 1: Awal Game (Pagi hari - Masuk rumah & tunggu di sofa)
    /// </summary>
    public void LompatKeMisi1_AwalGame()
    {
        ResetLayarHitam();
        TutupPanel();
        Debug.Log("<color=cyan>[DEV]</color> Melompat ke Misi 1: Masuk Rumah (Awal Game)");

        GameCheckpointManager.respawnDiTelepon = false;
        CutsceneSofa.barangBisaDiinteraksi = false;

        CutsceneSofa sofaPagi = FindAnyObjectByType<CutsceneSofa>();
        if (sofaPagi != null) sofaPagi.ResetKeKondisiPagiInstan();

        if (scriptPlayer != null)
        {
            scriptPlayer.sedangBawaBarang = false;
            if (scriptPlayer.visualTangan != null) scriptPlayer.visualTangan.SetActive(false);
            if (scriptPlayer.visualKorek != null) scriptPlayer.visualKorek.SetActive(false);

            if (posisiAwalTercatat)
            {
                PindahkanPlayer(posisiAwalGame, rotasiAwalGame);
            }
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Masuk ke dalam rumah", "Periksa bagian dalam rumah kakek");
        }

        MainkanSFXLompat();
        TampilkanNotifDev("Misi 1: Masuk Rumah (Awal Game)");
    }

    /// <summary>
    /// Checkpoint 2: Setelah Duduk di Sofa [Autosave 1] (Malam Tiba - Ambil Korek & Saklar)
    /// </summary>
    public void LompatKeMisi2_SetelahSofa()
    {
        ResetLayarHitam();
        TutupPanel();
        Debug.Log("<color=cyan>[DEV]</color> Melompat ke Misi 2: Setelah Duduk di Sofa (Malam Tiba) [Autosave]");

        // 1. Terapkan kondisi malam secara instan
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null)
        {
            sofa.TerapkanKondisiMalamInstan();
        }

        // 2. Korek langsung diberikan ke tangan player (tidak perlu ambil manual)
        SiapkanPlayerKorek();

        // 3. Pindahkan ke depan sofa ruang tamu
        if (scriptPlayer != null && sofa != null)
        {
            Vector3 posSofa = sofa.transform.position + (sofa.transform.forward * 1.5f);
            posSofa.y += 0.1f;
            PindahkanPlayer(posSofa, Quaternion.LookRotation(-sofa.transform.forward));
        }

        // 4. Reset telepon & seloker
        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.sudahSelesaiTelepon = false;
            TeleponRumah.Instance.sedangBisaDiangkat = false;
        }
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.ResetQuestState();
        }

        // 5. Update quest & picu autosave
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Ambil korek api di meja", "Gunakan sebagai sumber penerangan");
        }

        PicuAutosave();
        MainkanSFXLompat();
        TampilkanNotifDev("Misi 2: Malam Tiba (Setelah Duduk di Sofa)");
    }

    /// <summary>
    /// Checkpoint 3: Sebelum Melawan Seloker [Autosave 2] (Telepon Berdering)
    /// </summary>
    public void LompatKeMisi3_SebelumSeloker()
    {
        ResetLayarHitam();
        TutupPanel();
        Debug.Log("<color=cyan>[DEV]</color> Melompat ke Misi 3: Sebelum Seloker (Telepon Berdering) [Autosave]");

        // 1. Terapkan kondisi malam
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null) sofa.TerapkanKondisiMalamInstan();

        // 2. Set saklar listrik menyala stabil
        TuasListrik tuas = FindAnyObjectByType<TuasListrik>();
        if (tuas != null) tuas.SetKondisiListrikSudahMenyala();

        // 3. Matikan korek di meja & siapkan korek di tangan
        KorekMeja korek = FindAnyObjectByType<KorekMeja>();
        if (korek != null) korek.gameObject.SetActive(false);
        SiapkanPlayerKorek();

        // 4. Pindahkan player tepat ke depan telepon
        if (GameCheckpointManager.Instance != null && GameCheckpointManager.Instance.titikSpawnDepanTelepon != null)
        {
            PindahkanPlayer(GameCheckpointManager.Instance.titikSpawnDepanTelepon.position, GameCheckpointManager.Instance.titikSpawnDepanTelepon.rotation);
        }
        else if (TeleponRumah.Instance != null)
        {
            Vector3 pos = TeleponRumah.Instance.transform.position - (TeleponRumah.Instance.transform.forward * 1.2f);
            PindahkanPlayer(pos, TeleponRumah.Instance.transform.rotation);
        }

        // 5. Reset Seloker
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.ResetQuestState();
        }

        // 6. Buat telepon berdering
        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.sudahSelesaiTelepon = false;
            TeleponRumah.Instance.sedangBisaDiangkat = true;
            TeleponRumah.Instance.MulaiBerdering();
        }

        // 7. Update quest & autosave
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Angkat telepon yang berdering", "Cari sumber suara telepon di rumah");
        }

        PicuAutosave();
        MainkanSFXLompat();
        TampilkanNotifDev("Misi 3: Telepon Berdering (Sebelum Seloker)");
    }

    /// <summary>
    /// Checkpoint 4: Setelah Melawan Seloker [Autosave 3] (Toilet Selamat & Kunci Dapur Teka-Teki)
    /// </summary>
    public void LompatKeMisi4_SetelahSeloker()
    {
        ResetLayarHitam();
        TutupPanel();
        Debug.Log("<color=cyan>[DEV]</color> Melompat ke Misi 4: Setelah Melawan Seloker (Toilet Selamat / Kunci Dapur) [Autosave]");

        // 1. Malam & Listrik
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null) sofa.TerapkanKondisiMalamInstan();
        TuasListrik tuas = FindAnyObjectByType<TuasListrik>();
        if (tuas != null) tuas.SetKondisiListrikSudahMenyala();

        // 2. Korek di tangan aktif
        KorekMeja korek = FindAnyObjectByType<KorekMeja>();
        if (korek != null) korek.gameObject.SetActive(false);
        SiapkanPlayerKorek();

        // 3. Telepon selesai
        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.sudahSelesaiTelepon = true;
            TeleponRumah.Instance.sedangBisaDiangkat = false;
        }

        // 4. Seloker selesai & air surut
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.questAktif = false;
            SelokerQuestManager.Instance.questSelesai = true;
            if (SelokerQuestManager.Instance.toilet1 != null) SelokerQuestManager.Instance.toilet1.MatikanRembesanLangsung();
            if (SelokerQuestManager.Instance.toilet2 != null) SelokerQuestManager.Instance.toilet2.MatikanRembesanLangsung();
            if (SelokerQuestManager.Instance.audioSourceAmbient != null) SelokerQuestManager.Instance.audioSourceAmbient.Stop();

            // Munculkan kunci dapur
            if (SelokerQuestManager.Instance.objekKunciDapur != null)
            {
                SelokerQuestManager.Instance.objekKunciDapur.SetActive(true);
            }
        }

        // 5. Pindahkan player ke depan kunci dapur (bukan offset ngawur dari mesh toilet)
        Transform titikDapur = null;
        if (SelokerQuestManager.Instance != null)
        {
            if (SelokerQuestManager.Instance.titikSuaraDapur != null)
                titikDapur = SelokerQuestManager.Instance.titikSuaraDapur;
            else if (SelokerQuestManager.Instance.objekKunciDapur != null)
                titikDapur = SelokerQuestManager.Instance.objekKunciDapur.transform;
        }

        if (titikDapur != null)
        {
            TeleportDekatTitik(titikDapur, 1.7f);
        }
        else if (SelokerQuestManager.Instance != null && SelokerQuestManager.Instance.toilet1 != null)
        {
            TeleportDekatTitik(SelokerQuestManager.Instance.toilet1.transform, 2.2f);
        }

        // 6. Mulai quest teka-teki
        if (TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.MulaiQuestTekaTeki();
        }
        else if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Periksa Suara di Dapur", "Ada suara aneh terdengar dari arah dapur. Cari tahu apa yang terjadi.");
        }

        PicuAutosave();
        MainkanSFXLompat();
        TampilkanNotifDev("Misi 4: Setelah Seloker (Kunci Dapur & Teka-Teki)");
    }

    /// <summary>
    /// Checkpoint 5: Setelah Puzzle Jam Dinding Selesai [Terbaru] (Periksa Ruang Tengah)
    /// </summary>
    public void LompatKeMisi5_SetelahPuzzleJam()
    {
        ResetLayarHitam();
        TutupPanel();
        Debug.Log("<color=cyan>[DEV]</color> Melompat ke Misi 5: Setelah Puzzle Jam Dinding Selesai [Terbaru]");

        // Simpan titik ruang tengah DULU: objek "Cutscene Blokir tengah" dihancurkan saat malam diterapkan
        Vector3 posRuangTengah = Vector3.zero;
        Vector3 hadapRuangTengah = Vector3.forward;
        bool adaTitikRuangTengah = CobaAmbilTitikRuangTengah(out posRuangTengah, out hadapRuangTengah);

        // 1. Prasyarat sebelumnya
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null) sofa.TerapkanKondisiMalamInstan();
        TuasListrik tuas = FindAnyObjectByType<TuasListrik>();
        if (tuas != null) tuas.SetKondisiListrikSudahMenyala();
        KorekMeja korek = FindAnyObjectByType<KorekMeja>();
        if (korek != null) korek.gameObject.SetActive(false);
        SiapkanPlayerKorek();

        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.sudahSelesaiTelepon = true;
            TeleponRumah.Instance.sedangBisaDiangkat = false;
        }
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.questAktif = false;
            SelokerQuestManager.Instance.questSelesai = true;
            if (SelokerQuestManager.Instance.toilet1 != null) SelokerQuestManager.Instance.toilet1.MatikanRembesanLangsung();
            if (SelokerQuestManager.Instance.toilet2 != null) SelokerQuestManager.Instance.toilet2.MatikanRembesanLangsung();
            if (SelokerQuestManager.Instance.audioSourceAmbient != null) SelokerQuestManager.Instance.audioSourceAmbient.Stop();
        }

        // 2. Setel seluruh jam dinding ke posisi tepat
        JamDindingPuzzle[] semuaJam = FindObjectsByType<JamDindingPuzzle>();
        foreach (JamDindingPuzzle jam in semuaJam)
        {
            if (jam != null)
            {
                jam.ContextSetelKeTarget();
                jam.terkunci = true;
            }
        }

        if (PuzzleJamManager.Instance != null)
        {
            PuzzleJamManager.Instance.puzzleSelesai = true;
            // Buka peti secara instan
            if (PuzzleJamManager.Instance.tutupChest != null)
            {
                PuzzleJamManager.Instance.tutupChest.localRotation = PuzzleJamManager.Instance.tutupChest.localRotation * Quaternion.Euler(-50f, 0f, 0f);
            }
            if (PuzzleJamManager.Instance.kunciKamarAnak != null)
            {
                PuzzleJamManager.Instance.kunciKamarAnak.SetActive(true);
            }
            
            // Tukar objek penghalang instan
            if (PuzzleJamManager.Instance.objekPenghalangLama != null)
            {
                PuzzleJamManager.Instance.objekPenghalangLama.SetActive(false);
            }
            if (PuzzleJamManager.Instance.objekPenghalangBaru != null)
            {
                PuzzleJamManager.Instance.objekPenghalangBaru.SetActive(true);
            }
        }

        // 3. Laporkan ke TekaTekiManager
        if (TekaTekiManager.Instance != null)
        {
            TekaTekiManager.Instance.LaporkanTekaTekiSelesai("puzzle_jam_dinding");
        }

        // 4. Pindahkan player ke depan pintu ruang tengah (titik yang sudah di-cache)
        if (adaTitikRuangTengah)
        {
            Vector3 pos = posRuangTengah - hadapRuangTengah * 2.2f;
            pos.y = AmbilTinggiLantaiDalamRumah();
            PindahkanPlayer(pos, Quaternion.LookRotation(hadapRuangTengah));
        }

        // 5. Update quest & subtitle suara
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Periksa Peti", "Peti di ruangan ini telah terbuka. Ambil kunci di dalamnya untuk membuka kamar anak di lantai atas.");
        }
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Terdengar suara peti yang terbuka di dekat sini)", 4f);
        }

        PicuAutosave();
        MainkanSFXLompat();
        TampilkanNotifDev("Misi 5: Setelah Puzzle Jam Selesai (Periksa Peti) [Terbaru]");
    }

    // =========================================================================
    // FUNGSI BANTUAN
    // =========================================================================

    private void ResetLayarHitam()
    {
        // Hentikan coroutine CutsceneSofa yang mungkin sedang memudar layar ke hitam
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null) sofa.StopAllCoroutines();

        // Reset ScreenFader ke transparan instan
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.ResetKeTransparan();

        // Reset overlay hitam/putih jumpscare Seloker dan Game Over
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.StopAllCoroutines();
            if (SelokerQuestManager.Instance.blackoutCanvas != null)
                SelokerQuestManager.Instance.blackoutCanvas.alpha = 0f;
            if (SelokerQuestManager.Instance.flashPutihCanvas != null)
                SelokerQuestManager.Instance.flashPutihCanvas.alpha = 0f;
            if (SelokerQuestManager.Instance.panelGameOver != null)
                SelokerQuestManager.Instance.panelGameOver.SetActive(false);
        }

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.StopAllCoroutines();
            GameOverManager.Instance.sedangGameOver = false;
            if (GameOverManager.Instance.panelGameOver != null)
                GameOverManager.Instance.panelGameOver.SetActive(false);
            if (GameOverManager.Instance.flashPutihCanvas != null)
                GameOverManager.Instance.flashPutihCanvas.alpha = 0f;
        }
    }

    private void SiapkanPlayerKorek()
    {
        CariReferensiPlayer();
        if (scriptPlayer == null) return;

        scriptPlayer.sedangBawaBarang = true;
        if (scriptPlayer.tanganKanan != null) scriptPlayer.tanganKanan.SetActive(true);
        if (scriptPlayer.visualTangan != null) scriptPlayer.visualTangan.SetActive(true);
        if (scriptPlayer.visualKorek != null) scriptPlayer.visualKorek.SetActive(true);

        KorekMeja korekMeja = FindAnyObjectByType<KorekMeja>();
        if (korekMeja != null)
        {
            if (korekMeja.korekTanganPlayer != null)
                korekMeja.korekTanganPlayer.SetActive(true);
            korekMeja.gameObject.SetActive(false);
        }
    }

    private bool CobaAmbilTitikRuangTengah(out Vector3 posisi, out Vector3 hadap)
    {
        posisi = Vector3.zero;
        hadap = Vector3.forward;

        GameObject blokir = GameObject.Find("Cutscene Blokir tengah");
        if (blokir != null)
        {
            posisi = blokir.transform.position;
            hadap = ArahDatarMenghadap(blokir.transform);
            return true;
        }

        if (PuzzleJamManager.Instance != null && PuzzleJamManager.Instance.tutupChest != null)
        {
            posisi = PuzzleJamManager.Instance.tutupChest.position;
            hadap = ArahDatarMenghadap(PuzzleJamManager.Instance.tutupChest);
            return true;
        }

        return false;
    }

    private void TeleportDekatTitik(Transform target, float jarakMundur)
    {
        if (target == null) return;

        Vector3 hadap = ArahDatarMenghadap(target);
        Vector3 pos = target.position - (hadap * jarakMundur);
        pos.y = AmbilTinggiLantaiDalamRumah();
        PindahkanPlayer(pos, Quaternion.LookRotation(hadap));
    }

    private Vector3 ArahDatarMenghadap(Transform target)
    {
        Vector3 datar = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (datar.sqrMagnitude < 0.05f)
            datar = Vector3.ProjectOnPlane(target.up, Vector3.up);
        if (datar.sqrMagnitude < 0.05f)
            datar = Vector3.forward;
        return datar.normalized;
    }

    private float AmbilTinggiLantaiDalamRumah()
    {
        if (GameCheckpointManager.Instance != null && GameCheckpointManager.Instance.titikSpawnDepanTelepon != null)
            return GameCheckpointManager.Instance.titikSpawnDepanTelepon.position.y;
        if (scriptPlayer != null)
            return scriptPlayer.transform.position.y;
        return 0f;
    }

    private void PindahkanPlayer(Vector3 posisi, Quaternion rotasi)
    {
        CariReferensiPlayer();
        if (scriptPlayer == null) return;

        if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = false;

        scriptPlayer.transform.position = posisi;
        scriptPlayer.transform.rotation = rotasi;

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.identity;
            scriptPlayer.SinkronisasiRotasi(0f);
        }

        if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = true;
    }

    private void PicuAutosave()
    {
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TriggerAutosave();
        }
        else if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave();
        }
    }

    private void MainkanSFXLompat()
    {
        if (audioSource != null && sfxSuksesLompat != null)
        {
            audioSource.PlayOneShot(sfxSuksesLompat, 0.9f);
        }
    }

    private void TampilkanNotifDev(string namaMisi)
    {
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle($"[DEV CHECKPOINT]: Lompat ke {namaMisi}", 3.5f);
        }
    }

    // =========================================================================
    // GENERATOR UI RUNTIME MODERN
    // =========================================================================
    private void BangunUIDevOtomatis()
    {
        if (canvasDevObj != null) return;

        // 1. Root Canvas
        canvasDevObj = new GameObject("[UI_DEV] MissionSkipCanvas");
        Canvas canvas = canvasDevObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Selalu di atas UI lain

        CanvasScaler scaler = canvasDevObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasDevObj.AddComponent<GraphicRaycaster>();

        // Pastikan EventSystem ada di scene
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 2. Backdrop Gelap
        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(canvasDevObj.transform, false);
        Image bgImg = backdrop.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);
        RectTransform rtBackdrop = backdrop.GetComponent<RectTransform>();
        rtBackdrop.anchorMin = Vector2.zero;
        rtBackdrop.anchorMax = Vector2.one;
        rtBackdrop.offsetMin = Vector2.zero;
        rtBackdrop.offsetMax = Vector2.zero;

        // 3. Panel Utama
        panelUtama = new GameObject("PanelUtama");
        panelUtama.transform.SetParent(canvasDevObj.transform, false);
        Image panelImg = panelUtama.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);
        RectTransform rtPanel = panelUtama.GetComponent<RectTransform>();
        rtPanel.sizeDelta = new Vector2(720f, 620f);
        rtPanel.anchorMin = new Vector2(0.5f, 0.5f);
        rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
        rtPanel.pivot = new Vector2(0.5f, 0.5f);

        // Header Title
        BuatTeks(panelUtama.transform, "JudulDev", "=== DEVELOPER: SKIP CHECKPOINT MISI ===", 22, FontStyles.Bold, new Color(1f, 0.8f, 0.2f), new Vector2(0, 260), new Vector2(680, 40));
        BuatTeks(panelUtama.transform, "SubJudulDev", "Pilih checkpoint autosave untuk melompat langsung. Misi sebelumnya otomatis diselesaikan.", 13, FontStyles.Normal, new Color(0.75f, 0.75f, 0.8f), new Vector2(0, 230), new Vector2(680, 30));

        // Tombol-Tombol Misi
        float posY = 170f;
        float selisihY = 82f;

        BuatTombolMisi(panelUtama.transform, "BtnMisi1", "1. Masuk Rumah (Awal Game)", "Pagi hari - Masuk ke dalam rumah & tunggu di ruang tamu.", "[AWAL]", new Color(0.5f, 0.5f, 0.5f), posY, () => LompatKeMisi1_AwalGame());
        posY -= selisihY;

        BuatTombolMisi(panelUtama.transform, "BtnMisi2", "2. Setelah Duduk di Sofa", "Malam tiba - Sofa sudah diduduki, ambil korek api & saklar listrik.", "[AUTOSAVE 1]", new Color(0.2f, 0.8f, 0.9f), posY, () => LompatKeMisi2_SetelahSofa());
        posY -= selisihY;

        BuatTombolMisi(panelUtama.transform, "BtnMisi3", "3. Sebelum Melawan Seloker", "Listrik menyala stabil - Telepon berdering sebelum banjir toilet.", "[AUTOSAVE 2]", new Color(0.2f, 0.8f, 0.9f), posY, () => LompatKeMisi3_SebelumSeloker());
        posY -= selisihY;

        BuatTombolMisi(panelUtama.transform, "BtnMisi4", "4. Setelah Melawan Seloker", "Toilet selamat - Seloker dikalahkan, kunci dapur muncul & teka-teki aktif.", "[AUTOSAVE 3]", new Color(0.2f, 0.8f, 0.9f), posY, () => LompatKeMisi4_SetelahSeloker());
        posY -= selisihY;

        BuatTombolMisi(panelUtama.transform, "BtnMisi5", "5. Setelah Puzzle Jam Dinding Selesai", "5 Jam dinding selesai disetel - Suara benturan keras terdengar di ruang tengah.", "[TERBARU]", new Color(0.4f, 1f, 0.4f), posY, () => LompatKeMisi5_SetelahPuzzleJam());

        // Tombol Close
        GameObject btnCloseObj = new GameObject("BtnTutup");
        btnCloseObj.transform.SetParent(panelUtama.transform, false);
        Image btnCloseImg = btnCloseObj.AddComponent<Image>();
        btnCloseImg.color = new Color(0.7f, 0.15f, 0.15f, 0.9f);
        Button btnClose = btnCloseObj.AddComponent<Button>();
        RectTransform rtBtnClose = btnCloseObj.GetComponent<RectTransform>();
        rtBtnClose.sizeDelta = new Vector2(320f, 38f);
        rtBtnClose.anchoredPosition = new Vector2(0, -265f);
        btnClose.onClick.AddListener(TutupPanel);

        BuatTeks(btnCloseObj.transform, "TeksTutup", "[X] Tutup Panel (Tekan [ ` ])", 14, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(300, 30));

        // Sembunyikan seluruh canvas (termasuk backdrop gelap) di awal
        panelUtama.SetActive(false);
        canvasDevObj.SetActive(false);
    }

    private void BuatTombolMisi(Transform parent, string namaObj, string judul, string deskripsi, string labelBadge, Color warnaBadge, float posY, UnityEngine.Events.UnityAction aksi)
    {
        GameObject btnObj = new GameObject(namaObj);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.22f, 0.27f, 0.38f, 1f);
        cb.pressedColor = new Color(0.1f, 0.35f, 0.5f, 1f);
        btn.colors = cb;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(660f, 70f);
        rt.anchoredPosition = new Vector2(0, posY);

        btn.onClick.AddListener(aksi);

        // Judul
        BuatTeks(btnObj.transform, "Judul", judul, 15, FontStyles.Bold, Color.white, new Vector2(-60f, 14f), new Vector2(500f, 26f), TextAlignmentOptions.MidlineLeft);

        // Deskripsi
        BuatTeks(btnObj.transform, "Deskripsi", deskripsi, 12, FontStyles.Normal, new Color(0.7f, 0.72f, 0.78f), new Vector2(-60f, -14f), new Vector2(500f, 24f), TextAlignmentOptions.MidlineLeft);

        // Badge di sebelah kanan
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(btnObj.transform, false);
        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.color = new Color(warnaBadge.r * 0.2f, warnaBadge.g * 0.2f, warnaBadge.b * 0.2f, 0.85f);
        RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
        rtBadge.sizeDelta = new Vector2(110f, 26f);
        rtBadge.anchoredPosition = new Vector2(255f, 0);

        BuatTeks(badgeObj.transform, "TeksBadge", labelBadge, 11, FontStyles.Bold, warnaBadge, Vector2.zero, new Vector2(100f, 24f), TextAlignmentOptions.Center);
    }

    private TextMeshProUGUI BuatTeks(Transform parent, string namaObj, string konten, float ukuran, FontStyles gaya, Color warna, Vector2 posisi, Vector2 ukuranKotak, TextAlignmentOptions alig = TextAlignmentOptions.Center)
    {
        GameObject textObj = new GameObject(namaObj);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = konten;
        tmp.fontSize = ukuran;
        tmp.fontStyle = gaya;
        tmp.color = warna;
        tmp.alignment = alig;

        if (fontInterMedium != null)
        {
            tmp.font = fontInterMedium;
        }

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = ukuranKotak;
        rt.anchoredPosition = posisi;

        return tmp;
    }
}
