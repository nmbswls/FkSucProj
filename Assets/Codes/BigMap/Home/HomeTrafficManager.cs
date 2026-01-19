using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My.Map
{

    public class HomeTrafficManager : MonoBehaviour
    {
        public static HomeTrafficManager Instance;


        [Header("配置")]
        public GameObject NpcPrefab;
        public int MaxNpcCount = 80;
        public float SpawnInterval = 0.5f;
        public float PathRandomWidth = 0.5f;

        // 所有端点列表（既是起点也是终点）
        private List<HomeTrafficNode> _endPoints = new List<HomeTrafficNode>();
        private List<HomeBgNpc> _pool = new List<HomeBgNpc>();
        private float _timer;

        void Awake()
        {
            Instance = this;

            // 1. 收集所有 EndPoint
            var allNodes = transform.GetComponentsInChildren<HomeTrafficNode>();
            foreach (var node in allNodes)
            {
                if (node.Type == HomeTrafficNode.NodeType.EndPoint)
                    _endPoints.Add(node);
            }

            // 初始化对象池
            for (int i = 0; i < MaxNpcCount; i++)
            {
                var obj = Instantiate(NpcPrefab, transform);
                obj.SetActive(false);
                _pool.Add(obj.AddComponent<HomeBgNpc>());
            }
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                TrySpawnNpc();
                _timer = SpawnInterval + Random.Range(-0.2f, 0.2f);
            }
        }

        void TrySpawnNpc()
        {
            if (_endPoints.Count < 2) return; // 至少要有两个端点才能走

            var npc = _pool.Find(x => !x.IsActive);
            if (npc == null) return;
            npc.gameObject.SetActive(true);
            // 1. 随机选一个出生点 (StartNode)
            var startNode = _endPoints[Random.Range(0, _endPoints.Count)];

            // 2. 寻找一条通往“其他端点”的路
            Queue<Vector3> route = GenerateRouteToAnyExit(startNode);

            if (route != null && route.Count > 0)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-PathRandomWidth, PathRandomWidth),
                    Random.Range(-PathRandomWidth, PathRandomWidth),
                    0
                );

                // 加上偏移量
                npc.Init(startNode.transform.position + offset, route, offset);
            }
        }

        Queue<Vector3> GenerateRouteToAnyExit(HomeTrafficNode startNode)
        {
            Queue<Vector3> path = new Queue<Vector3>();
            HomeTrafficNode current = startNode;

            // 1. 使用 HashSet 记录已访问的节点，查找速度快 (O(1))
            HashSet<HomeTrafficNode> visited = new HashSet<HomeTrafficNode>();

            // 把起点先加进去，防止绕一大圈又走回起点
            visited.Add(startNode);

            bool foundExit = false;
            int maxSteps = 30;

            for (int i = 0; i < maxSteps; i++)
            {
                // --- 核心改动：更严格的筛选 ---

                // 筛选出可走的下一步：必须是不在 visited 集合里的点
                List<HomeTrafficNode> validNextNodes = current.NextNodes
                    .Where(n => !visited.Contains(n))
                    .ToList();

                // 如果所有路都走过了（或者是死胡同），那就断掉
                if (validNextNodes.Count == 0) break;

                // 随机选一条新路
                HomeTrafficNode nextNode = validNextNodes[Random.Range(0, validNextNodes.Count)];

                // 加入路径
                path.Enqueue(nextNode.transform.position);

                // 标记为已访问
                visited.Add(nextNode);

                // 更新当前节点
                current = nextNode;

                // --- 判断是否到达终点 ---
                if (current.Type == HomeTrafficNode.NodeType.EndPoint && current != startNode)
                {
                    foundExit = true;
                    break;
                }
            }

            return foundExit ? path : null;
        }
    }
}


