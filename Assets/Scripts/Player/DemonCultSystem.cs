using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Saving;

namespace My.Player
{
    public enum ECultAttribute
    {
        None = 0,
        TownDailyFaith = 1,
    }

    public enum CultTechNodeVisualState
    {
        Locked,
        Unlockable,
        InsufficientFaith,
        Unlocked,
    }

    // 教团：信仰账户 + 布道/教义节点（首期仅解锁与占位效果描述）
    public sealed class DemonCultSystem
    {
        readonly Dictionary<int, int> _techLevels = new();
        readonly HashSet<int> _unlockedSeats = new();
        readonly Dictionary<(int seatId, int nodeId), int> _seatTechLevels = new();
        readonly Dictionary<ECultAttribute, long> _cultAttributes = new();
        long _faith;
        GameLogicManager _logic;

        public event Action OnCultChanged;

        public long Faith => _faith;
        public int UnlockedSeatCount => _unlockedSeats.Count;

        public long GetCultAttributeValue(ECultAttribute attribute)
            => _cultAttributes.TryGetValue(attribute, out var value) ? value : 0;

        public int UnlockedSeatTechNodeCount
        {
            get
            {
                var count = 0;
                foreach (var pair in _seatTechLevels)
                {
                    if (pair.Value > 0) count++;
                }
                return count;
            }
        }

        public void Initialize(GameLogicManager logic, SaveData savingData)
        {
            _logic = logic;
            _techLevels.Clear();
            _unlockedSeats.Clear();
            _seatTechLevels.Clear();
            _faith = 0;

            var persist = savingData?.DemonCult;
            bool isFresh = persist == null
                || (persist.Faith <= 0
                    && (persist.TechNodes == null || persist.TechNodes.Count == 0));

            if (persist != null)
            {
                _faith = Math.Max(0, persist.Faith);
                if (persist.TechNodes != null)
                {
                    foreach (var entry in persist.TechNodes)
                    {
                        if (entry != null && entry.NodeId > 0 && entry.Level > 0)
                        {
                            _techLevels[entry.NodeId] = entry.Level;
                        }
                    }
                }
                if (persist.AncientSeats != null)
                {
                    foreach (var entry in persist.AncientSeats)
                    {
                        if (entry != null && entry.SeatId > 0 && entry.Unlocked)
                        {
                            _unlockedSeats.Add(entry.SeatId);
                        }
                    }
                }
                if (persist.SeatTechNodes != null)
                {
                    foreach (var entry in persist.SeatTechNodes)
                    {
                        if (entry != null && entry.SeatId > 0 && entry.NodeId > 0 && entry.Level > 0)
                            _seatTechLevels[(entry.SeatId, entry.NodeId)] = entry.Level;
                    }
                }
            }

            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats != null)
            {
                foreach (var seat in seats)
                {
                    if (seat != null && seat.DefaultUnlocked)
                    {
                        _unlockedSeats.Add(seat.SeatId);
                    }
                }
            }

            // 教座之心默认点亮；新档给少量信仰便于调试布道树
            if (GetTechNodeLevel(1) <= 0)
            {
                _techLevels[1] = 1;
            }

            if (isFresh)
            {
                _faith = 100;
            }

            RebuildCultAttributes();
            RefreshAutoUnlockedSeats();
        }

        public void SaveTo(SaveData savingData)
        {
            if (savingData == null)
            {
                return;
            }

            savingData.DemonCult ??= new DemonCultPersist();
            savingData.DemonCult.Faith = _faith;
            savingData.DemonCult.TechNodes ??= new List<CultTechNodeLevelPersist>();
            savingData.DemonCult.TechNodes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                savingData.DemonCult.TechNodes.Add(new CultTechNodeLevelPersist
                {
                    NodeId = pair.Key,
                    Level = pair.Value,
                });
            }
            savingData.DemonCult.AncientSeats ??= new List<CultAncientSeatPersist>();
            savingData.DemonCult.AncientSeats.Clear();
            foreach (var seatId in _unlockedSeats)
            {
                savingData.DemonCult.AncientSeats.Add(new CultAncientSeatPersist { SeatId = seatId, Unlocked = true });
            }
            savingData.DemonCult.SeatTechNodes ??= new List<CultSeatTechNodeLevelPersist>();
            savingData.DemonCult.SeatTechNodes.Clear();
            foreach (var pair in _seatTechLevels)
            {
                if (pair.Value <= 0) continue;
                savingData.DemonCult.SeatTechNodes.Add(new CultSeatTechNodeLevelPersist
                {
                    SeatId = pair.Key.seatId,
                    NodeId = pair.Key.nodeId,
                    Level = pair.Value,
                });
            }
        }

        public bool IsSeatUnlocked(int seatId) => seatId <= 0 || _unlockedSeats.Contains(seatId);

        public int GetTechNodeCount(int seatId)
        {
            int count = 0;
            var table = CfgMgr.Cfgs?.TbCultSeatTechNode?.DataList;
            if (table == null) return count;
            foreach (var node in table)
            {
                if (node != null && node.SeatId == seatId && GetSeatTechNodeLevel(seatId, node.NodeId) > 0) count++;
            }
            return count;
        }

        public void RefreshAutoUnlockedSeats()
        {
            var seats = CfgMgr.Cfgs?.TbCultAncientSeat?.DataList;
            if (seats == null) return;
            var changed = false;
            foreach (var seat in seats)
            {
                if (seat == null || _unlockedSeats.Contains(seat.SeatId)) continue;
                if (seat.DefaultUnlocked || _logic == null || _logic.CheckCommonCondsAll(seat.UnlockConds))
                {
                    _unlockedSeats.Add(seat.SeatId);
                    changed = true;
                }
            }
            if (changed) OnCultChanged?.Invoke();
        }

        public int GetTechNodeLevel(int nodeId)
        {
            return _techLevels.TryGetValue(nodeId, out var level) ? level : 0;
        }

        public int GetUnlockedTechCount()
        {
            int count = 0;
            foreach (var pair in _techLevels)
            {
                if (pair.Value > 0)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetSeatTechNodeLevel(int seatId, int nodeId)
            => _seatTechLevels.TryGetValue((seatId, nodeId), out var level) ? level : 0;

        public CultTechNodeVisualState GetSeatTechNodeVisualState(int seatId, int nodeId)
        {
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(nodeId);
            if (node == null || node.SeatId != seatId || !IsSeatUnlocked(seatId)) return CultTechNodeVisualState.Locked;
            int current = GetSeatTechNodeLevel(seatId, nodeId);
            if (current >= Math.Max(1, node.MaxLevel)) return CultTechNodeVisualState.Unlocked;
            var level = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckSeatPrerequisites(seatId, level)) return CultTechNodeVisualState.Locked;
            return _faith >= level.FaithCost ? CultTechNodeVisualState.Unlockable : CultTechNodeVisualState.InsufficientFaith;
        }

        public bool TryUnlockSeatTechNode(int seatId, int nodeId, out string failReason)
        {
            failReason = null;
            var node = CfgMgr.Cfgs?.TbCultSeatTechNode?.GetOrDefault(nodeId);
            if (node == null || node.SeatId != seatId) { failReason = "no_cfg"; return false; }
            int current = GetSeatTechNodeLevel(seatId, nodeId);
            if (current >= Math.Max(1, node.MaxLevel)) { failReason = "max_level"; return false; }
            var level = CfgMgr.Cfgs?.TbCultSeatTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null || !CheckSeatPrerequisites(seatId, level)) { failReason = "prereq"; return false; }
            if (_faith < level.FaithCost) { failReason = "faith"; return false; }
            _faith -= level.FaithCost;
            _seatTechLevels[(seatId, nodeId)] = current + 1;
            OnCultChanged?.Invoke();
            return true;
        }

        bool CheckSeatPrerequisites(int seatId, CultSeatTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null) return true;
            foreach (var id in level.PrereqNodeIds)
            {
                if (GetSeatTechNodeLevel(seatId, id) <= 0) return false;
            }
            return true;
        }

        public void AddFaith(long amount)
        {
            if (amount == 0)
            {
                return;
            }

            _faith = Math.Max(0, _faith + amount);
            OnCultChanged?.Invoke();
        }

        public CultTechNodeVisualState GetTechNodeVisualState(int nodeId)
        {
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                return CultTechNodeVisualState.Locked;
            }
            int current = GetTechNodeLevel(nodeId);
            if (current >= Math.Max(1, node.MaxLevel))
            {
                return CultTechNodeVisualState.Unlocked;
            }

            var level = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null)
            {
                return CultTechNodeVisualState.Locked;
            }

            if (!CheckPrerequisites(level))
            {
                return CultTechNodeVisualState.Locked;
            }

            return _faith >= level.FaithCost
                ? CultTechNodeVisualState.Unlockable
                : CultTechNodeVisualState.InsufficientFaith;
        }

        void RebuildCultAttributes()
        {
            _cultAttributes.Clear();
            foreach (var pair in _techLevels)
            {
                if (pair.Value <= 0) continue;
                for (var level = 1; level <= pair.Value; level++)
                {
                    var cfg = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(pair.Key, level);
                    if (cfg == null || cfg.CultAttrValue == 0 || string.IsNullOrEmpty(cfg.CultAttr)) continue;
                    if (!Enum.TryParse(cfg.CultAttr, true, out ECultAttribute attribute)
                        || attribute == ECultAttribute.None)
                    {
                        continue;
                    }
                    _cultAttributes[attribute] = GetCultAttributeValue(attribute) + cfg.CultAttrValue;
                }
            }
        }

        public bool TryUnlockNode(int nodeId, out string failReason)
        {
            failReason = null;
            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                failReason = "no_cfg";
                return false;
            }
            int current = GetTechNodeLevel(nodeId);
            if (current >= Math.Max(1, node.MaxLevel))
            {
                failReason = "max_level";
                return false;
            }

            var level = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(nodeId, current + 1);
            if (level == null)
            {
                failReason = "no_level_cfg";
                return false;
            }

            if (!CheckPrerequisites(level))
            {
                failReason = "prereq";
                return false;
            }

            if (_faith < level.FaithCost)
            {
                failReason = "faith";
                return false;
            }

            _faith -= level.FaithCost;
            _techLevels[nodeId] = current + 1;
            RebuildCultAttributes();
            OnCultChanged?.Invoke();
            return true;
        }

        bool CheckPrerequisites(CultTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null || level.PrereqNodeIds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < level.PrereqNodeIds.Count; i++)
            {
                if (GetTechNodeLevel(level.PrereqNodeIds[i]) <= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
