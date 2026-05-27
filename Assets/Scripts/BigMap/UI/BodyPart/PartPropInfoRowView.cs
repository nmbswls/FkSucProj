using TMPro;
using UnityEngine;

namespace My.UI.BodyPart
{
    // InfoRow_Template：单行文本（部位 Local 属性等）
    public sealed class PartPropInfoRowView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI titleText;

        public void Bind(string text)
        {
            if (titleText != null)
            {
                titleText.text = text ?? string.Empty;
            }
        }
    }
}
