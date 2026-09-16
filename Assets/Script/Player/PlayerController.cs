using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Referensi Komponen")]
    public CharacterController controller;
    public Transform playerCamera;
    
    [Header("Pengaturan Tangan & Cutscene")]
    [Tooltip("Masukkan KESELURUHAN objek tangan ke sini (seperti sebelumnya)")]
    public GameObject tanganKanan; 
    [Tooltip("Masukkan HANYA objek visual/model tangannya saja ke sini (tanpa cahaya)")]
    public GameObject visualTangan; 
    [Tooltip("Masukkan objek visual/model korek api dan api sprite ke sini (tanpa Point Light cahaya)")]
    public GameObject visualKorek;

    [Header("Pengaturan Gerak & Lari")]
    public float kecepatanJalan = 4f;
    public float kecepatanLari = 7f; // Menahan Shift untuk lari cepat
    public float gravitasi = -9.81f;

    [Header("Pengaturan Kamera")]
    public float sensitivitasMouse = 0.5f;
    private float rotasiX = 0f;
    private Vector3 kecepatanJatuh;

    [HideInInspector] 
    public bool sedangCutscene = false;

    [HideInInspector]
    public bool bukaInventory = false;
    
    [HideInInspector]
    public bool sedangBawaBarang = false; // Penanda apakah pemain sudah ambil barang

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Pastikan tangan dan korek tetap aktif jika player sudah membawa barang (termasuk saat cutscene)
        if (sedangBawaBarang)
        {
            if (visualTangan != null) visualTangan.SetActive(true);
            if (visualKorek != null) visualKorek.SetActive(true);
        }

        // JIKA SEDANG CUTSCENE, HANYA KUNCI KONTROL PERGERAKAN & KAMERA
        if (sedangCutscene)
        {
            return;
        }

        // --- TAMBAHAN BARU ---
        // Jika buka inventory, Hentikan pergerakan & kamera di bawahnya, TAPI tangan tetap menyala!
        if (bukaInventory) return;

        if (Mouse.current == null || Keyboard.current == null) return;

        // =======================
        // 1. KONTROL PANDANGAN (MOUSE) - STABIL & MULUS
        // =======================
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        float mouseX = mouseDelta.x * sensitivitasMouse;
        float mouseY = mouseDelta.y * sensitivitasMouse;

        rotasiX -= mouseY;
        rotasiX = Mathf.Clamp(rotasiX, -90f, 90f);
        
        playerCamera.localRotation = Quaternion.Euler(rotasiX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // =======================
        // 2. KONTROL PERGERAKAN (WASD + SHIFT)
        // =======================
        float sumbuX = 0f;
        float sumbuZ = 0f;

        if (Keyboard.current.wKey.isPressed) sumbuZ += 1f;
        if (Keyboard.current.sKey.isPressed) sumbuZ -= 1f;
        if (Keyboard.current.dKey.isPressed) sumbuX += 1f;
        if (Keyboard.current.aKey.isPressed) sumbuX -= 1f;

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float kecepatanSaatIni = isSprinting ? kecepatanLari : kecepatanJalan;

        Vector3 arahGerak = transform.right * sumbuX + transform.forward * sumbuZ;
        
        if (arahGerak.magnitude > 1f) 
        {
            arahGerak.Normalize();
        }

        controller.Move(arahGerak * kecepatanSaatIni * Time.deltaTime);

        // =======================
        // 3. GRAVITASI
        // =======================
        if (controller.isGrounded && kecepatanJatuh.y < 0)
        {
            kecepatanJatuh.y = -2f;
        }

        kecepatanJatuh.y += gravitasi * Time.deltaTime;
        controller.Move(kecepatanJatuh * Time.deltaTime);
    }

    public void SinkronisasiRotasi(float rotasiX_Baru)
    {
        rotasiX = rotasiX_Baru;
    }
}