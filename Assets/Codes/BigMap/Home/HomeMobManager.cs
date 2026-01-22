using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class HomeMobManager : MonoBehaviour
    {
        public static HomeMobManager Instance;

        [Header("全局引用")]
        public Transform Player;
        public Transform[] WorkSpots; // 工作地点列表
        public Transform[] TownGates; // 城镇出入口

        [Header("全局配置")]
        public float SaluteRadius = 5f; // 致敬半径
        public LayerMask NpcLayer;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterValidWorkPoint()
        {

        }

        // 获取一个随机工作点
        public Transform GetRandomWorkSpot()
        {
            if (WorkSpots.Length == 0) return transform;
            return WorkSpots[Random.Range(0, WorkSpots.Length)];
        }

        // 获取最近的出口
        public Transform GetNearestGate(Vector3 pos)
        {
            Transform bestTarget = null;
            float closestDistanceSqr = Mathf.Infinity;
            foreach (var gate in TownGates)
            {
                Vector3 directionToTarget = gate.position - pos;
                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = gate;
                }
            }
            return bestTarget;
        }
    }

}

