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
        [SerializeField] Button unlockButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] Color unlockableColor = new Color(0.9f, 0.75f, 0.2f, 1f);
        [SerializeField] Color unlockedColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI unlockButtonText;
        [SerializeField] TextMeshProUGUI descText;

        TalentTreePanel _host;

        public int TalentNodeId => talentNodeId;

        void Awake()
        {
            _host = GetComponentInParent<TalentTreePanel>();
            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveListener(OnUnlockClicked);
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        public void Refresh(PlayerProgressionSystem progression, int nodeId)
        {
            talentNodeId = nodeId;
            var row = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(talentNodeId);
            int cur = progression != null ? progression.GetTalentNodeLevel(talentNodeId) : 0;
            int max = row != null ? row.MaxLevel : 1;

            if (titleText != null)
            {
                string name = row != null && !string.IsNullOrEmpty(row.DisplayName)
                    ? row.DisplayName
                    : $"Node {talentNodeId}";
                titleText.text = name;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv{cur}/{max}";
            }

            if (descText != null)
            {
                descText.text = string.Empty;
            }

            if (progression == null)
            {
                ApplyVisual(PlayerTalentManager.TalentNodeVisualState.Locked, false, "解锁");
                return;
            }

            var st = progression.GetTalentNodeVisualState(talentNodeId);
            bool canClick = st == PlayerTalentManager.TalentNodeVisualState.Unlockable;
            string btnText = cur <= 0 ? "解锁" : (cur < max ? "升级" : "满级");
            ApplyVisual(st, canClick, btnText);
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

        void OnUnlockClicked()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            var progression = glm?.playerDataManager?.ProgressionSystem;
            if (progression == null)
            {
                Debug.LogWarning("TalentTree: progression unavailable.");
                return;
            }

            if (!progression.TryUpgradeTalentNode(talentNodeId, out var reason))
            {
                Debug.LogWarning("Talent upgrade failed: " + reason);
            }

            _host?.RefreshFromRuntime();
        }
    }
}
