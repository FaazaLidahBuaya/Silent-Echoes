using UnityEngine;
using UnityEngine.InputSystem;

public class ItemViewer : MonoBehaviour
{
    public float kecepatanRotasi = 0.5f;
    public float kecepatanZoom = 0.5f; // Bisa dibesarkan sedikit karena sekarang pergerakannya mulus
    public float minZoom = 0.5f;
    public float maxZoom = 3.0f;
    public float kehalusanZoom = 10f; // Semakin kecil angkanya, semakin lambat/mulus efeknya

    private Vector3 skalaAwal;
    private Vector3 targetSkala; // Menyimpan ukuran tujuan

    void Start()
    {
        skalaAwal = transform.localScale;
        targetSkala = skalaAwal; // Saat mulai, targetnya adalah ukuran saat ini
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. ROTASI
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            transform.Rotate(Vector3.up, -mouseDelta.x * kecepatanRotasi, Space.World);
            transform.Rotate(Vector3.right, mouseDelta.y * kecepatanRotasi, Space.World);
        }

        // 2. ZOOM (Menghitung target ukuran)
        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY != 0)
        {
            float arahZoom = Mathf.Clamp(scrollY, -1f, 1f); 
            
            // Tambahkan ke target skala, bukan langsung ke transform
            targetSkala += (Vector3.one * arahZoom * kecepatanZoom);
            
            // Batasi target skala agar tidak terlalu besar/kecil
            targetSkala.x = Mathf.Clamp(targetSkala.x, skalaAwal.x * minZoom, skalaAwal.x * maxZoom);
            targetSkala.y = Mathf.Clamp(targetSkala.y, skalaAwal.y * minZoom, skalaAwal.y * maxZoom);
            targetSkala.z = Mathf.Clamp(targetSkala.z, skalaAwal.z * minZoom, skalaAwal.z * maxZoom);
        }

        // 3. EFEK MULUS (Lerp ukuran benda menuju target ukuran secara perlahan)
        transform.localScale = Vector3.Lerp(transform.localScale, targetSkala, Time.deltaTime * kehalusanZoom);
    }
}