using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Quest;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 通用事件授予：GrantItemsOnce 领物品；AssemblePassive 构建时回溯装配
    public sealed class PlayerEventGrantSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerEventGrantSystem]";

        readonly PlayerSystemManager _owner;
        readonly EventGrantProgressionProvider _progressionProvider;
        readonly HashSet<string> _claimedOnceIds = new(StringComparer.Ordinal);
        readonly HashSet<string> _qualifiedPassiveIds = new(StringComparer.Ordinal);
        readonly List<EventGrant> _qualifiedPassiveGrants = new();
        readonly Dictionary<EStatType, List<EventGrant>> _grantsByStatType = new();

        GameLogicManager _logic;
        bool _eventsBound;
        bool _indexesBuilt;

        public EventGrantProgressionProvider ProgressionProvider => _progressionProvider;

        public IReadOnlyCollection<string> QualifiedPassiveIds => _qualifiedPassiveIds;

        public PlayerEventGrantSystem(PlayerSystemManager owner)
        {
            _owner = owner;
            _progressionProvider = new EventGrantProgressionProvider(this);
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _logic = ctx;
            _claimedOnceIds.Clear();
            var claimed = savingData?.PlayerData?.ClaimedEventGrantIds;
            if (claimed != null)
            {
                foreach (var id in claimed)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _claimedOnceIds.Add(id);
                    }
                }
            }

            EnsureIndexes();
            BindEvents();
        }

        public void PostInit(PlayerSystemManager owner)
        {
            EvaluateAll();
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.ClaimedEventGrantIds ??= new List<string>();
            pd.ClaimedEventGrantIds.Clear();
            foreach (var id in _claimedOnceIds)
            {
                pd.ClaimedEventGrantIds.Add(id);
            }
        }

        public bool IsClaimed(string grantId)
        {
            return !string.IsNullOrEmpty(grantId) && _claimedOnceIds.Contains(grantId);
        }

        public bool IsPassiveQualified(string grantId)
        {
            return !string.IsNullOrEmpty(grantId) && _qualifiedPassiveIds.Contains(grantId);
        }

        public void CollectQualifiedPassiveSkills(HashSet<string> applied, List<(string skillId, int level)> output)
        {
            if (output == null)
            {
                return;
            }

            for (int i = 0; i < _qualifiedPassiveGrants.Count; i++)
            {
                var grant = _qualifiedPassiveGrants[i];
                if (grant == null || string.IsNullOrEmpty(grant.PassiveId))
                {
                    continue;
                }

                if (applied != null && applied.Contains(grant.PassiveId))
                {
                    continue;
                }

                output.Add((grant.PassiveId, 1));
            }
        }

        public void AccumulateProgressionStats(StatMap targetMap)
        {
            if (targetMap == null)
            {
                return;
            }

            for (int i = 0; i < _qualifiedPassiveGrants.Count; i++)
            {
                var grant = _qualifiedPassiveGrants[i];
                if (grant?.StatBonuses == null)
                {
                    continue;
                }

                for (int j = 0; j < grant.StatBonuses.Count; j++)
                {
                    var bonus = grant.StatBonuses[j];
                    if (bonus == null || bonus.AttrId == 0 || bonus.Val == 0)
                    {
                        continue;
                    }

                    targetMap.Add(bonus.AttrId, bonus.Val);
                }
            }
        }

        // enable_conds 依赖的世界状态变化时调用（变量/任务完成等）
        public void OnWorldStateMaybeChanged()
        {
            EvaluateAll();
        }

        void BindEvents()
        {
            if (_eventsBound)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerStatisticChangedEvent>(OnStatisticChanged);
            PlayerEventBus.Subscribe<PlayerFuncUnlockEvent>(OnFuncUnlock);
            _eventsBound = true;
        }

        void OnStatisticChanged(PlayerStatisticChangedEvent e)
        {
            EvaluateForStatType(e.StatType);
        }

        void OnFuncUnlock(PlayerFuncUnlockEvent _)
        {
            OnWorldStateMaybeChanged();
        }

        void EnsureIndexes()
        {
            if (_indexesBuilt)
            {
                return;
            }

            _grantsByStatType.Clear();
            var table = CfgMgr.Cfgs?.TbEventGrant;
            if (table?.DataList == null)
            {
                _indexesBuilt = true;
                return;
            }

            foreach (var grant in table.DataList)
            {
                if (grant?.Targets == null || grant.Targets.Count == 0)
                {
                    // 无 target 的 once 也可在全量评估时处理
                    continue;
                }

                var seen = new HashSet<EStatType>();
                foreach (var t in grant.Targets)
                {
                    if (t == null || t.StatType == EStatType.None || !seen.Add(t.StatType))
                    {
                        continue;
                    }

                    if (!_grantsByStatType.TryGetValue(t.StatType, out var list))
                    {
                        list = new List<EventGrant>();
                        _grantsByStatType[t.StatType] = list;
                    }

                    list.Add(grant);
                }
            }

            _indexesBuilt = true;
        }

        void EvaluateAll()
        {
            EnsureIndexes();
            TryClaimAllOnce();
            RebuildQualifiedPassives();
        }

        void EvaluateForStatType(EStatType statType)
        {
            EnsureIndexes();
            if (_grantsByStatType.TryGetValue(statType, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    TryClaimOnce(list[i]);
                }
            }

            // 被动需要幂等重建（可能从未合格变为合格）
            RebuildQualifiedPassives();
        }

        void TryClaimAllOnce()
        {
            var table = CfgMgr.Cfgs?.TbEventGrant;
            if (table?.DataList == null)
            {
                return;
            }

            foreach (var grant in table.DataList)
            {
                TryClaimOnce(grant);
            }
        }

        void TryClaimOnce(EventGrant grant)
        {
            if (grant == null || grant.DeliverMode != EEventGrantDeliverMode.GrantItemsOnce)
            {
                return;
            }

            if (string.IsNullOrEmpty(grant.Id) || _claimedOnceIds.Contains(grant.Id))
            {
                return;
            }

            if (!IsQualified(grant))
            {
                return;
            }

            if (grant.Rewards != null)
            {
                foreach (var pair in grant.Rewards)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0)
                    {
                        continue;
                    }

                    _owner.GiveItemToPlayer(pair.Key, pair.Value);
                }
            }

            _claimedOnceIds.Add(grant.Id);
            Debug.Log($"{LogTag} Claimed once grant '{grant.Id}'.");
        }

        void RebuildQualifiedPassives()
        {
            _qualifiedPassiveIds.Clear();
            _qualifiedPassiveGrants.Clear();

            var table = CfgMgr.Cfgs?.TbEventGrant;
            if (table?.DataList != null)
            {
                foreach (var grant in table.DataList)
                {
                    if (grant == null || grant.DeliverMode != EEventGrantDeliverMode.AssemblePassive)
                    {
                        continue;
                    }

                    if (!IsQualified(grant))
                    {
                        continue;
                    }

                    _qualifiedPassiveIds.Add(grant.Id);
                    _qualifiedPassiveGrants.Add(grant);
                }
            }

            _progressionProvider.NotifyChanged();
            _owner?.ProgressionSystem?.ProgressionRoot?.ForceDirty();
            _owner?.SyncLearnedSkillsToPlayerEntity();
        }

        bool IsQualified(EventGrant grant)
        {
            if (grant == null)
            {
                return false;
            }

            if (_logic != null && !_logic.CheckCommonCondsAll(grant.EnableConds))
            {
                return false;
            }

            if (grant.Targets == null || grant.Targets.Count == 0)
            {
                return true;
            }

            var stats = _owner.StatisticSystem;
            if (stats == null)
            {
                return false;
            }

            for (int i = 0; i < grant.Targets.Count; i++)
            {
                var t = grant.Targets[i];
                if (t == null || t.StatType == EStatType.None)
                {
                    continue;
                }

                long cur = stats.Get(t.StatType, t.Arg0, t.Arg1);
                if (!Compare(cur, t.Op, t.Value))
                {
                    return false;
                }
            }

            return true;
        }

        static bool Compare(long cur, EStatCompareOp op, long value)
        {
            switch (op)
            {
                case EStatCompareOp.Eq:
                    return cur == value;
                case EStatCompareOp.Gte:
                default:
                    return cur >= value;
            }
        }
    }
}
