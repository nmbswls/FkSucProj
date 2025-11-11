using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using Map.Logic;
using My.Map.Logic.Chunk;


namespace My.Map
{
    public class AreaEffectLogicEntity : LogicEntityBase
    {

        public MapAreaEffectConfig cacheCfg;
        public AreaEffectLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }

        public class RuntimeTriggerInfo
        {
            public Dictionary<long, float> lastTriggerTimeDict = new();
            public int TotalTriggerCnt = 0;
        }

        public RuntimeTriggerInfo? runtimeInfo;


        public override EEntityType Type => EEntityType.AreaEffect;

        public override void Initialize()
        {
            base.Initialize();

            cacheCfg = MapAreaEffectLoader.Get(CfgId);

            if (string.IsNullOrEmpty(cacheCfg.BindingBuffId))
            {
                LogicManager.globalBuffManager.RequestAddBuff(this.Id, cacheCfg.BindingBuffId);
            }

            if (cacheCfg.HastriggerArea)
            {
                runtimeInfo = new();
            }
        }

        public void OnTriggerAreaTriggered(ILogicEntity triggerOne)
        {
            if (!cacheCfg.HastriggerArea)
            {
                return;
            }

            runtimeInfo.lastTriggerTimeDict.TryGetValue(triggerOne.Id, out float lastTime);
            if (lastTime != 0 && LogicTime.time < lastTime + 1.0f)
            {
                return;
            }

            if (cacheCfg.TriggerEffects != null)
            {
                foreach (var effect in cacheCfg.TriggerEffects)
                {
                    var ctx = new GameLogicManager.LogicFightEffectContext(LogicManager, new SourceKey()
                    {
                        type = SourceType.AreaEffect,
                    });
                    LogicManager.HandleLogicFightEffect(effect, ctx);
                }
            }
        }
    }



}

