using System.Collections;
using UnityEngine;

public class GerbangDepan : MonoBehaviour
{
    [Header("Referensi Player (WAJIB DIISI)")]
    [Tooltip("Masukkan obyek Player yang memiliki script PlayerController ke sini")]
    public PlayerController scriptPlayer; 

    [Header("Kamera & Target")]
    public Transform playerCamera;
    public Transform targetRumah;
    public Transform targetGerbang;

    [Header("Gerbang & Suara")]
    public Transform engselGerbang;
    public AudioSource audioSource;
    public AudioClip sfxGerbangBuka;

    [Header("Pengaturan Waktu (Detik)")]
    public float durasiTransisiKamera = 2.0f; 
    public float durasiMenatapRumah = 1.5f;
    public float kecepatanBukaGerbang = 1.5f;
    public Vector3 sudutBukaGerbang = new Vector3(0, 90, 0);

    private bool isCutscenePlaying = false;

    void Awake()
    {
        // Jika sedang respawn di telepon, gerbang depan sudah pernah dibuka
        if (GameCheckpointManager.respawnDiTelepon)
        {
            isCutscenePlaying = true;
            if (engselGerbang != null)
            {
                engselGerbang.rotation = engselGerbang.rotation * Quaternion.Euler(sudutBukaGerbang);
            }
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCutscenePlaying)
        {
            StartCoroutine(MulaiCutscene());
        }
    }

    IEnumerator MulaiCutscene()
    {
        isCutscenePlaying = true;

        // 1. KUNCI PEMAIN (Matikan Input WASD & Mouse secara instan)
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
        }

        // 2. Menoleh ke Rumah 
        yield return StartCoroutine(LookAtTargetSmoothly(targetRumah, durasiTransisiKamera));
        
        // Jeda menatap rumah
        yield return new WaitForSeconds(durasiMenatapRumah);

        // 3. Menoleh dari Rumah ke Gerbang
        yield return StartCoroutine(LookAtTargetSmoothly(targetGerbang, durasiTransisiKamera));
        
        // Jeda sebentar sebelum gerbang buka
        yield return new WaitForSeconds(0.3f); 

        // 4. Mainkan SFX dan Buka Gerbang
        if (audioSource != null && sfxGerbangBuka != null)
        {
            audioSource.PlayOneShot(sfxGerbangBuka);
        }
        yield return StartCoroutine(BukaGerbang());

        // 5. SINKRONISASI BADAN & KAMERA
        if (scriptPlayer != null && playerCamera != null)
        {
            // Ambil rotasi akhir kamera (Pandangan terakhir ke gerbang)
            float rotasiY_Akhir = playerCamera.eulerAngles.y;
            float rotasiX_Akhir = playerCamera.localEulerAngles.x;

            // Konversi sumbu X agar cocok dengan batas -90 sampai 90 derajat
            if (rotasiX_Akhir > 180f) rotasiX_Akhir -= 360f;

            // Putar seluruh Badan Player agar menghadap ke gerbang (Sumbu Y)
            scriptPlayer.transform.rotation = Quaternion.Euler(0, rotasiY_Akhir, 0);
            
            // Rapikan kemiringan atas/bawah Kamera (Sumbu X)
            playerCamera.localRotation = Quaternion.Euler(rotasiX_Akhir, 0, 0);

            // Perbarui nilai di dalam otak pergerakan player
            scriptPlayer.SinkronisasiRotasi(rotasiX_Akhir);

            // 6. LEPASKAN PEMAIN (Cutscene selesai, bebas bergerak lagi)
            scriptPlayer.sedangCutscene = false;
        }
    }

    IEnumerator LookAtTargetSmoothly(Transform target, float durasi)
    {
        Quaternion rotasiAwal = playerCamera.rotation;
        Vector3 arahTarget = target.position - playerCamera.position;
        Quaternion rotasiAkhir = Quaternion.LookRotation(arahTarget);

        float waktuBerjalan = 0f;

        while (waktuBerjalan < durasi)
        {
            waktuBerjalan += Time.deltaTime;
            float persentase = waktuBerjalan / durasi;
            float kurvaSmooth = Mathf.SmoothStep(0f, 1f, persentase);

            playerCamera.rotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, kurvaSmooth);
            yield return null;
        }

        playerCamera.rotation = rotasiAkhir; 
    }

    IEnumerator BukaGerbang()
    {
        Quaternion rotasiAwal = engselGerbang.rotation;
        Quaternion rotasiAkhir = rotasiAwal * Quaternion.Euler(sudutBukaGerbang);

        float waktu = 0;
        while (waktu < 1f)
        {
            waktu += Time.deltaTime * kecepatanBukaGerbang;
            engselGerbang.rotation = Quaternion.Slerp(rotasiAwal, rotasiAkhir, waktu);
            yield return null;
        }
    }
}