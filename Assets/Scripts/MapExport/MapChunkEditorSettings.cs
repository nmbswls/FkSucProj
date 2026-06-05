using My;
using UnityEngine;

namespace My.MapExport
{
    // 地图 Chunk / Paint 工作流的全局 Editor 配置（Resources/MapExport/MapChunkEditorSettings）
    [CreateAssetMenu(fileName = "MapChunkEditorSettings", menuName = "MapExport/Editor Settings")]
    public class MapChunkEditorSettings : ScriptableObject
    {
        public const string ResourcePath = "MapExport/MapChunkEditorSettings";

        static MapChunkEditorSettings _cached;

        public static MapChunkEditorSettings Instance
        {
            get
            {
                if (_cached == null)
                {
                    _cached = Resources.Load<MapChunkEditorSettings>(ResourcePath);
                }

                return _cached;
            }
        }

        [Header("Chunk Grid")]
        public float ChunkWorldSize = 32f;
        public float TexturePPU = 16f;
        [Tooltip("0 表示与 Texture PPU 相同")]
        public float PaintExportPPU = 16f;

        [Header("Paint Capture")]
        public Color PaintMaskColor = new Color(1f, 0f, 1f, 1f);
        public LayerMask PaintCaptureLayerMask = ~0;
        public float PaintCaptureCameraZ = -10f;
        [Range(0f, 0.49f)]
        public float PaintContextExpandRatio = 0.25f;

        [Header("Export")]
        public int BackgroundSortingOrder;
        public bool ExportGridRoot3DCollision;
        public float GridRootCollisionThickness = 0.3f;
        public string GridRootCollisionLayer = "Wall";

        [Header("Preview")]
        public bool PaintPreviewEnabled = true;
        public bool PaintAutoRefreshPreview;

        public float EffectiveChunkWorldSize =>
            ChunkWorldSize > 0f ? ChunkWorldSize : GameConsts.ChunkCellSize;

        public float EffectivePaintExportPpu =>
            PaintExportPPU > 0f ? PaintExportPPU : TexturePPU;

        public int PaintSlicePixelSize =>
            MapChunkUtility.ComputeSlicePixelSize(EffectiveChunkWorldSize, EffectivePaintExportPpu);

        public int SlicePixelSize =>
            MapChunkUtility.ComputeSlicePixelSize(EffectiveChunkWorldSize, TexturePPU);

#if UNITY_EDITOR
        public static MapChunkEditorSettings GetOrCreate()
        {
            var settings = Instance;
            if (settings != null)
            {
                return settings;
            }

            settings = CreateInstance<MapChunkEditorSettings>();
            const string assetPath = "Assets/Resources/MapExport/MapChunkEditorSettings.asset";
            var dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            _cached = settings;
            Debug.Log($"[MapChunkEditorSettings] Created default settings at {assetPath}");
            return settings;
        }

        public static void InvalidateCache()
        {
            _cached = null;
        }

        [UnityEditor.MenuItem("Window/Map Chunk Editor Settings")]
        static void OpenSettingsAsset()
        {
            var settings = GetOrCreate();
            UnityEditor.Selection.activeObject = settings;
            UnityEditor.EditorGUIUtility.PingObject(settings);
        }
#endif
    }
}
