using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace My.Map
{
    public class HomeLandmarkManager : MonoBehaviour
    {
        public static HomeLandmarkManager Instance;

        // 分类存储所有的地标点
        private Dictionary<HomeLandmarkSpot.ESpotType, List<HomeLandmarkSpot>> _landmarks = new Dictionary<HomeLandmarkSpot.ESpotType, List<HomeLandmarkSpot>>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // 初始化字典
            foreach (HomeLandmarkSpot.ESpotType type in System.Enum.GetValues(typeof(HomeLandmarkSpot.ESpotType)))
            {
                _landmarks[type] = new List<HomeLandmarkSpot>();
            }
        }

        // --- 动态维护接口 ---

        public void RegisterSpot(HomeLandmarkSpot spot)
        {
            if (!_landmarks[spot.Type].Contains(spot))
            {
                _landmarks[spot.Type].Add(spot);
            }
        }

        public void UnregisterSpot(HomeLandmarkSpot spot)
        {
            if (_landmarks.ContainsKey(spot.Type))
            {
                _landmarks[spot.Type].Remove(spot);
            }
        }

        // --- 核心功能：获取空闲点位 ---

        /// <summary>
        /// 获取一个随机的、未被占用的指定类型地标
        /// </summary>
        public HomeLandmarkSpot GetFreeSpot(HomeLandmarkSpot.ESpotType type, Vector3 nearPos = default, float range = -1f)
        {
            var list = _landmarks[type];

            // 筛选所有未占用的点
            var freeSpots = list.Where(s => !s.IsOccupied).ToList();

            if (freeSpots.Count == 0) return null; // 没有空位了

            // 逻辑A: 只要随机一个空的就行
            if (range < 0)
            {
                return freeSpots[Random.Range(0, freeSpots.Count)];
            }

            // 逻辑B: 获取附近的空位 (如果有需要 NPC 找最近的工作台)
            var nearbySpots = freeSpots.Where(s => Vector3.Distance(s.transform.position, nearPos) <= range).ToList();
            if (nearbySpots.Count > 0)
            {
                return nearbySpots[Random.Range(0, nearbySpots.Count)];
            }

            // 如果范围内没有，还是返回任意一个空的，或者返回 null
            return freeSpots[Random.Range(0, freeSpots.Count)];
        }

        // 获取最近的出口（出口不需要占用逻辑，多人可走同一门）
        public Transform GetNearestGate(Vector3 pos)
        {
            var gates = _landmarks[HomeLandmarkSpot.ESpotType.Gate];
            if (gates.Count == 0) return transform; // 防空

            return gates.OrderBy(g => Vector3.SqrMagnitude(g.transform.position - pos)).First().transform;
        }
    }

}


