using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace My.Map
{
    public class HomeFacilityManager : MonoBehaviour
    {
        public static HomeFacilityManager Instance;

        public Transform FacilityRoot;

        // 索引1：按设施类型存储设施
        private Dictionary<HomeFacility.FacilityType, List<HomeFacility>> _facilities = new Dictionary<HomeFacility.FacilityType, List<HomeFacility>>();

        // 索引2：按交互点类型存储所有点（包括野外的和设施内的）
        private Dictionary<HomeActionSpot.SpotType, List<HomeActionSpot>> _allSpots = new Dictionary<HomeActionSpot.SpotType, List<HomeActionSpot>>();

        void Awake() { Instance = this; }

        public void Start()
        {
            InitFacilities();
        }


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


