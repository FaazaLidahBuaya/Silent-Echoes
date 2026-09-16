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
    private bool inventoryAktif = false;

    [Header("Audio Inventory")]
    public AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.rKey.wasPressedThisFrame && !panelViewer.activeSelf)
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        inventoryAktif = !inventoryAktif;
        panelInventory.SetActive(inventoryAktif);

        if (scriptPlayer != null)
        {
            // --- UBAH BARIS INI ---
            // Ganti scriptPlayer.sedangCutscene menjadi scriptPlayer.bukaInventory
            scriptPlayer.bukaInventory = inventoryAktif; 
        }

        if (inventoryAktif)
        {
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false;
        }
    }

    public void TambahItem(string namaItem, Sprite logoItem, GameObject itemAsli, AudioClip sfx)
    {
        // 1. Putar suara jika ada
        if (audioSource != null && sfx != null)
        {
            audioSource.PlayOneShot(sfx);
        }

        // 2. Tambahkan ke daftar
        if (!daftarItem.ContainsKey(namaItem))
        {
            daftarItem.Add(namaItem, itemAsli);
            
            GameObject tombolBaru = Instantiate(prefabTombolItem, tempatLogoItem);
            
            Image img = tombolBaru.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = logoItem;
                img.color = Color.white; // Pastikan warna tombol / logo selalu putih murni
            }
            
            TextMeshProUGUI teksNama = tombolBaru.GetComponentInChildren<TextMeshProUGUI>();
            if (teksNama != null)
            {
                teksNama.text = namaItem;
            }
            
            tombolBaru.GetComponent<Button>().onClick.AddListener(() => MunculkanViewer(itemAsli));
            daftarTombolUI.Add(namaItem, tombolBaru);
        }
    }

    public void HapusItem(string namaItem)
    {
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