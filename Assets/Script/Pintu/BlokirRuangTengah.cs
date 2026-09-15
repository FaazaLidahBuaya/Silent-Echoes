using System.Collections;
using UnityEngine;

public class BlokirRuangTengah : MonoBehaviour
{
    [Header("Referensi (WAJIB DIISI)")]
    public PlayerController scriptPlayer;
    public Transform playerCamera;
    
    [Header("Target Menoleh")]
    public Transform targetRuangTamu; 

    [Header("Pengaturan Animasi")]
    public float durasiMenoleh = 1.5f;
    public float jarakMundur = 1.0f; 

    private bool sudahBerjalan = false; // Penanda agar hanya aktif 1 kali

    void OnTriggerEnter(Collider other)
    {
        // Cek apakah yang menyentuh adalah Player dan cutscene belum pernah aktif
        if (other.CompareTag("Player") && !sudahBerjalan)
        {
            StartCoroutine(BlokirDanMenoleh());
        }
    }

    IEnumerator BlokirDanMenoleh()
    {
        sudahBerjalan = true; // Langsung kunci agar tidak bisa terpicu dua kali

        // 1. KUNCI PEMAIN (Matikan Input)[cite: 1]
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
            scriptPlayer.controller.enabled = false; 
        }

        // 2. MUNDURKAN PLAYER SEDIKIT
        Vector3 posisiMundur = scriptPlayer.transform.position - (scriptPlayer.transform.forward * jarakMundur);
        scriptPlayer.transform.position = posisiMundur;

        // 3. PLAYER MENOLEH KE RUANG TAMU[cite: 1]
        yield return StartCoroutine(LookAtTargetSmoothly(targetRuangTamu, durasiMenoleh));

        // 4. MUNCULKAN SUBTITLE MENGGUNAKAN MANAGER
        if (SubtitleManager.Instance != null)
        {
            // Memanggil fungsi TampilkanSubtitle dari skrip mana saja cukup dengan 1 baris ini:
            SubtitleManager.Instance.TampilkanSubtitle("(Sebaiknya aku menunggu di ruang tamu dulu.)", 2.5f);
        }
        
        yield return new WaitForSeconds(2.5f); // Beri waktu pemain membaca teks

        // 5. SINKRONISASI ROTASI & KEMBALIKAN KONTROL[cite: 1]
        if (scriptPlayer != null && playerCamera != null)
        {
            float rotasiY_Akhir = playerCamera.eulerAngles.y;
            float rotasiX_Akhir = playerCamera.localEulerAngles.x;

            if (rotasiX_Akhir > 180f) rotasiX_Akhir -= 360f;

            scriptPlayer.transform.rotation = Quaternion.Euler(0, rotasiY_Akhir, 0);
            playerCamera.localRotation = Quaternion.Euler(rotasiX_Akhir, 0, 0);
            
            scriptPlayer.SinkronisasiRotasi(rotasiX_Akhir);

            scriptPlayer.controller.enabled = true;
            scriptPlayer.sedangCutscene = false;
        }

        // 6. HANCURKAN TRIGGER
        // Karena kamu sudah memasang invisible wall, trigger ini sudah tidak berguna lagi.
        // Hancurkan komponen collidernya agar ringan di memori.
        Destroy(GetComponent<Collider>());
    }

    IEnumerator LookAtTargetSmoothly(Transform target, float durasi)
    {
        Quaternion rotasiAwal = playerCamera.rotation;
        Vector3 arahTarget = target.position - playerCamera.position;
        arahTarget.y = 0; 
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
}   