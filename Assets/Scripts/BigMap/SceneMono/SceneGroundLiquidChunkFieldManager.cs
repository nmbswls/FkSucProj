using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class SceneGroundLiquidChunkFieldManager : MonoBehaviour
    {
        static readonly int LiquidTexId = Shader.PropertyToID("_LiquidTex");

        static readonly (int dx, int dy, float weight)[] SoftKernel =
        {
            (0, 0, 1f),
            (-1, 0, 0.55f), (1, 0, 0.55f), (0, -1, 0.55f), (0, 1, 0.55f),
            (-1, -1, 0.3f), (1, -1, 0.3f), (-1, 1, 0.3f), (1, 1, 0.3f),
        };

        [Header("Liquid Field Chunks")]
        public Transform liquidLayerContainer;
        [SerializeField] Material liquidChunkMaterial;
        [SerializeField] string sortingLayerName = "Ground";
        [SerializeField] int sortingOrder = 1;

        readonly Dictionary<Vector2Int, ChunkView> _activeChunks = new();
        readonly Queue<ChunkView> _chunkPool = new();
        readonly Color32[] _pixelScratch = new Color32[LiquidFieldConstants.ChunkTexSizeWithHalo * LiquidFieldConstants.ChunkTexSizeWithHalo];

        LogicGroundLiquidFieldManager FieldManager =>
            MainGameManager.Instance.gameLogicManager.GroundLiquidFieldManager;

        sealed class ChunkView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Material Material;
            public Texture2D Texture;
            public Sprite Sprite;
            public Vector2Int Coord;
        }

        void Awake()
        {
            EnsureContainer();
        }

        void OnDestroy()
        {
            DestroyAllChunkMaterials();
        }

        public void RegisterEvents()
        {
            FieldManager.OnChunkDirty += HandleChunkDirty;
        }

        public void UnRegisterEvents()
        {
            if (MainGameManager.Instance?.gameLogicManager?.GroundLiquidFieldManager == null)
            {
                return;
            }

            FieldManager.OnChunkDirty -= HandleChunkDirty;
        }

        public void ClearAllChunks()
        {
            foreach (var kvp in _activeChunks)
            {
                RecycleChunkView(kvp.Value);
            }

            _activeChunks.Clear();
        }

        void EnsureContainer()
        {
            if (liquidLayerContainer != null)
            {
                return;
            }

            var go = new GameObject("LayerLiquidField");
            go.transform.SetParent(transform, false);
            liquidLayerContainer = go.transform;
        }

        void HandleChunkDirty(Vector2Int chunkCoord)
        {
            if (!FieldManager.TryGetChunk(chunkCoord, out var chunk) || !chunk.HasVisibleContent())
            {
                if (_activeChunks.TryGetValue(chunkCoord, out var existing))
                {
                    RecycleChunkView(existing);
                    _activeChunks.Remove(chunkCoord);
                }

                return;
            }

            var view = EnsureChunkView(chunkCoord);
            RebuildChunkTexture(view);
        }

        ChunkView EnsureChunkView(Vector2Int chunkCoord)
        {
            if (_activeChunks.TryGetValue(chunkCoord, out var view))
            {
                return view;
            }

            view = _chunkPool.Count > 0 ? _chunkPool.Dequeue() : CreateChunkView();
            view.Coord = chunkCoord;
            view.Root.transform.SetParent(liquidLayerContainer, false);
            view.Root.SetActive(true);
            view.Root.name = $"LiquidFieldChunk_{chunkCoord.x}_{chunkCoord.y}";

            int coreSize = LiquidFieldConstants.ChunkTexSize;
            int halo = LiquidFieldConstants.ChunkHaloTexels;
            Vector2 chunkMin = LogicGroundLiquidFieldManager.ChunkWorldMin(chunkCoord);
            float ppu = coreSize / LiquidFieldConstants.ChunkWorldSize;

            view.Texture.Reinitialize(LiquidFieldConstants.ChunkTexSizeWithHalo, LiquidFieldConstants.ChunkTexSizeWithHalo);
            view.Texture.filterMode = FilterMode.Bilinear;
            view.Texture.wrapMode = TextureWrapMode.Clamp;

            if (view.Sprite != null)
            {
                Destroy(view.Sprite);
            }

            // 仅渲染核心区域，halo 留在纹理中供双线性采样，避免相邻 chunk 几何重叠导致 z-fighting
            view.Sprite = Sprite.Create(
                view.Texture,
                new Rect(halo, halo, coreSize, coreSize),
                new Vector2(0.5f, 0.5f),
                ppu);
            view.Renderer.sprite = view.Sprite;
            ApplyRendererSorting(view.Renderer);

            Vector2 center = chunkMin + Vector2.one * (LiquidFieldConstants.ChunkWorldSize * 0.5f);
            view.Root.transform.position = new Vector3(center.x, center.y, 0f);

            _activeChunks.Add(chunkCoord, view);
            return view;
        }

        ChunkView CreateChunkView()
        {
            if (liquidChunkMaterial == null)
            {
                Debug.LogError("SceneGroundLiquidChunkFieldManager: liquidChunkMaterial is missing");
            }

            var root = new GameObject("LiquidFieldChunk");
            var renderer = root.AddComponent<SpriteRenderer>();
            ApplyRendererSorting(renderer);

            Material mat = liquidChunkMaterial != null ? new Material(liquidChunkMaterial) : null;
            if (mat != null)
            {
                renderer.material = mat;
            }

            var texture = new Texture2D(
                LiquidFieldConstants.ChunkTexSizeWithHalo,
                LiquidFieldConstants.ChunkTexSizeWithHalo,
                TextureFormat.RGBA32,
                false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            return new ChunkView
            {
                Root = root,
                Renderer = renderer,
                Material = mat,
                Texture = texture
            };
        }

        void ApplyRendererSorting(SpriteRenderer renderer)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }

        void RebuildChunkTexture(ChunkView view)
        {
            int texSize = LiquidFieldConstants.ChunkTexSizeWithHalo;
            int coreSize = LiquidFieldConstants.ChunkTexSize;
            int halo = LiquidFieldConstants.ChunkHaloTexels;
            var clear = new Color32(0, 0, 0, 0);

            for (int i = 0; i < _pixelScratch.Length; i++)
            {
                _pixelScratch[i] = clear;
            }

            if (FieldManager.TryGetChunk(view.Coord, out var chunk))
            {
                for (int ty = 0; ty < coreSize; ty++)
                {
                    for (int tx = 0; tx < coreSize; tx++)
                    {
                        int idx = LiquidFieldChunkData.ToIndex(tx, ty);
                        byte intensity = chunk.Intensities[idx];
                        if (intensity == 0)
                        {
                            continue;
                        }

                        var type = (EGroundLiquidType)chunk.Types[idx];
                        int px = tx + halo;
                        int py = ty + halo;
                        StampMaskPixel(_pixelScratch, texSize, px, py, intensity, type);
                    }
                }
            }

            SyncHaloFromNeighbors(view.Coord, _pixelScratch, texSize, coreSize, halo);
            view.Texture.SetPixels32(_pixelScratch);
            view.Texture.Apply(false, false);
            BindChunkLiquidTexture(view);
        }

        void BindChunkLiquidTexture(ChunkView view)
        {
            if (view.Material == null)
            {
                return;
            }

            view.Material.SetTexture(LiquidTexId, view.Texture);
        }

        void SyncHaloFromNeighbors(Vector2Int chunkCoord, Color32[] pixels, int texSize, int coreSize, int halo)
        {
            CopyNeighborEdge(chunkCoord + Vector2Int.left, coreSize - 1, 0, chunkCoord, -1, 0, pixels, texSize, coreSize, halo, true);
            CopyNeighborEdge(chunkCoord + Vector2Int.right, 0, 0, chunkCoord, coreSize, 0, pixels, texSize, coreSize, halo, true);
            CopyNeighborEdge(chunkCoord + Vector2Int.down, 0, coreSize - 1, chunkCoord, 0, -1, pixels, texSize, coreSize, halo, false);
            CopyNeighborEdge(chunkCoord + Vector2Int.up, 0, 0, chunkCoord, 0, coreSize, pixels, texSize, coreSize, halo, false);

            CopyNeighborCorner(chunkCoord + new Vector2Int(-1, -1), coreSize - 1, coreSize - 1, chunkCoord, -1, -1, pixels, texSize, halo);
            CopyNeighborCorner(chunkCoord + new Vector2Int(1, -1), 0, coreSize - 1, chunkCoord, coreSize, -1, pixels, texSize, halo);
            CopyNeighborCorner(chunkCoord + new Vector2Int(-1, 1), coreSize - 1, 0, chunkCoord, -1, coreSize, pixels, texSize, halo);
            CopyNeighborCorner(chunkCoord + new Vector2Int(1, 1), 0, 0, chunkCoord, coreSize, coreSize, pixels, texSize, halo);
        }

        void CopyNeighborEdge(
            Vector2Int neighborCoord,
            int neighborStart,
            int neighborFixed,
            Vector2Int selfCoord,
            int selfStart,
            int selfFixed,
            Color32[] pixels,
            int texSize,
            int coreSize,
            int halo,
            bool horizontalEdge)
        {
            if (neighborCoord == selfCoord)
            {
                return;
            }

            if (!FieldManager.TryGetChunk(neighborCoord, out var neighbor))
            {
                return;
            }

            for (int i = 0; i < coreSize; i++)
            {
                int ntx = horizontalEdge ? neighborStart : i;
                int nty = horizontalEdge ? i : neighborStart;
                int stx = horizontalEdge ? selfStart : i;
                int sty = horizontalEdge ? i : selfStart;

                int nIdx = LiquidFieldChunkData.ToIndex(ntx, nty);
                byte intensity = neighbor.Intensities[nIdx];
                if (intensity == 0)
                {
                    continue;
                }

                var type = (EGroundLiquidType)neighbor.Types[nIdx];
                int px = stx + halo;
                int py = sty + halo;
                WriteMaskPixel(pixels, py * texSize + px, intensity, type);
            }
        }

        void CopyNeighborCorner(
            Vector2Int neighborCoord,
            int ntx,
            int nty,
            Vector2Int selfCoord,
            int stx,
            int sty,
            Color32[] pixels,
            int texSize,
            int halo)
        {
            if (neighborCoord == selfCoord)
            {
                return;
            }

            if (!FieldManager.TryGetCoreTexel(neighborCoord, ntx, nty, out byte intensity, out var type))
            {
                return;
            }

            int px = stx + halo;
            int py = sty + halo;
            if (px < 0 || py < 0 || px >= texSize || py >= texSize)
            {
                return;
            }

            WriteMaskPixel(pixels, py * texSize + px, intensity, type);
        }

        static void StampMaskPixel(Color32[] pixels, int texSize, int cx, int cy, byte intensity, EGroundLiquidType type)
        {
            if (intensity == 0 || type == EGroundLiquidType.None)
            {
                return;
            }

            for (int k = 0; k < SoftKernel.Length; k++)
            {
                var (dx, dy, weight) = SoftKernel[k];
                int px = cx + dx;
                int py = cy + dy;
                if (px < 0 || py < 0 || px >= texSize || py >= texSize)
                {
                    continue;
                }

                byte weighted = (byte)Mathf.Min(255, Mathf.RoundToInt(intensity * weight));
                WriteMaskPixel(pixels, py * texSize + px, weighted, type);
            }
        }

        static void WriteMaskPixel(Color32[] pixels, int index, byte intensity, EGroundLiquidType type)
        {
            if (intensity == 0 || type == EGroundLiquidType.None)
            {
                return;
            }

            var pixel = pixels[index];
            switch (type)
            {
                case EGroundLiquidType.GcLiquid:
                    if (intensity > pixel.r)
                    {
                        pixel.r = intensity;
                    }
                    break;
                case EGroundLiquidType.Milk:
                    if (intensity > pixel.g)
                    {
                        pixel.g = intensity;
                    }
                    break;
                default:
                    return;
            }

            pixel.a = 255;
            pixels[index] = pixel;
        }

        void RecycleChunkView(ChunkView view)
        {
            view.Root.SetActive(false);
            view.Root.transform.SetParent(transform, false);
            _chunkPool.Enqueue(view);
        }

        void DestroyAllChunkMaterials()
        {
            foreach (var view in _chunkPool)
            {
                DestroyChunkMaterial(view);
            }

            foreach (var kvp in _activeChunks)
            {
                DestroyChunkMaterial(kvp.Value);
            }
        }

        static void DestroyChunkMaterial(ChunkView view)
        {
            if (view.Material == null)
            {
                return;
            }

            Destroy(view.Material);
            view.Material = null;
            if (view.Renderer != null)
            {
                view.Renderer.sharedMaterial = null;
            }
        }
    }
}
