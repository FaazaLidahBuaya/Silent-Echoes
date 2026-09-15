using System.Collections;
using UnityEngine;

public class CutsceneMasukRumah : MonoBehaviour
{
    [Header("Referensi Player (WAJIB)")]
    public PlayerController scriptPlayer;
    public Transform playerBody;
    public Transform playerCamera;

    [Header("Titik Posisi Cutscene")]
    public Transform titikMulaiCutscene;
    public Transform titikMajuKeDalam;

    [Header("Target Menoleh (Kamera)")]
    public Transform targetKiri;
    public Transform targetKanan;
    public Transform targetPintu;

    [Header("Pengaturan Pintu Dorong")]
    public Transform engselPintu;
    public Vector3 sudutBukaPintu = new Vector3(0, 90, 0); 
    public float durasiBukaPintu = 2.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxPanggilKakek;
    public AudioClip sfxPintuBuka; 

    [Header("Durasi Animasi")]
    public float durasiMenoleh = 1.0f;
    public float durasiMaju = 2.5f;

    private bool cutsceneSelesai = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !cutsceneSelesai)
        {
            StartCoroutine(MulaiCutsceneMasuk());
        }
    }

    IEnumerator MulaiCutsceneMasuk()
    {
        cutsceneSelesai = true;

        // 1. KUNCI PEMAIN & SIAPKAN POSISI
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
            scriptPlayer.controller.enabled = false; 
        }

        // Pindahkan player tepat ke depan pintu
        playerBody.position = titikMulaiCutscene.position;
        
        // 2. MENATAP PINTU TERLEBIH DAHULU
        // Menatap pintu dengan durasi normal agar tidak terlalu kaget
        yield return StartCoroutine(LookAtTargetSmoothly(targetPintu, durasiMenoleh));
        
        // Diam menatap pintu sebentar sebelum memanggil
        yield return new WaitForSeconds(1.0f); 

        // 3. MEMANGGIL KAKEK
        if (audioSource != null && sfxPanggilKakek != null)
        {
            audioSource.PlayOneShot(sfxPanggilKakek);
        }

        // Tunggu agak lama (seolah menunggu balasan dari dalam rumah)
        yield return new WaitForSeconds(2.0f);

        // 4. MENCARI KAKEK (MENOLEH KIRI KANAN)
        // Karena tidak ada jawaban, menoleh ke Kiri
        yield return StartCoroutine(LookAtTargetSmoothly(targetKiri, durasiMenoleh));
        yield return new WaitForSeconds(0.5f); 

        // Menoleh ke Kanan
        yield return StartCoroutine(LookAtTargetSmoothly(targetKanan, durasiMenoleh));
        yield return new WaitForSeconds(0.5f); 

        // Kembali menatap pintu
        yield return StartCoroutine(LookAtTargetSmoothly(targetPintu, durasiMenoleh));
        yield return new WaitForSeconds(0.5f);

        // 5. PINTU DIDORONG & PLAYER MAJU BERSAMAAN
        if (audioSource != null && sfxPintuBuka != null)
        {
            audioSource.PlayOneShot(sfxPintuBuka);
        }

        Coroutine pintuBerputar = StartCoroutine(BukaPintuSmoothly(durasiBukaPintu));
        Coroutine playerMaju = StartCoroutine(MovePlayerSmoothly(titikMajuKeDalam, durasiMaju));

        yield return playerMaju;
        yield return pintuBerputar;

        // 6. SINKRONISASI KAMERA & KEMBALIKAN KONTROL
        if (scriptPlayer != null && playerCamera != null)
        {
            float rotasiY_Akhir = playerCamera.eulerAngles.y;
            float rotasiX_Akhir = playerCamera.localEulerAngles.x;

            if (rotasiX_Akhir > 180f) rotasiX_Akhir -= 360f;

            playerBody.rotation = Quaternion.Euler(0, rotasiY_Akhir, 0);
            playerCamera.localRotation = Quaternion.Euler(rotasiX_Akhir, 0, 0);

            scriptPlayer.SinkronisasiRotasi(rotasiX_Akhir);
            
            scriptPlayer.controller.enabled = true;
            scriptPlayer.sedangCutscene = false;
        }
    }

    IEnumerator LookAtTargetSmoothly(Transform target, float durasi)
    {
        Quaternion rotasiAwal = playerCamera.rotation;
        Vector3 arahTarget = target.position - playerCamera.position;
        Quaternion rotasiAkhir = Quaternion.LookRotation(arahTarget);

        float waktu = 0f;
        while (waktu < durasi)
        {
            waktu += Time.deltaTime;
            float persentase = waktu / durasi;
            float kurva = Mathf.SmoothStep(0f, 1f, persentase);
            playerCamera.rotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, kurva);
            yield return null;
        }
        playerCamera.rotation = rotasiAkhir;
    }

    IEnumerator BukaPintuSmoothly(float durasi)
    {
        Quaternion rotasiAwal = engselPintu.rotation;
        Quaternion rotasiAkhir = rotasiAwal * Quaternion.Euler(sudutBukaPintu);

        float waktu = 0f;
        while (waktu < durasi)
        {
            waktu += Time.deltaTime;
            float persentase = waktu / durasi;
            float kurva = Mathf.SmoothStep(0f, 1f, persentase); 
            engselPintu.rotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, kurva);
            yield return null;
        }
        engselPintu.rotation = rotasiAkhir;
    }

    IEnumerator MovePlayerSmoothly(Transform targetTitik, float durasi)
    {
        Vector3 posisiAwal = playerBody.position;
        Vector3 posisiAkhir = targetTitik.position;

        float waktu = 0f;
        while (waktu < durasi)
        {
            waktu += Time.deltaTime;
            float persentase = waktu / durasi;
            float kurva = Mathf.SmoothStep(0f, 1f, persentase);
            playerBody.position = Vector3.Lerp(posisiAwal, posisiAkhir, kurva);
            yield return null;
        }
        playerBody.position = posisiAkhir;
    }
}