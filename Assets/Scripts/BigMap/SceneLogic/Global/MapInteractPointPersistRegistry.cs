using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Logic;
using My.Saving;

namespace My
{
    // 具名交互点 / 可移除障碍：按刷新项 UniqName 稀疏存档 LocalSwitch（对齐钓鱼点 UniqName 桶 + NPC LocalSwitch Registry）。
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
            }
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

            if (list == null || list.Count == 0)
            {
                _byUniqName.Remove(uniqName);
                return;
            }

            _byUniqName[uniqName] = new MapInteractPointPersistData
            {
                LocalSwitches = list,
            };
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

            if (!_byUniqName.TryGetValue(rec.SrcUniqName, out var st) || st?.LocalSwitches == null || st.LocalSwitches.Count == 0)
            {
                return;
            }

            rec.LocalSwitches = CloneList(st.LocalSwitches);
        }

        static MapInteractPointPersistData Clone(MapInteractPointPersistData s)
        {
            if (s == null)
            {
                return null;
            }

            return new MapInteractPointPersistData
            {
                LocalSwitches = CloneList(s.LocalSwitches),
            };
        }

        static List<string> CloneList(List<string> src)
        {
            if (src == null || src.Count == 0)
            {
                return null;
            }

            return new List<string>(src);
        }
    }
}
