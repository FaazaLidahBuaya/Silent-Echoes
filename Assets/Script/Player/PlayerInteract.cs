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
    private ToiletValve katupSedangDipegang = null;

    void Update()
    {
        if (Keyboard.current == null) return;

        // 1. Selalu jalankan deteksi objek setiap frame
        DeteksiObjek();

        // 2. Interaksi tekan sekali (wasPressedThisFrame)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            CobaInteraksi();
        }

        // 3. Interaksi TAHAN tombol E (isPressed) khusus katup toilet
        if (Keyboard.current.eKey.isPressed)
        {
            if (objekDituju != null)
            {
                ToiletValve katup = objekDituju.GetComponent<ToiletValve>();
                if (katup == null) katup = objekDituju.GetComponentInParent<ToiletValve>();
                if (katup == null) katup = objekDituju.GetComponentInChildren<ToiletValve>();

                if (katup != null && katup.sudahAdaGagang && !katup.isMaxTight)
                {
                    katupSedangDipegang = katup;
                    katup.PutarKatup(Time.deltaTime);
                }
            }
        }
        else
        {
            if (katupSedangDipegang != null)
            {
                katupSedangDipegang.LepasPutar();
                katupSedangDipegang = null;
            }
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
            ToiletValve katup = hit.collider.GetComponent<ToiletValve>();
            if (katup == null) katup = hit.collider.GetComponentInParent<ToiletValve>();
            if (katup == null) katup = hit.collider.GetComponentInChildren<ToiletValve>();
            DokumenPickup dokumen = hit.collider.GetComponent<DokumenPickup>();
            TriggerTekaTeki tekaTeki = hit.collider.GetComponent<TriggerTekaTeki>();

            // Sofa hanya bisa diinteraksi jika cutscene belum pernah terpicu
            if (sofa != null && sofa.sudahTerpicu)
            {
                sofa = null;
            }

            // Katup toilet HANYA bisa diinteraksi jika quest Seloker sudah aktif!
            if (katup != null)
            {
                bool questJalan = (SelokerQuestManager.Instance != null && SelokerQuestManager.Instance.questAktif);
                // Jika quest belum aktif sama sekali, katup belum bisa disentuh
                if (!questJalan)
                {
                    katup = null;
                }
                // Jika ini katup 2 (butuh gagang), hanya bisa disentuh setelah toilet 2 mulai bocor!
                else if (katup.butuhGagang && !katup.sudahAdaGagang && SelokerQuestManager.Instance != null && !SelokerQuestManager.Instance.toilet2BocorDimulai)
                {
                    katup = null;
                }
            }

            // Telepon hanya bisa diinteraksi jika sedang berdering
            if (telepon != null && (!telepon.sedangBisaDiangkat || telepon.sudahSelesaiTelepon))
            {
                telepon = null;
            }

            bool bisaDiinteraksi = pintu != null || sofa != null || korek != null || laci != null || tuas != null || item != null || telepon != null || katup != null || dokumen != null || tekaTeki != null;

            if (bisaDiinteraksi)
            {
                crosshair.color = warnaInteraksi;
                objekDituju = hit.collider.gameObject;

                // Tampilkan nama objek / aksi interaksi
                if (teksInteraksi != null)
                {
                    if (katup != null)
                    {
                        if (!katup.sudahAdaGagang)
                        {
                            int persen = Mathf.RoundToInt(katup.tightness * 100f);
                            // Cek apakah player punya item katup di inventory
                            bool punyaGagang = InventoryManager.Instance != null && InventoryManager.Instance.CekItem("Gagang Katup");
                            teksInteraksi.text = punyaGagang ? $"[E] Pasang Gagang Katup ({persen}%)" : $"(Gagang katup hilang! Sisa: {persen}%)";
                        }
                        else if (katup.isMaxTight)
                        {
                            teksInteraksi.text = "[Katup Tertutup Rapat]";
                        }
                        else
                        {
                            int persen = Mathf.RoundToInt(katup.tightness * 100f);
                            teksInteraksi.text = $"[Tahan E] Putar Katup ({persen}%)";
                        }
                    }
                    else if (dokumen != null)
                    {
                        teksInteraksi.text = $"[E] Periksa {dokumen.dataDokumen.judulDokumen}";
                    }
                    else if (item != null)
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
                    else if (tekaTeki != null && !tekaTeki.sudahSelesai)
                    {
                        teksInteraksi.text = $"[E] {tekaTeki.promptInteraksi}";
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

        if (katupSedangDipegang != null)
        {
            katupSedangDipegang.LepasPutar();
            katupSedangDipegang = null;
        }
    }

    void CobaInteraksi()
    {
        if (objekDituju != null)
        {
            // Coba pasang gagang katup
            ToiletValve katup = objekDituju.GetComponent<ToiletValve>();
            if (katup == null) katup = objekDituju.GetComponentInParent<ToiletValve>();
            if (katup == null) katup = objekDituju.GetComponentInChildren<ToiletValve>();
            if (katup != null && !katup.sudahAdaGagang)
            {
                if (InventoryManager.Instance != null && InventoryManager.Instance.CekItem("Gagang Katup"))
                {
                    InventoryManager.Instance.HapusItem("Gagang Katup");
                    katup.PasangGagang();
                    return;
                }
            }

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
            if (tuas != null) tuas.InteraksiTuas();

            // Coba ambil / periksa dokumen cerita
            DokumenPickup dokumen = objekDituju.GetComponent<DokumenPickup>();
            if (dokumen != null) dokumen.AmbilDokumen();

            // Coba ambil item biasa
            ItemPickup item = objekDituju.GetComponent<ItemPickup>();
            if (item != null) item.AmbilItem();

            // Coba angkat telepon
            TeleponRumah telepon = objekDituju.GetComponent<TeleponRumah>();
            if (telepon != null) telepon.AngkatTelepon();

            // Coba interaksi teka-teki
            TriggerTekaTeki tekaTeki = objekDituju.GetComponent<TriggerTekaTeki>();
            if (tekaTeki != null) tekaTeki.InteraksiPlayer();
        }
    }
}
