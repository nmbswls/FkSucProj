
using System.Collections.Generic;
using System.Linq;
using My.Home;
using UnityEngine;

namespace My
{

    public sealed class BuildMaskRuntime
    {
        public readonly int width;
        public readonly int height;
        public readonly int originX;
        public readonly int originY;

        private readonly byte[] buildableBits;
        private readonly byte[] occupancyBits; // 初始占用，运行时可拷贝一份作动态占用

        public BuildMaskRuntime(int w, int h, int ox, int oy, byte[] buildBits, byte[] occBits)
        {
            width = w; height = h; originX = ox; originY = oy;
            buildableBits = buildBits ?? new byte[(w * h + 7) / 8];
            occupancyBits = occBits ?? new byte[(w * h + 7) / 8];
        }

        private static int Index(int x, int y, int w) => y * w + x;

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool GetBit(byte[] bits, int idx)
        {
            int bi = idx >> 3;
            int mask = 1 << (idx & 7);
            return (bits[bi] & mask) != 0;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool IsBuildableCell(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return GetBit(buildableBits, Index(x, y, width));
        }

        public bool IsInitiallyOccupied(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return GetBit(occupancyBits, Index(x, y, width));
        }
    }


    public class HomeSceneManager : MonoBehaviour
    {
        public static HomeSceneManager Instance { get; private set; }

        public HomeDataManager DataSource { get { return MainGameManager.Instance.gameLogicManager.homeDataManager; } }
        public PreviewTilemapController previewTilemapController;

        //public List<HomeScenePlacement> homeScenePlacements = new();
        private BuildMaskRuntime runtime;
        public Grid BuildGrid;
        private HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();

        public bool IsOccupied(Vector3Int cell) => occupied.Contains(cell);


        public void Occupy(IEnumerable<Vector3Int> cells)
        {
            foreach (var c in cells) occupied.Add(c);
        }

        public void Vacate(IEnumerable<Vector3Int> cells)
        {
            foreach (var c in cells) occupied.Remove(c);
        }




        public void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitHomePlacements()
        {
            foreach(var on in DataSource.PlacementInfos)
            {

            }

            InitBuildMask();
        }

        public void InitBuildMask()
        {

            BuildMaskAsset buildMaskAsset = Resources.Load<BuildMaskAsset>("BuildMask");

            runtime = new BuildMaskRuntime(
                buildMaskAsset.width,
                buildMaskAsset.height,
                buildMaskAsset.originX,
                buildMaskAsset.originY,
                buildMaskAsset.buildableBits,
                buildMaskAsset.occupancyBits
            );
        }


        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            return BuildGrid.WorldToCell(worldPos);
        }

        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return BuildGrid.CellToWorld(cellPos);
        }

        public bool CanPlace(HomePlaceableObject obj, EPlacementRotation rot, Vector3Int pivotCell)
        {
            foreach (var offset in obj.GetFootprint(rot))
            {
                var cell = pivotCell + new Vector3Int(offset.x, offset.y, 0);
                int lx = cell.x - runtime.originX;
                int ly = cell.y - runtime.originY;
                if (!runtime.IsBuildableCell(lx, ly)) return false;
                if (IsOccupied(cell)) return false;
            }
            return true;
        }

        /// <summary>
        /// 放置
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rot"></param>
        /// <param name="pivotCell"></param>
        /// <param name="isMove"></param>
        public void TryPlace(HomePlaceableObject obj, EPlacementRotation rot, Vector3Int pivotCell, bool isMove)
        {
            // 实例化实际对象
            //var go = Instantiate(GetPrefabFor(obj, rot));
            //go.transform.position = CellToWorld(pivotCell);
            if(isMove)
            {
                DataSource.MovePlacement(obj.id, pivotCell, rot);
            }
            else
            {
                DataSource.AddPlacement(obj.id, pivotCell, rot);
            }

            // var chunkPos = SceneAOIManager.Instance.WorldToChunk(CellToWorld(pivotCell));
            //SceneAOIManager.Instance.ForceUpdateOneChunk(chunkPos);
            // 占用格子
            var cells = obj.GetFootprint(rot).Select(off => pivotCell + new Vector3Int(off.x, off.y, 0));
            Occupy(cells);
        }

        public bool CanBuildAtWorld(Vector3 worldPos)
        {
            Vector3Int cell = BuildGrid.WorldToCell(worldPos);
            int lx = cell.x - runtime.originX;
            int ly = cell.y - runtime.originY;
            if (!runtime.InBounds(lx, ly)) return false;
            return runtime.IsBuildableCell(lx, ly) && !runtime.IsInitiallyOccupied(lx, ly);
        }

        public void AddHomePlacement()
        {

        }


        public void InitPreview()
        {
            //previewTilemapController
        }
    }
}