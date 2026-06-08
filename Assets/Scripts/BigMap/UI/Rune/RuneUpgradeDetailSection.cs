using My.Config;
using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeDetailSection : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI upgradeTitle;
        [SerializeField] TextMeshProUGUI upgradeBody;
        [SerializeField] TextMeshProUGUI lockHint;

        public void Clear()
        {
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
        }

        public void ShowUpgrade(string upgradeId, PlayerRuneSystem runeSystem)
        {
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

            if (lockHint == null)
            {
                return;
            }

            if (runeSystem == null)
            {
                lockHint.text = string.Empty;
                return;
            }

            var state = runeSystem.GetUpgradeNodeState(upgradeId);
            if (RuneUpgradeCatalog.IsInitialUpgrade(def) && runeSystem.OwnsRune(def.BaseRuneId))
            {
                state = ERuneUpgradeNodeState.Unlocked;
            }

            lockHint.text = state switch
            {
                ERuneUpgradeNodeState.Unlocked => RuneUpgradeCatalog.IsInitialUpgrade(def)
                    ? "初始效果（已拥有）"
                    : "已解锁",
                ERuneUpgradeNodeState.Available => "使用对应道具解锁",
                _ => "暂不可解锁",
            };
        }
    }
}
