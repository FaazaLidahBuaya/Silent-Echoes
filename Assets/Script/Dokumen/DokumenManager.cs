using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class DokumenManager : MonoBehaviour
{
    public static DokumenManager Instance;

    [Header("Referensi Player")]
    public PlayerController scriptPlayer;

    [Header("Tombol Buka Catatan / Dokumen")]
    [Tooltip("Tombol keyboard untuk membuka/menutup dokumen (Default: J untuk Journal)")]
    public Key tombolBuka = Key.J;

    [Header("1. Panel Utama Sistem")]
    [Tooltip("Panel utama yang menampung seluruh tampilan dokumen")]
    public GameObject panelUtama;

    [Header("2. Tampilan Daftar Dokumen")]
    [Tooltip("Tampilan daftar / grid dokumen yang sudah dikoleksi")]
    public GameObject panelDaftarDokumen;
    public Transform tempatDaftarTombol;
    public GameObject prefabTombolDokumen;
    [Tooltip("Teks jika belum ada dokumen yang diambil")]
    public GameObject teksDaftarKosong;

    [Header("3. Tampilan Pembaca Dokumen (2 Panel Bersebelahan)")]
    [Tooltip("Panel pembaca dokumen (2 Kolom: Kiri Foto, Kanan Teks)")]
    public GameObject panelBacaDokumen;

    [Header("--- Kolom Kiri: Foto / Gambar Halaman Dokumen ---")]
    [Tooltip("UI Image untuk menampilkan foto/gambar halaman dokumen")]
    public Image gambarHalamanUI;
    [Tooltip("Tombol halaman sebelumnya (<)")]
    public Button tombolPrev;
    [Tooltip("Tombol halaman berikutnya (>)")]
    public Button tombolNext;
    [Tooltip("Teks nomor halaman (misal: 'Halaman 1 / 2')")]
    public TextMeshProUGUI teksNomorHalaman;

    [Header("--- Kolom Kanan: Penjelasan & Transkrip Teks ---")]
    [Tooltip("Teks judul dokumen di kolom kanan")]
    public TextMeshProUGUI teksJudul;
    [Tooltip("Teks penjelasan/transkrip agar tulisan tangan mudah terbaca")]
    public TextMeshProUGUI teksPenjelasan;

    [Header("Font UI")]
    public TMP_FontAsset fontInterMedium;

    [Header("Audio (Opsional)")]
    public AudioSource audioSource;
    public AudioClip sfxBukaBuku;
    public AudioClip sfxBalikHalaman;

    // Data penyimpanan statis agar dokumen tidak hilang saat reload scene / checkpoint
    public static HashSet<string> idDokumenTersimpan = new HashSet<string>();
    private List<DokumenItem> daftarDokumenKoleksi = new List<DokumenItem>();
    private DokumenItem dokumenSedangDibaca;
    private int indeksHalamanAktif = 0;
    [HideInInspector] public bool sedangBukaDokumen = false;
    [HideInInspector] public int frameTerakhirBuka = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Reset tracking statis saat scene dimuat ulang agar data segar
        idDokumenTersimpan.Clear();
        CariKomponenDaftarOtomatis();

        try
        {
            PerbaikiLayoutDokumenGayaKonsisten();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DokumenManager] Catatan layout: " + ex.Message);
        }
    }

    void Start()
    {
        if (scriptPlayer == null)
        {
            scriptPlayer = FindAnyObjectByType<PlayerController>();
        }

        // Cari komponen UI otomatis jika belum terhubung di Inspector
        CariKomponenDaftarOtomatis();

        try
        {
            PerbaikiLayoutDokumenGayaKonsisten();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DokumenManager] Catatan layout: " + ex.Message);
        }

        // Pastikan EventSystem aktif dan mendukung New Input System
        PastikanEventSystemSiap();

        // Hubungkan listener tombol navigasi halaman
        if (tombolPrev != null) tombolPrev.onClick.AddListener(HalamanSebelumnya);
        if (tombolNext != null) tombolNext.onClick.AddListener(HalamanBerikutnya);

        // Pastikan saat mulai game semua panel dokumen tertutup
        if (panelUtama != null) panelUtama.SetActive(false);
        if (panelBacaDokumen != null) panelBacaDokumen.SetActive(false);
        if (panelDaftarDokumen != null) panelDaftarDokumen.SetActive(true);

        // Otomatis aktifkan Preserve Aspect dan matikan RaycastTarget pada foto agar tidak menutupi tombol
        if (gambarHalamanUI != null)
        {
            gambarHalamanUI.preserveAspect = true;
            gambarHalamanUI.raycastTarget = false;
        }

        TMP_FontAsset fInter = DapatkanFontInterMedium();
        if (fInter != null)
        {
            if (teksJudul != null) teksJudul.font = fInter;
            if (teksPenjelasan != null) teksPenjelasan.font = fInter;
            if (teksNomorHalaman != null) teksNomorHalaman.font = fInter;
        }
    }

    /// <summary>
    /// Menata layout panel dokumen agar identik dan konsisten dengan sistem Inventory Tas Item
    /// Termasuk menambahkan Tab Navigasi TAS ITEM [R] & DOKUMEN [J] di bagian atas
    /// </summary>
    public void PerbaikiLayoutDokumenGayaKonsisten()
    {
        if (panelDaftarDokumen == null) return;

        // 1. Siapkan Tab Navigasi di bagian atas daftar
        Transform cariNav = panelDaftarDokumen.transform.Find("NavigasiTab_Dokumen");
        if (cariNav == null)
        {
            GameObject navObj = new GameObject("NavigasiTab_Dokumen", typeof(RectTransform));
            navObj.transform.SetParent(panelDaftarDokumen.transform, false);
            cariNav = navObj.transform;

            RectTransform rtNav = navObj.GetComponent<RectTransform>();
            rtNav.anchorMin = new Vector2(0.5f, 0.5f);
            rtNav.anchorMax = new Vector2(0.5f, 0.5f);
            rtNav.pivot = new Vector2(0.5f, 0.5f);
            rtNav.anchoredPosition = new Vector2(0f, 260f);
            rtNav.sizeDelta = new Vector2(650f, 50f);

            // Tab Tas Item [R] (Bisa diklik untuk pindah tas)
            BuatTombolTab(navObj.transform, "TAS ITEM [R]", new Vector2(-190f, 0f), false, () =>
            {
                TutupPanelDokumen();
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.ToggleInventory();
                }
            });

            // Tab Dokumen [J] (Sedang aktif)
            BuatTombolTab(navObj.transform, "DOKUMEN [J]", new Vector2(0f, 0f), true, null);

            // Tombol Tutup [Q]
            BuatTombolTab(navObj.transform, "TUTUP [Q]", new Vector2(190f, 0f), false, () =>
            {
                TutupPanelDokumen();
            });
        }

        // 2. Sesuaikan ScrollView_Daftar agar proporsional dan bersahabat dengan mata
        Transform cariScroll = panelDaftarDokumen.transform.Find("ScrollView_Daftar");
        if (cariScroll != null)
        {
            RectTransform rtScroll = cariScroll.GetComponent<RectTransform>();
            if (rtScroll != null)
            {
                rtScroll.anchorMin = new Vector2(0.5f, 0.5f);
                rtScroll.anchorMax = new Vector2(0.5f, 0.5f);
                rtScroll.pivot = new Vector2(0.5f, 0.5f);
                rtScroll.anchoredPosition = new Vector2(0f, -20f);
                rtScroll.sizeDelta = new Vector2(650f, 500f);
            }

            Image img = cariScroll.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.06f, 0.06f, 0.08f, 0.75f);
            }

            ScrollRect sr = cariScroll.GetComponent<ScrollRect>();
            if (sr != null)
            {
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 30f;
            }

            // Sembunyikan scrollbar visual lama agar tampilan bersih seperti inventory
            Transform sbH = cariScroll.Find("Scrollbar Horizontal");
            if (sbH != null) sbH.gameObject.SetActive(false);
            Transform sbV = cariScroll.Find("Scrollbar Vertical");
            if (sbV != null) sbV.gameObject.SetActive(false);
        }

        // 3. Rapikan Teks Kosong jika dokumen belum ada
        if (teksDaftarKosong != null)
        {
            RectTransform rtTeks = teksDaftarKosong.GetComponent<RectTransform>();
            if (rtTeks != null)
            {
                rtTeks.anchorMin = new Vector2(0.5f, 0.5f);
                rtTeks.anchorMax = new Vector2(0.5f, 0.5f);
                rtTeks.pivot = new Vector2(0.5f, 0.5f);
                rtTeks.anchoredPosition = Vector2.zero;
                rtTeks.sizeDelta = new Vector2(450f, 60f);
            }

            TextMeshProUGUI tmp = teksDaftarKosong.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                TMP_FontAsset fInter = DapatkanFontInterMedium();
                if (fInter != null) tmp.font = fInter;
                tmp.text = "(Belum ada dokumen yang ditemukan)";
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Italic;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.6f, 0.58f, 0.52f, 0.75f);
            }
        }
    }

    public TMP_FontAsset DapatkanFontInterMedium()
    {
        if (fontInterMedium != null) return fontInterMedium;

        if (prefabTombolDokumen != null)
        {
            var tmpPrefab = prefabTombolDokumen.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpPrefab != null && tmpPrefab.font != null && tmpPrefab.font.name.Contains("Inter"))
            {
                fontInterMedium = tmpPrefab.font;
                return fontInterMedium;
            }
        }

        TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in allFonts)
        {
            if (f.name.Contains("Inter") && f.name.Contains("Medium"))
            {
                fontInterMedium = f;
                return fontInterMedium;
            }
        }
        foreach (var f in allFonts)
        {
            if (f.name.Contains("Inter"))
            {
                fontInterMedium = f;
                return fontInterMedium;
            }
        }

#if UNITY_EDITOR
        fontInterMedium = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Inter/Inter_18pt-Medium SDF.asset");
        if (fontInterMedium != null) return fontInterMedium;
#endif

        return null;
    }

    private GameObject BuatTombolTab(Transform parent, string judul, Vector2 pos, bool isActive, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Tab_" + judul, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(175f, 42f);

        Image img = btnObj.GetComponent<Image>();
        img.color = isActive ? new Color(0.28f, 0.25f, 0.20f, 0.95f) : new Color(0.12f, 0.12f, 0.14f, 0.75f);

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = img.color;
        cb.highlightedColor = new Color(0.35f, 0.30f, 0.22f, 1f);
        cb.pressedColor = new Color(0.08f, 0.08f, 0.09f, 1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        if (onClick != null)
        {
            btn.onClick.AddListener(onClick);
        }

        // Teks Tab
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);

        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset fInter = DapatkanFontInterMedium();
        if (fInter != null) tmp.font = fInter;
        tmp.text = judul;
        tmp.fontSize = 16f;
        tmp.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = isActive ? new Color(0.96f, 0.94f, 0.88f, 1f) : new Color(0.65f, 0.63f, 0.58f, 0.85f);
        tmp.characterSpacing = 3f;

        return btnObj;
    }

    /// <summary>
    /// Mencari otomatis referensi PanelUtama, PanelDaftar, Content, dan TeksKosong jika slot Inspector kosong
    /// </summary>
    public void CariKomponenDaftarOtomatis()
    {
        // 1. Cari Canvas utama untuk mencari panel yang sedang non-aktif
        Canvas canvasUtama = GetComponentInParent<Canvas>();
        if (canvasUtama == null) canvasUtama = FindAnyObjectByType<Canvas>();

        // 2. Cari PanelUtamaDokumen jika null
        if (panelUtama == null && canvasUtama != null)
        {
            foreach (Transform t in canvasUtama.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == "PanelUtamaDokumen" || t.gameObject.name == "PanelUtama")
                {
                    panelUtama = t.gameObject;
                    break;
                }
            }
        }

        // 3. Cari PanelDaftar jika null
        if (panelDaftarDokumen == null && panelUtama != null)
        {
            foreach (Transform t in panelUtama.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == "PanelDaftar")
                {
                    panelDaftarDokumen = t.gameObject;
                    break;
                }
            }
        }

        // 4. Cari tempatDaftarTombol (Content di dalam Viewport)
        if (tempatDaftarTombol == null && panelDaftarDokumen != null)
        {
            foreach (Transform t in panelDaftarDokumen.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == "Content")
                {
                    tempatDaftarTombol = t;
                    break;
                }
            }
        }

        // 5. Cari TeksKosong jika null
        if (teksDaftarKosong == null && panelDaftarDokumen != null)
        {
            foreach (Transform t in panelDaftarDokumen.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == "TeksKosong")
                {
                    teksDaftarKosong = t.gameObject;
                    break;
                }
            }
        }

        // 6. Cari PanelBaca jika null
        if (panelBacaDokumen == null && panelUtama != null)
        {
            foreach (Transform t in panelUtama.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == "PanelBaca")
                {
                    panelBacaDokumen = t.gameObject;
                    break;
                }
            }
        }

        // 7. Pastikan VerticalLayoutGroup pada Content mengontrol tinggi anak
        if (tempatDaftarTombol != null)
        {
            VerticalLayoutGroup vlg = tempatDaftarTombol.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
            }
        }
    }

    private void PastikanEventSystemSiap()
    {
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            StandaloneInputModule oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }

    void Update()
    {
        bool tombolBukaDitekan = false;
        bool rDitekan = false;
        bool qDitekan = false;
        bool escDitekan = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current[tombolBuka].wasPressedThisFrame) tombolBukaDitekan = true;
            if (Keyboard.current.rKey.wasPressedThisFrame) rDitekan = true;
            if (Keyboard.current.qKey.wasPressedThisFrame) qDitekan = true;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) escDitekan = true;
        }
        else
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.J)) tombolBukaDitekan = true;
                if (Input.GetKeyDown(KeyCode.R)) rDitekan = true;
                if (Input.GetKeyDown(KeyCode.Q)) qDitekan = true;
                if (Input.GetKeyDown(KeyCode.Escape)) escDitekan = true;
            }
            catch {}
        }

        // 1. TUTUP DENGAN TOMBOL Q atau ESC
        if ((qDitekan || escDitekan) && sedangBukaDokumen)
        {
            if (panelBacaDokumen != null && panelBacaDokumen.activeSelf)
            {
                KembaliKeDaftar();
            }
            else
            {
                TutupPanelDokumen();
            }
            return;
        }

        // Cegah eksekusi pada frame yang sama saat baru dibuka / dialihkan dari tas item
        if (Time.frameCount == frameTerakhirBuka) return;

        // 2. Cek input tombol buka / tutup dokumen (J)
        if (tombolBukaDitekan)
        {
            TogglePanelDokumen();
        }

        // 3. Tombol R saat Dokumen terbuka -> Pindah langsung ke Tas Item
        if (rDitekan && sedangBukaDokumen)
        {
            TutupPanelDokumen();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ToggleInventory();
            }
        }

        // 4. Navigasi cepat dengan tombol panah kiri / kanan atau A / D saat membaca
        if (sedangBukaDokumen && panelBacaDokumen != null && panelBacaDokumen.activeSelf)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                {
                    HalamanSebelumnya();
                }
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                {
                    HalamanBerikutnya();
                }
            }
        }
    }

    public void TogglePanelDokumen()
    {
        if (sedangBukaDokumen)
        {
            TutupPanelDokumen();
        }
        else
        {
            BukaPanelDokumen();
        }
    }

    /// <summary>
    /// Membuka panel dokumen dan OTOMATIS MENJEDA (PAUSE) GAME
    /// </summary>
    public void BukaPanelDokumen()
    {
        // Tutup tas inventory item jika sedang terbuka agar tidak bertabrakan
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryAktif)
        {
            InventoryManager.Instance.ToggleInventory();
        }

        CariKomponenDaftarOtomatis();
        PerbaikiLayoutDokumenGayaKonsisten();

        sedangBukaDokumen = true;
        frameTerakhirBuka = Time.frameCount;
        if (panelUtama != null) panelUtama.SetActive(true);

        // 1. OTOMATIS PAUSE GAME
        Time.timeScale = 0f;

        // 2. Kunci player & aktifkan kursor mouse
        if (scriptPlayer == null) scriptPlayer = FindAnyObjectByType<PlayerController>();
        if (scriptPlayer != null)
        {
            scriptPlayer.bukaInventory = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Mainkan audio jika ada
        if (audioSource != null && sfxBukaBuku != null)
        {
            audioSource.PlayOneShot(sfxBukaBuku);
        }

        // Tampilkan daftar dokumen
        KembaliKeDaftar();
        PerbaruiTampilanDaftar();
    }

    /// <summary>
    /// Menutup panel dokumen dan MELANJUTKAN (UNPAUSE) GAME
    /// </summary>
    public void TutupPanelDokumen()
    {
        sedangBukaDokumen = false;

        if (panelUtama != null) panelUtama.SetActive(false);
        if (panelBacaDokumen != null) panelBacaDokumen.SetActive(false);

        // 1. UNPAUSE GAME
        Time.timeScale = 1f;

        // 2. Kembalikan kontrol player & kunci kursor
        if (scriptPlayer != null)
        {
            scriptPlayer.bukaInventory = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Menambahkan dokumen baru yang diambil oleh pemain di dunia game
    /// </summary>
    public void TambahDokumen(DokumenItem dataBaru, bool bukaLangsung = false)
    {
        if (dataBaru == null) return;

        CariKomponenDaftarOtomatis();

        // Cegah duplikasi di koleksi
        if (!string.IsNullOrEmpty(dataBaru.idDokumen))
        {
            if (daftarDokumenKoleksi.Exists(d => d.idDokumen == dataBaru.idDokumen))
            {
                Debug.LogWarning($"[DokumenManager] Dokumen ID '{dataBaru.idDokumen}' sudah pernah diambil!");
                return;
            }
            idDokumenTersimpan.Add(dataBaru.idDokumen);
        }

        daftarDokumenKoleksi.Add(dataBaru);
        Debug.Log($"[DokumenManager] Dokumen '{dataBaru.judulDokumen}' berhasil masuk daftar! (Total koleksi: {daftarDokumenKoleksi.Count})");

        // Perbarui tombol-tombol di dalam Content panel daftar
        PerbaruiTampilanDaftar();

        // Jika diset untuk langsung dibaca saat diambil
        if (bukaLangsung)
        {
            BukaPanelDokumen();
            BukaDetailDokumen(dataBaru);
        }
    }

    /// <summary>
    /// Membuat ulang seluruh tombol dokumen di dalam Content sesuai daftar koleksi
    /// </summary>
    public void PerbaruiTampilanDaftar()
    {
        CariKomponenDaftarOtomatis();

        if (tempatDaftarTombol != null)
        {
            // Hapus tombol dokumen yang lama agar sinkron
            List<GameObject> tombolLama = new List<GameObject>();
            foreach (Transform child in tempatDaftarTombol)
            {
                if (teksDaftarKosong != null && child.gameObject == teksDaftarKosong) continue;
                tombolLama.Add(child.gameObject);
            }
            foreach (var t in tombolLama)
            {
                Destroy(t);
            }

            // Buat tombol baru untuk setiap dokumen yang sudah dikoleksi
            if (prefabTombolDokumen != null)
            {
                foreach (var data in daftarDokumenKoleksi)
                {
                    BuatTombolDokumen(data);
                }
            }
            else
            {
                Debug.LogError("[DokumenManager] Prefab Tombol Dokumen belum di-assign di Inspector DokumenManager!");
            }
        }
        else
        {
            Debug.LogWarning("[DokumenManager] Tempat Daftar Tombol (Content) belum terhubung! Pastikan ada objek 'Content' di dalam ScrollView_Daftar.");
        }

        // Tampilkan teks 'Belum ada dokumen' jika koleksi masih 0
        if (teksDaftarKosong != null)
        {
            teksDaftarKosong.SetActive(daftarDokumenKoleksi.Count == 0);
        }
    }

    private void BuatTombolDokumen(DokumenItem data)
    {
        if (prefabTombolDokumen == null || tempatDaftarTombol == null) return;

        GameObject tombolObj = Instantiate(prefabTombolDokumen, tempatDaftarTombol);
        tombolObj.SetActive(true);
        tombolObj.transform.localScale = Vector3.one;
        
        // 1. Pastikan ukuran tinggi tombol proporsional
        LayoutElement le = tombolObj.GetComponent<LayoutElement>();
        if (le == null) le = tombolObj.AddComponent<LayoutElement>();
        le.minHeight = 55f;
        le.preferredHeight = 55f;

        // 2. Styling Background Tombol (Nuansa Horror Gelap Elegan, BUKAN putih polos)
        Image bgImage = tombolObj.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.type = Image.Type.Simple;
            bgImage.color = new Color(0.12f, 0.12f, 0.14f, 0.88f); // Latar gelap elegan
        }

        // 3. Efek Hover & Klik Tombol
        Button btn = tombolObj.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.12f, 0.12f, 0.14f, 0.88f);
            cb.highlightedColor = new Color(0.28f, 0.25f, 0.20f, 0.98f); // Warna hangat saat disorot kursor
            cb.pressedColor = new Color(0.08f, 0.08f, 0.09f, 1f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => BukaDetailDokumen(data));
        }

        // 4. Styling Teks Judul Catatan
        TextMeshProUGUI teksTombol = tombolObj.GetComponentInChildren<TextMeshProUGUI>();
        if (teksTombol != null)
        {
            TMP_FontAsset fInter = DapatkanFontInterMedium();
            if (fInter != null) teksTombol.font = fInter;
            teksTombol.text = data.judulDokumen;
            teksTombol.color = new Color(0.92f, 0.90f, 0.84f, 1f); // Warna putih gading / kertas tua
            teksTombol.alignment = TextAlignmentOptions.MidlineLeft;
            teksTombol.fontSize = 20f;
            teksTombol.enableAutoSizing = false;
            teksTombol.margin = new Vector4(20f, 0f, 15f, 0f);
        }

        // 5. Tampilkan Icon Buku di Sisi Kiri (Jika ada iconUI)
        if (data.iconUI != null)
        {
            Transform iconChild = tombolObj.transform.Find("IconDokumen");
            if (iconChild == null)
            {
                GameObject iconObj = new GameObject("IconDokumen", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(tombolObj.transform, false);
                iconChild = iconObj.transform;

                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(15f, 0f);
                iconRt.sizeDelta = new Vector2(30f, 30f);

                Image iconImg = iconObj.GetComponent<Image>();
                iconImg.sprite = data.iconUI;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Geser teks ke kanan agar memberi ruang bagi icon
                if (teksTombol != null)
                {
                    teksTombol.margin = new Vector4(55f, 0f, 15f, 0f);
                }
            }
        }
    }

    /// <summary>
    /// Membuka 2 Panel Bersebelahan untuk membaca dokumen (Kiri Foto, Kanan Penjelasan)
    /// </summary>
    public void BukaDetailDokumen(DokumenItem data)
    {
        if (data == null) return;

        dokumenSedangDibaca = data;
        indeksHalamanAktif = 0;

        if (panelDaftarDokumen != null) panelDaftarDokumen.SetActive(false);
        if (panelBacaDokumen != null) panelBacaDokumen.SetActive(true);

        // Tampilkan halaman pertama
        TampilkanHalaman(indeksHalamanAktif);
    }

    /// <summary>
    /// Menampilkan isi halaman di kolom kiri (foto) dan kolom kanan (penjelasan teks)
    /// </summary>
    private void TampilkanHalaman(int index)
    {
        if (dokumenSedangDibaca == null || dokumenSedangDibaca.daftarHalaman == null || dokumenSedangDibaca.daftarHalaman.Count == 0)
        {
            if (teksJudul != null) teksJudul.text = dokumenSedangDibaca != null ? dokumenSedangDibaca.judulDokumen : "";
            if (teksPenjelasan != null) teksPenjelasan.text = "(Dokumen kosong / tidak ada tulisan)";
            if (teksNomorHalaman != null) teksNomorHalaman.text = "0 / 0";
            if (gambarHalamanUI != null) gambarHalamanUI.gameObject.SetActive(false);
            if (tombolPrev != null) tombolPrev.gameObject.SetActive(false);
            if (tombolNext != null) tombolNext.gameObject.SetActive(false);
            return;
        }

        int totalHalaman = dokumenSedangDibaca.daftarHalaman.Count;

        // Batasi rentang index halaman
        indeksHalamanAktif = Mathf.Clamp(index, 0, totalHalaman - 1);
        HalamanDokumen halaman = dokumenSedangDibaca.daftarHalaman[indeksHalamanAktif];

        // --- KOLOM KIRI: Foto Gambar Halaman ---
        if (gambarHalamanUI != null)
        {
            if (halaman.fotoHalaman != null)
            {
                gambarHalamanUI.sprite = halaman.fotoHalaman;
                gambarHalamanUI.gameObject.SetActive(true);
            }
            else
            {
                gambarHalamanUI.gameObject.SetActive(false);
            }
        }

        // Update indikator nomor halaman
        if (teksNomorHalaman != null)
        {
            teksNomorHalaman.text = $"{indeksHalamanAktif + 1} / {totalHalaman}";
        }

        // Tombol navigasi: aktifkan jika ada lebih dari 1 halaman
        bool butuhNavigasi = totalHalaman > 1;
        if (tombolPrev != null)
        {
            tombolPrev.gameObject.SetActive(butuhNavigasi);
            tombolPrev.interactable = butuhNavigasi;
        }
        if (tombolNext != null)
        {
            tombolNext.gameObject.SetActive(butuhNavigasi);
            tombolNext.interactable = butuhNavigasi;
        }

        // --- KOLOM KANAN: Penjelasan & Transkrip Teks yang SINKRON ---
        if (teksJudul != null)
        {
            teksJudul.text = dokumenSedangDibaca.judulDokumen;
        }

        if (teksPenjelasan != null)
        {
            teksPenjelasan.text = halaman.teksPenjelasan;
        }
    }

    public void HalamanBerikutnya()
    {
        if (dokumenSedangDibaca == null || dokumenSedangDibaca.daftarHalaman == null || dokumenSedangDibaca.daftarHalaman.Count <= 1) return;

        int total = dokumenSedangDibaca.daftarHalaman.Count;
        int nextIndex = (indeksHalamanAktif + 1) % total; // Berputar kembali ke halaman 1 jika di lembar terakhir
        
        if (audioSource != null && sfxBalikHalaman != null) audioSource.PlayOneShot(sfxBalikHalaman);
        TampilkanHalaman(nextIndex);
    }

    public void HalamanSebelumnya()
    {
        if (dokumenSedangDibaca == null || dokumenSedangDibaca.daftarHalaman == null || dokumenSedangDibaca.daftarHalaman.Count <= 1) return;

        int total = dokumenSedangDibaca.daftarHalaman.Count;
        int prevIndex = (indeksHalamanAktif - 1 + total) % total; // Berputar ke halaman akhir jika di lembar pertama
        
        if (audioSource != null && sfxBalikHalaman != null) audioSource.PlayOneShot(sfxBalikHalaman);
        TampilkanHalaman(prevIndex);
    }

    /// <summary>
    /// Tombol kembali dari mode baca 2 panel ke tampilan daftar dokumen
    /// </summary>
    public void KembaliKeDaftar()
    {
        dokumenSedangDibaca = null;

        if (panelBacaDokumen != null) panelBacaDokumen.SetActive(false);
        if (panelDaftarDokumen != null) panelDaftarDokumen.SetActive(true);
        PerbaruiTampilanDaftar();
    }
}
