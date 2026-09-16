using System.Collections;
using UnityEngine;

public class DrawerController : MonoBehaviour
{
    [Header("Pengaturan Posisi Laci")]
    public Transform objekLaci; // Masukkan objek laci di sini
    public Vector3 jarakBuka = new Vector3(0, 0, 0.5f); // Sesuaikan sumbu (X, Y, atau Z) seberapa jauh laci maju
    public float kecepatanBuka = 2f;

    // Menggunakan Vector3 untuk posisi, bukan Quaternion untuk rotasi
    private Vector3 posisiTertutup;
    private Vector3 posisiTerbuka;

    [Header("Pengaturan Audio")]
    public AudioSource audioSource;
    public AudioClip sfxBuka;
    public AudioClip sfxTutup;

    [Header("Pengaturan Kunci Laci")]
    public bool isTerkunci = false;
    public AudioClip sfxTerkunci;

    private bool isMembukaAtauMenutup = false;
    public bool isOpen { get; private set; } = false;

    void Start()
    {
        if (objekLaci != null)
        {
            // 1. Simpan posisi asli sebagai posisi "Tertutup"
            posisiTertutup = objekLaci.localPosition;
            
            // 2. Kalkulasi posisi "Terbuka" (Posisi tertutup ditambah jarak buka)
            posisiTerbuka = posisiTertutup + jarakBuka;
        }
    }

    public void InteraksiLaci()
    {
        if (isMembukaAtauMenutup) return;

        if (isTerkunci)
        {
            if (audioSource != null && sfxTerkunci != null)
            {
                audioSource.PlayOneShot(sfxTerkunci);
            }
            return; 
        }

        // 3. Panggil Coroutine untuk menggerakkan posisi Laci
        if (isOpen)
        {
            StartCoroutine(GerakkanLaci(posisiTertutup, sfxTutup));
        }
        else
        {
            StartCoroutine(GerakkanLaci(posisiTerbuka, sfxBuka));
        }
        
        isOpen = !isOpen;
    }

    // 4. Coroutine diganti untuk menerima targetPosisi (Vector3)
    IEnumerator GerakkanLaci(Vector3 targetPosisi, AudioClip sfx)
    {
        isMembukaAtauMenutup = true;

        if (audioSource != null && sfx != null)
        {
            audioSource.PlayOneShot(sfx);
        }

        Vector3 posisiAwal = objekLaci.localPosition;
        
        float waktu = 0f;
        while (waktu < 1f)
        {
            waktu += Time.deltaTime * kecepatanBuka;
            
            // Menggunakan Vector3.Lerp untuk pergerakan lurus mulus (maju/mundur)
            objekLaci.localPosition = Vector3.Lerp(posisiAwal, targetPosisi, waktu);
            yield return null;
        }

        // Pastikan posisi akhirnya pas
        objekLaci.localPosition = targetPosisi;
        isMembukaAtauMenutup = false;
    }
}