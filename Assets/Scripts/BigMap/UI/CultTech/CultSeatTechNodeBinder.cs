using UnityEngine;

namespace My.UI.CultTech
{
    public sealed class CultSeatTechNodeBinder : MonoBehaviour
    {
        [SerializeField] int seatTechNodeId;
        public int SeatTechNodeId => seatTechNodeId;
        public void SetNodeIdForEditor(int nodeId) => seatTechNodeId = nodeId;
    }
}
