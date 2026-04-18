using System.Collections.Generic;
using cfg.demo;
using My.Map;
using My.Map.Logic;
using SimpleJSON;
using UnityEngine;

namespace My.Dialog
{
    /// <summary>
    /// 运行时评估 DialogCondition（动态分支选项等）
    /// </summary>
    public static class DialogConditionRuntime
    {
        public static bool Evaluate(DialogCondition cond, LogicEntityBase srcEntity, My.GameLogicManager glm)
        {
            if (cond == null) return true;
            if (glm == null) return false;

            switch (cond)
            {
                case ConditionNpcInCombat c:
                    return EvaluateNpcInCombat(c, srcEntity);
                case ConditionNpcHasLocalSwitch c:
                    return EvaluateNpcLocalSwitch(c, srcEntity);
                case ConditionCommonCondsList c:
                    return EvaluateCommonCondsList(c, glm);
                case ConditionLocalVariableInt c:
                    return EvaluateLocalVariableInt(c, srcEntity);
                case ConditionLocalVariableString c:
                    return EvaluateLocalVariableString(c, srcEntity);
                case ConditionCheckPlayerLevel c:
                    return EvaluatePlayerLevel(c, glm);
                default:
                    Debug.LogWarning($"[Dialog] Unhandled condition type: {cond.GetType().Name}");
                    return false;
            }
        }

        public static bool AllPass(IReadOnlyList<DialogCondition> list, LogicEntityBase srcEntity, My.GameLogicManager glm)
        {
            if (list == null || list.Count == 0) return true;
            for (var i = 0; i < list.Count; i++)
            {
                if (!Evaluate(list[i], srcEntity, glm))
                    return false;
            }
            return true;
        }

        private static bool EvaluateNpcInCombat(ConditionNpcInCombat c, LogicEntityBase srcEntity)
        {
            var inCombat = srcEntity is NpcUnitLogicEntity n && n.IsInCombat;
            return c.RequireInCombat ? inCombat : !inCombat;
        }

        private static bool EvaluateNpcLocalSwitch(ConditionNpcHasLocalSwitch c, LogicEntityBase srcEntity)
        {
            if (string.IsNullOrEmpty(c.SwitchName)) return false;
            if (srcEntity == null) return false;
            return srcEntity.CheckLocalSwitch(c.SwitchName);
        }

        private static bool EvaluateCommonCondsList(ConditionCommonCondsList c, My.GameLogicManager glm)
        {
            if (c.Items == null || c.Items.Count == 0)
                return true;

            var list = new List<CommonCheckCond>(c.Items.Count);
            for (var i = 0; i < c.Items.Count; i++)
            {
                var s = c.Items[i];
                if (s == null) continue;
                list.Add(ToCfgCond(s));
            }

            return glm.CheckCommonCondsAll(list);
        }

        private static CommonCheckCond ToCfgCond(SerializableCommonCheckCond s)
        {
            var o = new JSONObject();
            o["type"] = new JSONNumber((int)s.Type);
            o["param1"] = new JSONNumber(s.Param1);
            o["param2"] = new JSONNumber(s.Param2);
            o["param3"] = new JSONNumber(s.Param3);
            o["param4"] = new JSONNumber(s.Param4);
            o["param5"] = new JSONString(s.Param5 ?? "");
            o["param6"] = new JSONString(s.Param6 ?? "");
            return new CommonCheckCond(o);
        }

        private static bool EvaluateLocalVariableInt(ConditionLocalVariableInt c, LogicEntityBase srcEntity)
        {
            if (srcEntity == null || string.IsNullOrEmpty(c.VariableKey)) return false;
            if (!TryGetBindingStringVariable(srcEntity, c.VariableKey, out var raw) || raw == null)
                return false;
            if (!long.TryParse(raw, out var v)) return false;
            return c.Compare switch
            {
                ConditionLocalVariableInt.CompareType.Equals => v == c.Value,
                ConditionLocalVariableInt.CompareType.NotEquals => v != c.Value,
                ConditionLocalVariableInt.CompareType.Greater => v > c.Value,
                ConditionLocalVariableInt.CompareType.Less => v < c.Value,
                ConditionLocalVariableInt.CompareType.GE => v >= c.Value,
                ConditionLocalVariableInt.CompareType.LE => v <= c.Value,
                _ => false,
            };
        }

        private static bool EvaluateLocalVariableString(ConditionLocalVariableString c, LogicEntityBase srcEntity)
        {
            if (srcEntity == null || string.IsNullOrEmpty(c.VariableKey)) return false;
            if (!TryGetBindingStringVariable(srcEntity, c.VariableKey, out var v) || v == null)
                v = "";
            return c.Compare switch
            {
                ConditionLocalVariableString.CompareType.Equals => v == c.Value,
                ConditionLocalVariableString.CompareType.NotEquals => v != c.Value,
                _ => false,
            };
        }

        private static bool EvaluatePlayerLevel(ConditionCheckPlayerLevel c, My.GameLogicManager glm)
        {
            var p = glm.playerLogicEntity;
            if (p == null) return false;
            var attrId = string.IsNullOrEmpty(c.AttrIdForLevel) ? "PlayerSan" : c.AttrIdForLevel;
            var lv = p.GetAttr(attrId);
            return lv >= c.PlayerLevel;
        }

        private static bool TryGetBindingStringVariable(LogicEntityBase src, string key, out string raw)
        {
            raw = null;
            if (src?.BindingRecord is LogicEntityRecord4InteractPoint ip && ip.DynamicVariables != null)
                return ip.DynamicVariables.TryGetValue(key, out raw);
            return false;
        }
    }
}
