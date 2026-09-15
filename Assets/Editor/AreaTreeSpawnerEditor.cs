using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AreaTreeSpawner))]
public class AreaTreeSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Gambar Inspector bawaan
        DrawDefaultInspector();

        AreaTreeSpawner spawner = (AreaTreeSpawner)target;

        GUILayout.Space(15);
        GUILayout.Label("Tindakan Cepat (Actions)", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
        if (GUILayout.Button("🌲 GENERATE POHON SEKARANG", GUILayout.Height(38)))
        {
            spawner.GenerateTrees();
        }

        GUI.backgroundColor = new Color(0.95f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Hapus Semua Pohon (Clear)", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Konfirmasi", "Hapus semua pohon yang sudah di-generate oleh kotak ini?", "Ya, Hapus", "Batal"))
            {
                spawner.ClearTrees();
            }
        }

        GUI.backgroundColor = Color.white;
    }
}
