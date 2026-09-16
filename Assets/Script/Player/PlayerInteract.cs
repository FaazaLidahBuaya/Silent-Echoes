using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteract : MonoBehaviour
{
    [Header("Pengaturan Raycast")]
    public Transform playerCamera;
    public float jarakInteraksi = 3f;

    [Header("Pengaturan Crosshair")]
    public Image crosshair;
    public Color warnaNormal = Color.white;
    public Color warnaInteraksi = Color.red; 

    [Header("Pengaturan Info Teks Interaksi")]
    public TMPro.TextMeshProUGUI teksInteraksi; // Hubungkan TextMeshPro UI di Inspector (misal: "Ambil Obeng [E]")

    private GameObject objekDituju = null; 

    void Update()
    {
        if (Keyboard.current == null) return;

        // 1. Selalu jalankan deteksi objek setiap frame
        DeteksiObjek();

        // 2. Jika tombol E ditekan, jalankan interaksi
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            CobaInteraksi();
        }
    }

    void DeteksiObjek()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, jarakInteraksi))
        {
            DoorController pintu = hit.collider.GetComponent<DoorController>();
            CutsceneSofa sofa = hit.collider.GetComponent<CutsceneSofa>();
            KorekMeja korek = hit.collider.GetComponent<KorekMeja>();
            DrawerController laci = hit.collider.GetComponent<DrawerController>();
            TuasListrik tuas = hit.collider.GetComponent<TuasListrik>();
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();
            TeleponRumah telepon = hit.collider.GetComponent<TeleponRumah>();

            // Jika belum bangun dari sofa (belum malam), item pickup dan korek meja belum bisa diambil & tidak memunculkan teks
            if (!CutsceneSofa.barangBisaDiinteraksi)
            {
                item = null;
                korek = null;
            }
            else if (korek != null && !korek.bisaDiambil)
            {
                korek = null;
            }

            // Telepon hanya bisa diinteraksi jika sedang berdering
            if (telepon != null && (!telepon.sedangBisaDiangkat || telepon.sudahSelesaiTelepon))
            {
                telepon = null;
            }

            bool bisaDiinteraksi = pintu != null || sofa != null || korek != null || laci != null || tuas != null || item != null || telepon != null;

            if (bisaDiinteraksi)
            {
                crosshair.color = warnaInteraksi;
                objekDituju = hit.collider.gameObject;

                // Tampilkan nama objek / aksi interaksi
                if (teksInteraksi != null)
                {
                    if (item != null)
                    {
                        teksInteraksi.text = $"[E] Ambil {item.namaItem}";
                    }
                    else if (korek != null)
                    {
                        teksInteraksi.text = "[E] Ambil Korek Api";
                    }
                    else if (pintu != null)
                    {
                        teksInteraksi.text = pintu.isOpen ? "[E] Tutup" : "[E] Buka";
                    }
                    else if (laci != null)
                    {
                        teksInteraksi.text = laci.isOpen ? "[E] Tutup" : "[E] Buka";
                    }
                    else if (sofa != null)
                    {
                        teksInteraksi.text = "[E] Duduk";
                    }
                    else if (tuas != null)
                    {
                        if (tuas.sedangProses || tuas.listrikMenyalaStabil)
                        {
                            ResetCrosshairDanTeks();
                            return;
                        }
                        else if (!tuas.sudahDitarik)
                        {
                            teksInteraksi.text = "[E] Tarik Tuas";
                        }
                        else if (!tuas.sudahDiperbaiki)
                        {
                            if (!tuas.coverTerbuka)
                            {
                                teksInteraksi.text = "[E] Buka Penutup Saklar";
                            }
                            else
                            {
                                teksInteraksi.text = "[E] Pasang Sekring";
                            }
                        }
                        else
                        {
                            teksInteraksi.text = "[E] Nyalakan Saklar";
                        }
                    }
                    else if (telepon != null)
                    {
                        teksInteraksi.text = "[E] Angkat Telepon";
                    }
                    teksInteraksi.gameObject.SetActive(true);
                }
            }
            else
            {
                ResetCrosshairDanTeks();
            }
        }
        else
        {
            ResetCrosshairDanTeks();
        }
    }

    void ResetCrosshairDanTeks()
    {
        if (crosshair != null) crosshair.color = warnaNormal;
        objekDituju = null;

        if (teksInteraksi != null)
        {
            teksInteraksi.text = "";
            teksInteraksi.gameObject.SetActive(false);
        }
    }

    void CobaInteraksi()
    {
        if (objekDituju != null)
        {
            // Coba buka pintu
            DoorController pintu = objekDituju.GetComponent<DoorController>();
            if (pintu != null) pintu.InteraksiPintu();

            // Coba duduk di sofa
            CutsceneSofa sofa = objekDituju.GetComponent<CutsceneSofa>();
            if (sofa != null) sofa.InteraksiSofa();

            // Coba ambil korek meja
            KorekMeja korek = objekDituju.GetComponent<KorekMeja>();
            if (korek != null) korek.AmbilKorek();

            // Coba buka laci
            DrawerController laci = objekDituju.GetComponent<DrawerController>();
            if (laci != null) laci.InteraksiLaci(); 
            
            // Coba tarik tuas
            TuasListrik tuas = objekDituju.GetComponent<TuasListrik>();
            if (tuas != null) tuas.InteraksiTuas(); // <--- TAMBAHAN UNTUK TUAS

            // Coba ambil item biasa
            ItemPickup item = objekDituju.GetComponent<ItemPickup>();
            if (item != null) item.AmbilItem();

            // Coba angkat telepon
            TeleponRumah telepon = objekDituju.GetComponent<TeleponRumah>();
            if (telepon != null) telepon.AngkatTelepon();
        }
    }
}