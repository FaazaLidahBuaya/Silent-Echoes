using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupPuzzleJamEditor
{
    [InitializeOnLoadMethod]
    private static void AutoSetupIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (Object.FindAnyObjectByType<PuzzleJamManager>() == null)
            {
                SetupPuzzleJam();
            }
        };
    }

    [MenuItem("Tools/Setup Puzzle Jam Dinding")]
    public static void SetupPuzzleJam()
    {
        Debug.Log("<color=yellow>[SetupPuzzleJam]</color> Memulai setup otomatis Puzzle Jam Dinding...");

        // 1. Cari semua objek JamPutar di scene
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        List<GameObject> listJamObjects = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("JamPutar") || obj.name.Contains("JamPutar"))
            {
                listJamObjects.Add(obj);
            }
        }

        // Urutkan berdasarkan nama agar urutan JamPutar, JamPutar (1), dst rapi
        listJamObjects.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        Debug.Log($"[SetupPuzzleJam] Ditemukan {listJamObjects.Count} jam dinding di scene.");

        // Target jam dan menit default untuk 5 jam
        (int jam, int menit, int awalJam, int awalMenit)[] presets = new (int, int, int, int)[]
        {
            (3, 30, 12, 0),  // Jam 1: Target 15:30 (3:30) - Sesuai instruksi user
            (8, 15, 1, 45),  // Jam 2: Target 08:15
            (12, 45, 6, 20), // Jam 3: Target 12:45
            (6, 0, 11, 30),  // Jam 4: Target 06:00
            (7, 20, 2, 50)   // Jam 5: Target 07:20
        };

        List<JamDindingPuzzle> daftarKomponenJam = new List<JamDindingPuzzle>();

        for (int i = 0; i < listJamObjects.Count; i++)
        {
            GameObject jamObj = listJamObjects[i];
            JamDindingPuzzle puzzleComp = jamObj.GetComponent<JamDindingPuzzle>();
            if (puzzleComp == null)
            {
                puzzleComp = Undo.AddComponent<JamDindingPuzzle>(jamObj);
            }

            puzzleComp.namaJam = $"Jam Dinding {i + 1} ({jamObj.name})";

            if (i < presets.Length)
            {
                puzzleComp.targetJam = presets[i].jam;
                puzzleComp.targetMenit = presets[i].menit;
                puzzleComp.jamSekarang = presets[i].awalJam;
                puzzleComp.menitSekarang = presets[i].awalMenit;
            }

            // Inisialisasi jarum dan collider
            puzzleComp.InisialisasiKomponen();
            puzzleComp.TerapkanRotasiVisual(true);

            daftarKomponenJam.Add(puzzleComp);
            EditorUtility.SetDirty(jamObj);
        }

        // 2. Setup Manager Puzzle Jam di Scene
        PuzzleJamManager manager = Object.FindAnyObjectByType<PuzzleJamManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("[MANAGER] Puzzle Jam Dinding");
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Puzzle Jam Manager");
            manager = managerObj.AddComponent<PuzzleJamManager>();
        }

        manager.daftarJam = daftarKomponenJam;

        // Cari objek Peti dan Kunci (otomatis mencari berdasarkan nama standar)
        GameObject peti = GameObject.Find("Chest");
        if (peti != null)
        {
            Transform tutup = peti.transform.Find("Lid");
            if (tutup == null) tutup = peti.transform.Find("Tutup");
            if (tutup != null) manager.tutupChest = tutup;
            else manager.tutupChest = peti.transform;
        }

        GameObject kunci = GameObject.Find("Kunci Kamar Anak");
        if (kunci != null)
        {
            manager.kunciKamarAnak = kunci;
        }

        // Hubungkan SFX Buka Peti
        AudioClip sfxChest = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Item/door_open.mp3");
        manager.sfxChestBuka = sfxChest;

        manager.InisialisasiManager();
        EditorUtility.SetDirty(manager.gameObject);

        // Mark scene dirty agar tersimpan
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("<color=green>[SetupPuzzleJam]</color> Setup Puzzle Jam Dinding SELESAI SUKSES! 5 jam dinding dan Manager siap digunakan.");
    }
}
