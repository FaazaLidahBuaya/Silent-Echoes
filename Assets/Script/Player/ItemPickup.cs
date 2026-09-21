using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public string namaItem;
    public Sprite logoUI;
    
    [Header("Audio")]
    public AudioClip sfxAmbil; // Suara unik untuk barang ini

    public virtual void AmbilItem()
    {
        if (InventoryManager.Instance != null)
        {
            // Kirim GameObject dan SFX ini ke InventoryManager
            InventoryManager.Instance.TambahItem(namaItem, logoUI, gameObject, sfxAmbil);
            
            // Sembunyikan objek dari dunia game
            gameObject.SetActive(false); 
        }
    }
}