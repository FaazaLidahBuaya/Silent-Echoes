using UnityEngine;
using UnityEditor;

public class TerrainFlattenBordersWindow : EditorWindow
{
    private Terrain targetTerrain;
    [Range(0.01f, 0.45f)]
    private float borderPercent = 0.15f; // Lebar persentase pinggiran bukit yang ingin diratakan (15%)
    private float flatHeightNormalized = 0f; // Ketinggian target (0 = paling dasar rata)
    private bool blendSmoothly = true;

    [MenuItem("Tools/Silent Echoes/Flatten Terrain Borders")]
    public static void ShowWindow()
    {
        GetWindow<TerrainFlattenBordersWindow>("Flatten Terrain Borders");
    }

    private void OnEnable()
    {
        if (targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Border Flattener", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tool ini akan meratakan bukit-bukit di pinggiran Terrain menjadi datar kembali sesuai tinggi target.", MessageType.Info);

        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        borderPercent = EditorGUILayout.Slider("Lebar Border (%)", borderPercent, 0.01f, 0.45f);
        flatHeightNormalized = EditorGUILayout.FloatField("Target Height (0 = Datar Bawah)", flatHeightNormalized);
        blendSmoothly = EditorGUILayout.Toggle("Haluskan Transisi (Smooth Blend)", blendSmoothly);

        GUILayout.Space(10);

        if (targetTerrain == null)
        {
            EditorGUILayout.HelpBox("Pilih Terrain terlebih dahulu!", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Ratakan Pinggiran Terrain Sekarang", GUILayout.Height(35)))
        {
            FlattenBorders();
        }

        if (GUILayout.Button("Ratakan SEMUA Terrain Menjadi Datar Total", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Konfirmasi", "Apakah Anda yakin ingin meratakan seluruh terrain menjadi datar total?", "Ya", "Batal"))
            {
                FlattenEntireTerrain();
            }
        }
    }

    private void FlattenBorders()
    {
        TerrainData tData = targetTerrain.terrainData;
        Undo.RegisterCompleteObjectUndo(tData, "Flatten Terrain Borders");

        int res = tData.heightmapResolution;
        float[,] heights = tData.GetHeights(0, 0, res, res);

        int borderSize = Mathf.RoundToInt(res * borderPercent);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int distFromEdge = Mathf.Min(Mathf.Min(x, res - 1 - x), Mathf.Min(y, res - 1 - y));

                if (distFromEdge < borderSize)
                {
                    if (blendSmoothly)
                    {
                        float t = (float)distFromEdge / borderSize; // 0 di ujung border, 1 di dalam
                        t = Mathf.SmoothStep(0f, 1f, t);
                        heights[y, x] = Mathf.Lerp(flatHeightNormalized, heights[y, x], t);
                    }
                    else
                    {
                        heights[y, x] = flatHeightNormalized;
                    }
                }
            }
        }

        tData.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(tData);
        Debug.Log("<color=green>[Terrain Tool]</color> Pinggiran Terrain berhasil diratakan!");
    }

    private void FlattenEntireTerrain()
    {
        TerrainData tData = targetTerrain.terrainData;
        Undo.RegisterCompleteObjectUndo(tData, "Flatten Entire Terrain");

        int res = tData.heightmapResolution;
        float[,] heights = new float[res, res];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                heights[y, x] = flatHeightNormalized;
            }
        }

        tData.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(tData);
        Debug.Log("<color=green>[Terrain Tool]</color> Seluruh Terrain berhasil diratakan total!");
    }
}
