using System;
using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My.Config;
using My.Player;
using My.Saving;
using UnityEngine;

namespace My.MiniGame.Dream
{
    // 抽象团体日刷 / 日限 / 阶段结算；落盘字段在 PlayerData，运行时由本服务持有
    public static class AbstractGroupDreamService
    {
        static readonly Dictionary<string, AbstractGroupPersist> Groups =
            new(StringComparer.Ordinal);

        static AbstractGroupDailyRefreshPersist DailyRefresh = new();
        static DreamDailyLimitPersist DailyLimit = new();

        public static void LoadFromSave(PlayerData pd)
        {
            Groups.Clear();
            if (pd?.AbstractGroupById != null)
            {
                foreach (var kv in pd.AbstractGroupById)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
                    Groups[kv.Key] = CloneGroup(kv.Value);
                }
            }

            DailyRefresh = CloneDaily(pd?.AbstractGroupDailyRefresh);
            DailyLimit = CloneLimit(pd?.DreamDailyLimit);
        }

        public static void ApplyToSave(PlayerData pd)
        {
            if (pd == null) return;
            pd.AbstractGroupById ??= new Dictionary<string, AbstractGroupPersist>(StringComparer.Ordinal);
            pd.AbstractGroupById.Clear();
            foreach (var kv in Groups)
            {
                pd.AbstractGroupById[kv.Key] = CloneGroup(kv.Value);
            }

            pd.AbstractGroupDailyRefresh = CloneDaily(DailyRefresh);
            pd.DreamDailyLimit = CloneLimit(DailyLimit);
        }

        public static void OnSettlementDayBalance(GameLogicManager glm)
        {
            if (glm == null) return;
            SyncLimitDay(glm.SettlementDayIndex);
            DailyLimit.DreamUsedToday = false;
            RollDailyGroup(glm, force: true);
        }

        public static bool IsDreamAllowedTonight(GameLogicManager glm, out string failReason)
        {
            failReason = null;
            if (glm == null)
            {
                failReason = "no_logic";
                return false;
            }

            if (glm.DayPeriod != GameLogicManager.EDayPeriod.Night)
            {
                failReason = "not_night";
                return false;
            }

            EnsureDailySynced(glm);
            if (DailyLimit.DreamUsedToday)
            {
                failReason = "used_today";
                return false;
            }

            return true;
        }

        public static bool IsDreamUsedToday(GameLogicManager glm)
        {
            if (glm == null) return true;
            EnsureDailySynced(glm);
            return DailyLimit.DreamUsedToday;
        }

        public static void MarkDreamUsedToday(GameLogicManager glm)
        {
            if (glm == null) return;
            EnsureDailySynced(glm);
            DailyLimit.DreamUsedToday = true;
        }

        public static void EnsureDailySynced(GameLogicManager glm)
        {
            if (glm == null) return;
            SyncLimitDay(glm.SettlementDayIndex);
            if (DailyRefresh.SettlementDayIndex != glm.SettlementDayIndex)
            {
                RollDailyGroup(glm, force: true);
            }
        }

        public static bool TryGetTodayGroupEntry(
            GameLogicManager glm,
            out AbstractGroup groupCfg,
            out AbstractGroupStage stageCfg)
        {
            groupCfg = null;
            stageCfg = null;
            if (glm == null) return false;
            EnsureDailySynced(glm);
            if (string.IsNullOrEmpty(DailyRefresh.SelectedGroupId) || DailyRefresh.SelectedStage <= 0)
            {
                return false;
            }

            groupCfg = CfgMgr.Cfgs?.TbAbstractGroup?.GetOrDefault(DailyRefresh.SelectedGroupId);
            stageCfg = CfgMgr.Cfgs?.TbAbstractGroupStage?.Get(
                DailyRefresh.SelectedGroupId,
                DailyRefresh.SelectedStage);
            return groupCfg != null && stageCfg != null;
        }

        public static bool TryCreateGameplayContext(
            GameLogicManager glm,
            out DreamGameplayContext ctx,
            out string failReason)
        {
            ctx = null;
            failReason = null;
            if (!IsDreamAllowedTonight(glm, out failReason))
            {
                return false;
            }

            if (!TryGetTodayGroupEntry(glm, out var groupCfg, out var stageCfg))
            {
                failReason = "no_group_today";
                return false;
            }

            ctx = new DreamGameplayContext
            {
                ThemeId = stageCfg.DreamThemeId ?? string.Empty,
                ThemeDisplayName = string.IsNullOrEmpty(stageCfg.DreamThemeDisplayName)
                    ? stageCfg.DisplayName
                    : stageCfg.DreamThemeDisplayName,
                CoreMaxHp = Math.Max(1, stageCfg.CoreMaxHp),
                MaxHp = Math.Max(1, stageCfg.PlayerMaxHp),
                BulletDamage = Math.Max(1, stageCfg.BulletDamage),
                AoeDamage = Math.Max(1, stageCfg.AoeDamage),
                ProjectileDamage = Math.Max(1, stageCfg.ProjectileDamage),
                RequiredScore = Math.Max(0, stageCfg.RequiredScore),
                EntrySource = DreamEntrySourceKind.AbstractGroupEntry,
                AbstractGroupId = groupCfg.GroupId,
                AbstractGroupStage = stageCfg.Stage,
            };
            return true;
        }

        public static string ApplySettlement(GameLogicManager glm, DreamSettlementPayload payload)
        {
            if (glm == null || payload == null) return string.Empty;
            if (payload.EntrySource != DreamEntrySourceKind.AbstractGroupEntry) return string.Empty;
            if (string.IsNullOrEmpty(payload.AbstractGroupId) || payload.AbstractGroupStage <= 0)
            {
                return string.Empty;
            }

            var persist = GetOrCreate(payload.AbstractGroupId);
            var stage = payload.AbstractGroupStage;
            IncrementDict(persist.AttemptCountByStage, stage, 1);

            var totalScore = payload.ForceScore + payload.SoothingScore + payload.TrickScore;
            if (persist.BestScoreByStage == null) persist.BestScoreByStage = new Dictionary<int, int>();
            if (!persist.BestScoreByStage.TryGetValue(stage, out var best) || totalScore > best)
            {
                persist.BestScoreByStage[stage] = totalScore;
            }

            if (!payload.Won)
            {
                return string.Empty;
            }

            var clearedBefore = GetDict(persist.ClearCountByStage, stage);
            IncrementDict(persist.ClearCountByStage, stage, 1);
            if (stage > persist.HighestClearedStage)
            {
                persist.HighestClearedStage = stage;
            }

            var note = new StringBuilder();
            var firstClear = clearedBefore <= 0;
            GrantStageRewards(glm, payload.AbstractGroupId, stage, firstClear, note);

            var groupCfg = CfgMgr.Cfgs?.TbAbstractGroup?.GetOrDefault(payload.AbstractGroupId);
            var maxStage = groupCfg != null ? Math.Max(1, groupCfg.MaxStage) : stage;
            if (persist.CurrentStage <= stage && persist.CurrentStage < maxStage)
            {
                persist.CurrentStage = stage + 1;
            }
            else if (persist.CurrentStage < maxStage)
            {
                persist.CurrentStage = Math.Min(maxStage, Math.Max(persist.CurrentStage, stage + 1));
            }

            if (persist.HighestClearedStage >= maxStage && !persist.SecretUnitGranted)
            {
                if (groupCfg == null || groupCfg.RetiredAfterMax)
                {
                    persist.Retired = true;
                }

                var cult = glm.playerDataManager?.ProgressionSystem?.DemonCult;
                if (cult != null && cult.TryAcquireSecretUnit($"abstract_group:{payload.AbstractGroupId}", out var unitId))
                {
                    persist.SecretUnitGranted = true;
                    note.AppendLine($"获得秘会：{unitId}");
                    Debug.Log($"[AbstractGroupDream] Secret unit granted from {payload.AbstractGroupId}: {unitId}");
                }
                else
                {
                    Debug.LogWarning($"[AbstractGroupDream] Failed to grant secret unit for {payload.AbstractGroupId}");
                }
            }

            return note.ToString().TrimEnd();
        }

        static void GrantStageRewards(
            GameLogicManager glm,
            string groupId,
            int stage,
            bool firstClear,
            StringBuilder note)
        {
            var table = CfgMgr.Cfgs?.TbAbstractGroupStageReward;
            if (table?.DataList == null) return;

            foreach (var row in table.DataList)
            {
                if (row == null) continue;
                if (!string.Equals(row.GroupId, groupId, StringComparison.Ordinal)) continue;
                if (row.Stage != stage) continue;
                if (row.OnlyFirstClear && !firstClear) continue;

                if (!string.IsNullOrEmpty(row.ItemId) && row.ItemCount > 0)
                {
                    glm.playerDataManager?.GiveItemToPlayer(row.ItemId, row.ItemCount);
                    note.AppendLine($"物品 {row.ItemId} x{row.ItemCount}");
                }

                if (row.Faith != 0)
                {
                    glm.playerDataManager?.ProgressionSystem?.DemonCult?.AddFaith(row.Faith);
                    note.AppendLine($"信仰 +{row.Faith}");
                }

                if (row.Jingyuan > 0)
                {
                    glm.worldPersistState?.AddJingYuanPoolStored(row.Jingyuan);
                    note.AppendLine($"精元池 +{row.Jingyuan}");
                }

                if (row.FallenBaseAmount > 0)
                {
                    FallenPopulationService.AddBaseAmount(glm.playerDataManager, row.FallenBaseAmount);
                    note.AppendLine($"基础沉沦 +{row.FallenBaseAmount}");
                }
            }
        }

        static void RollDailyGroup(GameLogicManager glm, bool force)
        {
            var day = glm.SettlementDayIndex;
            if (!force && DailyRefresh.SettlementDayIndex == day) return;

            DailyRefresh.SettlementDayIndex = day;
            DailyRefresh.SelectedGroupId = string.Empty;
            DailyRefresh.SelectedStage = 0;

            var groupTable = CfgMgr.Cfgs?.TbAbstractGroup;
            var stageTable = CfgMgr.Cfgs?.TbAbstractGroupStage;
            if (groupTable?.DataList == null || stageTable == null) return;

            var candidates = new List<(string groupId, int stage, int weight)>();
            foreach (var g in groupTable.DataList)
            {
                if (g == null || string.IsNullOrEmpty(g.GroupId)) continue;
                if (!glm.CheckCommonCondsAll(g.UnlockConds)) continue;

                var persist = GetOrCreate(g.GroupId);
                if (persist.Retired) continue;

                var stage = Math.Max(1, persist.CurrentStage);
                if (stage > Math.Max(1, g.MaxStage)) continue;

                var stageCfg = stageTable.Get(g.GroupId, stage);
                if (stageCfg == null || stageCfg.RefreshWeight <= 0) continue;
                if (!glm.CheckCommonCondsAll(stageCfg.AppearConds)) continue;

                candidates.Add((g.GroupId, stage, stageCfg.RefreshWeight));
            }

            if (candidates.Count == 0) return;

            var sum = 0;
            foreach (var c in candidates) sum += Math.Max(1, c.weight);
            var roll = UnityEngine.Random.Range(0, sum);
            var acc = 0;
            foreach (var c in candidates)
            {
                acc += Math.Max(1, c.weight);
                if (roll < acc)
                {
                    DailyRefresh.SelectedGroupId = c.groupId;
                    DailyRefresh.SelectedStage = c.stage;
                    var p = GetOrCreate(c.groupId);
                    p.LastRefreshSettlementDay = day;
                    Debug.Log($"[AbstractGroupDream] Daily pick {c.groupId} stage {c.stage}");
                    return;
                }
            }

            var last = candidates[candidates.Count - 1];
            DailyRefresh.SelectedGroupId = last.groupId;
            DailyRefresh.SelectedStage = last.stage;
        }

        static void SyncLimitDay(int settlementDay)
        {
            if (DailyLimit.SettlementDayIndex == settlementDay) return;
            DailyLimit.SettlementDayIndex = settlementDay;
            DailyLimit.DreamUsedToday = false;
        }

        static AbstractGroupPersist GetOrCreate(string groupId)
        {
            if (!Groups.TryGetValue(groupId, out var p) || p == null)
            {
                p = new AbstractGroupPersist
                {
                    GroupId = groupId,
                    CurrentStage = 1,
                };
                Groups[groupId] = p;
            }

            if (p.CurrentStage < 1) p.CurrentStage = 1;
            p.AttemptCountByStage ??= new Dictionary<int, int>();
            p.ClearCountByStage ??= new Dictionary<int, int>();
            p.BestScoreByStage ??= new Dictionary<int, int>();
            return p;
        }

        static void IncrementDict(Dictionary<int, int> map, int key, int delta)
        {
            if (map == null) return;
            map.TryGetValue(key, out var v);
            map[key] = v + delta;
        }

        static int GetDict(Dictionary<int, int> map, int key)
            => map != null && map.TryGetValue(key, out var v) ? v : 0;

        static AbstractGroupPersist CloneGroup(AbstractGroupPersist src)
        {
            var d = new AbstractGroupPersist
            {
                GroupId = src.GroupId ?? string.Empty,
                CurrentStage = src.CurrentStage < 1 ? 1 : src.CurrentStage,
                HighestClearedStage = src.HighestClearedStage,
                LastRefreshSettlementDay = src.LastRefreshSettlementDay,
                Retired = src.Retired,
                SecretUnitGranted = src.SecretUnitGranted,
                AttemptCountByStage = new Dictionary<int, int>(),
                ClearCountByStage = new Dictionary<int, int>(),
                BestScoreByStage = new Dictionary<int, int>(),
            };
            CopyDict(src.AttemptCountByStage, d.AttemptCountByStage);
            CopyDict(src.ClearCountByStage, d.ClearCountByStage);
            CopyDict(src.BestScoreByStage, d.BestScoreByStage);
            return d;
        }

        static void CopyDict(Dictionary<int, int> from, Dictionary<int, int> to)
        {
            if (from == null || to == null) return;
            foreach (var kv in from) to[kv.Key] = kv.Value;
        }

        static AbstractGroupDailyRefreshPersist CloneDaily(AbstractGroupDailyRefreshPersist src)
        {
            src ??= new AbstractGroupDailyRefreshPersist();
            return new AbstractGroupDailyRefreshPersist
            {
                SettlementDayIndex = src.SettlementDayIndex,
                SelectedGroupId = src.SelectedGroupId ?? string.Empty,
                SelectedStage = src.SelectedStage,
            };
        }

        static DreamDailyLimitPersist CloneLimit(DreamDailyLimitPersist src)
        {
            src ??= new DreamDailyLimitPersist();
            return new DreamDailyLimitPersist
            {
                SettlementDayIndex = src.SettlementDayIndex,
                DreamUsedToday = src.DreamUsedToday,
            };
        }
    }
}
