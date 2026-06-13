using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Player;

namespace My.UI.SkillLoadout
{
    public static class SkillLoadoutSlotTypeUtil
    {
        public static ESkillLoadoutSlotType ResolveSlotType(SkillLearnEntry entry, string skillId = null)
        {
            if (entry != null && entry.LoadoutSlotType != ESkillLoadoutSlotType.None)
            {
                return entry.LoadoutSlotType;
            }

            string id = entry?.SkillId ?? skillId;
            if (string.IsNullOrEmpty(id))
            {
                return ESkillLoadoutSlotType.None;
            }

            var cfg = SkillLibrary.GetSkillConfig(id);
            if (cfg != null && cfg.IsPassive)
            {
                return ESkillLoadoutSlotType.Passive;
            }

            return ESkillLoadoutSlotType.OtherActive;
        }

        public static bool CanAssignToSlot(
            ESkillLoadoutSlotType slotType,
            SkillLoadoutSlotKind slotKind,
            int slotIndex,
            out string failReason)
        {
            failReason = null;

            if (slotType == ESkillLoadoutSlotType.Passive)
            {
                if (slotKind != SkillLoadoutSlotKind.Passive)
                {
                    failReason = "wrong_skill_kind";
                    return false;
                }

                return slotIndex >= 0 && slotIndex < PlayerSkillSystem.PassiveSlotCount;
            }

            if (slotKind != SkillLoadoutSlotKind.Active)
            {
                failReason = "wrong_skill_kind";
                return false;
            }

            switch (slotType)
            {
                case ESkillLoadoutSlotType.LeftClick:
                    if (slotIndex != 0)
                    {
                        failReason = "wrong_slot_type";
                    }

                    break;
                case ESkillLoadoutSlotType.RightClick:
                    if (slotIndex != 1)
                    {
                        failReason = "wrong_slot_type";
                    }

                    break;
                case ESkillLoadoutSlotType.Dash:
                    if (slotIndex != 2)
                    {
                        failReason = "wrong_slot_type";
                    }

                    break;
                case ESkillLoadoutSlotType.OtherActive:
                    if (slotIndex < 3 || slotIndex > 7)
                    {
                        failReason = "wrong_slot_type";
                    }

                    break;
                default:
                    failReason = "unknown_slot_type";
                    return false;
            }

            return string.IsNullOrEmpty(failReason);
        }

        public static int ResolveTargetSlot(PlayerSkillSystem sys, ESkillLoadoutSlotType slotType, string skillId)
        {
            if (sys == null || string.IsNullOrEmpty(skillId))
            {
                return -1;
            }

            if (slotType == ESkillLoadoutSlotType.Passive)
            {
                for (int i = 0; i < sys.PassiveSkillSlots.Length; i++)
                {
                    if (string.Equals(sys.PassiveSkillSlots[i], skillId, System.StringComparison.Ordinal))
                    {
                        return i;
                    }
                }

                for (int i = 0; i < sys.PassiveSkillSlots.Length; i++)
                {
                    if (string.IsNullOrEmpty(sys.PassiveSkillSlots[i]))
                    {
                        return i;
                    }
                }

                return -1;
            }

            int fixedSlot = slotType switch
            {
                ESkillLoadoutSlotType.LeftClick => 0,
                ESkillLoadoutSlotType.RightClick => 1,
                ESkillLoadoutSlotType.Dash => 2,
                _ => -1,
            };

            if (fixedSlot >= 0)
            {
                return fixedSlot;
            }

            for (int i = 3; i <= 7; i++)
            {
                if (string.Equals(sys.NormalSkillSlots[i], skillId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            for (int i = 3; i <= 7; i++)
            {
                if (string.IsNullOrEmpty(sys.NormalSkillSlots[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string ResolveAssignFailTip(string reason)
        {
            return reason switch
            {
                "wrong_slot_type" => "该技能不能装配到此槽位",
                "wrong_skill_kind" => "技能类型与槽位不匹配",
                "not_in_school_pool" => "该技能不属于当前学派",
                "not_learned" => "尚未学习该技能",
                "duplicate" => "该技能已在其他槽位装配",
                "passive_full" => "被动槽已满，请先卸下一个被动技能",
                "active_full" => "可装配的技能槽已满，请先卸下一个技能",
                _ => string.IsNullOrEmpty(reason) ? "无法装配该技能" : reason,
            };
        }
    }
}
