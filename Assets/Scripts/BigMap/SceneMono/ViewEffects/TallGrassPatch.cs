using System.Collections.Generic;
using My.Map.Scene;
using UnityEngine;

namespace My.Map
{
    // 每 Tilemap 格一个高草堆：3 层 × 2 簇，静态 Y 排序 + 动画随机相位
    [ExecuteAlways]
    public class TallGrassPatch : MonoBehaviour
    {
        public const int LayerCount = 3;
        public const int SlotsPerLayer = 2;
        public const int ClusterCount = LayerCount * SlotsPerLayer;

        static readonly Vector2[] SlotOffsets =
        {
            new(-0.22f, 0.14f), new(0.22f, 0.14f),
            new(-0.20f, 0.02f), new(0.20f, 0.02f),
            new(-0.18f, -0.10f), new(0.18f, -0.10f),
        };

        [Header("Cluster")]
        public GameObject ClusterPrefab;

        [Header("Sprites")]
        public Sprite[] SpriteVariants;

        [Header("Layout")]
        public float Jitter = 0.10f;
        public int BaseSortOrder;

        [Header("Runtime")]
        [SerializeField] bool rebuildOnAwake = true;

        readonly List<GameObject> _clusterInstances = new();

        void Awake()
        {
            if (!rebuildOnAwake)
            {
                return;
            }

            RebuildClusters();
        }

        public void RebuildClusters()
        {
            ClearClusterChildren();
            if (ClusterPrefab == null || SpriteVariants == null || SpriteVariants.Length == 0)
            {
                return;
            }

            int cellSeed = ComputeCellSeed(transform.position);
            for (int i = 0; i < ClusterCount; i++)
            {
                var localPos = ComputeClusterLocalPosition(i, cellSeed, Jitter);
                var clusterGo = InstantiateCluster(i);
                if (clusterGo == null)
                {
                    continue;
                }

                clusterGo.transform.SetParent(transform, false);
                clusterGo.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
                clusterGo.name = $"cluster_{i}";

                var worldY = transform.position.y + localPos.y;
                int sortOrder = BaseSortOrder - Mathf.RoundToInt(worldY * YSortOrder.factor);
                var sprite = PickSprite(cellSeed, i);
                float phase = HashTo01(cellSeed, i, 7919);
                bool flipX = HashTo01(cellSeed, i, 1301) >= 0.5f;

                var cluster = clusterGo.GetComponent<TallGrassCluster>();
                if (cluster == null)
                {
                    cluster = clusterGo.GetComponentInChildren<TallGrassCluster>(true);
                }

                if (cluster != null)
                {
                    cluster.Setup(sprite, sortOrder, phase, flipX);
                }

                _clusterInstances.Add(clusterGo);
            }
        }

        GameObject InstantiateCluster(int index)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = UnityEditor.PrefabUtility.InstantiatePrefab(ClusterPrefab) as GameObject;
                if (instance != null)
                {
                    return instance;
                }
            }
#endif
            return Instantiate(ClusterPrefab);
        }

        void ClearClusterChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (!child.name.StartsWith("cluster_"))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _clusterInstances.Clear();
        }

        public static Vector2 ComputeClusterLocalPosition(int clusterIndex, int cellSeed, float jitter)
        {
            if (clusterIndex < 0 || clusterIndex >= SlotOffsets.Length)
            {
                return Vector2.zero;
            }

            var baseOffset = SlotOffsets[clusterIndex];
            float jx = (HashTo01(cellSeed, clusterIndex, 101) - 0.5f) * 2f;
            float jy = (HashTo01(cellSeed, clusterIndex, 102) - 0.5f) * 2f;
            return baseOffset + new Vector2(jx * jitter, jy * jitter);
        }

        static int ComputeCellSeed(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x * 100f);
            int y = Mathf.FloorToInt(worldPos.y * 100f);
            unchecked
            {
                return x * 73856093 ^ y * 19349663;
            }
        }

        static float HashTo01(int cellSeed, int index, int salt)
        {
            unchecked
            {
                int h = cellSeed ^ index * 668265263 ^ salt * 374761393;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                if (h < 0)
                {
                    h = -h;
                }

                return (h % 10000) / 10000f;
            }
        }

        Sprite PickSprite(int cellSeed, int clusterIndex)
        {
            if (SpriteVariants == null || SpriteVariants.Length == 0)
            {
                return null;
            }

            int idx = Mathf.Abs(Hash(cellSeed, clusterIndex, 17)) % SpriteVariants.Length;
            return SpriteVariants[idx];
        }

        static int Hash(int cellSeed, int index, int salt)
        {
            unchecked
            {
                int h = cellSeed ^ index * 668265263 ^ salt * 374761393;
                h = (h ^ (h >> 13)) * 1274126177;
                return h;
            }
        }
    }
}
