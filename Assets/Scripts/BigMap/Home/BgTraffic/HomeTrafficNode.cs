using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class HomeTrafficNode : MonoBehaviour
    {
        // 只有两种：普通路点，或者端点（出入口合一）
        public enum NodeType { Normal, EndPoint }

        public NodeType Type = NodeType.Normal;

        [Header("连接的节点（双向路记得互相拖）")]
        public List<HomeTrafficNode> NextNodes = new List<HomeTrafficNode>();

        void OnDrawGizmos()
        {
            // 端点显示为红色，普通点显示为黄色
            Gizmos.color = Type == NodeType.EndPoint ? Color.red : Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.3f);

            Gizmos.color = Color.white;
            foreach (var node in NextNodes)
            {
                if (node != null)
                {
                    Gizmos.DrawLine(transform.position, node.transform.position);
                }
            }
        }
    }
}


