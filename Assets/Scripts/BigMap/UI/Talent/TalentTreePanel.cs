using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Talent
{
    public sealed class TalentTreePanel : PanelBase, IPlayerProgressionHubPage
    {
        public const string Pid = "TalentTreePanel";

        IPlayerProgressionHubHost _progressionHubHost;

        [SerializeField] Button closeButton;
        [SerializeField] TalentTreeView treeView;

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

            EnsureTreeView();
            WireCloseButton();
        }

        void EnsureTreeView()
        {
            if (treeView != null)
            {
                return;
            }
            treeView = GetComponent<TalentTreeView>();
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

            EnsureTreeView();
            treeView?.Bind(TalentTreeView.PlayerMainTreeId, ResolveProgression());
        }

        public override void Show()
        {
            base.Show();
            RefreshFromRuntime();
        }

        public void OnNodeSelected(int nodeId)
        {
            treeView?.SelectNode(nodeId);
        }

        public void RefreshFromRuntime()
        {
            EnsureTreeView();
            treeView?.Refresh();
        }

        static PlayerProgressionSystem ResolveProgression()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            return glm?.playerDataManager?.ProgressionSystem;
        }

        void OnCloseClicked()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return;
            }

            Debug.LogError("[TalentTreePanel] Not hosted by PlayerProgressionHubPanel.");
        }
    }
}
