using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 剪贴板上的单条知识便签（prefab 内 template）
    public sealed class KnowledgeClipboardNoteView : MonoBehaviour
    {
        [SerializeField] Image paperImage;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;

        static readonly Color[] PaperTints =
        {
            new Color(0.96f, 0.93f, 0.84f, 1f),
            new Color(0.93f, 0.95f, 0.88f, 1f),
            new Color(0.97f, 0.90f, 0.86f, 1f),
            new Color(0.90f, 0.93f, 0.96f, 1f),
        };

        public void Bind(string title, string body, int styleSeed)
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (bodyText != null)
            {
                bodyText.text = body ?? string.Empty;
            }

            if (paperImage != null)
            {
                paperImage.color = PaperTints[Mathf.Abs(styleSeed) % PaperTints.Length];
            }
        }
    }
}
