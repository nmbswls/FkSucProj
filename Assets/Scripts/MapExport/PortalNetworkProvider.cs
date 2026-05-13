using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.MapExport
{
    // 场景内可复用的路点/传送巡逻网：节点由 Transform 列表提供，边通过一对 Transform 装配（无向）
    public class PortalNetworkProvider : MonoBehaviour
    {
        [SerializeField]
        string networkId;

        [Tooltip("导出时使用 GameObject.name 作为 node_id，同一网络内需唯一")]
        [SerializeField]
        List<Transform> nodes = new();

        [Serializable]
        public class EdgeBinding
        {
            public Transform a;
            public Transform b;
            [Tooltip("0 表示运行时可按几何距离推导")]
            public float weight;
        }

        [SerializeField]
        List<EdgeBinding> edges = new();

        public string NetworkId => string.IsNullOrEmpty(networkId) ? gameObject.name : networkId;

        public IReadOnlyList<Transform> Nodes => nodes;

        public IReadOnlyList<EdgeBinding> Edges => edges;

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (nodes == null || edges == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.9f);
            foreach (var e in edges)
            {
                if (e?.a == null || e?.b == null)
                {
                    continue;
                }

                Gizmos.DrawLine(e.a.position, e.b.position);
            }

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            foreach (var t in nodes)
            {
                if (t == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(t.position, 0.08f);
            }
        }
#endif
    }
}
