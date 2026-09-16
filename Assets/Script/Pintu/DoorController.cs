using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Pengaturan Posisi Pintu")]
    public Transform engselPintu; 
    public Vector3 sudutBuka = new Vector3(0, 90, 0); 
    public float kecepatanBuka = 2f;
    private Quaternion rotasiTertutup;
    private Quaternion rotasiTerbuka;

    [Header("Pengaturan Audio")]
    public AudioSource audioSource;
    public AudioClip sfxBuka;
    public AudioClip sfxTutup;
    
    [Header("Pengaturan Kunci Pintu")]
    public bool isTerkunci = false; 
    public string namaKunciDibutuhkan = "Kunci Pintu Utama"; 
    public AudioClip sfxTerkunci;
    public AudioClip sfxBukaKunci; 

    [Header("Pengaturan Subtitle (Opsional)")]
    public bool tampilkanSubtitleGagal = true;
    [TextArea] public string teksGagal = "(Pintu ini terkunci. Sepertinya aku butuh kunci)";
    
    public bool tampilkanSubtitleBerhasil = true;
    [TextArea] public string teksBerhasil = "(Kunci berhasil digunakan)";
  
    private bool isMembukaAtauMenutup = false;
    public bool isOpen { get; private set; } = false;

    void Start()
    {
        if (engselPintu != null)
        {
            rotasiTertutup = engselPintu.localRotation;
            rotasiTerbuka = rotasiTertutup * Quaternion.Euler(sudutBuka);
        }
    }

    public void InteraksiPintu()
    {
        if (isMembukaAtauMenutup) return;

        if (isTerkunci)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.CekItem(namaKunciDibutuhkan))
            {
                // JIKA BERHASIL DIBUKA KUNCINYA
                isTerkunci = false;
                if (audioSource != null && sfxBukaKunci != null)
                    audioSource.PlayOneShot(sfxBukaKunci);
                
                // Munculkan subtitle sukses jika dicentang
                if (tampilkanSubtitleBerhasil && SubtitleManager.Instance != null)
                {
                    SubtitleManager.Instance.TampilkanSubtitle(teksBerhasil, 2f);
                }
            }
            else
            {
                // JIKA GAGAL DIBUKA (TIDAK PUNYA KUNCI)
                if (audioSource != null && sfxTerkunci != null)
                    audioSource.PlayOneShot(sfxTerkunci);
                
                // Munculkan subtitle gagal jika dicentang
                if (tampilkanSubtitleGagal && SubtitleManager.Instance != null)
                {
                    SubtitleManager.Instance.TampilkanSubtitle(teksGagal, 2f);
                }
                return; 
            }
        }

        // BUKA / TUTUP PINTU
        if (isOpen)
        {
            StartCoroutine(GerakkanPintu(rotasiTertutup, sfxTutup));
        }
        else
        {
            StartCoroutine(GerakkanPintu(rotasiTerbuka, sfxBuka));
        }
        
        isOpen = !isOpen;
    }

    IEnumerator GerakkanPintu(Quaternion targetRotasi, AudioClip sfx)
    {
        isMembukaAtauMenutup = true;
        if (audioSource != null && sfx != null) audioSource.PlayOneShot(sfx);

        Quaternion rotasiAwal = engselPintu.localRotation;
        float waktu = 0f;
        while (waktu < 1f)
        {
            waktu += Time.deltaTime * kecepatanBuka;
            engselPintu.localRotation = Quaternion.Slerp(rotasiAwal, targetRotasi, waktu);
            yield return null;
        }
        engselPintu.localRotation = targetRotasi;
        isMembukaAtauMenutup = false;
    }
}