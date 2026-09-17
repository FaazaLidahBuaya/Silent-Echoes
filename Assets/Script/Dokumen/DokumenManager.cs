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

    [Header("Audio (Opsional)")]
    public AudioSource audioSource;
    public AudioClip sfxBukaBuku;
    public AudioClip sfxBalikHalaman;

    // Data penyimpanan statis agar dokumen tidak hilang saat reload scene / checkpoint
    public static HashSet<string> idDokumenTersimpan = new HashSet<string>();
    private List<DokumenItem> daftarDokumenKoleksi = new List<DokumenItem>();

    private DokumenItem dokumenSedangDibaca;
    private int indeksHalamanAktif = 0;
    private bool sedangBukaDokumen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Reset tracking statis saat scene dimuat ulang agar data segar
        idDokumenTersimpan.Clear();
        CariKomponenDaftarOtomatis();
    }

    void Start()
    {
        if (scriptPlayer == null)
        {
            scriptPlayer = FindAnyObjectByType<PlayerController>();
        }

        // Cari komponen UI otomatis jika belum terhubung di Inspector
        CariKomponenDaftarOtomatis();

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
        if (Keyboard.current == null) return;

        // Cek input tombol buka / tutup dokumen (J)
        if (Keyboard.current[tombolBuka].wasPressedThisFrame)
        {
            TogglePanelDokumen();
        }

        // Tombol ESC / Batal untuk menutup atau kembali ke daftar
        if (Keyboard.current.escapeKey.wasPressedThisFrame && sedangBukaDokumen)
        {
            if (panelBacaDokumen != null && panelBacaDokumen.activeSelf)
            {
                KembaliKeDaftar();
            }
            else
            {
                TutupPanelDokumen();
            }
        }

        // Navigasi cepat dengan tombol panah kiri / kanan atau A / D saat membaca
        if (sedangBukaDokumen && panelBacaDokumen != null && panelBacaDokumen.activeSelf)
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
        sedangBukaDokumen = true;
        if (panelUtama != null) panelUtama.SetActive(true);

        // 1. OTOMATIS PAUSE GAME
        Time.timeScale = 0f;

        // 2. Kunci player & aktifkan kursor mouse
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
