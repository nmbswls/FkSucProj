using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeDetailSection : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI upgradeTitle;
        [SerializeField] TextMeshProUGUI upgradeBody;
        [SerializeField] Button unlockButton;
        [SerializeField] TextMeshProUGUI unlockButtonLabel;
        [SerializeField] TextMeshProUGUI lockHint;

        string _currentUpgradeId;
        RuneLoadoutPanel _hostPanel;

        void Awake()
        {
            if (unlockButton != null)
            {
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        public void SetHostPanel(RuneLoadoutPanel hostPanel)
        {
            _hostPanel = hostPanel;
        }

        public void Clear()
        {
            _currentUpgradeId = null;
            if (upgradeTitle != null)
            {
                upgradeTitle.text = string.Empty;
            }

            if (upgradeBody != null)
            {
                upgradeBody.text = string.Empty;
            }

            if (lockHint != null)
            {
                lockHint.text = string.Empty;
            }

            SetUnlockButtonVisible(false);
        }

        public void ShowUpgrade(string upgradeId, PlayerRuneSystem runeSystem)
        {
            _currentUpgradeId = upgradeId;
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null)
            {
                Clear();
                return;
            }

            if (upgradeTitle != null)
            {
                upgradeTitle.text = def.Name ?? string.Empty;
            }

            if (upgradeBody != null)
            {
                upgradeBody.text = def.Desc ?? string.Empty;
            }

            if (runeSystem == null)
            {
                SetUnlockButtonVisible(false);
                return;
            }

            var state = runeSystem.GetUpgradeNodeState(upgradeId, out var lockReason);
            if (RuneUpgradeCatalog.IsInitialUpgrade(def) && runeSystem.OwnsRune(def.BaseRuneId))
            {
                state = ERuneUpgradeNodeState.Unlocked;
            }

            switch (state)
            {
                case ERuneUpgradeNodeState.Available:
                    if (lockHint != null)
                    {
                        lockHint.text = string.Empty;
                    }

                    SetUnlockButtonVisible(true);
                    if (unlockButtonLabel != null)
                    {
                        unlockButtonLabel.text = "解锁";
                    }

                    if (unlockButton != null)
                    {
                        unlockButton.interactable = true;
                    }
                    break;
                case ERuneUpgradeNodeState.Unlocked:
                    if (lockHint != null)
                    {
                        lockHint.text = RuneUpgradeCatalog.IsInitialUpgrade(def) ? "初始效果（已拥有）" : "已解锁";
                    }

                    SetUnlockButtonVisible(true);
                    if (unlockButtonLabel != null)
                    {
                        unlockButtonLabel.text = "已解锁";
                    }

                    if (unlockButton != null)
                    {
                        unlockButton.interactable = false;
                    }
                    break;
                default:
                    if (lockHint != null)
                    {
                        lockHint.text = TranslateLockReason(lockReason);
                    }

                    SetUnlockButtonVisible(false);
                    break;
            }
        }

        void OnUnlockClicked()
        {
            if (string.IsNullOrEmpty(_currentUpgradeId))
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null)
            {
                return;
            }

            if (pdm.TryUnlockRuneUpgrade(_currentUpgradeId, out var failReason))
            {
                _hostPanel?.RefreshAll();
                return;
            }

            if (lockHint != null)
            {
                lockHint.text = TranslateLockReason(failReason);
            }
        }

        void SetUnlockButtonVisible(bool visible)
        {
            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(visible);
            }
        }

        static string TranslateLockReason(string reason)
        {
            return reason switch
            {
                "invalid_upgrade" => "无效升级项",
                "already_unlocked" => "已解锁",
                "base_rune_not_owned" => "尚未拥有对应符文",
                "prerequisite_missing" => "前置升级未满足",
                _ => string.IsNullOrEmpty(reason) ? "暂不可解锁" : reason,
            };
        }
    }
}
