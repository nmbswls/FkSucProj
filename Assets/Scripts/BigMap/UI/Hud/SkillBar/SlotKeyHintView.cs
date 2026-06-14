using TMPro;
using UnityEngine;

namespace My.UI
{
    // 底部栏槽位按键提示徽章，由 OverworldMainBottomBar 在运行时拼装到每个槽。
    public class SlotKeyHintView : MonoBehaviour
    {
        public TextMeshProUGUI label;

        public void SetText(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
