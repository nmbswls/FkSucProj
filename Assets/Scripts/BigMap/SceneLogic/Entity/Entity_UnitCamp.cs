using Config.Map;
using Config;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using My.Map.Logic;

namespace My.Map
{
    public class DynamicSpawnerLogicEntity : LogicEntityBase
    {

        public MapDynamicSpawnerConfig cacheCfg;

        public DynamicSpawnerLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            //var record = (LogicEntityRecord4DynamicSpawner)bindingRecord;

        }

        public override EEntityType Type => EEntityType.DynamicSpawner;

        /// <summary>
        /// 已刷新列表
        /// </summary>
        public Dictionary<int, long> MemberId2EntityMap = new();
        protected HashSet<int> currActiveMemberSet = new();
        protected List<int> _tmpClearMemberList = new();

        public bool MarkCleared = false;

        public override void Initialize()
        {
            base.Initialize();

            if(cacheCfg.SpawnOnCreate)
            {
                RefreshSpawner();
            }
        }

        protected override void LoadCfg()
        {
            cacheCfg = MapDynamicSpawnerCfgLoader.Get(CfgId);
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            CheckWaveCleared();
        }


        private float _checkTimer = 0;
        protected void CheckWaveCleared()
        {
            if (LogicTime.time < _checkTimer) 
            {
                return;
            }
            _checkTimer = LogicTime.time + 0.5f;

            if(MarkCleared)
            {
                return;
            }

            bool allCleared = true;
            foreach (var kv in MemberId2EntityMap)
            {
                long entityId = kv.Value;

                var existEntity = LogicManager.GetLogicEntity(entityId) as LogicEntityBase;
                if (existEntity == null || existEntity.MarkDestroyed)
                {
                    continue;
                }

                if (existEntity is BaseUnitLogicEntity existUnit && existUnit.IsDead)
                {
                    continue;
                }

                allCleared = false;
                break;
            }

            if(allCleared)
            {
                MarkCleared = true;
            }
        }

        /// <summary>
        /// 重刷一次 删除旧的 创建新的
        /// </summary>
        public void RefreshSpawner(bool force = false)
        {
            // 强制重刷 否则增量刷新
            if(force)
            {
                foreach(var kv in MemberId2EntityMap)
                {
                    var entity = LogicManager.GetLogicEntity(kv.Value, false) as LogicEntityBase;
                    entity?.DoEntityDestroyed("spawner_remove");
                }
                MemberId2EntityMap.Clear();
                currActiveMemberSet.Clear();
            }

            foreach (var memberInfo in cacheCfg.SpawnInfos)
            {
                bool needRespawn = false;
                if(!MemberId2EntityMap.TryGetValue(memberInfo.MemberId, out var entityId))
                {
                    needRespawn = true;
                }
                else
                {
                    var existEntity = LogicManager.GetLogicEntity(entityId) as LogicEntityBase;
                    if(existEntity == null || existEntity.MarkDestroyed )
                    {
                        needRespawn = true;
                    }

                    if(existEntity is BaseUnitLogicEntity existUnit && existUnit.IsDead)
                    {
                        needRespawn = true;
                    }
                }

                if (needRespawn)
                {
                    MemberId2EntityMap.Remove(memberInfo.MemberId);

                    var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(memberInfo.InitInfo);
                    if(record == null)
                    {
                        Debug.Log($"event group:{Id} not record create member:{memberInfo.MemberId} entity:{record.Id}");
                        continue;
                    }

                    record.LifeBindEntityId = this.Id;
                    record.Position = this.Pos + memberInfo.InitInfo.Position;
                    MemberId2EntityMap[memberInfo.MemberId] = record.Id;
                    LogicManager.AddNewEntityRecord(record);

                    Debug.Log($"event group:{Id} create member:{memberInfo.MemberId} entity:{record.Id}");

                    currActiveMemberSet.Add(memberInfo.MemberId);

                    // 强制激活一次
                    LogicManager.GetLogicEntity(MemberId2EntityMap[memberInfo.MemberId]);
                }
            }

            MarkCleared = true;
        }
    }
}