using UnityEngine;

[DefaultExecutionOrder(150)] // Berjalan setelah FaceCamera (agar efek gelombang dan ayunan tidak tertimpa)
public class FlamePhysicsSway : MonoBehaviour
{
    [Header("1. Efek Gelombang Api (Flame Wave Motion)")]
    public bool aktifkanEfekGelombang = true;
    [Tooltip("Kecepatan liukan gelombang lidah api (lebih santai dan anggun)")]
    public float frekuensiGelombang = 4.2f;
    [Tooltip("Kekuatan liukan gelombang (derajat)")]
    public float kekuatanGelombangRotasi = 2.8f;
    [Tooltip("Amplitudo gelombang mengembang-mengempis lidah api (Scale Wave)")]
    public float amplitudoGelombangSkala = 0.045f;
    [Tooltip("Amplitudo gelombang goyangan posisi kepala api")]
    public float amplitudoGelombangPosisi = 0.0006f;

    [Header("2. Respon Berjalan (WASD)")]
    [Tooltip("Ayunan api mengikuti irama langkah kaki saat jalan")]
    public float kekuatanGoyangLangkah = 6f;
    [Tooltip("Frekuensi irama langkah kaki")]
    public float frekuensiLangkah = 7.5f;
    [Tooltip("Kemiringan api tertiup angin saat melangkah")]
    public float kekuatanTerpaanAngin = 4.5f;
    [Tooltip("Batas sudut maksimal kemiringan api (derajat)")]
    public float batasMaksimalTilt = 22f;
    [Tooltip("Kecepatan kepegasan api kembali tegak saat diam")]
    public float kecepatanKembali = 6.5f;

    [Header("3. Respon Menoleh Kamera (Mouse Look)")]
    [Tooltip("Balikkan arah kemiringan saat menoleh jika terasa terbalik")]
    public bool balikArahMenoleh = false;
    [Tooltip("Seberapa kuat api tertinggal saat mouse menoleh cepat")]
    public float kekuatanTiltKamera = 10f;

    [Header("Referensi (Otomatis jika kosong)")]
    public CharacterController controllerPlayer;
    public Transform playerCamera;

    private Quaternion rotasiAwal;
    private Vector3 skalaAwal;
    private Vector3 posisiAwalLokal;
    private Vector3 rotasiKameraSebelumnyaEuler;
    private Vector3 posisiDuniaSebelumnya;

    private float sudutTiltKiriKanan = 0f;
    private float sudutTiltMajuMundur = 0f;
    private float iramaLangkah = 0f;
    private float seedAcak;

    void Start()
    {
        rotasiAwal = transform.localRotation;
        skalaAwal = transform.localScale;
        posisiAwalLokal = transform.localPosition;
        posisiDuniaSebelumnya = transform.position;
        seedAcak = Random.Range(0f, 100f);

        // Cari CharacterController otomatis
        if (controllerPlayer == null)
        {
            controllerPlayer = GetComponentInParent<CharacterController>();
            if (controllerPlayer == null)
            {
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) controllerPlayer = pc.controller;
            }
        }

        // Cari Kamera Player otomatis
        if (playerCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
            else
            {
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null && pc.playerCamera != null) playerCamera = pc.playerCamera;
            }
        }

        if (playerCamera != null)
        {
            rotasiKameraSebelumnyaEuler = playerCamera.eulerAngles;
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0.0001f) return;

        // =========================================================================
        // A. HITUNG KECEPATAN GERAK DUNIA & RELATIF TERHADAP KAMERA
        // =========================================================================
        Vector3 deltaPosisiDunia = (transform.position - posisiDuniaSebelumnya) / dt;
        posisiDuniaSebelumnya = transform.position;

        Vector3 kecepatanDunia = (controllerPlayer != null && controllerPlayer.velocity.sqrMagnitude > 0.01f)
            ? controllerPlayer.velocity
            : deltaPosisiDunia;

        // Proyeksikan kecepatan terhadap arah pandang kamera
        Vector3 kecKamera = (playerCamera != null)
            ? playerCamera.InverseTransformDirection(kecepatanDunia)
            : transform.InverseTransformDirection(kecepatanDunia);

        float speedMaju = kecKamera.z;
        float speedSamping = kecKamera.x;
        float speedHorizontal = new Vector2(speedMaju, speedSamping).magnitude;

        float targetTiltZ = 0f; // Roll pada pandangan player (Kiri/Kanan)
        float targetTiltX = 0f; // Pitch (Maju/Mundur)

        // =========================================================================
        // B. EFEK GELOMBANG MAJEMUK LIDAH API (FLAME WAVE MOTION)
        // =========================================================================
        float waveRot = 0f;
        float waveScaleY = 0f;
        float waveScaleX = 0f;
        float wavePosX = 0f;
        float wavePosY = 0f;

        if (aktifkanEfekGelombang)
        {
            // Kecepatan gelombang bertambah dinamis saat player melangkah (halus)
            float pengaliKecepatan = 1f + Mathf.Clamp01(speedHorizontal / 4f) * 0.35f;
            float t = Time.time * (frekuensiGelombang * pengaliKecepatan) + seedAcak;

            // 1. Gelombang rotasi majemuk (harmonic wave) yang menciptakan efek liukan lidah api
            waveRot = Mathf.Sin(t) * kekuatanGelombangRotasi
                    + Mathf.Sin(t * 2.2f + 0.9f) * (kekuatanGelombangRotasi * 0.45f)
                    + Mathf.Cos(t * 3.6f) * (kekuatanGelombangRotasi * 0.2f);

            // 2. Gelombang denyut jilatan lidah api (Scale Wave - memanjang & meramping)
            waveScaleY = (Mathf.Sin(t * 1.8f) * 0.7f + Mathf.Sin(t * 3.4f) * 0.3f) * amplitudoGelombangSkala;
            waveScaleX = -(Mathf.Cos(t * 1.4f) * 0.7f) * (amplitudoGelombangSkala * 0.6f);

            // 3. Gelombang getaran mikro posisi (posisi kepala api meliuk)
            wavePosX = Mathf.Sin(t * 1.3f) * amplitudoGelombangPosisi;
            wavePosY = Mathf.Abs(Mathf.Sin(t * 1.7f)) * (amplitudoGelombangPosisi * 0.7f);

            targetTiltZ += waveRot;
        }

        // =========================================================================
        // C. RESPON SAAT BERJALAN (WASD) -> AYUNAN LANGKAH & TERPAAN ANGIN
        // =========================================================================
        if (speedHorizontal > 0.15f)
        {
            // Irama ayunan langkah kaki (Footstep Sway)
            float faktorLari = (speedHorizontal > 5f) ? 1.4f : 1f;
            iramaLangkah += dt * (frekuensiLangkah * faktorLari);

            float goyangLangkah = Mathf.Sin(iramaLangkah) * kekuatanGoyangLangkah * Mathf.Clamp01(speedHorizontal / 4f);
            targetTiltZ += goyangLangkah;

            // Terpaan angin samping saat strafe (A/D)
            targetTiltZ += -speedSamping * (kekuatanTerpaanAngin * 0.5f);

            // Terpaan angin maju (W/S)
            targetTiltX += speedMaju * (kekuatanTerpaanAngin * 0.4f);

            // Turbulensi gelepar saat menembus angin (dibuat tenang dan natural)
            float turbulensiJalan = (Mathf.PerlinNoise(Time.time * 8f, seedAcak) - 0.5f) * 2f * (kekuatanTerpaanAngin * 0.25f);
            targetTiltZ += turbulensiJalan;
        }
        else
        {
            iramaLangkah = Mathf.Lerp(iramaLangkah, 0f, dt * 2f);
        }

        // =========================================================================
        // D. RESPON MENOLEHKAN KAMERA (MOUSE LOOK SWAY)
        // =========================================================================
        if (playerCamera != null)
        {
            Vector3 rotasiSekarangEuler = playerCamera.eulerAngles;

            float deltaYaw = Mathf.DeltaAngle(rotasiKameraSebelumnyaEuler.y, rotasiSekarangEuler.y) / dt;
            float deltaPitch = Mathf.DeltaAngle(rotasiKameraSebelumnyaEuler.x, rotasiSekarangEuler.x) / dt;

            float arah = balikArahMenoleh ? -1f : 1f;
            targetTiltZ += deltaYaw * (kekuatanTiltKamera * 0.035f) * arah;
            targetTiltX += deltaPitch * (kekuatanTiltKamera * 0.025f) * arah;

            rotasiKameraSebelumnyaEuler = rotasiSekarangEuler;
        }

        // Batasi sudut kemiringan maksimal
        targetTiltZ = Mathf.Clamp(targetTiltZ, -batasMaksimalTilt, batasMaksimalTilt);
        targetTiltX = Mathf.Clamp(targetTiltX, -batasMaksimalTilt, batasMaksimalTilt);

        // =========================================================================
        // E. INTERPOLASI PEGAS HALUS (SPRING DAMPENING)
        // =========================================================================
        sudutTiltKiriKanan = Mathf.Lerp(sudutTiltKiriKanan, targetTiltZ, dt * kecepatanKembali);
        sudutTiltMajuMundur = Mathf.Lerp(sudutTiltMajuMundur, targetTiltX, dt * kecepatanKembali);

        // Terapkan rotasi kemiringan pada api
        Quaternion rotasiKemiringan = Quaternion.Euler(sudutTiltMajuMundur, 0f, sudutTiltKiriKanan);
        transform.localRotation = rotasiAwal * rotasiKemiringan;

        // =========================================================================
        // F. TERAPKAN GELOMBANG SKALA & GELOMBANG POSISI
        // =========================================================================
        if (skalaAwal != Vector3.zero)
        {
            float faktorTarikGerak = 1f + Mathf.Clamp(speedHorizontal * 0.06f, 0f, 0.45f);
            float faktorKempis = 1f / Mathf.Sqrt(faktorTarikGerak);

            transform.localScale = new Vector3(
                skalaAwal.x * (faktorKempis + waveScaleX),
                skalaAwal.y * (faktorTarikGerak + waveScaleY),
                skalaAwal.z * (faktorKempis + waveScaleX)
            );
        }

        if (aktifkanEfekGelombang)
        {
            transform.localPosition = posisiAwalLokal + new Vector3(wavePosX, wavePosY, 0f);
        }
    }
}
