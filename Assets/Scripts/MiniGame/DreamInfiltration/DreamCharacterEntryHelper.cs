using cfg.demo;
using My.Config;
using My.Map;
using My.Player;
using UnityEngine;

namespace My.MiniGame.Dream
{
    public static class DreamCharacterEntryHelper
    {
        public static bool TryCreateGameplayContext(string characterKey, int entryId, out DreamGameplayContext ctx)
        {
            ctx = null;
            var table = CfgMgr.Cfgs?.TbCharDreamEntryInfo;
            if (table == null)
            {
                Debug.LogWarning("[DreamInfiltration] TbCharDreamEntryInfo missing.");
                return false;
            }

            var entry = table.GetOrDefault(entryId);
            if (entry == null)
            {
                Debug.LogWarning($"[DreamInfiltration] Char dream entry not found: {entryId}");
                return false;
            }

            if (!string.IsNullOrEmpty(characterKey)
                && !string.Equals(entry.CharacterKey, characterKey, System.StringComparison.Ordinal))
            {
                Debug.LogWarning($"[DreamInfiltration] CharacterKey mismatch: {characterKey} vs {entry.CharacterKey}");
                return false;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var psm = glm?.playerDataManager;
            if (!IsCharacterEntryUnlocked(entry, psm, glm))
            {
                Debug.Log("[DreamInfiltration] Character dream entry locked.");
                return false;
            }

            var character = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(entry.CharacterKey);
            var characterName = character?.Name;
            ctx = new DreamGameplayContext
            {
                ThemeId = "character_dream",
                ThemeDisplayName = string.IsNullOrEmpty(characterName)
                    ? $"角色梦境 #{entryId}"
                    : $"{characterName}的梦境",
                EntrySource = DreamEntrySourceKind.CharacterEntry,
                CharacterKey = entry.CharacterKey,
                CharDreamEntryId = entry.Id,
            };
            return true;
        }

        public static bool IsCharacterEntryUnlocked(
            CharDreamEntryInfo entry,
            PlayerSystemManager psm,
            GameLogicManager glm)
        {
            if (entry == null) return false;

            if (glm == null || !glm.CheckCommonCondsAll(entry.ShowConds))
            {
                return false;
            }

            if (!MatchesLocalSwitchConditions(entry, psm))
            {
                return false;
            }

            if (entry.Priority <= 1) return true;
            if (psm == null) return false;

            var prev = FindEntryByPriority(entry.CharacterKey, entry.Priority - 1);
            if (prev == null) return false;

            return psm.HasAnyDreamEntryWin(entry.CharacterKey, prev.Id);
        }

        static bool MatchesLocalSwitchConditions(CharDreamEntryInfo entry, PlayerSystemManager psm)
        {
            if (string.IsNullOrWhiteSpace(entry.HasLocals)
                && string.IsNullOrWhiteSpace(entry.NoLocals))
            {
                return true;
            }

            if (psm == null || string.IsNullOrEmpty(entry.CharacterKey))
            {
                return false;
            }

            foreach (var switchName in SplitSwitchNames(entry.HasLocals))
            {
                if (!psm.NamedNpcHasLocalSwitch(entry.CharacterKey, switchName))
                {
                    return false;
                }
            }

            foreach (var switchName in SplitSwitchNames(entry.NoLocals))
            {
                if (psm.NamedNpcHasLocalSwitch(entry.CharacterKey, switchName))
                {
                    return false;
                }
            }

            return true;
        }

        static System.Collections.Generic.IEnumerable<string> SplitSwitchNames(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                yield break;
            }

            foreach (var part in raw.Split(new[] { ',', ';', '|' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var switchName = part.Trim();
                if (!string.IsNullOrEmpty(switchName))
                {
                    yield return switchName;
                }
            }
        }

        static CharDreamEntryInfo FindEntryByPriority(string characterKey, int priority)
        {
            var list = CfgMgr.Cfgs?.TbCharDreamEntryInfo?.DataList;
            if (list == null || string.IsNullOrEmpty(characterKey)) return null;

            foreach (var row in list)
            {
                if (row != null
                    && row.Priority == priority
                    && string.Equals(row.CharacterKey, characterKey, System.StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }
    }
}
