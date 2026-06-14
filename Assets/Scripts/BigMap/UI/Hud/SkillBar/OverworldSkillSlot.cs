using My.Map;
using My.Map.Entity;
using My.Player;
using UnityEngine;

namespace My.UI
{
    // 大地图底部技能栏的具体技能槽。
    // 在 SkillSlotBase（config 层展示）基础上，维护运行时状态：
    //   runtime 引用、可用性判断、hint 光效。
    // 按键提示由 OverworldMainBottomBar 通过 SlotKeyHintView 统一拼装，此类不再持有。
    public class OverworldSkillSlot : SkillSlotBase, IHighlightableObj
    {
        static readonly Color UsableColor = Color.white;
        static readonly Color DeniedColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);

        public GameObject Outline;

        OverworldMainBottomBar _bar;
        int _barSlotIndex;
        int _skillSourceIndex;

        SkillRuntime _skillRuntime;

        // 由 MainBottomBarSlotWrapper 在 Init 时调用
        public void SetupForBar(OverworldMainBottomBar bar, int barSlotIndex, int skillSourceIndex)
        {
            _bar = bar;
            _barSlotIndex = barSlotIndex;
            _skillSourceIndex = skillSourceIndex;

            if (emptyIcon != null)
            {
                emptyIcon.gameObject.SetActive(false);
            }

            if (icon != null)
            {
                icon.gameObject.SetActive(true);
            }

            SetLockOverlay(false);
        }

        protected override void OnClick()
        {
            _bar?.OnSkillSlotClicked(_barSlotIndex, _skillSourceIndex);
        }

        public void Refresh(bool hint, string[] showSkills, PlayerLogicEntity player, bool humanQuickBar)
        {
            if (showSkills == null || _skillSourceIndex < 0 || _skillSourceIndex >= showSkills.Length)
            {
                Clear();
                return;
            }

            string skillName = showSkills[_skillSourceIndex];
            if (string.IsNullOrEmpty(skillName))
            {
                Clear();
                return;
            }

            // 优先从 player runtime 绑定（含冷却等实时状态）
            if (player != null
                && player.ablilityManager.SkillRuntimes.TryGetValue(skillName, out var rt)
                && rt != null)
            {
                BindRuntime(rt, hint, player);
            }
            else
            {
                // runtime 不存在时退回 config 层展示
                _skillRuntime = null;
                BindByConfig(skillName);
                RefreshUsability(null, skillName);

                if (hint)
                {
                    PlayShine();
                }
            }
        }

        void BindRuntime(SkillRuntime runtime, bool hint, PlayerLogicEntity player)
        {
            _skillRuntime = runtime;

            Sprite sprite = null;
            if (runtime.cacheConfig != null && !string.IsNullOrEmpty(runtime.cacheConfig.IconPath))
            {
                sprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{runtime.cacheConfig.IconPath}");
            }

            if (sprite == null)
            {
                sprite = SimpleResManager.Load<Sprite>("Sprites/Skill/fallback");
            }

            ApplyIcon(sprite);
            ConfigureHover(runtime.cacheConfig?.SkillId);
            RefreshUsability(player, runtime.cacheConfig?.SkillId);

            if (hint)
            {
                PlayShine();
            }
        }

        void RefreshUsability(PlayerLogicEntity player, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                SetIconTint(UsableColor);
                SetLockOverlay(false);
                return;
            }

            if (player == null)
            {
                player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            }

            if (player == null)
            {
                SetIconTint(UsableColor);
                SetLockOverlay(false);
                return;
            }

            bool canUse = SkillCastConditionUtil.TryEvaluateReadiness(
                player, player.ablilityManager, skillId, out _);
            SetIconTint(canUse ? UsableColor : DeniedColor);
            SetLockOverlay(!canUse);
        }

        public void SetHighlightStatus(bool isHighlight)
        {
            if (Outline != null)
            {
                Outline.SetActive(isHighlight);
            }
        }
    }
}
