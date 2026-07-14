using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Logic;
using My.Saving;

namespace My
{
    // 具名交互点 / 可移除障碍：按 UniqName 稀疏存档本地开关、整数值与定时刷新状态。
    public sealed class MapInteractPointPersistRegistry
    {
        readonly Dictionary<string, MapInteractPointPersistData> _byUniqName = new(StringComparer.Ordinal);

        public void LoadFromSave(IDictionary<string, MapInteractPointPersistData> source)
        {
            _byUniqName.Clear();
            if (source == null)
            {
                return;
            }

            foreach (var kv in source)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value == null)
                {
                    continue;
                }

                _byUniqName[kv.Key] = Clone(kv.Value);
            }
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.InteractPointByUniqName ??= new Dictionary<string, MapInteractPointPersistData>(StringComparer.Ordinal);
            pd.InteractPointByUniqName.Clear();

            foreach (var kv in _byUniqName)
            {
                pd.InteractPointByUniqName[kv.Key] = Clone(kv.Value);
            }
        }

        public bool ContainsLocalSwitch(string uniqName, string switchName)
        {
            if (string.IsNullOrEmpty(uniqName) || string.IsNullOrEmpty(switchName))
            {
                return false;
            }

            return _byUniqName.TryGetValue(uniqName, out var st)
                   && st?.LocalSwitches != null
                   && st.LocalSwitches.Contains(switchName);
        }

        public void SetLocalSwitch(string uniqName, string switchName, bool isOn)
        {
            if (string.IsNullOrEmpty(uniqName) || string.IsNullOrEmpty(switchName))
            {
                return;
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            var list = st.LocalSwitches;
            if (isOn)
            {
                if (list == null)
                {
                    list = new List<string>();
                    st.LocalSwitches = list;
                }

                if (!list.Contains(switchName))
                {
                    list.Add(switchName);
                    list.Sort(StringComparer.Ordinal);
                }
            }
            else
            {
                if (list == null)
                {
                    return;
                }

                list.Remove(switchName);
                st.LocalSwitches = list.Count == 0 ? null : list;
                PruneIfEmpty(uniqName, st);
            }
        }

        public void SetStatus(string uniqName, int status)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            st.HasStatus = true;
            st.Status = status;
        }

        public int GetLocalIntValue(string uniqName, string key)
        {
            if (string.IsNullOrEmpty(uniqName) || string.IsNullOrEmpty(key))
            {
                return 0;
            }

            return _byUniqName.TryGetValue(uniqName, out var st)
                   && st?.LocalIntValues != null
                   && st.LocalIntValues.TryGetValue(key, out int value)
                ? value
                : 0;
        }

        public void SetLocalIntValue(string uniqName, string key, int value)
        {
            if (string.IsNullOrEmpty(uniqName) || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                if (value == 0)
                {
                    return;
                }

                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            if (value == 0)
            {
                st.LocalIntValues?.Remove(key);
                if (st.LocalIntValues?.Count == 0)
                {
                    st.LocalIntValues = null;
                }
                PruneIfEmpty(uniqName, st);
                return;
            }

            st.LocalIntValues ??= new Dictionary<string, int>(StringComparer.Ordinal);
            st.LocalIntValues[key] = value;
        }

        public bool TryGetLastRefreshSettlementDay(string uniqName, out int settlementDayIndex)
        {
            settlementDayIndex = 0;
            if (string.IsNullOrEmpty(uniqName)
                || !_byUniqName.TryGetValue(uniqName, out var st)
                || st?.TimedRefresh?.HasLastRefreshSettlementDay != true)
            {
                return false;
            }

            settlementDayIndex = st.TimedRefresh.LastRefreshSettlementDay;
            return true;
        }

        public void RecordRefreshSettlementDay(string uniqName, int settlementDayIndex)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            st.TimedRefresh ??= new TimedRefreshState();
            st.TimedRefresh.HasLastRefreshSettlementDay = true;
            st.TimedRefresh.LastRefreshSettlementDay = settlementDayIndex;
        }

        public void ReplaceRuntimeLocalSwitches(string uniqName, IEnumerable<string> activeSwitches)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            List<string> list = null;
            if (activeSwitches != null)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var s in activeSwitches)
                {
                    if (string.IsNullOrEmpty(s) || !seen.Add(s))
                    {
                        continue;
                    }

                    list ??= new List<string>();
                    list.Add(s);
                }

                if (list != null && list.Count > 0)
                {
                    list.Sort(StringComparer.Ordinal);
                }
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                if (list == null || list.Count == 0)
                {
                    return;
                }

                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            st.LocalSwitches = list;
            PruneIfEmpty(uniqName, st);
        }

        public void ReplaceRuntimeLocalIntValues(string uniqName, IDictionary<string, int> values)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            Dictionary<string, int> copy = null;
            if (values != null)
            {
                foreach (var pair in values)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value == 0)
                    {
                        continue;
                    }

                    copy ??= new Dictionary<string, int>(StringComparer.Ordinal);
                    copy[pair.Key] = pair.Value;
                }
            }

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                if (copy == null)
                {
                    return;
                }

                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            st.LocalIntValues = copy;
            PruneIfEmpty(uniqName, st);
        }

        public void ReplaceRuntimeTimedRefreshState(string uniqName, TimedRefreshState refreshState)
        {
            if (string.IsNullOrEmpty(uniqName))
            {
                return;
            }

            TimedRefreshState copy = Clone(refreshState);

            if (!_byUniqName.TryGetValue(uniqName, out var st) || st == null)
            {
                if (copy == null)
                {
                    return;
                }

                st = new MapInteractPointPersistData();
                _byUniqName[uniqName] = st;
            }

            st.TimedRefresh = copy;
            PruneIfEmpty(uniqName, st);
        }

        public void TryApplyToRecordBeforeSpawn(LogicEntityRecord rec)
        {
            if (rec == null || string.IsNullOrEmpty(rec.SrcUniqName))
            {
                return;
            }

            if (!MapInteractPointPersistUtil.ShouldPersistEntity(rec.EntityType, rec.CfgId))
            {
                return;
            }

            if (!_byUniqName.TryGetValue(rec.SrcUniqName, out var st) || st == null)
            {
                return;
            }

            rec.LocalSwitches = CloneList(st.LocalSwitches);
            rec.LocalIntValues = CloneIntValues(st.LocalIntValues);
            if (rec is LogicEntityRecord4InteractPoint interactPointRecord)
            {
                if (st.HasStatus)
                {
                    interactPointRecord.Status = st.Status;
                }
                interactPointRecord.TimedRefresh = Clone(st.TimedRefresh);
            }
        }

        static MapInteractPointPersistData Clone(MapInteractPointPersistData s)
        {
            if (s == null)
            {
                return null;
            }

            return new MapInteractPointPersistData
            {
                HasStatus = s.HasStatus,
                Status = s.Status,
                LocalSwitches = CloneList(s.LocalSwitches),
                LocalIntValues = CloneIntValues(s.LocalIntValues),
                TimedRefresh = Clone(s.TimedRefresh),
            };
        }

        void PruneIfEmpty(string uniqName, MapInteractPointPersistData st)
        {
            if (st?.HasStatus != true
                && (st?.LocalSwitches == null || st.LocalSwitches.Count == 0)
                && (st?.LocalIntValues == null || st.LocalIntValues.Count == 0)
                && st?.TimedRefresh?.HasLastRefreshSettlementDay != true)
            {
                _byUniqName.Remove(uniqName);
            }
        }

        static List<string> CloneList(List<string> src)
        {
            if (src == null || src.Count == 0)
            {
                return null;
            }

            return new List<string>(src);
        }

        static Dictionary<string, int> CloneIntValues(Dictionary<string, int> src)
        {
            if (src == null || src.Count == 0)
            {
                return null;
            }

            return new Dictionary<string, int>(src, StringComparer.Ordinal);
        }

        static TimedRefreshState Clone(TimedRefreshState src)
        {
            if (src?.HasLastRefreshSettlementDay != true)
            {
                return null;
            }

            return new TimedRefreshState
            {
                HasLastRefreshSettlementDay = true,
                LastRefreshSettlementDay = src.LastRefreshSettlementDay,
            };
        }
    }
}
