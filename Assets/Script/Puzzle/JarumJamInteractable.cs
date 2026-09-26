using UnityEngine;

public enum TipeJarum
{
    JarumJam,
    JarumMenit
}

public class JarumJamInteractable : MonoBehaviour
{
    [Header("Pengaturan Jarum")]
    public TipeJarum tipeJarum = TipeJarum.JarumJam;
    public JamDindingPuzzle jamDindingParent;

    void Awake()
    {
        if (jamDindingParent == null)
        {
            jamDindingParent = GetComponentInParent<JamDindingPuzzle>();
        }

        InisialisasiCollider();
    }

    /// <summary>
    /// Memastikan objek jarum memiliki Collider agar raycast PlayerInteract dapat mendeteksinya.
    /// </summary>
    public void InisialisasiCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                Bounds b = mf.sharedMesh.bounds;
                Vector3 size = b.size;
                // Beri ketebalan minimum agar mudah dibidik crosshair pemain
                if (size.x < 0.05f) size.x = 0.05f;
                if (size.y < 0.05f) size.y = 0.05f;
                if (size.z < 0.05f) size.z = 0.05f;
                box.center = b.center;
                box.size = size;
            }
            else
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.08f, 0.25f, 0.08f);
            }
        }
    }

    /// <summary>
    /// Teks prompt interaksi yang akan tampil di crosshair
    /// </summary>
    public string DapatkanPrompt()
    {
        if (jamDindingParent == null) return "[E] Putar Jarum";

        if (jamDindingParent.ApakahTerkunci())
        {
            return "[Jam Terkunci]";
        }

        if (tipeJarum == TipeJarum.JarumJam)
        {
            return "[E] Putar Jarum Jam";
        }
        else
        {
            return "[E] Putar Jarum Menit";
        }
    }

    /// <summary>
    /// Menjalankan rotasi jarum saat pemain menekan tombol E
    /// </summary>
    public void Interaksi()
    {
        if (jamDindingParent != null)
        {
            jamDindingParent.PutarJarum(tipeJarum);
        }
    }
}
