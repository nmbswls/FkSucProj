using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Logic;
using My;
using UnityEngine;

namespace My.Map.Entity
{
    public static class FishingSpotFishRoll
    {
        // 从 TbFishingSpotFish 按 spot_id 取行，过滤 unlock_conds 后按 weight 加权
        public static string RollFishItemId(FishingSpot cfg, GameLogicManager glm)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.Id) || glm == null || CfgMgr.Cfgs == null)
            {
                return string.Empty;
            }

            var rows = CfgMgr.Cfgs.TbFishingSpotFish.DataList;
            var pool = new List<FishingSpotFish>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.SpotId != cfg.Id)
                {
                    continue;
                }

                if (!glm.CheckCommonCondsAll(row.UnlockConds))
                {
                    continue;
                }

                pool.Add(row);
            }

            if (pool.Count == 0)
            {
                return string.Empty;
            }

            int sum = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var w = pool[i].Weight;
                if (w > 0) sum += w;
            }

            if (sum <= 0)
            {
                return pool[0].FishItemId;
            }

            int r = Random.Range(0, sum);
            for (int i = 0; i < pool.Count; i++)
            {
                var e = pool[i];
                if (e.Weight <= 0) continue;
                r -= e.Weight;
                if (r < 0)
                {
                    return e.FishItemId;
                }
            }

            return pool[pool.Count - 1].FishItemId;
        }
    }

    public class FishingSpotLogicEntity : LogicEntityBase
    {
        public FishingSpot CacheCfg { get; private set; }

        public int RemainingUses { get; private set; }

        private bool _miniGameOpen;

        public FishingSpotLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var fishRec = bindingRecord as LogicEntityRecord4FishingSpot;
            SrcUniqName = fishRec != null ? fishRec.SrcUniqName : string.Empty;
            CacheCfg = CfgMgr.Cfgs.TbFishingSpot.GetOrDefault(cfgId);
            if (CacheCfg == null)
            {
                Debug.LogError($"[FishingSpot] Luban cfg not found: {cfgId}");
            }

            var day = logicManager.SettlementDayIndex;
            var st = logicManager.worldPersistState.GetOrCreateFishingSpotState(SrcUniqName, cfgId, day);
            if (st == null)
            {
                Debug.LogError($"[FishingSpot] invalid UniqName for entity cfg={cfgId}; Remaining forced to 0.");
                RemainingUses = 0;
            }
            else
            {
                RemainingUses = st.Remaining;
            }
        }

        public override EEntityType Type => EEntityType.FishingSpot;

        public bool CanFishNow()
        {
            return RemainingUses > 0 && !_miniGameOpen && CacheCfg != null;
        }

        public void SetMiniGameOpen(bool v)
        {
            _miniGameOpen = v;
        }

        public void ReloadRemainingFromPlayerSave()
        {
            var st = LogicManager.worldPersistState.GetFishingSpotStateOrNull(SrcUniqName);
            if (st != null)
            {
                RemainingUses = st.Remaining;
            }
        }

        /// <summary>
        /// 小游戏结束后调用：扣次数、给鱼（道具）。
        /// </summary>
        public bool TryCompleteOneCatchAfterMiniGame()
        {
            if (RemainingUses <= 0 || CacheCfg == null)
            {
                return false;
            }

            var fishId = FishingSpotFishRoll.RollFishItemId(CacheCfg, LogicManager);
            if (string.IsNullOrEmpty(fishId))
            {
                Debug.LogWarning("[FishingSpot] empty fish roll");
                return false;
            }

            if (!LogicManager.playerDataManager.CanGainItems(fishId, 1))
            {
                Debug.LogWarning("[FishingSpot] bag cannot accept item " + fishId);
                return false;
            }

            LogicManager.playerDataManager.TryGiveItem(fishId, 1, 0);
            LogicManager.worldPersistState.TryConsumeOneFishingUse(SrcUniqName);
            RemainingUses = Mathf.Max(0, RemainingUses - 1);
            return true;
        }
    }
}
