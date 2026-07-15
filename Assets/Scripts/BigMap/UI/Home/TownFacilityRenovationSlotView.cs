using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Home
{
    // 详情面板内单个改造项槽位；在 prefab 中预置，运行时只刷新文案与样式。
    public sealed class TownFacilityRenovationSlotView : MonoBehaviour
    {
        public Button Button;
        public TextMeshProUGUI Label;
        public TextMeshProUGUI Desc;
    }
}
