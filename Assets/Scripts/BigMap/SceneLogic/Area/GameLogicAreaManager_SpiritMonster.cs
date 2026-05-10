using cfg.demo;
using My.Config;
using My.Map;
using System.Collections.Generic;
using My.Map.Entity;
using static My.Map.Fight.FightStruct;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    public partial class GameLogicAreaManager
    {
        private float _lastSpiritBudgetRefreshLogicTime;
        private readonly List<long> _runningSpriteEntites = new();
        private readonly Dictionary<long, int> _hSpiritConsumedBudget = new();
        private readonly List<SpiritMonsterTypeBudget> _spiritSpawnScratch = new();

        // 区域清理：在 Repo.Clear 之前强制删掉仍跟踪的影怪，避免泄漏与脏引用。
        private void SpiritMonster_OnAreaCleanBeforeRepoClear()
        {
            if (Repo != null)
            {
                foreach (var id in _runningSpriteEntites)
                {
                    if (Repo.HasRecord(id))
                    {
                        ForceDestroyEntityNow(id, "spirit_area_clean");
                    }
                }
            }

            _runningSpriteEntites.Clear();
            _hSpiritConsumedBudget.Clear();
            _lastSpiritBudgetRefreshLogicTime = 0f;
        }

        private void SpiritMonster_SweepStaleTracked()
        {
            for (int i = _runningSpriteEntites.Count - 1; i >= 0; i--)
            {
                long id = _runningSpriteEntites[i];
                if (Repo == null || !Repo.HasRecord(id))
                {
                    _runningSpriteEntites.RemoveAt(i);
                    _hSpiritConsumedBudget.Remove(id);
                }
            }
        }

        private void SpiritMonster_DestroyAllTracked(string reason)
        {
            if (Repo == null)
            {
                _runningSpriteEntites.Clear();
                _hSpiritConsumedBudget.Clear();
                return;
            }

            foreach (var id in _runningSpriteEntites)
            {
                if (Repo.HasRecord(id))
                {
                    ForceDestroyEntityNow(id, reason);
                }
            }

            _runningSpriteEntites.Clear();
            _hSpiritConsumedBudget.Clear();
        }

        private int SpiritMonster_ComputeUsedBudget()
        {
            int sum = 0;
            foreach (var kv in _hSpiritConsumedBudget)
            {
                if (Repo != null && Repo.HasRecord(kv.Key))
                {
                    sum += kv.Value;
                }
            }

            return sum;
        }

        private void SpiritMonster_PruneOverBudget(int maxTotalBudget)
        {
            while (SpiritMonster_ComputeUsedBudget() > maxTotalBudget && _runningSpriteEntites.Count > 0)
            {
                long bestId = _runningSpriteEntites[0];
                int bestCost = -1;
                foreach (var id in _runningSpriteEntites)
                {
                    if (!_hSpiritConsumedBudget.TryGetValue(id, out var c))
                    {
                        c = 1;
                    }

                    if (c > bestCost)
                    {
                        bestCost = c;
                        bestId = id;
                    }
                }

                _runningSpriteEntites.Remove(bestId);
                _hSpiritConsumedBudget.Remove(bestId);
                if (Repo != null && Repo.HasRecord(bestId))
                {
                    ForceDestroyEntityNow(bestId, "spirit_budget_prune");
                }
            }
        }

        private SpiritMonsterTypeBudget SpiritMonster_PickWeightedSpawn(int remainingBudget)
        {
            _spiritSpawnScratch.Clear();
            int sanCorruptLevel = logicManager.playerLogicEntity.SanCorruptLevel;
            foreach (var row in CfgMgr.Cfgs.TbSpiritMonsterTypeBudget.DataList)
            {
                if (row == null)
                {
                    continue;
                }

                int cost = Mathf.Max(1, row.BudgetCost);
                if (sanCorruptLevel < row.MinSanCorruptLevel)
                {
                    continue;
                }

                if (row.SpawnWeight <= 0)
                {
                    continue;
                }

                if (cost > remainingBudget)
                {
                    continue;
                }

                _spiritSpawnScratch.Add(row);
            }

            if (_spiritSpawnScratch.Count == 0)
            {
                return null;
            }

            int totalW = 0;
            foreach (var r in _spiritSpawnScratch)
            {
                totalW += r.SpawnWeight;
            }

            if (totalW <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalW);
            foreach (var r in _spiritSpawnScratch)
            {
                roll -= r.SpawnWeight;
                if (roll < 0)
                {
                    return r;
                }
            }

            return _spiritSpawnScratch[_spiritSpawnScratch.Count - 1];
        }

        private bool SpiritMonster_TrySpawnOne(SpiritMonsterTypeBudget row)
        {
            var npcId = PlayerGamePlayRule.GetTrueHSpiritName(row.NpcBaseType, logicManager.playerDataManager.Level);

            var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(npcId);
            if (npcCfg == null)
            {
                Debug.LogError($"SpiritMonster_TrySpawnOne missing npc cfg {npcId}");
                return false;
            }

            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float rad = UnityEngine.Random.Range(2.5f, 9f);
            var offset = new Vector2(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad);

            var initInfo = new EntityInitInfo4Npc
            {
                CfgId = npcId,
                Position = logicManager.playerLogicEntity.Pos + offset,
                IsPeace = npcCfg.IsPeace,
                EnmityConfId = npcCfg.EmnityCfgId,
                OrgFactionId = (EFactionId)npcCfg.FactionId,
            };

            var record = CreateEntityRecordFromInitInfo(initInfo);
            if (record == null)
            {
                Debug.LogError("TickRefreshSpiritMonster spawn fail (CreateEntityRecordFromInitInfo).");
                return false;
            }

            logicManager.AddNewEntityRecord(record);

            int cost = Mathf.Max(1, row.BudgetCost);
            _runningSpriteEntites.Add(record.Id);
            _hSpiritConsumedBudget[record.Id] = cost;
            return true;
        }

        protected void TickRefreshSpiritMonster(float dt)
        {
            if (logicManager.playerLogicEntity == null || Repo == null)
            {
                return;
            }

            if (logicManager.PlayerHumanMode)
            {
                SpiritMonster_DestroyAllTracked("spirit_human_mode");
                return;
            }

            SpiritMonster_SweepStaleTracked();

            var sanCfg = CfgMgr.Cfgs.TbPlayerSanCorruptLevel.GetOrDefault(logicManager.playerLogicEntity.SanCorruptLevel);
            if (sanCfg == null)
            {
                return;
            }

            int maxBudget = Mathf.Max(0, sanCfg.SpiritMonsterTotalBudget);
            float interval = Mathf.Max(0.25f, sanCfg.SpiritMonsterRefreshIntervalSec);

            if (maxBudget <= 0)
            {
                if (_runningSpriteEntites.Count > 0)
                {
                    SpiritMonster_DestroyAllTracked("spirit_san_budget_zero");
                }

                return;
            }

            SpiritMonster_PruneOverBudget(maxBudget);

            if (LogicTime.time - _lastSpiritBudgetRefreshLogicTime < interval)
            {
                return;
            }

            _lastSpiritBudgetRefreshLogicTime = LogicTime.time;

            int used = SpiritMonster_ComputeUsedBudget();
            int safety = 0;
            while (used < maxBudget && safety++ < 64)
            {
                int remain = maxBudget - used;
                var pick = SpiritMonster_PickWeightedSpawn(remain);
                if (pick == null)
                {
                    break;
                }

                if (!SpiritMonster_TrySpawnOne(pick))
                {
                    break;
                }

                used = SpiritMonster_ComputeUsedBudget();
            }
        }

        protected void OnHSpiritClear(long entityId)
        {
            if (!_runningSpriteEntites.Remove(entityId))
            {
                return;
            }

            _hSpiritConsumedBudget.Remove(entityId);

            Repo.Records.TryGetValue(entityId, out var rawRec);
            if (logicManager.playerLogicEntity == null)
            {
                return;
            }

            string cfgId = rawRec != null ? rawRec.CfgId : string.Empty;
            long restore = PlayerGamePlayRule.GetHSpiritRestoreSan(cfgId);
            Debug.Log("h spirit killed, restore sanity " + restore);
            logicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerSanity, restore, false, EDmgFlag.None, null);
        }
    }
}
