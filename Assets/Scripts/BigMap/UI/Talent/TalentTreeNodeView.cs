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
        [SerializeField] int talentNodeId;
        [SerializeField] Button unlockButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] Color unlockableColor = new Color(0.9f, 0.75f, 0.2f, 1f);
        [SerializeField] Color unlockedColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        [SerializeField] TextMeshProUGUI titleText;

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

        public void Refresh(PlayerProgressionSystem progression)
        {
            if (CfgMgr.Cfgs?.TbTalentNode != null)
            {
                var row = CfgMgr.Cfgs.TbTalentNode.GetOrDefault(talentNodeId);
                if (titleText != null)
                {
                    titleText.text = row != null && !string.IsNullOrEmpty(row.DisplayName)
                        ? row.DisplayName
                        : $"Node {talentNodeId}";
                }
            }

            if (progression == null)
            {
                ApplyVisual(PlayerTalentManager.TalentNodeVisualState.Locked, false);
                return;
            }

            var st = progression.GetTalentNodeVisualState(talentNodeId);
            bool canClick = st == PlayerTalentManager.TalentNodeVisualState.Unlockable;
            ApplyVisual(st, canClick);
        }

        void ApplyVisual(PlayerTalentManager.TalentNodeVisualState st, bool canClick)
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

            if (!progression.TryUnlockTalentNode(talentNodeId, out var reason))
            {
                Debug.LogWarning("Talent unlock failed: " + reason);
            }

            _host?.RefreshFromRuntime();
        }
    }
}
