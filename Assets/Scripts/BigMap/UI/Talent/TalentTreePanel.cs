using System.Collections.Generic;
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

        public bool IsHostedByHub => _progressionHubHost != null;

        public Transform NodeViewContainer;

        public Dictionary<int, TalentTreeNodeView> NodeViewMap { get; private set; } = new();
        [SerializeField] Button closeButton;
        [SerializeField] TextMeshProUGUI debugTipText;

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
            if (!IsHostedByHub)
            {
                Debug.LogError("[TalentTreePanel] Setup without hub host.");
                return;
            }

            RefreshFromRuntime();
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[TalentTreePanel] Show without hub host.");
                return;
            }

            base.Show();
            RefreshFromRuntime();
        }

        public void RefreshFromRuntime()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            var progression = glm?.playerDataManager?.ProgressionSystem;


            if (NodeViewContainer != null)
            {
                for(int i=0;i< NodeViewContainer.childCount;i++)
                {
                    var one = NodeViewContainer.GetChild(i);
                    var binder = one.GetComponent<TalentTreeNodeBinder>();
                    if(binder == null)
                    {
                        Debug.LogError("?");
                        continue;
                    }

                    var view = one.GetComponentInChildren<TalentTreeNodeView>();
                    if(view == null)
                    {
                        Debug.LogError("?");
                        continue;
                    }

                    view.Refresh(progression, binder.TalentNodeId);
                    NodeViewMap[binder.TalentNodeId] = view;
                }
            }

            if (debugTipText != null)
            {
                debugTipText.text = progression != null
                    ? "Click yellow nodes to unlock or upgrade. First node has no cost."
                    : "No progression (not in game context).";
            }
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
