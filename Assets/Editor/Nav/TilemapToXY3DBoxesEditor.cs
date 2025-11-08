using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class TilemapToXY3DBoxesEditor : EditorWindow
{
    private Tilemap tilemap;
    private Transform parentRoot;
    private float thickness = 0.2f; // 沿 Z 轴的厚度
    private float zOffset = 0f; // 整体 Z 偏移（通常 0）
    private string generatedName = "_XY3D_Obstacles";

    [MenuItem("Tools/NavMesh (XY)/Tilemap 2D → 3D Boxes (XY)")]
    private static void Open()
    {
        GetWindow<TilemapToXY3DBoxesEditor>("Tilemap → 3D Boxes (XY)");
    }

    private void OnGUI()
    {
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap (Walls/Obstacles)", tilemap, typeof(Tilemap), true);
        parentRoot = (Transform)EditorGUILayout.ObjectField("Parent Root (optional)", parentRoot, typeof(Transform), true);
        thickness = EditorGUILayout.FloatField("Thickness (Z)", Mathf.Max(0.01f, thickness));
        zOffset = EditorGUILayout.FloatField("Z Offset", zOffset);
        generatedName = EditorGUILayout.TextField("Generated Child Name", generatedName);

        if (GUILayout.Button("Generate / Update"))
        {
            if (!tilemap)
            {
                EditorUtility.DisplayDialog("Error", "Assign a Tilemap.", "OK");
                return;
            }
            Generate(tilemap, parentRoot, thickness, zOffset, generatedName);
        }
    }

    private static void Generate(Tilemap tilemap, Transform parentRoot, float thick, float zOffset, string childName)
    {
        Transform host = parentRoot ? parentRoot : tilemap.transform;

        var existing = host.Find(childName);
        if (existing) Object.DestroyImmediate(existing.gameObject);

        var holder = new GameObject(childName);
        holder.transform.SetParent(host, false);

        BoundsInt bounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.layoutGrid ? tilemap.layoutGrid.cellSize : Vector3.one;
        int count = 0;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell)) continue;

                Vector3 worldXY = tilemap.GetCellCenterWorld(cell);
                var go = new GameObject($"Box_{x}_{y}");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = new Vector3(worldXY.x, worldXY.y, zOffset);

                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(cellSize.x, cellSize.y, thick);
                box.center = Vector3.zero;

                count++;
            }
        }

        EditorUtility.DisplayDialog("Done", $"Generated {count} BoxColliders in {holder.name}.", "OK");
        Selection.activeObject = holder;
        EditorGUIUtility.PingObject(holder);
    }
}