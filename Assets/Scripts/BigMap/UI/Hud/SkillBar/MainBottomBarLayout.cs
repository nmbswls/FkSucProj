using System.Collections.Generic;
using My.Map;
using My.Player;

namespace My.UI
{
    public enum MainBottomBarSlotKind
    {
        Weapon,
        Skill,
    }

    // 单个槽位定义：类型 + 在武器/技能数据源中的索引
    public struct MainBottomBarSlotDef
    {
        public MainBottomBarSlotKind Kind;
        public int SourceIndex;

        public MainBottomBarSlotDef(MainBottomBarSlotKind kind, int sourceIndex)
        {
            Kind = kind;
            SourceIndex = sourceIndex;
        }
    }

    // 不同模式下底部栏的武器/技能槽组合与映射
    public static class MainBottomBarLayout
    {
        public static List<MainBottomBarSlotDef> Build(GameLogicManager glm, PlayerSystemManager pdm)
        {
            var layout = new List<MainBottomBarSlotDef>();
            if (glm == null || pdm == null)
            {
                return layout;
            }

            var showSkills = pdm.GetSkillSlotsByState();
            int skillCount = showSkills?.Length ?? 0;

            // 应用发情技能条
            if(pdm.IsUsingFaQingSkillBar())
            {
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 0));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 1));

                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 3));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 4));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 5));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 6));
                return layout;
            }

            // 人类快捷栏：武器槽在前，技能槽在后（同尺寸横向排列）
            if (glm.IsHumanQuickBarAvailable())
            {
                if (OverworldHUDPanel.ShouldUseAttachStruggleSkill())
                {
                    layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 0));
                }
                else
                {
                    layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Weapon, 0));
                }
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Weapon, 1));

                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 3));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 4));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 5));
                layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 6));
                return layout;
            }

            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 0));
            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 1));

            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 3));
            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 4));
            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 5));
            layout.Add(new MainBottomBarSlotDef(MainBottomBarSlotKind.Skill, 6));

            return layout;
        }

        // 根据槽在 bar 中的位置返回按键提示文字
        public static string GetKeyHintText(IReadOnlyList<MainBottomBarSlotDef> layout, int barSlotIndex)
        {
            if (barSlotIndex < 0 || barSlotIndex >= layout.Count)
            {
                return "";
            }

            var def = layout[barSlotIndex];
            if (OverworldHUDPanel.ShouldUseAttachStruggleSkill()
                && def.Kind == MainBottomBarSlotKind.Skill
                && def.SourceIndex == 0)
            {
                return "左键";
            }

            if (def.Kind == MainBottomBarSlotKind.Weapon)
            {
                return def.SourceIndex == 0 ? "左键" : "右键";
            }

            // 统计前置武器槽数量，确定技能编号从 1 开始
            int weaponCount = 0;
            for (int i = 0; i < layout.Count; i++)
            {
                if (layout[i].Kind == MainBottomBarSlotKind.Weapon) weaponCount++;
                else break;
            }

            return (barSlotIndex - weaponCount + 1).ToString();
        }

        public static int GetBarMode(GameLogicManager glm, PlayerSystemManager pdm)
        {
            if (pdm == null)
            {
                return 0;
            }

            if (pdm.IsUsingFaQingSkillBar())
            {
                return 2;
            }

            if (glm != null && glm.IsHumanQuickBarAvailable())
            {
                return 1;
            }

            return 0;
        }

        public static int ComputeLayoutSignature(IReadOnlyList<MainBottomBarSlotDef> layout, int barMode)
        {
            int sig = barMode * 1009;
            sig = sig * 31 + layout.Count;
            for (int i = 0; i < layout.Count; i++)
            {
                sig = sig * 31 + (int)layout[i].Kind;
                sig = sig * 17 + layout[i].SourceIndex;
            }

            return sig;
        }
    }
}
