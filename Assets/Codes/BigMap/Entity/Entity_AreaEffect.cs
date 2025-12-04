using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using Map.Logic;
using My.Map.Logic;
using static My.GameLogicManager;
using System.Linq;
using System.Security.Principal;


namespace My.Map
{
    public class AreaEffectLogicEntity : LogicEntityBase
    {

        public MapAreaEffectConfig cacheCfg;
        public AreaEffectLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {

        }


        public HashSet<long> currAffectedEntites = new();
        public HashSet<long> newEntities = new();
        public int TotalTriggerCnt = 0;

        private float _lastCheckTimer;

        public override EEntityType Type => EEntityType.AreaEffect;

        public override void Initialize()
        {
            base.Initialize();

            cacheCfg = MapAreaEffectLoader.Get(CfgId);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public void UpdateAffectedLogics(List<ILogicEntity> inEnties)
        {
            newEntities.Clear();
            foreach (var inEntity in inEnties)
            {
                // 新的
                if(!currAffectedEntites.Contains(inEntity.Id))
                {
                    LogicManager.globalBuffManager.RequestAddBuff(inEntity.Id, cacheCfg.AreaBuffId, casterId:Id);
                }
                // 维护tmp2
                newEntities.Add(inEntity.Id);
            }

            foreach (var curEntity in currAffectedEntites)
            {
                if(!newEntities.Contains(curEntity))
                {
                    LogicManager.globalBuffManager.RemoveAllBuffById(curEntity, cacheCfg.AreaBuffId, casterId: Id);
                }
            }

            var t = currAffectedEntites;
            currAffectedEntites = newEntities;
            newEntities = t;
        }


        public override void OnEntityDie(int reason, ResourceDeltaIntent lastIntent = null)
        {
            base.OnEntityDie(reason, lastIntent);

            // 清理
            foreach(var curEntity in currAffectedEntites)
            {
                LogicManager.globalBuffManager.RemoveAllBuffById(curEntity, cacheCfg.AreaBuffId, casterId: Id);
            }
            currAffectedEntites.Clear();
        }
    }



}

