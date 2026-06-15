using System;
using System.Collections.Generic;
using My.Config;
using My.Map.Logic;
using My.MiniGame.Dream;
using My.Saving;
using cfg.demo;

namespace My
{
    // CharacterKey 档案：具名 NPC 的运行时 LocalSwitch 以本 Registry 为唯一真相源；Spawn 时 TryApplyToRecordBeforeSpawn 注入 Record。
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
                DesireCrystalTaken = s.DesireCrystalTaken,
                DesireCrystalTakenDay = s.DesireCrystalTakenDay,
                FinishedUniqDreamingIds = s.FinishedUniqDreamingIds != null
                    ? new List<string>(s.FinishedUniqDreamingIds)
                    : new List<string>(),
                DreamEntryWinCounts = CloneDreamEntryWinCounts(s.DreamEntryWinCounts),
                FavorValue = s.FavorValue,
                GiftsGivenToday = s.GiftsGivenToday,
                LastGiftSettlementDay = s.LastGiftSettlementDay,
            };
        }

        NpcCharacterPersistData GetOrCreate(string characterKey)
        {
            if (!_byKey.TryGetValue(characterKey, out var st) || st == null)
            {
                st = new NpcCharacterPersistData();
                _byKey[characterKey] = st;
            }

            return st;
        }

        static void NormalizeGiftDayCounter(NpcCharacterPersistData st, int settlementDay)
        {
            if (st == null)
            {
                return;
            }

            if (st.LastGiftSettlementDay != settlementDay)
            {
                st.GiftsGivenToday = 0;
                st.LastGiftSettlementDay = settlementDay;
            }
        }

        public int GetFavorValue(string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey))
            {
                return 0;
            }

            return _byKey.TryGetValue(characterKey, out var st) && st != null ? st.FavorValue : 0;
        }

        public void AddFavorValue(string characterKey, int delta)
        {
            if (string.IsNullOrEmpty(characterKey) || delta == 0)
            {
                return;
            }

            var st = GetOrCreate(characterKey);
            st.FavorValue = Math.Max(0, st.FavorValue + delta);
        }

        public int GetFavorLevel(string characterKey)
        {
            int favor = GetFavorValue(characterKey);
            if (string.IsNullOrEmpty(characterKey) || CfgMgr.Cfgs?.TbCharacterFavorInfo?.DataList == null)
            {
                return 0;
            }

            int level = 0;
            foreach (var row in CfgMgr.Cfgs.TbCharacterFavorInfo.DataList)
            {
                if (row == null || row.Key != characterKey)
                {
                    continue;
                }

                if (favor >= row.NeedValue && row.FavorLevel > level)
                {
                    level = row.FavorLevel;
                }
            }

            return level;
        }

        public bool CanGiveGiftToday(string characterKey, int giftsPerDay, int settlementDay)
        {
            if (string.IsNullOrEmpty(characterKey) || giftsPerDay <= 0)
            {
                return false;
            }

            if (!_byKey.TryGetValue(characterKey, out var st) || st == null)
            {
                return true;
            }

            NormalizeGiftDayCounter(st, settlementDay);
            return st.GiftsGivenToday < giftsPerDay;
        }

        public void RecordGiftGiven(string characterKey, int settlementDay)
        {
            if (string.IsNullOrEmpty(characterKey))
            {
                return;
            }

            var st = GetOrCreate(characterKey);
            NormalizeGiftDayCounter(st, settlementDay);
            st.GiftsGivenToday++;
        }

        public int GetGiftsGivenToday(string characterKey, int settlementDay)
        {
            if (string.IsNullOrEmpty(characterKey) || !_byKey.TryGetValue(characterKey, out var st) || st == null)
            {
                return 0;
            }

            NormalizeGiftDayCounter(st, settlementDay);
            return st.GiftsGivenToday;
        }

        static List<DreamEntryTendencyWinCounts> CloneDreamEntryWinCounts(List<DreamEntryTendencyWinCounts> src)
        {
            if (src == null || src.Count == 0)
            {
                return new List<DreamEntryTendencyWinCounts>();
            }

            var list = new List<DreamEntryTendencyWinCounts>(src.Count);
            foreach (var item in src)
            {
                if (item == null) continue;
                list.Add(new DreamEntryTendencyWinCounts
                {
                    CharDreamEntryId = item.CharDreamEntryId,
                    ForceWins = item.ForceWins,
                    SoothingWins = item.SoothingWins,
                    TrickWins = item.TrickWins,
                });
            }

            return list;
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

        public bool ContainsRuntimeLocalSwitch(string characterKey, string switchName)
        {
            if (string.IsNullOrEmpty(characterKey) || string.IsNullOrEmpty(switchName))
            {
                return false;
            }

            if (!_byKey.TryGetValue(characterKey, out var st) || st?.LocalSwitches == null)
            {
                return false;
            }

            return st.LocalSwitches.Contains(switchName);
        }

        public void SetRuntimeLocalSwitch(string characterKey, string switchName, bool isOn)
        {
            if (string.IsNullOrEmpty(characterKey) || string.IsNullOrEmpty(switchName))
            {
                return;
            }

            if (!_byKey.TryGetValue(characterKey, out var st))
            {
                st = new NpcCharacterPersistData();
                _byKey[characterKey] = st;
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

        public void AddCharLocalSwitches()
        {

        }

        public void IncrementDreamEntryTendencyWin(string characterKey, int entryId, DreamTendencyKind tendency)
        {
            if (string.IsNullOrEmpty(characterKey) || entryId <= 0)
            {
                return;
            }

            var st = GetOrCreate(characterKey);
            st.DreamEntryWinCounts ??= new List<DreamEntryTendencyWinCounts>();

            DreamEntryTendencyWinCounts row = null;
            foreach (var item in st.DreamEntryWinCounts)
            {
                if (item != null && item.CharDreamEntryId == entryId)
                {
                    row = item;
                    break;
                }
            }

            if (row == null)
            {
                row = new DreamEntryTendencyWinCounts { CharDreamEntryId = entryId };
                st.DreamEntryWinCounts.Add(row);
            }

            switch (tendency)
            {
                case DreamTendencyKind.Force:
                    row.ForceWins++;
                    break;
                case DreamTendencyKind.Soothing:
                    row.SoothingWins++;
                    break;
                default:
                    row.TrickWins++;
                    break;
            }
        }

        public bool TryGetDreamEntryWinCounts(string characterKey, int entryId, out DreamEntryTendencyWinCounts data)
        {
            data = null;
            if (string.IsNullOrEmpty(characterKey) || entryId <= 0)
            {
                return false;
            }

            if (!_byKey.TryGetValue(characterKey, out var st) || st?.DreamEntryWinCounts == null)
            {
                return false;
            }

            foreach (var item in st.DreamEntryWinCounts)
            {
                if (item != null && item.CharDreamEntryId == entryId)
                {
                    data = item;
                    return true;
                }
            }

            return false;
        }

        public bool HasAnyDreamEntryWin(string characterKey, int entryId)
        {
            if (!TryGetDreamEntryWinCounts(characterKey, entryId, out var data) || data == null)
            {
                return false;
            }

            return data.ForceWins > 0 || data.SoothingWins > 0 || data.TrickWins > 0;
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

        public bool IsDesireCrystalTaken(string characterKey)
        {
            return !string.IsNullOrEmpty(characterKey)
                   && _byKey.TryGetValue(characterKey, out var st)
                   && st != null
                   && st.DesireCrystalTaken;
        }

        public void SetDesireCrystalTaken(string characterKey, bool taken)
        {
            if (string.IsNullOrEmpty(characterKey))
            {
                return;
            }

            if (!_byKey.TryGetValue(characterKey, out var st))
            {
                st = new NpcCharacterPersistData();
                _byKey[characterKey] = st;
            }

            st.DesireCrystalTaken = taken;
        }

        public void RestoreNamedNpcDesireCrystal(string characterKey)
        {
            SetDesireCrystalTaken(characterKey, false);
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
