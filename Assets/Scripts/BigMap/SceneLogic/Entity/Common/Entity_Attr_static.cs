using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My.Map.Entity
{
    public static class AttrIdConsts
    {
        public const string Attack = "Attack";
        public const string Defense = "Defense";

        public const string PhysicalPower = "PhysicalPower"; // 肉体强度
        public const string HTechnique = "HTechnique"; // H技巧：仅用于 HAct 对抗 Adv
        public const string HStrength = "HStrength"; // H强度：放大 HAct 基础冲击量
        public const string Will = "Will"; // 意志

        public const string Arm_Final = "Arm_Final"; // 最终护甲 = （（白字护甲 * 白字护甲额外）+ 绿字护甲） * 绿字额外 



        public const string Arm_Base = "Arm_Base"; // 白字基础护甲（养成静态分量，不受衣装覆盖率缩放）
        public const string Arm_Inner = "Arm_Inner"; // 内部护甲
        public const string Arm_Inner_MinExposeRate = "Arm_Inner_MinExposeRate";
        public const string Arm_Inner_ExposeRate = "Arm_Inner_ExposeRate";

        public const string Arm_White = "Arm_White"; // 白字护甲 = 基础护甲 + （内部护甲） * 转换比例
        public const string Arm_Green = "Arm_Green"; // 绿字护甲
        public const string Arm_White_Percent = "Arm_White_Percent"; // 白字护甲倍率

        public const string Arm_Extra_1 = "Arm_Extra_1"; // 来自肉体耐受的护甲 绿字护甲的一部分

        public const string Final_Fix_DR_All = "Final_Fix_DR_All"; // 白字最终减伤
        public const string Weiyi_JianShang = "Weiyi_JianShang"; // 威仪减伤，按层叠加
        public const string Final_HImpulse_Reduce_Fix = "Final_HImpulse_Reduce_Fix"; // h冲击力最终减少

        public const string PhysicalResist = "PhysicalResist"; // 肉体耐受 目前只有玩家应用
        public const string PhysicalResistArmRate = "PhysicalResistArmRate"; // 肉体耐受 转化为护甲的效率 10000表示1

        public const string PlayerSensitivity = "PlayerSensitivity";
        public const string PlayerCharm = "PlayerCharm"; // 白字魅力（派生）
        public const string PlayerSpellPower = "PlayerSpellPower"; // 法术强度（由魅力派生，供异常状态等读取）

        public const string NPCSJProgress_GainRate = "NPCSJProgress_GainRate"; // 射精条增速万分比修正（全来源）
        public const string PlayerCharm_Inner = "PlayerCharm_Inner"; // 养成内魅力
        public const string PlayerCharm_Static = "PlayerCharm_Static"; // 养成静态魅力
        public const string PlayerCharm_Scaled = "PlayerCharm_Scaled"; // 衣装覆盖后内魅力

        public const string Special_JianShang = "Special_JianShang";
        public const string Special_YiShang = "Special_YiShang";

        public const string Basic_JianShang = "Basic_JianShang";
        public const string NonH_JianShang_Rate = "NonH_JianShang_Rate";
        public const string Basic_MoveSpeed = "Basic_MoveSpeed";
        public const string Basic_ExtraDmg = "Basic_ExtraDmg"; // 额外伤害
        public const string Spe_Player_ExtraDmg = "Spe_Player_ExtraDmg";

        public const string Final_JianShang = "Final_JianShang";

        public const string HImpulse_Pipeline = "HImpulse_Pipeline";
        public const string SrcLevel_Pipeline = "SrcLevel_Pipeline";
        public const string HTechnique_Pipeline = "HTechnique_Pipeline";
        public const string HStrength_Pipeline = "HStrength_Pipeline";
        // EBodyPart 整型（可选元数据）；战斗数值走上方同名 entity pipeline
        public const string HitPart_Pipeline = "HitPart";
        public const string XiXue_Pipeline = "XiXue_Pipeline";

        // 施放时由武器栏写入 ctx.CacheAttrVal，供 UseWeapon OnHitEffects 引用
        public const string CastWeaponLevel = "CastWeaponLevel";
        public const string CastStunValue = "CastStunValue";

        public const string PlayerGcThreshold = "PlayerGcThreshold";

        public const string Player_AttrctPower = "Player_AttrctPower";

        public const string Basic_KnockResistent = "Basic_KnockResistent";

        public const string Basic_PleasureAdd = "Basic_PleasureAdd";
        public const string Basic_HungerCost = "Basic_HungerCost";

        public const string Sensitivity_Percent = "Sensitivity_Percent";

        public const string HP = "HP";
        public const string HP_MAX = "HP.Max";

        public const string NPCHVal = "NPCHVal";
        public const string NPCHVal_Max = "NPCHVal.Max";
        public const string NPCSJProgress = "NPCSJProgress";

        public const string NPCHVal_Basic_Up = "NPCHVal_Up"; // NPCHVal 自然上涨
        public const string NPCSJProgress_Basic_Up = "NPCSJProgress_Basic_Up"; // NPCHVal 自然上涨

        public const string UnitKnockDown = "NPCKnockDown";

        // 欲望浓度增幅（万分比，10000=抵达该类型硬上限）
        public const string DesireDensityAmplify = "DesireDensityAmplify";

        public const string Unmovable = "Unmovable";
        public const string LockFace = "LockFace";
        public const string Stun = "Stun";
        public const string ForbidSkillOp = "ForbidSkillOp";
        public const string PeaceCombatRestricted = "PeaceCombatRestricted";
        public const string NoSelect = "NoSelect";
        public const string PerfectDodgeWindow = "PerfectDodgeWindow"; // 完美闪避判定窗口
        public const string ImmuneKnock = "ImmuneKnock";
        public const string Ghost = "Ghost";
        public const string Invisible = "Invisible";
        public const string HideView = "HideView";
        public const string SuperArmor = "SuperArmor";
        public const string ImmumeKaiYou = "ImmumeKaiYou";
        public const string Sleep = "Sleep";
        public const string FastTurn = "FastTurn";
        public const string NoInteract = "NoInteract";
        public const string Charmed = "Charmed";
        public const string Fear = "Fear";
        public const string ImmuneFear = "ImmuneFear";
        public const string Lured = "Lured";
        public const string ImmuneLured = "ImmuneLured";
        public const string ImmuneSteerInput = "ImmuneSteerInput";
        public const string ImmuneJianSu = "ImmuneJianSu";
        public const string NpcFcked = "NpcFcked"; // 特殊状态
        public const string UnitKnockfly = "UnitKnockfly"; // 击飞
        public const string UnitStagger = "UnitStagger"; // 踉跄
        public const string DesireMistAffected = "DesireMistAffected";

        public const string ImmuneEvilShock = "ImmuneEvilShock";
        public const string ImmuneDamage = "ImmuneDamage";
        public const string NoKiller = "NoKiller";

        public const string PlayerZhaZhiMode = "PlayerZhaZhiMode";
        public const string PlayerUnlockYuhuo = "PlayerUnlockYuhuo";
        public const string PlayerUnlockYindu = "PlayerUnlockYindu";
        public const string PlayerUnlockJiang = "PlayerUnlockJiang";
        public const string PlayerUnlockYijin = "PlayerUnlockYijin";

        public const string PlayerHunger = "PlayerHunger";
        public const string PlayerClothes = "PlayerClothes"; // 衣装
        public const string PlayerPleasure = "PlayerPleasure"; // 快乐条
        public const string PlayerSanity = "PlayerSanity";
        public const string PlayerEstrusProgrss = "PlayerEstrusProgrss"; // 发情值
        public const string PlayerOriginPower = "PlayerOriginPower";
        public const string PlayerJingYu = "PlayerJingYu"; // 精浴用属性做 buff只负责显示
        public const string PlayerJingYuRate = "PlayerJingYuRate"; // 额外精浴比例

        public const string Clothes_ExposeRate = "Clothes_ExposeRate"; // 衣装暴露绿

        public const string PlayerKnockDown = "PlayerKnockDown";

        public const string PlayerSJAmount_Fixed = "PlayerSJAmount_Fixed";
        public const string PlayerSJAmount_Precent = "PlayerSJAmount_Precent";

        public const string PlayerNaiLi = "PlayerNaiLi";
        public const string PlayerNaiLi_Recovery = "PlayerNaiLi.Recovery";


        public const string UnitHShield = "UnitHShield";
        public const string UnitHShieldMax = "UnitHShieldMax";

        public const string StatUnstoppable = "StatUnstoppable";

        public const string DamageXiXue = "DamageXiXue";

        public const string Ammo = "ammo";
        public const string AmmoMax = "Ammo.Max";
        public const string HValYiShang = "HValYiShang";

        #region 特殊状态、视觉等

        public const string UnitDizzy = "UnitDizzy";

        // 目击累计速率加成（万分比，10000=+100%）；观察者侧
        public const string UnitWitnessSpotRate = "UnitWitnessSpotRate";
        // 逃脱累计速率加成（万分比）；目标侧，与 UnitWitnessSpotRate 对冲
        public const string UnitWitnessEscapeRate = "UnitWitnessEscapeRate";

        // 视野距离/FOV 万分比修正（10000=100%）
        public const string UnitVisionRangeMul = "UnitVisionRangeMul";
        public const string UnitVisionFovMul = "UnitVisionFovMul";

        #endregion

        public const string DeepZhaChance = "DeepZhaChance";
    }

    public struct AttrKvPair
    {
        public string AttrId;
        public long Val;
    }


    public static class UnitAttrSystemUnits
    {
        public readonly static Dictionary<string, AttrNode> AttrGraph = new();
        public class AttrNode
        {
            public string attrId;                        // 仅用于数值类派生，资源.Current 不进入图
            public Func<AttrCalcContext, long>? Eval;               // 公式委托（可空：纯聚合）
            public readonly List<string> inputs = new();         // 依赖的属性Id
            public readonly List<AttrNode> outs = new();         // 反向邻接
            public int level = 0;
            public int componentId = 0;
        }
        private readonly static List<AttrNode> topo = new();            // 预编译拓扑序


        public static void InitGameAttrs()
        {
            List<(string output, string[] inputs, Func<AttrCalcContext, long> eval)> attrDefs = new()
            {
                ("Strength", null, null),
                ("Attack", new[] { "Strength" }, ctx => 50 + ctx.Get("Strength") * 5),

                (AttrIdConsts.PlayerCharm_Scaled, new[] { AttrIdConsts.PlayerCharm_Inner, AttrIdConsts.Clothes_ExposeRate },
                    ctx => (long)(ctx.Get(AttrIdConsts.PlayerCharm_Inner) * ctx.Get(AttrIdConsts.Clothes_ExposeRate) / 10000f)),
                (AttrIdConsts.PlayerCharm, new[] { AttrIdConsts.PlayerCharm_Scaled, AttrIdConsts.PlayerCharm_Static },
                    ctx => ctx.Get(AttrIdConsts.PlayerCharm_Scaled) + ctx.Get(AttrIdConsts.PlayerCharm_Static)),

                (AttrIdConsts.PlayerSpellPower, new[] { AttrIdConsts.PlayerCharm },
                    ctx =>
                    {
                        long charm = ctx.Get(AttrIdConsts.PlayerCharm);
                        if (charm <= 0)
                        {
                            return 0;
                        }

                        const long k = 500;
                        return charm * charm / (charm + k);
                    }),

                (AttrIdConsts.Arm_Inner_ExposeRate,
                    new[] { AttrIdConsts.Arm_Inner_MinExposeRate, AttrIdConsts.Clothes_ExposeRate },
                    ctx =>
                    {
                        long minRate = Math.Clamp(ctx.Get(AttrIdConsts.Arm_Inner_MinExposeRate), 0, 10000);
                        long clothesRate = Math.Clamp(ctx.Get(AttrIdConsts.Clothes_ExposeRate), 0, 10000);
                        return minRate + (10000 - minRate) * clothesRate / 10000;
                    }),
                (AttrIdConsts.Arm_White,
                    new[] { AttrIdConsts.Arm_Base, AttrIdConsts.Arm_Inner, AttrIdConsts.Arm_Inner_ExposeRate },
                    ctx => ctx.Get(AttrIdConsts.Arm_Base) +
                        (long)(ctx.Get(AttrIdConsts.Arm_Inner) * ctx.Get(AttrIdConsts.Arm_Inner_ExposeRate) / 10000f)),
                (AttrIdConsts.Arm_Extra_1, new[] { AttrIdConsts.PhysicalResist, AttrIdConsts.PhysicalResistArmRate, AttrIdConsts.Clothes_ExposeRate },
                    ctx => (long)(ctx.Get(AttrIdConsts.PhysicalResist) * ctx.Get(AttrIdConsts.PhysicalResistArmRate) / 10000f * ctx.Get(AttrIdConsts.Clothes_ExposeRate) / 10000f)),
                (AttrIdConsts.Arm_Green, new[] { AttrIdConsts.Arm_Extra_1 }, ctx => ctx.Get(AttrIdConsts.Arm_Extra_1)),
                (AttrIdConsts.Arm_Final, new[] { AttrIdConsts.Arm_White, AttrIdConsts.Arm_White_Percent, AttrIdConsts.Arm_Green },
                    ctx => (long)(ctx.Get(AttrIdConsts.Arm_White) * (10000 + ctx.Get(AttrIdConsts.Arm_White_Percent)) / 10000f) + ctx.Get(AttrIdConsts.Arm_Green)),
            };
            CompileGraph(attrDefs);
        }

        public static void CompileGraph(IEnumerable<(string output, string[] inputs, Func<AttrCalcContext, long> eval)> defs)
        {
            AttrGraph.Clear();
            topo.Clear();
            // 构建节点与边
            foreach (var (outId, ins, eval) in defs)
            {
                var n = AttrGraph.TryGetValue(outId, out var ex) ? ex : (AttrGraph[outId] = new AttrNode { attrId = outId });
                n.Eval = eval;
                if (ins != null)
                {
                    foreach (var i in ins)
                    {
                        var inNode = AttrGraph.TryGetValue(i, out var ei) ? ei : (AttrGraph[i] = new AttrNode { attrId = i });
                        n.inputs.Add(i);
                        inNode.outs.Add(n);
                    }
                }
            }
            // Kahn 拓扑 + 层级
            var indeg = new Dictionary<string, int>();
            foreach (var n in AttrGraph.Values) indeg[n.attrId] = 0;
            foreach (var n in AttrGraph.Values) foreach (var i in n.inputs) indeg[n.attrId]++;

            var q = new Queue<AttrNode>(AttrGraph.Values.Where(n => indeg[n.attrId] == 0));
            var level = new Dictionary<string, int>();
            while (q.Count > 0)
            {
                var u = q.Dequeue();
                topo.Add(u);
                var lv = 0; foreach (var i in u.inputs) lv = Math.Max(lv, level.GetValueOrDefault(i, 0) + 1);
                level[u.attrId] = lv; u.level = lv;
                foreach (var v in u.outs)
                {
                    indeg[v.attrId]--;
                    if (indeg[v.attrId] == 0) q.Enqueue(v);
                }
            }
            // 若有残余入度 -> 存在环。要求修正公式或标注为固定点子图（此处省略迭代实现，可选在运行期对小子图迭代 N 次）
            if (topo.Count != AttrGraph.Count)
                throw new InvalidOperationException("Dependency cycle detected in attribute graph.");
        }

        public static AttrNode GetAttrNode(string attrId)
        {
            AttrGraph.TryGetValue(attrId, out var node);
            return node;
        }
    }
}
