using UnityEngine;

namespace My.UI.CultTech
{
    public sealed class CultTechNodeBinder : MonoBehaviour
    {
        [SerializeField] int cultNodeId;

        public int CultNodeId => cultNodeId;

        public void SetNodeIdForEditor(int nodeId)
        {
            cultNodeId = nodeId;
        }
    }
}