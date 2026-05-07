using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Talent
{
    public sealed class TalentTreePanel : PanelBase
    {
        public const string Pid = "TalentTreePanel";

        [SerializeField] TalentTreeNodeView[] nodeViews;
        [SerializeField] Button closeButton;
        [SerializeField] TextMeshProUGUI debugTipText;

        public static TalentTreePanel Open()
        {
            return UIManager.Instance.ShowPanel(Pid) as TalentTreePanel;
        }

        public static void Toggle()
        {
            if (UIManager.Instance.IsPanelVisible(Pid))
            {
                UIManager.Instance.HidePanel(Pid);
                return;
            }

            Open();
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
                closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            RefreshFromRuntime();
        }

        public override void Show()
        {
            base.Show();
            RefreshFromRuntime();
        }

        public void RefreshFromRuntime()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            var progression = glm?.playerDataManager?.ProgressionSystem;

            if (nodeViews != null)
            {
                for (int i = 0; i < nodeViews.Length; i++)
                {
                    if (nodeViews[i] != null)
                    {
                        nodeViews[i].Refresh(progression);
                    }
                }
            }

            if (debugTipText != null)
            {
                debugTipText.text = progression != null
                    ? "Click yellow nodes to unlock. First node has no cost."
                    : "No progression (not in game context).";
            }
        }

        void OnCloseClicked()
        {
            UIManager.Instance.HidePanel(Pid);
        }
    }
}
