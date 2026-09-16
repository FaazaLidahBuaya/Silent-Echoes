using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour
{
    public enum SurfaceType
    {
        Rumput,
        Indoor
    }

    [Header("Referensi Controller")]
    public CharacterController controller;

    [Header("Status Permukaan Saat Ini")]
    [Tooltip("Permukaan default jika di luar trigger adalah Rumput")]
    public SurfaceType currentSurface = SurfaceType.Rumput;

    [Header("Audio Footstep Rumput")]
    public AudioClip langkahKiriRumput;
    public AudioClip langkahKananRumput;

    [Header("Audio Footstep Indoor")]
    public AudioClip langkahKiriIndoor;
    public AudioClip langkahKananIndoor;

    [Header("Pengaturan Interval Langkah")]
    [Tooltip("Jeda waktu antar langkah saat jalan santai (detik)")]
    public float intervalJalan = 0.5f;
    [Tooltip("Jeda waktu antar langkah saat lari cepat (detik)")]
    public float intervalLari = 0.32f;

    [Header("Volume & Variasi Suara")]
    [Range(0f, 1f)]
    public float volumeLangkah = 0.8f;
    [Tooltip("Variasi pitch acak sedikit agar suara langkah terdengar organik dan tidak kaku")]
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;

    private AudioSource audioSource;
    private float stepTimer = 0f;
    private bool isLeftFoot = true; // Bergantian kaki kiri dan kanan
    private int indoorTriggerCount = 0; // Menghitung trigger indoor yang sedang bersentuhan
    private Vector3 lastPosition;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Hitung jarak tempuh horizontal frame ini
        Vector3 currentPos = transform.position;
        Vector3 deltaMove = currentPos - lastPosition;
        deltaMove.y = 0f;
        float speed = deltaMove.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = currentPos;

        // Cek input WASD pemain
        bool isMovingInput = false;
        bool isSprinting = false;

        if (Keyboard.current != null)
        {
            isMovingInput = Keyboard.current.wKey.isPressed ||
                            Keyboard.current.aKey.isPressed ||
                            Keyboard.current.sKey.isPressed ||
                            Keyboard.current.dKey.isPressed;

            isSprinting = Keyboard.current.leftShiftKey.isPressed;
        }

        // Cek apakah player menyentuh tanah (menggunakan controller.isGrounded atau raycast sedikit ke bawah)
        bool isGrounded = true;
        if (controller != null)
        {
            isGrounded = controller.isGrounded || Physics.Raycast(transform.position, Vector3.down, 1.3f);
        }

        // Mainkan suara jika player sedang bergerak dan berada di tanah
        bool isMoving = isMovingInput && (speed > 0.05f || controller == null || controller.isGrounded);

        if (isGrounded && isMoving)
        {
            float currentInterval = isSprinting ? intervalLari : intervalJalan;

            stepTimer += Time.deltaTime;
            if (stepTimer >= currentInterval)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            // Set agar langkah pertama cepat terpicu begitu mulai jalan
            stepTimer = intervalJalan * 0.85f;
        }
    }

    private void PlayFootstep()
    {
        AudioClip clipToPlay = null;

        if (currentSurface == SurfaceType.Rumput)
        {
            clipToPlay = isLeftFoot ? langkahKiriRumput : langkahKananRumput;
        }
        else if (currentSurface == SurfaceType.Indoor)
        {
            clipToPlay = isLeftFoot ? langkahKiriIndoor : langkahKananIndoor;
        }

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clipToPlay, volumeLangkah);
        }

        // Ganti giliran kaki berikutnya
        isLeftFoot = !isLeftFoot;
    }

    public void SetIndoorZone(bool isInside)
    {
        if (isInside)
        {
            indoorTriggerCount++;
        }
        else
        {
            indoorTriggerCount = Mathf.Max(0, indoorTriggerCount - 1);
        }

        currentSurface = (indoorTriggerCount > 0) ? SurfaceType.Indoor : SurfaceType.Rumput;
    }
}
