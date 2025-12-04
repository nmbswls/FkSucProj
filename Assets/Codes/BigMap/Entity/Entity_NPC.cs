using Config.Unit;
using Config;
using UnityEngine;
using My.Map.Logic;
using My.Map.Entity;
using System.Collections.Generic;
using static My.Map.Entity.EntitySkillComboGraph;
using Map.Logic.Events;
using static UnityEngine.RuleTile.TilingRuleOutput;


namespace My.Map
{
    public class NpcUnitLogicEntity : BaseUnitLogicEntity
    {
        public MapNpcConfig cacheCfg;

        public NpcUnitLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            cacheCfg = MapNpcConfigLoader.Get(CfgId);
            this.unitCfg = cacheCfg;

            
        }

        public override EEntityType Type => EEntityType.Npc;

        // 预设程序生成行为
        public List<string> DefaultSkillList = new List<string>()
        {
            "player_shoot",
            "default_weapon",
            "default_enemy_qinfan",
        };



        protected override void InitAbility()
        {
            base.InitAbility();
        }

        protected override EntitySkillComboGraph GenerateComboGraph()
        {
            EntitySkillComboGraph graph = new();
            {
                var node = new ComboNode()
                {
                    NodeId = 100,
                    AbilityId = "guard_attack_01",
                    deriveWindows = new List<DeriveWindow>()
                    {
                        new DeriveWindow()
                        {
                            id = "1",
                            window = new TimeWindow(0.27f, 0.57f),
                        }
                    }
                };
                graph.ComboNodes.Add(node);

            }
            {
                var node = new ComboNode()
                {
                    NodeId = 101,
                    AbilityId = "guard_attack_02",
                };
                graph.ComboNodes.Add(node);

            }

            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 0,
                    toNodeId = 100,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "guard_attack"
                    },
                };

                graph.Transitions.Add(trans);
            }
            {
                var trans = new EntitySkillComboGraph.Transition()
                {
                    fromNodeId = 100,
                    toNodeId = 101,
                    triggerInput = new InputPattern()
                    {
                        SkillId = "guard_attack"
                    },
                    windowId = "1",
                };

                graph.Transitions.Add(trans);
            }

            graph.BuildGraph();
            return graph;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UnitBaseRecord.Unsensored)
            {
                LogicManager.globalBuffManager.RequestAddBuff(Id, "unsensored");
            }
        }

        protected override void InitAttribute()
        {
            var cacheCfg = MapNpcConfigLoader.Get(CfgId);
            moveSpeed = cacheCfg.MoveSpeed;

            base.InitAttribute();

        }

        protected override void InitAiBrain()
        {
            base.InitAiBrain();
        }

        public override bool CanWatch()
        {
            return true;
        }
    }
}

