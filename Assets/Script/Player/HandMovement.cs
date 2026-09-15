using UnityEngine;
using UnityEngine.InputSystem;

public class HandMovement : MonoBehaviour
{
    [Header("Pengaturan Sway (Delay Menoleh)")]
    public float swayMultiplier = 2f;
    public float swaySmoothness = 5f;
    public float maxSway = 5f;

    [Header("Pengaturan Bobbing (Tangan Goyang)")]
    public float bobbingSpeedWalking = 12f;
    public float bobbingSpeedRunning = 16f;
    public float bobbingAmount = 0.05f;
    public float bobbingSmoothness = 5f;

    private Vector3 posisiAwal;
    private Quaternion rotasiAwal;
    private float timer = 0f;

    void Start()
    {
        posisiAwal = transform.localPosition;
        rotasiAwal = transform.localRotation;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // --- TAMBAHAN BARU: Kunci Goyangan Tangan ---
        // Cek status dari PlayerController yang ada di Parent
        PlayerController player = GetComponentInParent<PlayerController>();
        if (player != null && (player.sedangCutscene || player.bukaInventory)) return;
        // --------------------------------------------

        HandleSway();
        HandleBobbing();
    }

    void HandleSway()
    {
        // 1. Ambil input mouse untuk menghitung seberapa cepat layar berputar
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = Mathf.Clamp(mouseDelta.x * swayMultiplier * -0.01f, -maxSway, maxSway);
        float mouseY = Mathf.Clamp(mouseDelta.y * swayMultiplier * -0.01f, -maxSway, maxSway);

        // 2. Buat target rotasi yang berlawanan dengan arah gerak mouse
        Quaternion rotasiX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotasiY = Quaternion.AngleAxis(-mouseX, Vector3.up);
        Quaternion rotasiTarget = rotasiAwal * rotasiX * rotasiY;

        // 3. Aplikasikan secara perlahan (delay effect)
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotasiTarget, Time.deltaTime * swaySmoothness);
    }

    void HandleBobbing()
    {
        // 1. Cek apakah pemain sedang menekan tombol WASD
        float sumbuX = 0f;
        float sumbuZ = 0f;
        if (Keyboard.current.wKey.isPressed) sumbuZ += 1f;
        if (Keyboard.current.sKey.isPressed) sumbuZ -= 1f;
        if (Keyboard.current.dKey.isPressed) sumbuX += 1f;
        if (Keyboard.current.aKey.isPressed) sumbuX -= 1f;

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float bobbingSpeed = isSprinting ? bobbingSpeedRunning : bobbingSpeedWalking;

        // 2. Jika pemain bergerak, jalankan efek goyang
        if (Mathf.Abs(sumbuX) > 0f || Mathf.Abs(sumbuZ) > 0f)
        {
            timer += Time.deltaTime * bobbingSpeed;
            
            // Menggunakan rumus Sin/Cos untuk membuat efek angka 8
            float bobY = Mathf.Sin(timer) * bobbingAmount;
            float bobX = Mathf.Cos(timer * 0.5f) * bobbingAmount;

            Vector3 targetPos = new Vector3(posisiAwal.x + bobX, posisiAwal.y + bobY, posisiAwal.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobbingSmoothness);
        }
        else
        {
            // Jika diam, kembalikan ke posisi awal
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, posisiAwal, Time.deltaTime * bobbingSmoothness);
        }
    }
}