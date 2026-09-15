using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform mainCamera;

    void Start()
    {
        // Mencari kamera utama secara otomatis
        mainCamera = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Membuat PNG selalu menghadap ke arah yang sama dengan kamera
            transform.forward = mainCamera.forward;
        }
    }
}