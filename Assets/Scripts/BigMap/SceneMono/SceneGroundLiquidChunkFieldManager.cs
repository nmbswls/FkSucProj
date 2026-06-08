using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class SceneGroundLiquidChunkFieldManager : MonoBehaviour
    {
        const string LiquidMaskLayerName = "LiquidMask";

        [Header("Liquid Field Chunks")]
        public Transform liquidLayerContainer;

        readonly Dictionary<Vector2Int, ChunkView> _activeChunks = new();
        readonly Queue<ChunkView> _chunkPool = new();
        readonly Color32[] _pixelScratch = new Color32[LiquidFieldConstants.ChunkTexSizeWithHalo * LiquidFieldConstants.ChunkTexSizeWithHalo];

        int _liquidMaskLayer = -1;

        LogicGroundLiquidFieldManager FieldManager =>
            MainGameManager.Instance.gameLogicManager.GroundLiquidFieldManager;

        sealed class ChunkView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public Texture2D Texture;
            public Sprite Sprite;
            public Vector2Int Coord;
        }

        void Awake()
        {
            _liquidMaskLayer = LayerMask.NameToLayer(LiquidMaskLayerName);
            EnsureContainer();
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

            float haloWorld = LiquidFieldConstants.ChunkHaloTexels * LiquidFieldConstants.SubCellWorldSize;
            Vector2 chunkMin = LogicGroundLiquidFieldManager.ChunkWorldMin(chunkCoord);
            float worldSize = LiquidFieldConstants.ChunkWorldSize + haloWorld * 2f;
            float ppu = LiquidFieldConstants.ChunkTexSizeWithHalo / worldSize;

            view.Texture.Reinitialize(LiquidFieldConstants.ChunkTexSizeWithHalo, LiquidFieldConstants.ChunkTexSizeWithHalo);
            view.Texture.filterMode = FilterMode.Bilinear;
            view.Texture.wrapMode = TextureWrapMode.Clamp;

            if (view.Sprite != null)
            {
                Destroy(view.Sprite);
            }

            view.Sprite = Sprite.Create(
                view.Texture,
                new Rect(0f, 0f, LiquidFieldConstants.ChunkTexSizeWithHalo, LiquidFieldConstants.ChunkTexSizeWithHalo),
                new Vector2(0.5f, 0.5f),
                ppu);
            view.Renderer.sprite = view.Sprite;

            Vector2 center = chunkMin + Vector2.one * (LiquidFieldConstants.ChunkWorldSize * 0.5f);
            view.Root.transform.position = new Vector3(center.x, center.y, 0f);

            if (_liquidMaskLayer >= 0)
            {
                view.Root.layer = _liquidMaskLayer;
            }

            _activeChunks.Add(chunkCoord, view);
            return view;
        }

        ChunkView CreateChunkView()
        {
            var root = new GameObject("LiquidFieldChunk");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 0;

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
                Texture = texture
            };
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
                        _pixelScratch[py * texSize + px] = BuildMaskPixel(intensity, type);
                    }
                }
            }

            SyncHaloFromNeighbors(view.Coord, _pixelScratch, texSize, coreSize, halo);
            view.Texture.SetPixels32(_pixelScratch);
            view.Texture.Apply(false, false);
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
                pixels[py * texSize + px] = BuildMaskPixel(intensity, type);
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

            pixels[py * texSize + px] = BuildMaskPixel(intensity, type);
        }

        static Color32 BuildMaskPixel(byte intensity, EGroundLiquidType type)
        {
            switch (type)
            {
                case EGroundLiquidType.GcLiquid:
                    return new Color32(intensity, 0, 0, 255);
                case EGroundLiquidType.Milk:
                    return new Color32(0, intensity, 0, 255);
                default:
                    return new Color32(0, 0, 0, 0);
            }
        }

        void RecycleChunkView(ChunkView view)
        {
            view.Root.SetActive(false);
            view.Root.transform.SetParent(transform, false);
            _chunkPool.Enqueue(view);
        }
    }
}
