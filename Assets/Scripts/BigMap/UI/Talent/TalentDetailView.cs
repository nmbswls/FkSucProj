using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentDetailView : MonoBehaviour
    {
        [SerializeField] GameObject contentRoot;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI detailStatusHint;

        public void Clear()
        {
            SetVisible(false);
            SetText(detailTitle, string.Empty);
            SetText(detailBody, string.Empty);
            SetText(detailStatusHint, string.Empty);
        }

        public void ShowNode(int nodeId, PlayerProgressionSystem progression)
        {
            if (nodeId <= 0 || !TalentNodeDisplayHelper.TryBuildSnapshot(nodeId, progression, out var snapshot))
            {
                Clear();
                return;
            }

            SetVisible(true);
            SetText(detailTitle, TalentNodeDisplayHelper.GetDisplayName(snapshot));
            SetText(detailBody, TalentNodeDisplayHelper.BuildDetailBody(snapshot, progression));
            SetText(detailStatusHint, TalentNodeDisplayHelper.BuildDetailStatusHint(snapshot, progression));
        }

        void Awake()
        {
            EnsureReferences();
        }

        void EnsureReferences()
        {
            if (contentRoot == null)
            {
                contentRoot = gameObject;
            }

            if (detailTitle == null)
            {
                detailTitle = transform.Find("DetailTitle")?.GetComponent<TextMeshProUGUI>();
            }

            if (detailBody == null)
            {
                detailBody = transform.Find("DetailBody")?.GetComponent<TextMeshProUGUI>();
            }

            if (detailStatusHint == null)
            {
                detailStatusHint = transform.Find("DetailStatusHint")?.GetComponent<TextMeshProUGUI>();
            }
        }

        void SetVisible(bool visible)
        {
            if (contentRoot != null)
            {
                contentRoot.SetActive(visible);
                return;
            }

            gameObject.SetActive(visible);
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
