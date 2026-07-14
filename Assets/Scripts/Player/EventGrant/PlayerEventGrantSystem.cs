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
    // 评估走 Tick 批处理 + 按 stat key 倒排，避免一次满足几百条时打爆主线程
    public sealed class PlayerEventGrantSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerEventGrantSystem]";

        readonly PlayerSystemManager _owner;
        readonly EventGrantProgressionProvider _progressionProvider;
        readonly HashSet<string> _claimedOnceIds = new(StringComparer.Ordinal);
        readonly HashSet<string> _qualifiedPassiveIds = new(StringComparer.Ordinal);
        readonly Dictionary<string, EventGrant> _qualifiedPassiveById = new(StringComparer.Ordinal);
        readonly List<EventGrant> _qualifiedPassiveGrants = new();

        // 精确 target key -> 依赖该 key 的 grant 列表
        readonly Dictionary<string, List<EventGrant>> _grantsByTargetKey = new(StringComparer.Ordinal);
        readonly List<EventGrant> _onceGrants = new();
        readonly List<EventGrant> _passiveGrants = new();
        readonly List<EventGrant> _grantsWithEnableConds = new();

        readonly HashSet<string> _dirtyStatKeys = new(StringComparer.Ordinal);
        readonly HashSet<EventGrant> _pendingEval = new();
        readonly List<EventGrant> _evalScratch = new();

        GameLogicManager _logic;
        bool _eventsBound;
        bool _indexesBuilt;
        bool _needFullRescan;
        bool _passiveAssemblyDirty;

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
            _qualifiedPassiveIds.Clear();
            _qualifiedPassiveById.Clear();
            _qualifiedPassiveGrants.Clear();
            _dirtyStatKeys.Clear();
            _pendingEval.Clear();
            _indexesBuilt = false;

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
            _needFullRescan = true;
        }

        public void PostInit(PlayerSystemManager owner)
        {
            FlushPending(forceFull: true);
        }

        public void Tick(float dt)
        {
            if (_needFullRescan || _dirtyStatKeys.Count > 0 || _pendingEval.Count > 0)
            {
                FlushPending(forceFull: _needFullRescan);
            }
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

        // 已兑现的通识类授予（剪贴板履历）：Once 已领取，或 Passive 已达成
        public void CollectUnlockedKnowledgeGrants(List<EventGrant> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            EnsureIndexes();

            var table = CfgMgr.Cfgs?.TbEventGrant;
            if (table == null)
            {
                return;
            }

            foreach (var grant in table.DataList)
            {
                if (grant == null || grant.Hidden || grant.Category != EEventGrantCategory.Knowledge)
                {
                    continue;
                }

                bool unlocked = grant.DeliverMode == EEventGrantDeliverMode.GrantItemsOnce
                    ? _claimedOnceIds.Contains(grant.Id)
                    : _qualifiedPassiveIds.Contains(grant.Id);
                if (!unlocked)
                {
                    continue;
                }

                output.Add(grant);
            }

            output.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
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

        public void OnWorldStateMaybeChanged()
        {
            // enable_conds 变化：只扫带门槛的 grant，不全表盲扫
            EnsureIndexes();
            for (int i = 0; i < _grantsWithEnableConds.Count; i++)
            {
                _pendingEval.Add(_grantsWithEnableConds[i]);
            }

            if (_grantsWithEnableConds.Count == 0)
            {
                return;
            }
        }

        void BindEvents()
        {
            if (_eventsBound)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerStatisticChangedEvent>(OnStatisticChanged);
            PlayerEventBus.Subscribe<PlayerFuncUnlockEvent>(OnFuncUnlock);
            PlayerEventBus.Subscribe<PlayerQuestCompleteEvent>(OnQuestComplete);
            PlayerEventBus.Subscribe<PlayerGlobalSwitchChangedEvent>(OnGlobalSwitchChanged);
            _eventsBound = true;
        }

        void OnStatisticChanged(PlayerStatisticChangedEvent e)
        {
            if (string.IsNullOrEmpty(e.Key))
            {
                return;
            }

            _dirtyStatKeys.Add(e.Key);
        }

        void OnFuncUnlock(PlayerFuncUnlockEvent _)
        {
            OnWorldStateMaybeChanged();
        }

        void OnQuestComplete(PlayerQuestCompleteEvent _)
        {
            OnWorldStateMaybeChanged();
        }

        void OnGlobalSwitchChanged(PlayerGlobalSwitchChangedEvent _)
        {
            OnWorldStateMaybeChanged();
        }

        void EnsureIndexes()
        {
            if (_indexesBuilt)
            {
                return;
            }

            _grantsByTargetKey.Clear();
            _onceGrants.Clear();
            _passiveGrants.Clear();
            _grantsWithEnableConds.Clear();

            var table = CfgMgr.Cfgs?.TbEventGrant;
            if (table?.DataList == null)
            {
                _indexesBuilt = true;
                return;
            }

            foreach (var grant in table.DataList)
            {
                if (grant == null || string.IsNullOrEmpty(grant.Id))
                {
                    continue;
                }

                if (grant.DeliverMode == EEventGrantDeliverMode.GrantItemsOnce)
                {
                    _onceGrants.Add(grant);
                }
                else if (grant.DeliverMode == EEventGrantDeliverMode.AssemblePassive)
                {
                    _passiveGrants.Add(grant);
                }

                if (grant.EnableConds != null && grant.EnableConds.Count > 0)
                {
                    _grantsWithEnableConds.Add(grant);
                }

                if (grant.Targets == null || grant.Targets.Count == 0)
                {
                    continue;
                }

                var seenKeys = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < grant.Targets.Count; i++)
                {
                    var t = grant.Targets[i];
                    if (t == null || t.StatType == EStatType.None)
                    {
                        continue;
                    }

                    var key = PlayerStatisticKeys.MakeKey(t.StatType, t.Arg0, t.Arg1);
                    if (!seenKeys.Add(key))
                    {
                        continue;
                    }

                    if (!_grantsByTargetKey.TryGetValue(key, out var list))
                    {
                        list = new List<EventGrant>();
                        _grantsByTargetKey[key] = list;
                    }

                    list.Add(grant);
                }
            }

            _indexesBuilt = true;
        }

        void FlushPending(bool forceFull)
        {
            EnsureIndexes();
            _needFullRescan = false;
            _passiveAssemblyDirty = false;

            if (forceFull)
            {
                // 读档全量重建：once 仍可补领并发 toast；被动只装配不刷屏
                for (int i = 0; i < _onceGrants.Count; i++)
                {
                    TryClaimOnce(_onceGrants[i]);
                }

                for (int i = 0; i < _passiveGrants.Count; i++)
                {
                    TryUpdatePassiveMembership(_passiveGrants[i], announce: false);
                }

                _dirtyStatKeys.Clear();
                _pendingEval.Clear();
                ApplyPassiveAssemblyIfDirty();
                return;
            }

            _evalScratch.Clear();
            foreach (var key in _dirtyStatKeys)
            {
                if (_grantsByTargetKey.TryGetValue(key, out var list))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        _pendingEval.Add(list[i]);
                    }
                }
            }

            _dirtyStatKeys.Clear();

            foreach (var grant in _pendingEval)
            {
                _evalScratch.Add(grant);
            }

            _pendingEval.Clear();

            for (int i = 0; i < _evalScratch.Count; i++)
            {
                var grant = _evalScratch[i];
                if (grant == null)
                {
                    continue;
                }

                if (grant.DeliverMode == EEventGrantDeliverMode.GrantItemsOnce)
                {
                    TryClaimOnce(grant);
                }
                else if (grant.DeliverMode == EEventGrantDeliverMode.AssemblePassive)
                {
                    TryUpdatePassiveMembership(grant, announce: true);
                }
            }

            ApplyPassiveAssemblyIfDirty();
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

            PlayerEventBus.Publish(new PlayerEventGrantClaimedEvent
            {
                GrantId = grant.Id,
                Category = grant.Category,
                Name = grant.Name ?? string.Empty,
                Desc = grant.Desc ?? string.Empty,
            });
        }

        void TryUpdatePassiveMembership(EventGrant grant, bool announce)
        {
            if (grant == null || grant.DeliverMode != EEventGrantDeliverMode.AssemblePassive)
            {
                return;
            }

            bool now = IsQualified(grant);
            bool was = _qualifiedPassiveIds.Contains(grant.Id);

            if (now == was)
            {
                return;
            }

            if (now)
            {
                _qualifiedPassiveIds.Add(grant.Id);
                _qualifiedPassiveById[grant.Id] = grant;
                _qualifiedPassiveGrants.Add(grant);
                if (announce)
                {
                    PlayerEventBus.Publish(new PlayerEventGrantClaimedEvent
                    {
                        GrantId = grant.Id,
                        Category = grant.Category,
                        Name = grant.Name ?? string.Empty,
                        Desc = grant.Desc ?? string.Empty,
                    });
                }
            }
            else
            {
                _qualifiedPassiveIds.Remove(grant.Id);
                _qualifiedPassiveById.Remove(grant.Id);
                for (int i = _qualifiedPassiveGrants.Count - 1; i >= 0; i--)
                {
                    if (_qualifiedPassiveGrants[i] != null && _qualifiedPassiveGrants[i].Id == grant.Id)
                    {
                        _qualifiedPassiveGrants.RemoveAt(i);
                        break;
                    }
                }
            }

            _passiveAssemblyDirty = true;
        }

        void ApplyPassiveAssemblyIfDirty()
        {
            if (!_passiveAssemblyDirty)
            {
                return;
            }

            _passiveAssemblyDirty = false;
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

                long cur = stats.Get(
                    t.StatType,
                    PlayerStatisticKeys.NormalizeArg(t.Arg0),
                    PlayerStatisticKeys.NormalizeArg(t.Arg1));
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
