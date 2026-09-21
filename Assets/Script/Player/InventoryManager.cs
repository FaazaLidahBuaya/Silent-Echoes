using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro; // WAJIB DITAMBAHKAN UNTUK TEXTMESHPRO

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI Inventory")]
    public GameObject panelInventory; 
    public Transform tempatLogoItem; 
    public GameObject prefabTombolItem; 

    [Header("UI Viewer Barang")]
    public GameObject panelViewer; 
    public Transform tempatSpawnBarang3D; 

    [Header("Referensi Player")]
    public PlayerController scriptPlayer; 

    private Dictionary<string, GameObject> daftarItem = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> daftarTombolUI = new Dictionary<string, GameObject>();
    private GameObject barangSedangDilihat;
    [HideInInspector] public bool inventoryAktif = false;
    [HideInInspector] public int frameTerakhirBuka = -1;

    [Header("Audio Inventory")]
    public AudioSource audioSource;

    [Header("UI Status Kosong")]
    public GameObject teksInventoryKosong;

    [Header("Font UI")]
    public TMP_FontAsset fontInterMedium;

    // Menyimpan daftar nama item yang sedang dimiliki player, tetap ada saat reload scene
    public static HashSet<string> itemDimiliki = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        CariKomponenInventoryOtomatis();

        try
        {
            PerbaikiLayoutInventoryGayaDokumen();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[InventoryManager] Catatan inisialisasi layout: " + ex.Message);
        }
    }

    void Start()
    {
        CariKomponenInventoryOtomatis();

        try
        {
            PerbaikiLayoutInventoryGayaDokumen();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[InventoryManager] Catatan inisialisasi layout: " + ex.Message);
        }

        // Jika sedang respawn dari checkpoint telepon, langsung restore item yang sebelumnya sudah diambil
        if (GameCheckpointManager.respawnDiTelepon)
        {
            RestoreSemuaItemTersimpan();
        }
    }

    /// <summary>
    /// Mencari otomatis komponen inventory jika ada referensi Inspector yang belum terpasang
    /// </summary>
    public void CariKomponenInventoryOtomatis()
    {
        if (scriptPlayer == null)
        {
            scriptPlayer = FindAnyObjectByType<PlayerController>();
        }

        if (panelInventory == null)
        {
            Canvas canvasUtama = FindAnyObjectByType<Canvas>();
            if (canvasUtama != null)
            {
                foreach (Transform t in canvasUtama.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Panel Inventory")
                    {
                        panelInventory = t.gameObject;
                        break;
                    }
                }
            }
        }

        if (tempatLogoItem == null && panelInventory != null)
        {
            foreach (Transform t in panelInventory.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Tempat Logo Item")
                {
                    tempatLogoItem = t;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Mengatur layout inventory menjadi persis seperti daftar dokumen:
    /// Berupa daftar vertikal rapi, berderet ke bawah di dalam ScrollView,
    /// setiap item berupa bar horizontal elegan dengan icon di kiri & teks di kanan.
    /// </summary>
    public void PerbaikiLayoutInventoryGayaDokumen()
    {
        if (panelInventory == null || tempatLogoItem == null) return;

        // 1. Bersihkan LayoutGroup salah di panel latar belakang (Panel Inventory)
        LayoutGroup layoutSalah = panelInventory.GetComponent<LayoutGroup>();
        if (layoutSalah != null)
        {
            if (Application.isPlaying) Destroy(layoutSalah);
            else DestroyImmediate(layoutSalah);
        }

        // 2. Siapkan wadah ScrollView_Inventory jika belum ada
        Transform parentScrollView = panelInventory.transform.Find("ScrollView_Inventory");
        GameObject scrollObj = (parentScrollView != null) ? parentScrollView.gameObject : null;

        if (scrollObj == null)
        {
            scrollObj = new GameObject("ScrollView_Inventory", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(RectMask2D));
            scrollObj.transform.SetParent(panelInventory.transform, false);
            parentScrollView = scrollObj.transform;

            // Berikan index agar muncul di bawah judul
            scrollObj.transform.SetSiblingIndex(0);
        }

        RectTransform rtScroll = scrollObj.GetComponent<RectTransform>();
        if (rtScroll != null)
        {
            rtScroll.anchorMin = new Vector2(0.5f, 0.5f);
            rtScroll.anchorMax = new Vector2(0.5f, 0.5f);
            rtScroll.pivot = new Vector2(0.5f, 0.5f);
            rtScroll.anchoredPosition = new Vector2(0f, -20f);
            rtScroll.sizeDelta = new Vector2(650f, 500f);
        }

        // Latar belakang panel daftar item (gelap transparan elegan)
        Image imgScroll = scrollObj.GetComponent<Image>();
        if (imgScroll != null)
        {
            imgScroll.color = new Color(0.06f, 0.06f, 0.08f, 0.75f);
            imgScroll.raycastTarget = true;
        }

        // 3. Pindahkan tempatLogoItem menjadi Content di dalam ScrollView_Inventory
        if (tempatLogoItem.parent != parentScrollView)
        {
            tempatLogoItem.SetParent(parentScrollView, false);
        }

        RectTransform rtTempat = tempatLogoItem.GetComponent<RectTransform>();
        if (rtTempat != null)
        {
            rtTempat.anchorMin = new Vector2(0f, 1f);
            rtTempat.anchorMax = new Vector2(1f, 1f);
            rtTempat.pivot = new Vector2(0.5f, 1f);
            rtTempat.anchoredPosition = Vector2.zero;
            rtTempat.sizeDelta = new Vector2(0f, 0f);
        }

        // Hapus GridLayoutGroup lama jika masih ada dengan aman
        LayoutGroup layoutTempat = tempatLogoItem.GetComponent<LayoutGroup>();
        if (layoutTempat != null && !(layoutTempat is VerticalLayoutGroup))
        {
            if (Application.isPlaying) Destroy(layoutTempat);
            else DestroyImmediate(layoutTempat);
        }

        // Pasang VerticalLayoutGroup persis seperti DokumenManager
        VerticalLayoutGroup vlg = tempatLogoItem.GetComponent<VerticalLayoutGroup>();
        if (vlg == null && (layoutTempat == null || layoutTempat is VerticalLayoutGroup))
        {
            vlg = tempatLogoItem.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        if (vlg != null)
        {
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        // Pasang ContentSizeFitter agar tinggi Content dinamis sesuai jumlah item
        ContentSizeFitter fitter = tempatLogoItem.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = tempatLogoItem.gameObject.AddComponent<ContentSizeFitter>();
        }
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Konfigurasi ScrollRect
        ScrollRect scrollRect = scrollObj.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.content = rtTempat;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
        }

        // 4. Siapkan Tab Navigasi TAS ITEM [R] & DOKUMEN [J] di atas daftar
        Transform headerJudul = panelInventory.transform.Find("JudulInventory");
        if (headerJudul != null)
        {
            if (Application.isPlaying) Destroy(headerJudul.gameObject);
            else DestroyImmediate(headerJudul.gameObject);
        }

        Transform cariNav = panelInventory.transform.Find("NavigasiTab_Inventory");
        if (cariNav == null)
        {
            GameObject navObj = new GameObject("NavigasiTab_Inventory", typeof(RectTransform));
            navObj.transform.SetParent(panelInventory.transform, false);
            cariNav = navObj.transform;

            RectTransform rtNav = navObj.GetComponent<RectTransform>();
            rtNav.anchorMin = new Vector2(0.5f, 0.5f);
            rtNav.anchorMax = new Vector2(0.5f, 0.5f);
            rtNav.pivot = new Vector2(0.5f, 0.5f);
            rtNav.anchoredPosition = new Vector2(0f, 260f);
            rtNav.sizeDelta = new Vector2(650f, 50f);

            // Tab Tas Item [R] (Sedang aktif)
            BuatTombolTab(navObj.transform, "TAS ITEM [R]", new Vector2(-190f, 0f), true, null);

            // Tab Dokumen [J] (Bisa diklik untuk pindah ke dokumen)
            BuatTombolTab(navObj.transform, "DOKUMEN [J]", new Vector2(0f, 0f), false, () =>
            {
                ToggleInventory();
                if (DokumenManager.Instance != null)
                {
                    DokumenManager.Instance.BukaPanelDokumen();
                }
            });

            // Tombol Tutup [Q]
            BuatTombolTab(navObj.transform, "TUTUP [Q]", new Vector2(190f, 0f), false, () =>
            {
                ToggleInventory();
            });
        }

        // 5. Siapkan Teks "Belum ada item" jika inventory kosong
        if (teksInventoryKosong == null)
        {
            Transform cariTeks = parentScrollView.Find("TeksKosongInventory");
            if (cariTeks != null)
            {
                teksInventoryKosong = cariTeks.gameObject;
                TextMeshProUGUI tmpExist = teksInventoryKosong.GetComponent<TextMeshProUGUI>();
                TMP_FontAsset fInter = DapatkanFontInterMedium();
                if (tmpExist != null && fInter != null) tmpExist.font = fInter;
            }
            else
            {
                GameObject teksObj = new GameObject("TeksKosongInventory", typeof(RectTransform), typeof(TextMeshProUGUI));
                teksObj.transform.SetParent(parentScrollView, false);
                teksInventoryKosong = teksObj;

                RectTransform rtTeks = teksObj.GetComponent<RectTransform>();
                rtTeks.anchorMin = new Vector2(0.5f, 0.5f);
                rtTeks.anchorMax = new Vector2(0.5f, 0.5f);
                rtTeks.pivot = new Vector2(0.5f, 0.5f);
                rtTeks.anchoredPosition = Vector2.zero;
                rtTeks.sizeDelta = new Vector2(400f, 60f);

                TextMeshProUGUI tmpTeks = teksObj.GetComponent<TextMeshProUGUI>();
                TMP_FontAsset fInter = DapatkanFontInterMedium();
                if (fInter != null) tmpTeks.font = fInter;
                tmpTeks.text = "(Belum ada item di dalam tas)";
                tmpTeks.fontSize = 20f;
                tmpTeks.alignment = TextAlignmentOptions.Center;
                tmpTeks.color = new Color(0.6f, 0.58f, 0.52f, 0.75f);
                tmpTeks.fontStyle = FontStyles.Italic;
            }
        }
        else
        {
            TextMeshProUGUI tmpExist = teksInventoryKosong.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset fInter = DapatkanFontInterMedium();
            if (tmpExist != null && fInter != null) tmpExist.font = fInter;
        }

        UpdateStatusTeksKosong();
    }

    public TMP_FontAsset DapatkanFontInterMedium()
    {
        if (fontInterMedium != null) return fontInterMedium;

        if (prefabTombolItem != null)
        {
            var tmpPrefab = prefabTombolItem.GetComponentInChildren<TextMeshProUGUI>();
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

    [ContextMenu("RAPIKAN LAYOUT GAYA DOKUMEN")]
    public void TestRapikanLayout()
    {
        PerbaikiLayoutInventoryGayaDokumen();
        Debug.Log("<color=green>[InventoryManager]</color> Layout inventory berhasil diubah ke gaya Dokumen!");
    }

    void Update()
    {
        bool rDitekan = false;
        bool jDitekan = false;
        bool qDitekan = false;
        bool escDitekan = false;

        // Input System Baru
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame) rDitekan = true;
            if (Keyboard.current.jKey.wasPressedThisFrame) jDitekan = true;
            if (Keyboard.current.qKey.wasPressedThisFrame) qDitekan = true;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) escDitekan = true;
        }
        else
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.R)) rDitekan = true;
                if (Input.GetKeyDown(KeyCode.J)) jDitekan = true;
                if (Input.GetKeyDown(KeyCode.Q)) qDitekan = true;
                if (Input.GetKeyDown(KeyCode.Escape)) escDitekan = true;
            }
            catch {}
        }

        // 1. TUTUP DENGAN TOMBOL Q atau ESC
        if (qDitekan || escDitekan)
        {
            // Jika sedang lihat barang 3D viewer, tutup viewernya dulu
            if (panelViewer != null && panelViewer.activeSelf)
            {
                TutupViewer();
                return;
            }

            // Jika panel inventory sedang terbuka, tutup
            if (inventoryAktif)
            {
                ToggleInventory();
                return;
            }
        }

        // Cegah eksekusi pada frame yang sama saat baru dibuka / dialihkan
        if (Time.frameCount == frameTerakhirBuka) return;

        // 2. Buka / Tutup Tas Item dengan tombol R
        if (rDitekan && (panelViewer == null || !panelViewer.activeSelf))
        {
            ToggleInventory();
        }

        // 3. Tekan J saat Tas Item aktif -> Langsung alihkan ke Dokumen
        if (jDitekan && inventoryAktif && (panelViewer == null || !panelViewer.activeSelf))
        {
            ToggleInventory();
            if (DokumenManager.Instance != null)
            {
                DokumenManager.Instance.BukaPanelDokumen();
            }
        }
    }

    public void ToggleInventory()
    {
        if (panelInventory == null)
        {
            CariKomponenInventoryOtomatis();
        }
        if (panelInventory == null) return;

        // Jika akan membuka tas item, pastikan panel dokumen ditutup terlebih dahulu
        if (!inventoryAktif && DokumenManager.Instance != null && DokumenManager.Instance.sedangBukaDokumen)
        {
            DokumenManager.Instance.TutupPanelDokumen();
        }

        inventoryAktif = !inventoryAktif;
        panelInventory.SetActive(inventoryAktif);

        if (inventoryAktif)
        {
            frameTerakhirBuka = Time.frameCount;
        }

        if (scriptPlayer == null)
        {
            scriptPlayer = FindAnyObjectByType<PlayerController>();
        }

        if (scriptPlayer != null)
        {
            scriptPlayer.bukaInventory = inventoryAktif; 
        }

        if (inventoryAktif)
        {
            UpdateStatusTeksKosong();
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
        }
    }

    private void UpdateStatusTeksKosong()
    {
        if (teksInventoryKosong != null)
        {
            teksInventoryKosong.SetActive(daftarItem.Count == 0);
        }
    }

    public void TambahItem(string namaItem, Sprite logoItem, GameObject itemAsli, AudioClip sfx, bool putarAudio = true)
    {
        // 1. Putar suara jika ada
        if (putarAudio && audioSource != null && sfx != null)
        {
            audioSource.PlayOneShot(sfx);
        }

        // Catat ke daftar statis agar tidak hilang saat reload scene (checkpoint / game over)
        if (!itemDimiliki.Contains(namaItem))
        {
            itemDimiliki.Add(namaItem);
        }

        // 2. Tambahkan ke daftar UI & viewer jika belum ada
        if (!daftarItem.ContainsKey(namaItem))
        {
            daftarItem.Add(namaItem, itemAsli);
            
            GameObject tombolBaru = Instantiate(prefabTombolItem, tempatLogoItem);
            tombolBaru.SetActive(true);
            tombolBaru.transform.localScale = Vector3.one;

            // Pastikan LayoutElement tombol tinggi 55 px
            LayoutElement le = tombolBaru.GetComponent<LayoutElement>();
            if (le == null) le = tombolBaru.AddComponent<LayoutElement>();
            le.minHeight = 55f;
            le.preferredHeight = 55f;

            // Background Tombol Gaya Horror Elegan (Persis DokumenManager)
            Image bgImage = tombolBaru.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.type = Image.Type.Simple;
                bgImage.color = new Color(0.12f, 0.12f, 0.14f, 0.88f);
            }

            // Warna Tombol Saat Disorot (Hover Glowing Hangat) & Diklik
            Button btn = tombolBaru.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(0.12f, 0.12f, 0.14f, 0.88f);
                cb.highlightedColor = new Color(0.28f, 0.25f, 0.20f, 0.98f);
                cb.pressedColor = new Color(0.08f, 0.08f, 0.09f, 1f);
                cb.selectedColor = cb.highlightedColor;
                btn.colors = cb;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => MunculkanViewer(itemAsli));
            }

            // Teks Nama Item di Sisi Kanan (Warna Putih Gading Elegan)
            TextMeshProUGUI teksNama = tombolBaru.GetComponentInChildren<TextMeshProUGUI>();
            if (teksNama != null)
            {
                TMP_FontAsset fInter = DapatkanFontInterMedium();
                if (fInter != null) teksNama.font = fInter;
                teksNama.text = namaItem;
                teksNama.color = new Color(0.92f, 0.90f, 0.84f, 1f);
                teksNama.alignment = TextAlignmentOptions.MidlineLeft;
                teksNama.fontSize = 20f;
                teksNama.enableAutoSizing = false;
                teksNama.margin = new Vector4(65f, 0f, 15f, 0f); // Beri jarak dari icon di kiri
            }

            // Icon Item di Sisi Kiri
            if (logoItem != null)
            {
                Transform iconChild = tombolBaru.transform.Find("IconItem");
                if (iconChild == null)
                {
                    GameObject iconObj = new GameObject("IconItem", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(tombolBaru.transform, false);
                    iconChild = iconObj.transform;
                }

                RectTransform iconRt = iconChild.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(15f, 0f);
                iconRt.sizeDelta = new Vector2(34f, 34f);

                Image iconImg = iconChild.GetComponent<Image>();
                iconImg.sprite = logoItem;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.color = Color.white;
            }

            daftarTombolUI.Add(namaItem, tombolBaru);
            UpdateStatusTeksKosong();
        }
    }

    public void HapusItem(string namaItem)
    {
        // Hapus dari daftar statis
        itemDimiliki.Remove(namaItem);

        if (daftarItem.ContainsKey(namaItem))
        {
            if (daftarItem[namaItem] != null)
            {
                Destroy(daftarItem[namaItem]);
            }
            daftarItem.Remove(namaItem);
        }

        if (daftarTombolUI.ContainsKey(namaItem))
        {
            if (daftarTombolUI[namaItem] != null)
            {
                Destroy(daftarTombolUI[namaItem]);
            }
            daftarTombolUI.Remove(namaItem);
        }

        UpdateStatusTeksKosong();
    }

    /// <summary>
    /// Mengembalikan semua item yang dimiliki pemain sebelum game over ke dalam inventory
    /// dan menyembunyikan model 3D item tersebut dari dunia game
    /// </summary>
    public void RestoreSemuaItemTersimpan()
    {
        if (itemDimiliki == null || itemDimiliki.Count == 0) return;

        ItemPickup[] semuaItemDiScene = FindObjectsByType<ItemPickup>(FindObjectsInactive.Include);
        foreach (ItemPickup item in semuaItemDiScene)
        {
            if (item != null && itemDimiliki.Contains(item.namaItem))
            {
                TambahItem(item.namaItem, item.logoUI, item.gameObject, null, false);
                item.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Dipanggil saat kembali ke Main Menu atau mulai game baru dari nol
    /// </summary>
    public static void ResetInventoryStatic()
    {
        if (itemDimiliki != null)
        {
            itemDimiliki.Clear();
        }
    }

    public bool CekItem(string namaItem)
    {
        return daftarItem.ContainsKey(namaItem);
    }

    public void MunculkanViewer(GameObject itemAsli)
    {
        panelInventory.SetActive(false); 
        panelViewer.SetActive(true);

        if (barangSedangDilihat != null)
        {
            barangSedangDilihat.SetActive(false);
        }

        barangSedangDilihat = itemAsli;
        barangSedangDilihat.transform.SetParent(tempatSpawnBarang3D);
        barangSedangDilihat.transform.localPosition = Vector3.zero;
        barangSedangDilihat.SetActive(true);

        Collider col = barangSedangDilihat.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (barangSedangDilihat.GetComponent<ItemViewer>() == null)
        {
            barangSedangDilihat.AddComponent<ItemViewer>();
        }
    }

    public void TutupViewer()
    {
        if (barangSedangDilihat != null)
        {
            barangSedangDilihat.SetActive(false); 
        }
        
        panelViewer.SetActive(false);
        panelInventory.SetActive(true); 
    }
}