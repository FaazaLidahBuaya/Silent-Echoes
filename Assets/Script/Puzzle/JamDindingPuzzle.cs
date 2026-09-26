using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class JamDindingPuzzle : MonoBehaviour
{
    public enum SumbuRotasi
    {
        SumbuZ,
        SumbuY,
        SumbuX
    }

    [Header("Identitas Jam")]
    public string namaJam = "Jam Dinding 1";

    [Header("Target Waktu Teka-Teki")]
    [Tooltip("Target jam yang benar (1 - 12, atau format 24 jam seperti 15)")]
    [Range(0, 23)]
    public int targetJam = 3;

    [Tooltip("Target menit yang benar (0 - 55)")]
    [Range(0, 59)]
    public int targetMenit = 30;

    [Header("Waktu Saat Ini (Bisa diatur untuk posisi awal)")]
    [Range(1, 12)]
    public int jamSekarang = 12;

    [Range(0, 59)]
    public int menitSekarang = 0;

    [Tooltip("Kelipatan putaran jarum menit per klik (default 5 menit = 30 derajat)")]
    public int stepMenit = 5;

    [Tooltip("Apakah jarum jam bergeser halus mengikuti jarum menit")]
    public bool jarumJamIkutMenit = false;

    [Header("Referensi Jarum (Otomatis jika kosong)")]
    public Transform jarumJam;    // Otomatis mencari jarumKecil
    public Transform jarumMenit;  // Otomatis mencari jarumBesar

    [Header("Pengaturan Sumbu Rotasi Model 3D")]
    public SumbuRotasi sumbuRotasi = SumbuRotasi.SumbuZ;
    [Tooltip("Centang jika jarum berputar berlawanan arah")]
    public bool arahTerbalik = false;
    public float durasiPutar = 0.15f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxPutarJarum;
    public AudioClip sfxJamTepat;
    [Range(0f, 1f)] public float volumeAudio = 0.8f;

    [Header("Status")]
    public bool sudahTepat = false;
    public bool terkunci = false;

    [Header("Event")]
    public UnityEvent onWaktuTepat;
    public UnityEvent onWaktuBerubah;

    private Quaternion rotasiDasarJam;
    private Quaternion rotasiDasarMenit;
    private Coroutine coroutineAnimasiJam;
    private Coroutine coroutineAnimasiMenit;
    private bool sudahInisialisasi = false;

    void Awake()
    {
        InisialisasiKomponen();
    }

    void Start()
    {
        // Terapkan rotasi visual sesuai posisi awal
        TerapkanRotasiVisual(true);
        // Cek apakah langsung tepat (misal saat load/preset)
        sudahTepat = CekApakahTepat();
    }

    public void InisialisasiKomponen()
    {
        if (sudahInisialisasi) return;

        // 1. Cari jarum otomatis jika belum dihubungkan di Inspector
        if (jarumJam == null)
        {
            jarumJam = TemukanTransformJarum("jarumKecil", "kecil", "hour", "jam");
        }
        if (jarumMenit == null)
        {
            jarumMenit = TemukanTransformJarum("jarumBesar", "besar", "minute", "menit");
        }

        // Debug: tampilkan nama node yang terdeteksi agar mudah diagnosa
        Debug.Log($"<color=yellow>[JamDinding:{namaJam}]</color> JarumJam → <b>{(jarumJam != null ? jarumJam.name : "TIDAK DITEMUKAN")}</b> | JarumMenit → <b>{(jarumMenit != null ? jarumMenit.name : "TIDAK DITEMUKAN")}</b>");

        // 2. Simpan rotasi dasar
        if (jarumJam != null)
        {
            rotasiDasarJam = jarumJam.localRotation;
            PasangInteractable(jarumJam, TipeJarum.JarumJam);
        }
        if (jarumMenit != null)
        {
            rotasiDasarMenit = jarumMenit.localRotation;
            PasangInteractable(jarumMenit, TipeJarum.JarumMenit);
        }

        // 3. AudioSource setup
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D Audio
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 10f;
            }
        }

        // 4. Editor fallback untuk SFX putar jarum
#if UNITY_EDITOR
        if (sfxPutarJarum == null)
        {
            sfxPutarJarum = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/Wheel.mp3");
        }
        if (sfxJamTepat == null)
        {
            sfxJamTepat = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/Key pick.mp3");
        }
#endif

        sudahInisialisasi = true;
    }

    private Transform TemukanTransformJarum(params string[] kataKunci)
    {
        Transform[] anak = GetComponentsInChildren<Transform>(true);

        // Prioritas 1: Node yang namanya cocok DAN punya MeshFilter (jarum 3D yang sebenarnya)
        foreach (Transform t in anak)
        {
            if (t == transform) continue;
            string namaKecil = t.name.ToLower();
            foreach (string kata in kataKunci)
            {
                if (namaKecil.Contains(kata.ToLower()) && t.GetComponent<MeshFilter>() != null)
                {
                    return t;
                }
            }
        }

        // Prioritas 2: Node yang namanya cocok (meski tanpa mesh, untuk rig/bone)
        foreach (Transform t in anak)
        {
            if (t == transform) continue;
            string namaKecil = t.name.ToLower();
            foreach (string kata in kataKunci)
            {
                if (namaKecil.Contains(kata.ToLower()))
                {
                    return t;
                }
            }
        }

        return null;
    }

    private void PasangInteractable(Transform tJarum, TipeJarum tipe)
    {
        JarumJamInteractable interactable = tJarum.GetComponent<JarumJamInteractable>();
        if (interactable == null)
        {
            interactable = tJarum.gameObject.AddComponent<JarumJamInteractable>();
        }
        interactable.tipeJarum = tipe;
        interactable.jamDindingParent = this;
        interactable.InisialisasiCollider();
    }

    public bool ApakahTerkunci()
    {
        if (terkunci) return true;
        if (PuzzleJamManager.Instance != null && PuzzleJamManager.Instance.puzzleSelesai) return true;
        return false;
    }

    /// <summary>
    /// Dipanggil saat pemain menekan 'E' pada salah satu jarum
    /// </summary>
    public void PutarJarum(TipeJarum tipe)
    {
        if (ApakahTerkunci()) return;

        if (tipe == TipeJarum.JarumJam)
        {
            PutarJarumJamInternal();
        }
        else
        {
            PutarJarumMenitInternal();
        }

        // Mainkan SFX putar
        if (audioSource != null && sfxPutarJarum != null)
        {
            audioSource.PlayOneShot(sfxPutarJarum, volumeAudio);
        }

        onWaktuBerubah?.Invoke();

        // Cek apakah waktu saat ini cocok dengan target
        bool tepatSekarang = CekApakahTepat();
        if (tepatSekarang && !sudahTepat)
        {
            sudahTepat = true;
            Debug.Log($"<color=green>[JamDindingPuzzle]</color> {namaJam} TEPAT! ({DapatkanTeksWaktu()})");

            if (audioSource != null && sfxJamTepat != null)
            {
                audioSource.PlayOneShot(sfxJamTepat, volumeAudio);
            }

            onWaktuTepat?.Invoke();
        }
        else if (!tepatSekarang && sudahTepat)
        {
            sudahTepat = false;
        }

        // Laporkan perubahan ke Manager
        if (PuzzleJamManager.Instance != null)
        {
            PuzzleJamManager.Instance.CekStatusPuzzle();
        }
    }

    private void PutarJarumJamInternal()
    {
        jamSekarang = (jamSekarang % 12) + 1; // 1 -> 2 -> ... -> 12 -> 1
        AnimasikanRotasiJarum(TipeJarum.JarumJam);
    }

    private void PutarJarumMenitInternal()
    {
        menitSekarang = (menitSekarang + stepMenit) % 60; // 0 -> 5 -> 10 -> ... -> 55 -> 0
        AnimasikanRotasiJarum(TipeJarum.JarumMenit);

        // Jika jarum jam ikut terpengaruh posisi menit, perbarui juga jarum jam
        if (jarumJamIkutMenit)
        {
            AnimasikanRotasiJarum(TipeJarum.JarumJam);
        }
    }

    public bool CekApakahTepat()
    {
        int targetNormalized = targetJam % 12;
        int currentNormalized = jamSekarang % 12;
        return (targetNormalized == currentNormalized) && (targetMenit == menitSekarang);
    }

    public void TerapkanRotasiVisual(bool instan)
    {
        if (jarumJam != null)
        {
            Quaternion targetRotJam = HitungRotasiTarget(TipeJarum.JarumJam);
            if (instan)
            {
                jarumJam.localRotation = targetRotJam;
            }
            else
            {
                if (coroutineAnimasiJam != null) StopCoroutine(coroutineAnimasiJam);
                coroutineAnimasiJam = StartCoroutine(ProsesAnimasiRotasi(jarumJam, targetRotJam));
            }
        }

        if (jarumMenit != null)
        {
            Quaternion targetRotMenit = HitungRotasiTarget(TipeJarum.JarumMenit);
            if (instan)
            {
                jarumMenit.localRotation = targetRotMenit;
            }
            else
            {
                if (coroutineAnimasiMenit != null) StopCoroutine(coroutineAnimasiMenit);
                coroutineAnimasiMenit = StartCoroutine(ProsesAnimasiRotasi(jarumMenit, targetRotMenit));
            }
        }
    }

    private void AnimasikanRotasiJarum(TipeJarum tipe)
    {
        if (tipe == TipeJarum.JarumJam && jarumJam != null)
        {
            Quaternion rotTarget = HitungRotasiTarget(TipeJarum.JarumJam);
            if (coroutineAnimasiJam != null) StopCoroutine(coroutineAnimasiJam);
            coroutineAnimasiJam = StartCoroutine(ProsesAnimasiRotasi(jarumJam, rotTarget));
        }
        else if (tipe == TipeJarum.JarumMenit && jarumMenit != null)
        {
            Quaternion rotTarget = HitungRotasiTarget(TipeJarum.JarumMenit);
            if (coroutineAnimasiMenit != null) StopCoroutine(coroutineAnimasiMenit);
            coroutineAnimasiMenit = StartCoroutine(ProsesAnimasiRotasi(jarumMenit, rotTarget));
        }
    }

    private Quaternion HitungRotasiTarget(TipeJarum tipe)
    {
        Vector3 axis = DapatkanVektorSumbu();
        float sudut = 0f;

        if (tipe == TipeJarum.JarumJam)
        {
            // 12 jam = 360 derajat (30 derajat per jam)
            sudut = (jamSekarang % 12) * 30f;
            if (jarumJamIkutMenit)
            {
                sudut += (menitSekarang / 60f) * 30f;
            }
            return rotasiDasarJam * Quaternion.AngleAxis(sudut, axis);
        }
        else
        {
            // 60 menit = 360 derajat (6 derajat per menit)
            sudut = (menitSekarang / 60f) * 360f;
            return rotasiDasarMenit * Quaternion.AngleAxis(sudut, axis);
        }
    }

    private Vector3 DapatkanVektorSumbu()
    {
        Vector3 v = Vector3.forward;
        switch (sumbuRotasi)
        {
            case SumbuRotasi.SumbuX: v = Vector3.right; break;
            case SumbuRotasi.SumbuY: v = Vector3.up; break;
            case SumbuRotasi.SumbuZ: v = Vector3.forward; break;
        }
        if (arahTerbalik) v = -v;
        return v;
    }

    private IEnumerator ProsesAnimasiRotasi(Transform targetTransform, Quaternion rotasiTujuan)
    {
        Quaternion awal = targetTransform.localRotation;
        float elapsed = 0f;

        while (elapsed < durasiPutar)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / durasiPutar);
            targetTransform.localRotation = Quaternion.Slerp(awal, rotasiTujuan, t);
            yield return null;
        }

        targetTransform.localRotation = rotasiTujuan;
    }

    public string DapatkanTeksWaktu()
    {
        return $"{jamSekarang:00}:{menitSekarang:00}";
    }

    public string DapatkanTeksJam()
    {
        return $"{jamSekarang:00}";
    }

    public string DapatkanTeksMenit()
    {
        return $"{menitSekarang:00}";
    }

    public string DapatkanTeksTarget()
    {
        return $"{(targetJam % 12 == 0 ? 12 : targetJam % 12):00}:{targetMenit:00}";
    }

    [ContextMenu("Tes Putar Jarum Jam")]
    public void ContextPutarJam()
    {
        InisialisasiKomponen();
        PutarJarum(TipeJarum.JarumJam);
    }

    [ContextMenu("Tes Putar Jarum Menit")]
    public void ContextPutarMenit()
    {
        InisialisasiKomponen();
        PutarJarum(TipeJarum.JarumMenit);
    }

    [ContextMenu("Setel Tepat ke Target")]
    public void ContextSetelKeTarget()
    {
        InisialisasiKomponen();
        jamSekarang = targetJam % 12 == 0 ? 12 : targetJam % 12;
        menitSekarang = targetMenit;
        TerapkanRotasiVisual(true);
        sudahTepat = true;
        if (PuzzleJamManager.Instance != null)
        {
            PuzzleJamManager.Instance.CekStatusPuzzle();
        }
    }
}
