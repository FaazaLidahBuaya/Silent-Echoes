#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Auto-run: spawns furniture into the room, then marks itself done.
/// Just save this file to Assets/Editor, go to Unity, let it compile, 
/// then run via: Tools > Spawn Furniture Sekarang
/// 
/// After running, Ctrl+Z works to undo everything.
/// </summary>
public class SpawnFurnitureNow : EditorWindow
{
    [MenuItem("Tools/Spawn Furniture Sekarang!")]
    public static void DoSpawn()
    {
        // Step 1: Find room center by scanning for a reference point
        // We'll ask the user to select any floor/object in the room first
        if (Selection.activeTransform == null)
        {
            // Try to find the room automatically by looking for known objects
            // If not found, prompt user
            EditorUtility.DisplayDialog("Pilih Objek Dulu",
                "Pilih salah satu objek di dalam ruangan (lantai, dinding, lampu) di Scene View atau Hierarchy, lalu jalankan lagi.",
                "OK");
            return;
        }

        Vector3 roomCenter = Selection.activeTransform.position;
        
        // Ask the user to confirm
        if (!EditorUtility.DisplayDialog("Konfirmasi Posisi",
            $"Furniture akan di-spawn di sekitar posisi:\n{roomCenter}\n\n" +
            "Lanjutkan?", "Ya, Spawn!", "Batal"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // Clean up old decoration
        GameObject old = GameObject.Find("Dekorasi_Ruangan");
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject root = new GameObject("Dekorasi_Ruangan");
        root.transform.position = roomCenter;
        Undo.RegisterCreatedObjectUndo(root, "Spawn Furniture");

        // ============ SPAWN HELPER ============
        System.Func<string, GameObject> load = (path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        System.Func<GameObject, Vector3> getBoundsSize = (prefab) =>
        {
            if (prefab == null) return Vector3.one;
            // Temporarily instantiate to get real bounds
            GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tmp.transform.position = Vector3.zero;
            tmp.transform.rotation = Quaternion.identity;
            tmp.transform.localScale = Vector3.one;
            Renderer[] rr = tmp.GetComponentsInChildren<Renderer>();
            if (rr.Length == 0) { Object.DestroyImmediate(tmp); return Vector3.one; }
            Bounds b = rr[0].bounds;
            for (int i = 1; i < rr.Length; i++) b.Encapsulate(rr[i].bounds);
            Object.DestroyImmediate(tmp);
            return b.size;
        };

        int spawnCount = 0;
        System.Action<GameObject, Vector3, float, Vector3, string> spawn = (prefab, localOffset, yRot, scale, name) =>
        {
            if (prefab == null) { Debug.LogWarning($"[SpawnFurniture] Prefab null for: {name}"); return; }
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (inst == null) inst = Object.Instantiate(prefab);
            inst.name = name;
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = localOffset;
            inst.transform.localRotation = Quaternion.Euler(0, yRot, 0);
            inst.transform.localScale = scale;
            Undo.RegisterCreatedObjectUndo(inst, "Spawn " + name);
            spawnCount++;
        };

        // ============ LOAD MODELS ============
        GameObject karpet = load("Assets/Asset/Furniture/Karpeto.fbx") ?? load("Assets/Asset/Furniture/Carpet.fbx");
        GameObject meja = load("Assets/Asset/Furniture/Mejatamu.fbx") ?? load("Assets/Asset/Furniture/Meja.fbx");
        GameObject sofa = load("Assets/Asset/Furniture/Sofa.fbx");
        GameObject kursi1 = load("Assets/Asset/Furniture/Kursi.fbx");
        GameObject kursi2 = load("Assets/Asset/Furniture/Kursi1.fbx");
        GameObject rak = load("Assets/Asset/Furniture/Rak1.fbx") ?? load("Assets/Asset/Furniture/Rak2.fbx");
        GameObject kabinet = load("Assets/Asset/Furniture/Kabinetrevisi.fbx") ?? load("Assets/Asset/Furniture/Kabinet.fbx");
        GameObject tanaman = load("Assets/Asset/Furniture/Tanaman.fbx");
        GameObject pot = load("Assets/Asset/Furniture/Pot.fbx");
        GameObject kipas = load("Assets/Asset/Furniture/Kipas angin.fbx");
        GameObject gelas = load("Assets/Asset/Furniture/Gelas/Gelas1.fbx");
        GameObject buku = load("Assets/Asset/Benda Kecil/Buku/Buku1.fbx");
        GameObject dokumen = load("Assets/Asset/Benda Kecil/Dokumen.fbx");
        GameObject kardus = load("Assets/Asset/Benda Kecil/Kardus/Kardus1.fbx");
        GameObject jam = load("Assets/Asset/Benda Kecil/Jam.fbx");
        GameObject lukisan1 = load("Assets/Asset/Furniture/Lukisan1.fbx");
        GameObject lukisan2 = load("Assets/Asset/Furniture/Lukisan2.fbx");
        GameObject lamputidur = load("Assets/Asset/Furniture/Lampu tidor.fbx");

        // ============ MEASURE ACTUAL SIZES ============
        // Log sizes for debugging
        string[] modelNames = { "Karpet", "Meja", "Sofa", "Kursi", "Rak", "Kabinet", "Kipas", "Lukisan1" };
        GameObject[] modelRefs = { karpet, meja, sofa, kursi1, rak, kabinet, kipas, lukisan1 };
        float[] modelWidths = new float[modelRefs.Length];

        for (int i = 0; i < modelRefs.Length; i++)
        {
            Vector3 s = getBoundsSize(modelRefs[i]);
            modelWidths[i] = Mathf.Max(s.x, s.z);
            Debug.Log($"[SpawnFurniture] {modelNames[i]}: {s.x:F2} x {s.y:F2} x {s.z:F2} (max horizontal: {modelWidths[i]:F2})");
        }

        // ============ CALCULATE SCALE FACTOR ============
        // Target room ~6x6 meters. If sofa is bigger than 2m, scale everything down proportionally.
        float sofaMaxDim = modelWidths[2]; // Sofa
        float targetSofaWidth = 1.8f; // Sofa should be ~1.8m wide
        float scaleFactor = 1f;
        
        if (sofaMaxDim > 0.1f)
        {
            scaleFactor = targetSofaWidth / sofaMaxDim;
            // Clamp to reasonable range
            scaleFactor = Mathf.Clamp(scaleFactor, 0.005f, 2f);
        }

        Debug.Log($"[SpawnFurniture] Sofa raw size: {sofaMaxDim:F2}m, scale factor: {scaleFactor:F4}");

        Vector3 sf = Vector3.one * scaleFactor;
        Vector3 sfSmall = Vector3.one * scaleFactor * 0.7f; // For small props

        // ============ LAYOUT ============
        // All positions relative to room center (0,0,0 = center of floor)
        // Room assumed ~6x6m, furniture scaled to fit
        //
        //        +Z (back wall)
        //     ┌──────────────────────┐
        //     │ Rak         Kabinet  │
        //     │       Sofa           │
        //     │                      │
        // -X  │ Kursi1  Meja  Kursi2 │ +X  
        //     │      (Karpet)        │
        //     │ Kardus       Kipas   │
        //     │ Tanaman        Pot   │
        //     └──────────────────────┘
        //        -Z (front/door)

        // 1. KARPET - center floor
        spawn(karpet, new Vector3(0, 0.01f, 0), 0, sf, "Karpet_Tengah");

        // 2. MEJA TAMU - center, on karpet
        spawn(meja, new Vector3(0, 0.02f, 0), 0, sf, "Meja_Tengah");

        // 3. SOFA - behind meja, facing forward (-Z)
        spawn(sofa, new Vector3(0, 0, 2.2f), 180, sf, "Sofa_Tamu");

        // 4. KURSI KIRI & KANAN - flanking the meja
        spawn(kursi1, new Vector3(-2.2f, 0, 0), 90, sf, "Kursi_Kiri");
        spawn(kursi2, new Vector3(2.2f, 0, 0), -90, sf, "Kursi_Kanan");

        // 5. RAK BUKU - back-left against wall
        spawn(rak, new Vector3(-2.5f, 0, 2.6f), 0, sf, "Rak_Buku");

        // 6. KABINET - back-right against wall
        spawn(kabinet, new Vector3(2.5f, 0, 2.6f), 180, sf, "Kabinet_Sudut");

        // 7. TANAMAN - front-left corner
        spawn(tanaman, new Vector3(-2.5f, 0, -2.5f), 0, sf, "Tanaman_Sudut");

        // 8. POT - front-right corner
        spawn(pot, new Vector3(2.5f, 0, -2.5f), 0, sf, "Pot_Sudut");

        // 9. KIPAS ANGIN - right side, between kursi and front
        spawn(kipas, new Vector3(2.0f, 0, -1.5f), -30, sf, "Kipas_Angin");

        // 10. GELAS on meja (slightly above table)
        float mejaH = getBoundsSize(meja).y * scaleFactor;
        spawn(gelas, new Vector3(0.25f, mejaH + 0.02f, 0.1f), 15, sfSmall, "Gelas_Meja");

        // 11. BUKU on meja
        spawn(buku, new Vector3(-0.25f, mejaH + 0.02f, -0.1f), 25, sfSmall, "Buku_Meja");

        // 12. DOKUMEN on meja
        spawn(dokumen, new Vector3(0.05f, mejaH + 0.02f, -0.2f), -8, sfSmall, "Dokumen_Meja");

        // 13. JAM on kabinet
        float kabinetH = getBoundsSize(kabinet).y * scaleFactor;
        spawn(jam, new Vector3(2.5f, kabinetH + 0.02f, 2.6f), 0, sfSmall, "Jam_Kabinet");

        // 14. KARDUS - front-left near tanaman
        spawn(kardus, new Vector3(-2.0f, 0, -1.8f), 12, sf * 0.9f, "Kardus_1");
        spawn(kardus, new Vector3(-1.7f, 0, -2.0f), 35, sf * 0.75f, "Kardus_2");

        // 15. LUKISAN - on walls (high up)
        spawn(lukisan1, new Vector3(0, 2.0f, 2.9f), 0, sf, "Lukisan_Belakang");
        spawn(lukisan2, new Vector3(-2.9f, 2.0f, 0.5f), 90, sf, "Lukisan_Kiri");

        // 16. LAMPU TIDUR on kabinet
        if (lamputidur != null)
            spawn(lamputidur, new Vector3(2.3f, kabinetH + 0.02f, 2.4f), -20, sfSmall, "Lampu_Tidur");

        Selection.activeGameObject = root;
        Undo.CollapseUndoOperations(undoGroup);

        // Mark scene dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Selesai! 🎉",
            $"Berhasil spawn {spawnCount} furniture!\n\n" +
            $"Scale factor: {scaleFactor:F3}x\n" +
            $"Posisi pusat: {roomCenter}\n\n" +
            "• Semua ada di bawah 'Dekorasi_Ruangan'\n" +
            "• Ctrl+Z untuk undo\n" +
            "• Geser/putar manual kalau perlu",
            "OK");
    }
}
#endif
