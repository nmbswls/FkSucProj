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
        public const string Special_JianShang = "Special_JianShang";
        public const string Special_YiShang = "Special_YiShang";
        public const string PlayerGcThreshold = "PlayerGcThreshold";
        public const string Basic_JianShang = "Basic_JianShang";
        public const string Basic_MoveSpeed = "Basic_MoveSpeed";
        public const string Basic_ExtraDmg = "Basic_ExtraDmg";
        public const string Spe_Player_ExtraDmg = "Spe_Player_ExtraDmg";

        public const string Player_AttrctPower = "Player_AttrctPower";

        public const string Basic_KnockResistent = "Basic_KnockResistent";

        public const string Basic_PleasureAdd = "Basic_PleasureAdd";
        public const string Basic_HungerCost = "Basic_HungerCost";

        public const string HP = "HP";
        public const string HP_MAX = "HP.Max";

        public const string Unmovable = "Unmovable";
        public const string LockFace = "LockFace";
        public const string Stun = "Stun";
        public const string ForbidSkillOp = "ForbidSkillOp";
        public const string NoSelect = "NoSelect";
        public const string ImmuneKnock = "ImmuneKnock";
        public const string Ghost = "Ghost";
        public const string Invisible = "Invisible";
        public const string HideView = "HideView";
        public const string SuperArmor = "SuperArmor";
        public const string ImmumeKaiYou = "ImmumeKaiYou";
        public const string Sleep = "Sleep";
        public const string FastTurn = "FastTurn";

        public const string ImmuneEvilShock = "ImmuneEvilShock";
        public const string NoKiller = "NoKiller";

        public const string PlayerHunger = "PlayerHunger";
        public const string PlayerClothes = "PlayerClothes";
        public const string PlayerPleasure = "PlayerPleasure";
        public const string PlayerSan = "PlayerSan";
        public const string PlayerFaQingVal = "PlayerFaQingVal";
        public const string PlayerOriginPower = "PlayerOriginPower";

        public const string PlayerKnockDown = "PlayerKnockDown";

        public const string PlayerNaiLi = "PlayerNaiLi";
        public const string PlayerNaiLi_Recovery = "PlayerNaiLi.Recovery";


        public const string UnitHVal = "UnitHVal";
        public const string UnitHShield = "UnitHShield";
        public const string StatUnstoppable = "StatUnstoppable";
        public const string SJProgress = "SJProgress";

        public const string DamageXiXue = "DamageXiXue";

        public const string HValYiShang = "HValYiShang";

        #region 特殊状态、视觉等

        public const string UnitDizzy = "UnitDizzy";

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
            List<(string output, string[] inputs, Func<AttrCalcContext, long> eval)> attrDefs = new() {
            ("Strength", null, null),
            ("Attack", new []{"Strength"}, ctx => 50 + ctx.Get("Strength") * 5),
        };
            CompileGraph(attrDefs);
        }

        public static void CompileGraph(IEnumerable<(string output, string[] inputs, Func<AttrCalcContext, long> eval)> defs)
        {
            AttrGraph.Clear();
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

