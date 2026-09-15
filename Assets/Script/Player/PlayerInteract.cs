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
            bool bisaDiinteraksi = hit.collider.GetComponent<DoorController>() != null ||
                       hit.collider.GetComponent<CutsceneSofa>() != null ||
                       hit.collider.GetComponent<KorekMeja>() != null ||
                       hit.collider.GetComponent<DrawerController>() != null ||
                       hit.collider.GetComponent<TuasListrik>() != null ||
                       hit.collider.GetComponent<ItemPickup>() != null; // <--- TAMBAHAN UNTUK ITEM

            if (bisaDiinteraksi)
            {
                crosshair.color = warnaInteraksi;
                objekDituju = hit.collider.gameObject; 
            }
            else
            {
                crosshair.color = warnaNormal;
                objekDituju = null;
            }
        }
        else
        {
            crosshair.color = warnaNormal;
            objekDituju = null;
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
        }
    }
}