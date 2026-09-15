using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AreaTreeSpawner : MonoBehaviour
{
    [Header("Pengaturan Area Spawn (Box)")]
    [Tooltip("Ukuran kotak area spawn (Panjang X, Lebar Z, Tinggi Y)")]
    public Vector3 boxAreaSize = new Vector3(50f, 20f, 50f);

    [Header("Prefab / Model Pohon")]
    [Tooltip("Daftar model pohon yang akan di-spawn secara acak (bisa isi Pohon1, Pohon2, Pohon3, dll)")]
    public GameObject[] treePrefabs;

    [Header("Jumlah & Jarak")]
    [Range(1, 1000)]
    public int jumlahPohon = 30;
    [Tooltip("Jarak minimal antar pohon agar tidak saling tumpuk di titik yang sama")]
    public float minDistanceBetweenTrees = 3f;

    [Header("Variasi Ukuran & Rotasi (Biar Natural)")]
    [Tooltip("Model Pohon FBX memiliki skala asli ~100x")]
    public float minScale = 85f;
    public float maxScale = 120f;
    public bool randomYRotation = true;

    [Header("Penyesuaian Tanah (Snap to Ground)")]
    public bool snapToGround = true;
    public LayerMask groundLayer = ~0; // Default mendeteksi semua layer termasuk Terrain

    [Header("Optimasi Otomatis (Anti-Lag)")]
    [Tooltip("Otomatis matikan Cast Shadows pada daun pohon agar FPS tetap tinggi")]
    public bool disableLeavesShadows = true;

    [HideInInspector]
    [SerializeField]
    private List<GameObject> spawnedTrees = new List<GameObject>();

    // Menggambar kotak area hijau transparan di Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.3f);
        Gizmos.DrawCube(transform.position, boxAreaSize);

        Gizmos.color = new Color(0.1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, boxAreaSize);
    }

    public void GenerateTrees()
    {
        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("<color=red>[Tree Spawner]</color> Masukkan minimal 1 Prefab Pohon ke dalam list treePrefabs!");
            return;
        }

        // Hapus pohon generasi sebelumnya jika ada
        ClearTrees();

        Vector3 center = transform.position;
        Vector3 halfSize = boxAreaSize * 0.5f;

        List<Vector3> placedPositions = new List<Vector3>();
        int maxAttemptsPerTree = 25;

        // Container agar Hierarchy tetap rapi
        GameObject container = new GameObject("Spawned_Trees_" + gameObject.name);
        container.transform.parent = this.transform;
        container.transform.localPosition = Vector3.zero;

        for (int i = 0; i < jumlahPohon; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool foundValidSpot = false;

            for (int attempt = 0; attempt < maxAttemptsPerTree; attempt++)
            {
                float randX = Random.Range(center.x - halfSize.x, center.x + halfSize.x);
                float randZ = Random.Range(center.z - halfSize.z, center.z + halfSize.z);
                float startY = center.y + halfSize.y;

                Vector3 candidatePos = new Vector3(randX, startY, randZ);

                // Cek jarak dengan pohon lain
                bool tooClose = false;
                foreach (Vector3 p in placedPositions)
                {
                    if (Vector2.Distance(new Vector2(candidatePos.x, candidatePos.z), new Vector2(p.x, p.z)) < minDistanceBetweenTrees)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                if (snapToGround)
                {
                    Ray ray = new Ray(candidatePos, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, boxAreaSize.y * 2f, groundLayer))
                    {
                        candidatePos = hit.point;
                        foundValidSpot = true;
                        spawnPos = candidatePos;
                        break;
                    }
                }
                else
                {
                    candidatePos.y = center.y;
                    foundValidSpot = true;
                    spawnPos = candidatePos;
                    break;
                }
            }

            if (!foundValidSpot) continue;

            placedPositions.Add(spawnPos);

            // Pilih prefab pohon secara acak
            GameObject chosenPrefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            if (chosenPrefab == null) continue;

            // Instantiate
#if UNITY_EDITOR
            GameObject newTree = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, container.transform);
#else
            GameObject newTree = Instantiate(chosenPrefab, container.transform);
#endif
            newTree.transform.position = spawnPos;

            // Variasi Rotasi & Skala (Koreksi orientasi FBX: X = -90 derajat agar berdiri tegak)
            Quaternion baseRot = chosenPrefab.transform.rotation;
            Vector3 euler = baseRot.eulerAngles;
            if (Mathf.Approximately(euler.x, 0f) && Mathf.Approximately(euler.y, 0f) && Mathf.Approximately(euler.z, 0f))
            {
                // Standar FBX Blender ke Unity butuh X = -90 agar berdiri tegak
                euler.x = -90f;
            }

            if (randomYRotation)
            {
                // Putar secara acak di poros batang pohon
                euler.z = Random.Range(0f, 360f);
            }

            newTree.transform.rotation = Quaternion.Euler(euler);

            float scale = Random.Range(minScale, maxScale);
            newTree.transform.localScale = Vector3.one * scale;

            // Optimasi otomatis: Matikan Cast Shadows pada daun (Kunci anti-lag utama!)
            if (disableLeavesShadows)
            {
                MeshRenderer[] renderers = newTree.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer r in renderers)
                {
                    bool isLeaf = r.gameObject.name.ToLower().Contains("daun") || r.gameObject.name.ToLower().Contains("leaf");
                    if (!isLeaf && r.sharedMaterials != null)
                    {
                        foreach (Material mat in r.sharedMaterials)
                        {
                            if (mat != null && mat.name.ToLower().Contains("daun"))
                            {
                                isLeaf = true;
                                break;
                            }
                        }
                    }

                    if (isLeaf)
                    {
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        r.receiveShadows = false; // Matikan overdraw shadow pada jutaan helai daun
                    }
                }
            }

            spawnedTrees.Add(newTree);
        }

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(container, "Generate Trees");
#endif
        Debug.Log($"<color=green>[Tree Spawner]</color> Sukses men-generate {spawnedTrees.Count} pohon di area {gameObject.name}!");
    }

    public void ClearTrees()
    {
        // Bersihkan child container
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Spawned_Trees_"))
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        spawnedTrees.Clear();
        Debug.Log("<color=yellow>[Tree Spawner]</color> Pohon di area berhasil dibersihkan.");
    }
}
