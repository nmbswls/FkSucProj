

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My.Map
{

    public class HomeFacility : MonoBehaviour
    {
        public enum FacilityType { LumberMill, Church, Tavern, TownSquare }
        public FacilityType Category;

        private List<HomeActionSpot> _mySpots = new List<HomeActionSpot>();

        void Awake()
        {
            // 自动抓取子物体里的所有点
            _mySpots = GetComponentsInChildren<HomeActionSpot>().ToList();
        }

        /// <summary>
        /// 在本设施内，找一个特定类型的空位
        /// </summary>
        public bool TryGetSpot(HomeActionSpot.SpotType spotType, out HomeActionSpot spot, out int slotIndex, out Vector3 pos)
        {
            spot = null; slotIndex = -1; pos = Vector3.zero;

            // 筛选符合类型的点（比如在教堂里找 Worship 点，而不是 Gate 点）
            var validSpots = _mySpots.Where(s => s.Type == spotType).ToList();

            // 随机打乱以避免总是去同一个点
            validSpots.Sort((a, b) => Random.Range(-1, 2));

            foreach (var s in validSpots)
            {
                int idx = s.TryGetFreeSlotIndex();
                if (idx != -1)
                {
                    spot = s;
                    slotIndex = idx;
                    // 这里我们稍微取巧，不立即 Occupy，而是计算出位置返回，让 NPC 决定
                    // 注意：在多线程或高并发下最好在这里预占位
                    Vector3 offset = spot.IsQueueMode ? -spot.transform.forward * (idx * spot.Spacing) : Vector3.zero;
                    pos = spot.transform.position + spot.transform.rotation * offset;
                    return true;
                }
            }
            return false;
        }
    }

}