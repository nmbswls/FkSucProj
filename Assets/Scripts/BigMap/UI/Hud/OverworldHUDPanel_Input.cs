using My.Input;
using UnityEngine;

namespace My.UI
{
    public partial class OverworldHUDPanel
    {
        public override int FocusPriority => 0;

        public bool OnConfirm() => false;

        public bool OnCancel() => false;

        public bool OnNavigate(Vector2 dir) => false;

        public bool CapturesNavigateAxisForWorld => false;

        public bool OnHotkey(string keyName)
        {
            if (HudMode == EHudMode.Normal)
            {
                return PeeviewUseSkillByKey(keyName);
            }

            return false;
        }

        public bool OnScroll(float deltaY)
        {
            if (HudMode != EHudMode.Normal)
            {
                return false;
            }

            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable() || Mathf.Abs(deltaY) < 0.01f)
            {
                return false;
            }

            lgm.playerDataManager.HumanQuickBar.CycleConsumableSelection(deltaY > 0f ? 1 : -1);
            return true;
        }

        public bool OnHoldStart(string holdKey) => false;

        public bool OnHoldUpdate(string holdKey)
        {
            if (HudMode == EHudMode.Normal)
            {
                var skillId = GetSkillIdByKey(holdKey);
                if (!string.IsNullOrEmpty(skillId))
                {
                    MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHold(skillId);
                }
            }

            return false;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            if (HudMode == EHudMode.Normal)
            {
                var skillId = GetSkillIdByKey(holdKey);
                if (!string.IsNullOrEmpty(skillId))
                {
                    MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHoldEnd(skillId);
                }
            }
            return false;
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            if (HudMode == EHudMode.Normal)
            {
                if (button == 0)
                {
                    PeeviewUseSkillByKey(EInputKey.MouseLeft.ToString());
                }
                else if (button == 1)
                {
                    PeeviewUseSkillByKey(EInputKey.MouseRight.ToString());
                }
            }
            else if (HudMode == EHudMode.PreviewSkill)
            {
                if (button == 0)
                {
                    overworldSkillPreviewUI.ConfirmSkillCast(mousePos);
                }
                else if (button == 1)
                {
                    CancelSkillCast();
                }
            }
            return false;
        }
    }
}
