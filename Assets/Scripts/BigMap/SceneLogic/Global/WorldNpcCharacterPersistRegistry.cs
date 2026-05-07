using System;
using System.Collections.Generic;
using My.Map.Logic;
using My.Saving;

namespace My
{
    // CharacterKey 档案：内存为权威来源；仅在 Spawn 时 Apply 到 Record；运行时仅随 SetLocalSwitch 写入本 Registry
    public sealed class WorldNpcCharacterPersistRegistry
    {
        readonly Dictionary<string, NpcCharacterPersistData> _byKey = new(StringComparer.Ordinal);

        public void LoadFromSave(IDictionary<string, NpcCharacterPersistData> source)
        {
            _byKey.Clear();
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

                _byKey[kv.Key] = Clone(kv.Value);
            }
        }

        public void SaveTo(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            if (pd.NpcCharacterPersistByKey == null)
            {
                pd.NpcCharacterPersistByKey = new Dictionary<string, NpcCharacterPersistData>(StringComparer.Ordinal);
            }
            else
            {
                pd.NpcCharacterPersistByKey.Clear();
            }

            foreach (var kv in _byKey)
            {
                pd.NpcCharacterPersistByKey[kv.Key] = Clone(kv.Value);
            }
        }

        static NpcCharacterPersistData Clone(NpcCharacterPersistData s)
        {
            if (s == null)
            {
                return null;
            }

            return new NpcCharacterPersistData
            {
                LocalSwitches = CloneList(s.LocalSwitches),
            };
        }

        static List<string> CloneList(List<string> src)
        {
            if (src == null)
            {
                return null;
            }

            return new List<string>(src);
        }

        public void ReplaceRuntimeLocalSwitches(string characterKey, IEnumerable<string> activeSwitches)
        {
            if (string.IsNullOrEmpty(characterKey))
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

            if (!_byKey.TryGetValue(characterKey, out var st))
            {
                st = new NpcCharacterPersistData();
                _byKey[characterKey] = st;
            }

            st.LocalSwitches = list == null || list.Count == 0 ? null : new List<string>(list);
        }

        public void TryApplyToRecordBeforeSpawn(LogicEntityRecord4Npc rec)
        {
            if (rec == null || string.IsNullOrEmpty(rec.CharacterKey))
            {
                return;
            }

            if (!_byKey.TryGetValue(rec.CharacterKey, out var st))
            {
                return;
            }

            rec.LocalSwitches = CloneList(st.LocalSwitches);
        }

        public bool TryGetSnapshot(string characterKey, out NpcCharacterPersistData data)
        {
            data = null;
            if (string.IsNullOrEmpty(characterKey))
            {
                return false;
            }

            if (!_byKey.TryGetValue(characterKey, out var st))
            {
                return false;
            }

            data = Clone(st);
            return true;
        }
    }
}
