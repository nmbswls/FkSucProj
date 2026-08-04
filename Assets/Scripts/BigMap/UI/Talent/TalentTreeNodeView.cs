using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Talent
{
    // 与 Luban TalentNode.node_id 对应；布局/连线由预制体内摆放
    public sealed class TalentTreeNodeView : MonoBehaviour
    {
        public int talentNodeId { get; private set; }
        [SerializeField] Button selectButton;
        [SerializeField] Button unlockButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Image selectionFrame;
        [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] Color unlockableColor = new Color(0.9f, 0.75f, 0.2f, 1f);
        [SerializeField] Color unlockedColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI unlockButtonText;
        [SerializeField] TextMeshProUGUI descText;

        TalentTreeView _host;
        TalentNodeHoverProvider _hoverProvider;
        ITalentProgressionContext _progression;

        public int TalentNodeId => talentNodeId;

        void Awake()
        {
            _host = GetComponentInParent<TalentTreeView>();
            _hoverProvider = GetComponent<TalentNodeHoverProvider>();
            if (_hoverProvider == null)
            {
                _hoverProvider = gameObject.AddComponent<TalentNodeHoverProvider>();
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectClicked);
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveListener(OnUnlockClicked);
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        public void BindHost(TalentTreeView host)
        {
            _host = host;
        }

        public void Refresh(ITalentProgressionContext progression, int nodeId, bool isSelected = false)
        {
            _progression = progression;
            talentNodeId = nodeId;
            _hoverProvider?.Configure(nodeId, progression);
            var row = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(talentNodeId);
            bool placeholder = row == null || row.MaxLevel <= 0;
            int cur = progression != null ? progression.GetTalentNodeLevel(talentNodeId) : 0;
            int max = row != null && row.MaxLevel > 0 ? row.MaxLevel : 0;

            if (titleText != null)
            {
                string name = row != null && !string.IsNullOrEmpty(row.DisplayName)
                    ? row.DisplayName
                    : placeholder ? "预留节点" : $"Node {talentNodeId}";
                titleText.text = name;
            }

            if (levelText != null)
            {
                levelText.text = placeholder ? "未开放" : $"Lv{cur}/{max}";
            }

            if (descText != null)
            {
                descText.text = placeholder ? "该节点尚未开放" : string.Empty;
            }

            if (placeholder)
            {
                ApplyVisual(PlayerTalentManager.TalentNodeVisualState.Locked, false, "未开放");
                SetSelected(false);
                if (selectButton != null) selectButton.interactable = false;
                if (unlockButton != null) unlockButton.interactable = false;
                return;
            }

            if (selectButton != null) selectButton.interactable = true;

            if (progression == null)
            {
                ApplyVisual(PlayerTalentManager.TalentNodeVisualState.Locked, false, "解锁");
                SetSelected(false);
                return;
            }

            var st = progression.GetTalentNodeVisualState(talentNodeId);
            bool canClick = st == PlayerTalentManager.TalentNodeVisualState.Unlockable;
            string btnText = cur <= 0 ? "解锁" : (cur < max ? "升级" : "满级");
            ApplyVisual(st, canClick, btnText);
            SetSelected(isSelected);
        }

        public void SetSelected(bool selected)
        {
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(selected);
            }
        }

        void ApplyVisual(PlayerTalentManager.TalentNodeVisualState st, bool canClick, string btnText)
        {
            if (nodeBackground != null)
            {
                nodeBackground.color = st switch
                {
                    PlayerTalentManager.TalentNodeVisualState.Unlocked => unlockedColor,
                    PlayerTalentManager.TalentNodeVisualState.Unlockable => unlockableColor,
                    _ => lockedColor,
                };
            }

            if (unlockButtonText != null)
            {
                unlockButtonText.text = btnText;
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = canClick;
            }
        }

        void OnSelectClicked()
        {
            if (_host != null && talentNodeId > 0)
            {
                _host.SelectNode(talentNodeId);
            }
        }

        void OnUnlockClicked()
        {
            if (_progression == null)
            {
                Debug.LogWarning("TalentTree: progression unavailable.");
                return;
            }

            string reason = null;
            if (_host == null || !_host.TryUpgradeNode(talentNodeId, out reason))
            {
                Debug.LogWarning("Talent upgrade failed: " + reason);
            }

        }
    }
}
