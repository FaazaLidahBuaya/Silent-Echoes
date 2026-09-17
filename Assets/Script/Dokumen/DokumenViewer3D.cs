using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script kontrol rotasi & zoom model 3D dokumen di panel kiri.
/// Menggunakan Time.unscaledDeltaTime agar tetap responsif saat game di-pause (Time.timeScale = 0).
/// </summary>
public class DokumenViewer3D : MonoBehaviour
{
    [Header("Pengaturan Rotasi (Klik & Drag Mouse Kiri)")]
    public float kecepatanRotasi = 0.5f;

    [Header("Pengaturan Zoom (Scroll Wheel Mouse)")]
    public float kecepatanZoom = 0.4f;
    public float minZoom = 0.4f;
    public float maxZoom = 2.5f;
    public float kehalusanZoom = 12f;

    private Vector3 skalaAwal;
    private Vector3 targetSkala;
    private Quaternion rotasiAwal;

    void Awake()
    {
        skalaAwal = transform.localScale;
        targetSkala = skalaAwal;
        rotasiAwal = transform.localRotation;
    }

    public void ResetTampilan()
    {
        transform.localRotation = rotasiAwal;
        transform.localScale = skalaAwal;
        targetSkala = skalaAwal;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. ROTASI DENGAN MOUSE DRAG KIRI
        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            transform.Rotate(Vector3.up, -delta.x * kecepatanRotasi, Space.World);
            transform.Rotate(Vector3.right, delta.y * kecepatanRotasi, Space.World);
        }

        // 2. ZOOM DENGAN SCROLL MOUSE
        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            float arahZoom = Mathf.Clamp(scrollY, -1f, 1f);
            targetSkala += (Vector3.one * arahZoom * kecepatanZoom);

            targetSkala.x = Mathf.Clamp(targetSkala.x, skalaAwal.x * minZoom, skalaAwal.x * maxZoom);
            targetSkala.y = Mathf.Clamp(targetSkala.y, skalaAwal.y * minZoom, skalaAwal.y * maxZoom);
            targetSkala.z = Mathf.Clamp(targetSkala.z, skalaAwal.z * minZoom, skalaAwal.z * maxZoom);
        }

        // 3. LERP SKALA (Gunakan unscaledDeltaTime karena game sedang Pause!)
        transform.localScale = Vector3.Lerp(transform.localScale, targetSkala, Time.unscaledDeltaTime * kehalusanZoom);
    }
}
