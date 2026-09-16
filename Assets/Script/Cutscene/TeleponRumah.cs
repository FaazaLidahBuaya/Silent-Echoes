using System.Collections;
using UnityEngine;

public class TeleponRumah : MonoBehaviour
{
    public static TeleponRumah Instance;

    [Header("Referensi Player & Kamera")]
    public PlayerController scriptPlayer;
    public Transform playerCamera;

    [Header("Objek Gagang Telepon")]
    [Tooltip("Gagang telepon di atas bodi telepon (CUKUP SATU OBJEK INI SAJA)")]
    public GameObject gagangDiMeja;

    [Header("Posisi Kuping Kiri (Offset Relatif Kamera)")]
    [Tooltip("Posisi gagang di samping telinga kiri player (X: kiri, Y: atas/bawah, Z: maju/mundur)")]
    public Vector3 offsetKupingKiri = new Vector3(-0.35f, -0.05f, 0.4f);
    public Vector3 rotasiOffsetKuping = new Vector3(15f, 25f, -10f);

    [Header("Audio Dering & Telepon")]
    public AudioSource audioSourceTelepon;
    public AudioClip sfxDeringTelepon;
    public AudioClip sfxAngkatTelepon;
    public AudioClip sfxZzttKresek;
    public AudioClip sfxTutupTelepon;

    [Header("Audio Kejadian di Belakang")]
    [Tooltip("AudioSource yang diletakkan di arah belakang lorong")]
    public AudioSource audioSourceBelakang;
    public AudioClip sfxBarangJatuhPrang;

    [Header("Durasi Animasi Angkat")]
    public float durasiAngkatGagang = 0.9f;
    public float durasiKembalikanGagang = 0.5f;

    [Header("Target Menoleh ke Belakang")]
    public Transform targetArahBelakang;
    public float durasiMenolehBelakang = 1.0f;

    [Header("Status Telepon")]
    public bool sedangBisaDiangkat = false;
    public bool sudahSelesaiTelepon = false;

    private Coroutine deringCoroutine;

    // Simpan posisi, rotasi, dan parent asli gagang di meja
    private Transform parentAsliGagang;
    private Vector3 posisiAsliMeja;
    private Quaternion rotasiAsliMeja;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (gagangDiMeja != null)
        {
            gagangDiMeja.SetActive(true);
            parentAsliGagang = gagangDiMeja.transform.parent;
            posisiAsliMeja = gagangDiMeja.transform.position;
            rotasiAsliMeja = gagangDiMeja.transform.rotation;
        }
    }

    /// <summary>
    /// Dipanggil beberapa detik setelah saklar dinyalakan
    /// </summary>
    public void MulaiBerdering()
    {
        if (sudahSelesaiTelepon) return;
        sedangBisaDiangkat = true;

        if (gagangDiMeja != null)
        {
            gagangDiMeja.SetActive(true);
        }

        if (deringCoroutine != null) StopCoroutine(deringCoroutine);
        deringCoroutine = StartCoroutine(LoopDering());

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Angkat telepon yang berdering", "Cari sumber suara telepon di rumah");
        }
    }

    IEnumerator LoopDering()
    {
        while (sedangBisaDiangkat && !sudahSelesaiTelepon)
        {
            if (audioSourceTelepon != null && sfxDeringTelepon != null)
            {
                audioSourceTelepon.PlayOneShot(sfxDeringTelepon);
            }
            // Jeda antar dering telepon kuno (~3-4 detik)
            yield return new WaitForSeconds(3.5f);
        }
    }

    public void AngkatTelepon()
    {
        if (!sedangBisaDiangkat || sudahSelesaiTelepon) return;
        StartCoroutine(ProsesCutsceneTelepon());
    }

    IEnumerator ProsesCutsceneTelepon()
    {
        sedangBisaDiangkat = false;
        sudahSelesaiTelepon = true;

        if (deringCoroutine != null)
        {
            StopCoroutine(deringCoroutine);
        }

        if (audioSourceTelepon != null)
        {
            audioSourceTelepon.Stop();
        }

        // 1. Kunci pergerakan player
        if (scriptPlayer != null)
        {
            scriptPlayer.sedangCutscene = true;
            scriptPlayer.controller.enabled = false;
        }

        // 2. Mainkan suara angkat telepon
        if (audioSourceTelepon != null && sfxAngkatTelepon != null)
        {
            audioSourceTelepon.PlayOneShot(sfxAngkatTelepon);
        }

        // ANIMASI SMOOTH: Gagang melayang naik menuju telinga kiri player
        if (gagangDiMeja != null && playerCamera != null)
        {
            Vector3 posMulai = gagangDiMeja.transform.position;
            Quaternion rotMulai = gagangDiMeja.transform.rotation;

            float t = 0f;
            while (t < durasiAngkatGagang)
            {
                t += Time.deltaTime;
                float kurva = Mathf.SmoothStep(0f, 1f, t / durasiAngkatGagang);
                Vector3 targetPosKuping = playerCamera.TransformPoint(offsetKupingKiri);
                Quaternion targetRotKuping = playerCamera.rotation * Quaternion.Euler(rotasiOffsetKuping);

                gagangDiMeja.transform.position = Vector3.Lerp(posMulai, targetPosKuping, kurva);
                gagangDiMeja.transform.rotation = Quaternion.Slerp(rotMulai, targetRotKuping, kurva);
                yield return null;
            }

            // Tempelkan sementara sebagai child kamera player selama menelepon
            gagangDiMeja.transform.SetParent(playerCamera);
            gagangDiMeja.transform.localPosition = offsetKupingKiri;
            gagangDiMeja.transform.localRotation = Quaternion.Euler(rotasiOffsetKuping);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Suara telepon kresek-kresek (zzzt zztt tidak jelas)
        if (audioSourceTelepon != null && sfxZzttKresek != null)
        {
            audioSourceTelepon.clip = sfxZzttKresek;
            audioSourceTelepon.loop = true;
            audioSourceTelepon.Play();
        }

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Halo...?)", 2.5f);
        }

        yield return new WaitForSeconds(3.0f);

        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(......)", 2.5f);
        }

        yield return new WaitForSeconds(3.0f);

        // 4. Suara barang jatuh dari arah belakang: PRRRAAANGGG!!
        if (audioSourceBelakang != null && sfxBarangJatuhPrang != null)
        {
            audioSourceBelakang.PlayOneShot(sfxBarangJatuhPrang);
        }
        else if (audioSourceTelepon != null && sfxBarangJatuhPrang != null)
        {
            audioSourceTelepon.PlayOneShot(sfxBarangJatuhPrang);
        }

        // Hentikan suara kresek telepon seketika
        if (audioSourceTelepon != null)
        {
            audioSourceTelepon.Stop();
            audioSourceTelepon.loop = false;
        }

        yield return new WaitForSeconds(0.4f);

        // 5. Player spontan menutup telepon: Animasi gagang kembali ke dudukan meja
        if (audioSourceTelepon != null && sfxTutupTelepon != null)
        {
            audioSourceTelepon.PlayOneShot(sfxTutupTelepon);
        }

        if (gagangDiMeja != null)
        {
            gagangDiMeja.transform.SetParent(parentAsliGagang);

            Vector3 posDariKuping = gagangDiMeja.transform.position;
            Quaternion rotDariKuping = gagangDiMeja.transform.rotation;

            float tBalik = 0f;
            while (tBalik < durasiKembalikanGagang)
            {
                tBalik += Time.deltaTime;
                float kurvaBalik = Mathf.SmoothStep(0f, 1f, tBalik / durasiKembalikanGagang);
                gagangDiMeja.transform.position = Vector3.Lerp(posDariKuping, posisiAsliMeja, kurvaBalik);
                gagangDiMeja.transform.rotation = Quaternion.Slerp(rotDariKuping, rotasiAsliMeja, kurvaBalik);
                yield return null;
            }

            gagangDiMeja.transform.position = posisiAsliMeja;
            gagangDiMeja.transform.rotation = rotasiAsliMeja;
        }

        // 6. Player kaget dan otomatis menoleh ke arah belakang
        if (targetArahBelakang != null && playerCamera != null)
        {
            yield return StartCoroutine(LookAtTargetSmoothly(targetArahBelakang, durasiMenolehBelakang));
        }

        yield return new WaitForSeconds(0.5f);

        // Subtitle kaget & rasa ingin tahu
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.TampilkanSubtitle("(Suara apa itu tadi di belakang...?! Aku harus mengeceknya!)", 3.5f);
        }

        // 7. Update Quest
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuest("Periksa suara di belakang", "Selidiki benda yang terjatuh di lorong");
        }

        // 8. Kembalikan kontrol player
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
}
