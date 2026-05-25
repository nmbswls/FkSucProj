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

            EnsureNodeViewContainer();
            WireCloseButton();
        }

        void EnsureNodeViewContainer()
        {
            if (NodeViewContainer != null)
            {
                return;
            }

            var treeRoot = transform.Find("TreeRoot");
            if (treeRoot == null)
            {
                return;
            }

            NodeViewContainer = treeRoot.Find("Scroll View/Viewport/Content")
                               ?? treeRoot.Find("Scroll/Viewport/Content")
                               ?? treeRoot.Find("Viewport/Content")
                               ?? treeRoot;
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

            EnsureNodeViewContainer();
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
            EnsureNodeViewContainer();
            RefreshFromRuntime();
        }

        public void RefreshFromRuntime()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            var progression = glm?.playerDataManager?.ProgressionSystem;

            EnsureNodeViewContainer();
            if (NodeViewContainer != null)
            {
                for (int i = 0; i < NodeViewContainer.childCount; i++)
                {
                    var one = NodeViewContainer.GetChild(i);
                    var binder = one.GetComponent<TalentTreeNodeBinder>();
                    if (binder == null)
                    {
                        continue;
                    }

                    var view = one.GetComponentInChildren<TalentTreeNodeView>(true);
                    if (view == null)
                    {
                        Debug.LogWarning($"[TalentTreePanel] Missing view for node binder on {one.name}");
                        continue;
                    }

                    view.Refresh(progression, binder.TalentNodeId);
                    NodeViewMap[binder.TalentNodeId] = view;
                }
            }

            if (debugTipText != null)
            {
                debugTipText.text = progression != null
                    ? "黄色节点可解锁或升级；灰色按钮表示条件未满足或已满级。"
                    : "未进入游戏上下文，无法读取养成数据。";
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
