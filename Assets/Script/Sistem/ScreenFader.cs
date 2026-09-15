using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("Referensi UI")]
    [Tooltip("Masukkan UI Image warna hitam di sini")]
    public Image layarHitam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (layarHitam != null)
        {
            // Pastikan saat mulai game layar transparan
            SetAlpha(0f);
            layarHitam.gameObject.SetActive(true);
        }
    }

    // Fungsi berkedip (mengantuk)
    public IEnumerator EfekBerkedip(int jumlahKedipan, float kecepatan)
    {
        for (int i = 0; i < jumlahKedipan; i++)
        {
            // Menutup mata (Hitam)
            yield return StartCoroutine(TransisiAlpha(1f, kecepatan));
            yield return new WaitForSeconds(0.2f);
            // Membuka mata (Terang)
            yield return StartCoroutine(TransisiAlpha(0f, kecepatan));
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Transisi memudar ke Hitam / Terang
    public IEnumerator TransisiLayar(bool menujuHitam, float durasi)
    {
        float targetAlpha = menujuHitam ? 1f : 0f;
        yield return StartCoroutine(TransisiAlpha(targetAlpha, durasi));
    }

    private IEnumerator TransisiAlpha(float targetAlpha, float durasi)
    {
        float startAlpha = layarHitam.color.a;
        float waktu = 0;

        while (waktu < 1f)
        {
            waktu += Time.deltaTime / durasi;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, waktu));
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color warna = layarHitam.color;
        warna.a = alpha;
        layarHitam.color = warna;
    }
}