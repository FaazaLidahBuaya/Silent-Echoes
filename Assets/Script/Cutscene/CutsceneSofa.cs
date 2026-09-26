using System.Collections;
using UnityEngine;

public class CutsceneSofa : MonoBehaviour
{
    [Header("Referensi Player")]
    public PlayerController scriptPlayer;
    public Transform playerCamera;

    [Header("Pengaturan Posisi & Animasi")]
    public Transform titikDuduk;
    public Transform targetLihatKiri;
    public Transform targetLihatKanan;
    public Transform targetLihatDepan; 
    public float kecepatanJalanKeSofa = 2f; 
    public float durasiMenoleh = 1.5f;

    [Header("1. Pengaturan Skybox & Waktu (Sore -> Malam)")]
    [Tooltip("Material Skybox Sore (sunset) saat awal permainan")]
    public Material skyboxSore;
    [Tooltip("Material Skybox Malam yang aktif setelah cutscene sofa")]
    public Material skyboxMalam;
    public Color backgroundMalam = new Color(0.02f, 0.02f, 0.05f); 
    [Range(0f, 1f)] public float intensitasCahayaMalam = 0.2f; 
    public Color warnaFog = new Color(0.04f, 0.04f, 0.08f);        
    public float ketebalanFog = 0.02f;          
    public float jarakRenderMalam = 100f;        

    [Header("2. Pengaturan Event Pintu & Blocker")]
    public GameObject invisibleWall;            
    public GameObject triggerBlokirTengah;      
    public DoorController pintuUtama;           
    public DoorController[] pintuYangAkanTerbuka;

    [Header("3. Pengaturan Lingkungan & Barang")]
    public Light matahari;
    public Light[] daftarLampuRumah; // Daftar lampu dipertahankan
    public KorekMeja korekDiMeja; // Memanggil script korek yang ada di meja

    // VARIABEL GLOBAL UNTUK SISTEM INTERAKSI BARANG
    public static bool barangBisaDiinteraksi = false;

    public bool sudahTerpicu { get; private set; } = false;
    private Vector3 posisiAwalPlayer; // Menyimpan posisi awal pemain

    private void Awake()
    {
        // Jika sedang respawn setelah Game Over di telepon, langsung terapkan kondisi malam sejak frame pertama
        if (GameCheckpointManager.respawnDiTelepon)
        {
            TerapkanKondisiMalamInstan();
        }
    }

    private void Start()
    {
        // Jika sedang respawn di telepon, jangan kembalikan ke sore!
        if (GameCheckpointManager.respawnDiTelepon)
        {
            return;
        }

        // Pastikan di awal permainan barang belum bisa diinteraksi
        barangBisaDiinteraksi = false;

        // Pastikan di awal permainan menggunakan Skybox Sore
        if (skyboxSore != null)
        {
            RenderSettings.skybox = skyboxSore;
        }

        if (playerCamera != null)
        {
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }
        }
    }

    /// <summary>
    /// Menerapkan kondisi malam, pencahayaan, pintu, dan menonaktifkan sofa secara instan tanpa cutscene
    /// Digunakan saat respawn checkpoint di depan telepon
    /// </summary>
    public void TerapkanKondisiMalamInstan()
    {
        sudahTerpicu = true;

        // Nonaktifkan collider sofa agar tidak bisa diinteraksi sama sekali oleh raycast
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 1. Pengaturan visual malam & kamera
        if (playerCamera != null)
        {
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = (skyboxMalam != null) ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundMalam;
                cam.farClipPlane = jarakRenderMalam;
            }
        }

        if (skyboxMalam != null)
        {
            RenderSettings.skybox = skyboxMalam;
        }
        else
        {
            RenderSettings.skybox = null;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = backgroundMalam;
        RenderSettings.ambientIntensity = intensitasCahayaMalam;
        RenderSettings.reflectionIntensity = 0.1f;

        RenderSettings.fog = true;
        RenderSettings.fogColor = warnaFog;
        RenderSettings.fogDensity = ketebalanFog;

        DynamicGI.UpdateEnvironment();

        // 2. Matikan matahari & daftar lampu rumah
        if (matahari != null) matahari.enabled = false;

        if (daftarLampuRumah != null)
        {
            foreach (Light lampu in daftarLampuRumah)
            {
                if (lampu != null) lampu.enabled = false;
            }
        }

        // 3. Hancurkan invisible wall & trigger blocker
        if (invisibleWall != null) Destroy(invisibleWall);
        if (triggerBlokirTengah != null) Destroy(triggerBlokirTengah);

        // 4. Kunci pintu utama & buka pintu ruangan lain
        if (pintuUtama != null && pintuUtama.engselPintu != null)
        {
            pintuUtama.engselPintu.localRotation = Quaternion.Euler(0, 0, 0);
            pintuUtama.isTerkunci = true;
        }

        if (pintuYangAkanTerbuka != null)
        {
            foreach (DoorController pintu in pintuYangAkanTerbuka)
            {
                if (pintu != null) pintu.isTerkunci = false;
            }
        }

        // 5. Izinkan interaksi barang
        barangBisaDiinteraksi = true;

        // 6. Matikan/hancurkan korek di meja karena sudah dipegang player
        if (korekDiMeja != null)
        {
            korekDiMeja.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Kembalikan pencahayaan & skybox ke pagi/sore awal game.
    /// Dipakai saat dev skip kembali ke Misi 1 agar kamar tidak tertinggal gelap malam.
    /// </summary>
    public void ResetKeKondisiPagiInstan()
    {
        sudahTerpicu = false;
        barangBisaDiinteraksi = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        if (playerCamera != null)
        {
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
            }
        }

        if (skyboxSore != null)
        {
            RenderSettings.skybox = skyboxSore;
        }

        RenderSettings.fog = false;

        if (matahari != null) matahari.enabled = true;

        DynamicGI.UpdateEnvironment();
    }

    public void InteraksiSofa()
    {
        if (!sudahTerpicu)
        {
            StartCoroutine(JalankanCutsceneSofa());
        }
    }

    IEnumerator JalankanCutsceneSofa()
    {
        sudahTerpicu = true;

        // Simpan posisi awal sebelum pemain berjalan ke sofa
        if (scriptPlayer != null)
        {
            posisiAwalPlayer = scriptPlayer.transform.position;
        }

        // 1. KUNCI PLAYER
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
            scriptPlayer.controller.enabled = false;
        }
        
        // 2. BERGESER KE SOFA
        yield return StartCoroutine(BergerakKeTitikDuduk());

        // 3. ANIMASI MENOLEH
        yield return StartCoroutine(LookAtTargetSmoothly(targetLihatKanan, durasiMenoleh));
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(LookAtTargetSmoothly(targetLihatKiri, durasiMenoleh));
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(LookAtTargetSmoothly(targetLihatDepan, 1.5f));

        // 4. PEMAIN MENGANTUK (Berkedip 2x)
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.EfekBerkedip(2, 1.5f)); 
            yield return StartCoroutine(ScreenFader.Instance.TransisiLayar(true, 2.5f));
        }

        // ==========================================
        // 5. FASE "LOADING" & PERUBAHAN EVENT DUNIA
        // ==========================================
        
        yield return new WaitForSeconds(3.0f); 

        // --- A. PENGATURAN VISUAL MALAM & PENCAHAYAAN (MENGGUNAKAN SKYBOX MALAM) ---
        Camera cam = playerCamera.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = (skyboxMalam != null) ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor; 
            cam.backgroundColor = backgroundMalam;       
            cam.farClipPlane = jarakRenderMalam;          
        }
        
        if (skyboxMalam != null)
        {
            RenderSettings.skybox = skyboxMalam;
        }
        else
        {
            RenderSettings.skybox = null; 
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = backgroundMalam;
        RenderSettings.ambientIntensity = intensitasCahayaMalam; 
        RenderSettings.reflectionIntensity = 0.1f; 

        RenderSettings.fog = true;
        RenderSettings.fogColor = warnaFog;
        RenderSettings.fogDensity = ketebalanFog; 
        
        DynamicGI.UpdateEnvironment(); 

        // Matikan lampu & matahari
        if (matahari != null) matahari.enabled = false;
        
        // Matikan daftar lampu rumah
        foreach (Light lampu in daftarLampuRumah)
        {
            if (lampu != null) lampu.enabled = false;
        }

        // --- B. PENGATURAN EVENT (PINTU & BLOCKER) ---
        if (invisibleWall != null) Destroy(invisibleWall);
        if (triggerBlokirTengah != null) Destroy(triggerBlokirTengah);

        // Tutup paksa pintu utama & Kunci
        if (pintuUtama != null && pintuUtama.engselPintu != null)
        {
            pintuUtama.engselPintu.localRotation = Quaternion.Euler(0, 0, 0); 
            pintuUtama.isTerkunci = true;
        }

        // Buka kunci pintu-pintu ruangan yang sebelumnya diblokir
        foreach (DoorController pintu in pintuYangAkanTerbuka)
        {
            if (pintu != null) pintu.isTerkunci = false;
        }

        // Aktifkan izin interaksi barang-barang
        barangBisaDiinteraksi = true;

        yield return new WaitForSeconds(2.0f); 
        // ==========================================

        // 6. BANGUN TIDUR
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.TransisiLayar(false, 3f));
        }

        // 7. TELEPORT KEMBALI KE POSISI AWAL
        if (scriptPlayer != null)
        {
            scriptPlayer.transform.position = posisiAwalPlayer;
        }

        // 8. KEMBALIKAN KONTROL
        if (scriptPlayer != null && playerCamera != null)
        {
            float rotasiY_Akhir = playerCamera.eulerAngles.y;
            float rotasiX_Akhir = playerCamera.localEulerAngles.x;

            if (rotasiX_Akhir > 180f) rotasiX_Akhir -= 360f;

            scriptPlayer.transform.rotation = Quaternion.Euler(0, rotasiY_Akhir, 0);
            playerCamera.localRotation = Quaternion.Euler(rotasiX_Akhir, 0, 0);
            
            scriptPlayer.SinkronisasiRotasi(rotasiX_Akhir);
            scriptPlayer.controller.enabled = true;
            scriptPlayer.sedangCutscene = false;
        }

        // 9. NYALAKAN KOREK SETELAH TELEPORT SELESAI
        if (korekDiMeja != null)
        {
            korekDiMeja.NyalakanPerlahan();
        }

        // 10. UPDATE QUEST PETUNJUK
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Ambil korek api di meja", "Gunakan sebagai sumber penerangan");
        }

        // 11. AUTOSAVE SAAT PLAYER BANGUN
        if (GameCheckpointManager.Instance != null)
        {
            GameCheckpointManager.Instance.TriggerAutosave();
        }
        else if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave();
        }
    }

    IEnumerator BergerakKeTitikDuduk()
    {
        Vector3 posisiAwal = scriptPlayer.transform.position;
        Vector3 posisiAkhir = titikDuduk.position;
        float waktuBerjalan = 0f;
        float jarak = Vector3.Distance(posisiAwal, posisiAkhir);
        float durasiJalan = jarak / kecepatanJalanKeSofa;

        while (waktuBerjalan < durasiJalan)
        {
            waktuBerjalan += Time.deltaTime;
            float persentase = waktuBerjalan / durasiJalan;
            scriptPlayer.transform.position = Vector3.Lerp(posisiAwal, posisiAkhir, Mathf.SmoothStep(0f, 1f, persentase));
            yield return null;
        }
        scriptPlayer.transform.position = posisiAkhir;
    }

    IEnumerator LookAtTargetSmoothly(Transform target, float durasi)
    {
        Quaternion rotasiAwal = playerCamera.rotation;
        Vector3 arahTarget = target.position - playerCamera.position;
        Quaternion rotasiAkhir = Quaternion.LookRotation(arahTarget);

        float waktuBerjalan = 0f;
        while (waktuBerjalan < durasi)
        {
            waktuBerjalan += Time.deltaTime;
            float persentase = waktuBerjalan / durasi;
            float kurvaSmooth = Mathf.SmoothStep(0f, 1f, persentase);
            playerCamera.rotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, kurvaSmooth);
            yield return null;
        }
        playerCamera.rotation = rotasiAkhir;
    }
}