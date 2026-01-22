
using System.Collections.Generic;
using System.Linq;
using My.Home;
using My.Map;
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

        [Header("收集组件")]
        public Transform FacilityRoot;
        public Transform ActionSlotRoot;

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
                // 创建
            }

            InitBuildMask();

            InitFacilities();
            InitGlobalActionSpots();

            var prefab = Resources.Load<GameObject>("Home/SimpleNpc/1");

            for(int i=0;i<5;i++)
            {
                var go = GameObject.Instantiate(prefab, this.transform);
                go.SetActive(true);

                var simpleNpc = go.GetComponent<HomeSimpleNpc>();
                homeSimpleNpc.Add(simpleNpc);
            }
        }

        protected void InitBuildMask()
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

        public List<HomeSimpleNpc> homeSimpleNpc = new List<HomeSimpleNpc>();

        // 索引1：按设施类型存储设施
        private Dictionary<HomeFacility.FacilityType, List<HomeFacility>> _facilities = new Dictionary<HomeFacility.FacilityType, List<HomeFacility>>();

        // 索引2：按交互点类型存储所有点（包括野外的和设施内的）
        private Dictionary<HomeActionSpot.SpotType, List<HomeActionSpot>> _allSpots = new Dictionary<HomeActionSpot.SpotType, List<HomeActionSpot>>();


        /// <summary>
        /// 先固定
        /// </summary>
        private void InitFacilities()
        {
            for (int i = 0; i < FacilityRoot.childCount; i++)
            {
                var tr = FacilityRoot.GetChild(i);
                var facility = tr.GetComponent<HomeFacility>();
                if (facility == null) continue;

                if (!_facilities.TryGetValue(facility.Category, out var facilityList))
                {
                    facilityList = new();
                    _facilities[facility.Category] = facilityList;
                }

                facilityList.Add(facility);
                RegisterFacility(facility);
            }
        }


        private void InitGlobalActionSpots()
        {
            for (int i = 0; i < ActionSlotRoot.childCount; i++)
            {
                var tr = ActionSlotRoot.GetChild(i);
                var actionSpot = tr.GetComponent<HomeActionSpot>();
                if (actionSpot == null) continue;

                if (!_allSpots.TryGetValue(actionSpot.Type, out var spotList))
                {
                    spotList = new();
                    _allSpots[actionSpot.Type] = spotList;
                }

                spotList.Add(actionSpot);
                RegisterGlobalSpot(actionSpot);
            }
        }

        // --- 注册逻辑 ---
        public void RegisterFacility(HomeFacility f)
        {
            if (!_facilities.ContainsKey(f.Category)) _facilities[f.Category] = new List<HomeFacility>();
            _facilities[f.Category].Add(f);
        }

        public void RegisterGlobalSpot(HomeActionSpot s)
        {
            if (!_allSpots.ContainsKey(s.Type)) _allSpots[s.Type] = new List<HomeActionSpot>();
            _allSpots[s.Type].Add(s);
        }

        public HomeFacility GetRandomFacility(HomeFacility.FacilityType type)
        {
            if (!_facilities.ContainsKey(type) || _facilities[type].Count == 0) return null;
            return _facilities[type][Random.Range(0, _facilities[type].Count)];
        }

        public HomeActionSpot GetRandomGlobalSpot(HomeActionSpot.SpotType type)
        {
            if (!_allSpots.ContainsKey(type)) return null;

            // 简单的随机策略，实际可以加入距离判断
            var available = _allSpots[type].Where(s => s.TryGetFreeSlotIndex() != -1).ToList();
            if (available.Count == 0) return null;

            return available[Random.Range(0, available.Count)];
        }
    }
}