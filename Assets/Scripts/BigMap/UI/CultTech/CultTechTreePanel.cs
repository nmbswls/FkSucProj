using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultTechTreePanel : PanelBase
    {
        public const string Pid = "CultTechTreePanel";

        public static CultTechTreePanel Open()
        {
            return UIManager.Instance.ShowPanel(Pid) as CultTechTreePanel;
        }

        [SerializeField] Button closeButton;
        [SerializeField] CultTechTreeView treeView;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            if (treeView == null)
            {
                treeView = GetComponentInChildren<CultTechTreeView>(true);
            }

            EnsureCloseButton();
        }

        void EnsureCloseButton()
        {
            if (closeButton == null)
            {
                var existing = transform.Find("CloseButton");
                if (existing != null)
                {
                    closeButton = existing.GetComponent<Button>();
                }
            }

            if (closeButton == null)
            {
                var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-18f, -18f);
                rect.sizeDelta = new Vector2(42f, 42f);
                go.GetComponent<Image>().color = new Color(0.22f, 0.1f, 0.14f, 1f);
                closeButton = go.GetComponent<Button>();

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "X";
                label.fontSize = 20f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            treeView?.Bind(ResolveDemonCult());
        }

        public override void Show()
        {
            base.Show();
            treeView?.Refresh();
        }

        static DemonCultSystem ResolveDemonCult()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            return glm?.playerDataManager?.ProgressionSystem?.DemonCult;
        }
    }
}
