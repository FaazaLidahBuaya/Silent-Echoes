using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameCheckpointManager : MonoBehaviour
{
    public static GameCheckpointManager Instance;

    // Static flag untuk mengingat apakah pemain sedang respawn dari checkpoint telepon
    public static bool respawnDiTelepon = false;

    // Static flag untuk mengingat apakah gagang katup sudah terpasang
    public static bool gagangKatupSudahTerpasang = false;

    [Header("Referensi Checkpoint")]
    [Tooltip("Posisi player saat respawn tepat di depan telepon")]
    public Transform titikSpawnDepanTelepon;

    [Header("Referensi Komponen Utama")]
    public PlayerController scriptPlayer;
    public Transform playerCamera;

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
        // Jika sedang respawn setelah Game Over
        if (respawnDiTelepon)
        {
            StartCoroutine(TerapkanKondisiRespawnTelepon());
        }
    }

    /// <summary>
    /// Dipanggil otomatis saat player menekan [E] untuk mengangkat telepon
    /// </summary>
    public void SimpanCheckpointTelepon()
    {
        respawnDiTelepon = true;

        // Picu animasi autosave di pojok kanan bawah
        if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave();
        }
    }

    /// <summary>
    /// Fungsi umum untuk memicu efek animasi autosave kapan saja
    /// </summary>
    public void TriggerAutosave(float durasi = 3.5f)
    {
        if (AutosaveUI.Instance != null)
        {
            AutosaveUI.Instance.TriggerAutosave(durasi);
        }
    }

    /// <summary>
    /// Mengatur posisi player dan kondisi dunia game agar langsung berada di depan telepon yang berdering
    /// </summary>
    IEnumerator TerapkanKondisiRespawnTelepon()
    {
        // Beri waktu 1 frame agar semua script Awake/Start selesai inisialisasi
        yield return null;

        // 1. Terapkan kondisi malam dan nonaktifkan interaksi sofa
        CutsceneSofa sofa = FindAnyObjectByType<CutsceneSofa>();
        if (sofa != null)
        {
            sofa.TerapkanKondisiMalamInstan();
        }

        // 2. Set Tuas Listrik sudah diperbaiki dan lampu menyala
        TuasListrik tuas = FindAnyObjectByType<TuasListrik>();
        if (tuas != null)
        {
            tuas.SetKondisiListrikSudahMenyala();
        }

        // 3. Matikan korek di meja karena sudah dipegang player
        KorekMeja korek = FindAnyObjectByType<KorekMeja>();
        if (korek != null)
        {
            korek.gameObject.SetActive(false);
        }

        // 4. Matikan pickup Fuse dari dunia game (karena sudah dipasang di saklar tuas)
        ItemPickup[] semuaItemDiScene = FindObjectsByType<ItemPickup>(FindObjectsInactive.Include);
        foreach (ItemPickup item in semuaItemDiScene)
        {
            if (item != null && item.namaItem == "Fuse")
            {
                item.gameObject.SetActive(false);
            }
        }

        // 5. Restore semua item yang dimiliki pemain sebelum Game Over
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RestoreSemuaItemTersimpan();
        }

        // 6. Jika gagang katup sudah pernah dipasang sebelum mati, pastikan terpasang di katup 2
        if (gagangKatupSudahTerpasang)
        {
            if (SelokerQuestManager.Instance != null && SelokerQuestManager.Instance.katup2 != null)
            {
                SelokerQuestManager.Instance.katup2.sudahAdaGagang = true;
                if (SelokerQuestManager.Instance.katup2.visualGagang != null)
                {
                    SelokerQuestManager.Instance.katup2.visualGagang.SetActive(true);
                }
            }

            foreach (ItemPickup item in semuaItemDiScene)
            {
                if (item != null && item.namaItem == "Gagang Katup")
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        // 7. Pindahkan player tepat ke depan telepon
        if (titikSpawnDepanTelepon != null && scriptPlayer != null)
        {
            if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = false;

            scriptPlayer.transform.position = titikSpawnDepanTelepon.position;
            scriptPlayer.transform.rotation = titikSpawnDepanTelepon.rotation;

            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.identity;
                scriptPlayer.SinkronisasiRotasi(0f);
            }

            if (scriptPlayer.controller != null) scriptPlayer.controller.enabled = true;
        }

        // 8. Pastikan player memegang korek api
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangBawaBarang = true;
            if (scriptPlayer.visualTangan != null) scriptPlayer.visualTangan.SetActive(true);
            if (scriptPlayer.visualKorek != null) scriptPlayer.visualKorek.SetActive(true);
            scriptPlayer.sedangCutscene = false;
            scriptPlayer.bukaInventory = false;
        }

        // 9. Pastikan kursor terkunci kembali untuk gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 10. Reset quest toilet jika sebelumnya aktif
        if (SelokerQuestManager.Instance != null)
        {
            SelokerQuestManager.Instance.ResetQuestState();

            if (gagangKatupSudahTerpasang && SelokerQuestManager.Instance.katup2 != null)
            {
                SelokerQuestManager.Instance.katup2.sudahAdaGagang = true;
                if (SelokerQuestManager.Instance.katup2.visualGagang != null)
                {
                    SelokerQuestManager.Instance.katup2.visualGagang.SetActive(true);
                }
            }
        }

        // 11. Buat telepon berdering kembali agar player bisa langsung mengangkatnya
        if (TeleponRumah.Instance != null)
        {
            TeleponRumah.Instance.sudahSelesaiTelepon = false;
            TeleponRumah.Instance.sedangBisaDiangkat = true;
            TeleponRumah.Instance.MulaiBerdering();
        }
    }

    /// <summary>
    /// Reset semua data checkpoint saat kembali ke Main Menu atau mulai game baru dari awal
    /// </summary>
    public static void ResetAllCheckpointData()
    {
        respawnDiTelepon = false;
        gagangKatupSudahTerpasang = false;
        CutsceneSofa.barangBisaDiinteraksi = false;
        InventoryManager.ResetInventoryStatic();
    }

    /// <summary>
    /// Fungsi tombol Retry pada panel Game Over
    /// </summary>
    public void TombolRetry()
    {
        // Tetap tandai respawnDiTelepon = true
        respawnDiTelepon = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
