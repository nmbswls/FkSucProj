using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Talent
{
    public sealed class TalentTreePanel : PanelBase
    {
        public const string Pid = "TalentTreePanel";

        IPlayerProgressionHubHost _progressionHubHost;

        [SerializeField] TalentTreeNodeView[] nodeViews;
        [SerializeField] Button closeButton;
        [SerializeField] TextMeshProUGUI debugTipText;

        public static TalentTreePanel Open()
        {
            PlayerProgressionHubPanel.OpenTalents();
            var hubMono = UIManager.Instance.GetShowingPanel(PlayerProgressionHubPanel.Pid) as MonoBehaviour;
            return hubMono != null ? hubMono.GetComponentInChildren<TalentTreePanel>(true) : null;
        }

        public static void Toggle()
        {
            PlayerProgressionHubPanel.ToggleTalents();
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
            WireCloseButton();
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            WireCloseButton();
        }

        void WireCloseButton()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
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
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
            }
            else
            {
                UIManager.Instance.HidePanel(Pid);
            }
        }
    }
}
