using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    [Header("Kecepatan Arus Air")]
    public float speedX = 0.0f; // Kecepatan mengalir ke samping
    public float speedY = 0.5f; // Kecepatan mengalir ke depan/belakang

    private Renderer rend;

    void Start()
    {
        // Mengambil komponen Renderer dari objek sungai
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // Menghitung jarak pergeseran berdasarkan waktu
        float offsetX = Time.time * speedX;
        float offsetY = Time.time * speedY;

        // Menerapkan pergeseran ke tekstur URP (nama propertinya "_BaseMap")
        rend.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
    }
}