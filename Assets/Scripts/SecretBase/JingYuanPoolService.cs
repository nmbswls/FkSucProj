using System;
using cfg.demo;
using My.Config;
using My.Player;
using UnityEngine;

namespace My.SecretBase
{
    // 秘密基地精元池：容量随设施等级、存取/分解/升级/秘仪
    // 允许短暂超额；日结开始时丢弃超出容量的部分，便于玩家在当日内分解/转化
    public static class JingYuanPoolService
    {
        public const string FacilityId = "jingyuan_pool";
        public const string JingYuanItemId = "jingyuan";
        public const string RitualBasicId = "pool_offer_basic";
        public const string RitualBulkId = "pool_offer_bulk";

        public static event Action OnPoolChanged;

        public static int GetFacilityLevel(GameLogicManager glm)
            => glm?.worldPersistState?.GetSecretBaseFacilityLevel(FacilityId) ?? 1;

        public static SecretBaseFacilityLevel GetLevelConfig(GameLogicManager glm, int level = 0)
        {
            if (level <= 0) level = GetFacilityLevel(glm);
            return CfgMgr.Cfgs?.TbSecretBaseFacilityLevel?.Get(FacilityId, level);
        }

        public static SecretBaseFacilityLevel GetNextLevelConfig(GameLogicManager glm)
        {
            var next = GetFacilityLevel(glm) + 1;
            return CfgMgr.Cfgs?.TbSecretBaseFacilityLevel?.Get(FacilityId, next);
        }

        public static long GetCapacity(GameLogicManager glm)
        {
            var cfg = GetLevelConfig(glm);
            return cfg != null ? Math.Max(0, cfg.PoolCapacity) : 500;
        }

        public static long GetStored(GameLogicManager glm)
            => glm?.worldPersistState?.JingYuanPoolStored ?? 0;

        public static long GetOverflow(GameLogicManager glm)
            => Math.Max(0, GetStored(glm) - GetCapacity(glm));

        // 软空位：已满/超额时为 0（仅用于 UI 提示；入池仍允许超额）
        public static long GetFreeSpace(GameLogicManager glm)
            => Math.Max(0, GetCapacity(glm) - GetStored(glm));

        public static bool TryDepositFromInventory(GameLogicManager glm, long amount, out string failReason)
        {
            failReason = null;
            if (glm?.playerDataManager == null || amount <= 0)
            {
                failReason = "invalid";
                return false;
            }

            var owned = glm.playerDataManager.InventorySystem?.GetItemTotal(JingYuanItemId, includeWarehouse: true) ?? 0;
            var take = Math.Min(amount, owned);
            if (take <= 0)
            {
                failReason = "item_not_enough";
                return false;
            }

            var left = glm.playerDataManager.CostItem(JingYuanItemId, take);
            var spent = take - left;
            if (spent <= 0)
            {
                failReason = "item_not_enough";
                return false;
            }

            glm.worldPersistState.AddJingYuanPoolStored(spent);
            OnPoolChanged?.Invoke();
            return true;
        }

        public static bool TryWithdrawToInventory(GameLogicManager glm, long amount, out string failReason)
        {
            failReason = null;
            if (glm?.playerDataManager == null || amount <= 0)
            {
                failReason = "invalid";
                return false;
            }

            var stored = GetStored(glm);
            if (stored <= 0)
            {
                failReason = "pool_empty";
                return false;
            }

            var take = Math.Min(amount, stored);
            glm.worldPersistState.AddJingYuanPoolStored(-take);
            glm.playerDataManager.GiveItemToPlayer(JingYuanItemId, take);
            OnPoolChanged?.Invoke();
            return true;
        }

        // 任务/系统入池：允许短暂超出容量
        public static bool TryAddToPool(GameLogicManager glm, long amount, out long accepted, out string failReason)
        {
            accepted = 0;
            failReason = null;
            if (glm?.worldPersistState == null || amount <= 0)
            {
                failReason = "invalid";
                return false;
            }

            accepted = amount;
            glm.worldPersistState.AddJingYuanPoolStored(accepted);
            OnPoolChanged?.Invoke();
            return true;
        }

        // 日结开始时调用：丢弃超出容量部分
        public static long ClampOverflowAtSettlement(GameLogicManager glm)
        {
            if (glm?.worldPersistState == null) return 0;
            var cap = GetCapacity(glm);
            var stored = GetStored(glm);
            if (stored <= cap) return 0;
            var discarded = stored - cap;
            glm.worldPersistState.SetJingYuanPoolStored(cap);
            OnPoolChanged?.Invoke();
            Debug.Log($"JingYuan pool overflow discarded at settlement: {discarded}");
            return discarded;
        }

        public static bool TryDecompose(GameLogicManager glm, long jingyuanAmount, out long residueGained, out string failReason)
        {
            residueGained = 0;
            failReason = null;
            if (glm?.worldPersistState == null || jingyuanAmount <= 0)
            {
                failReason = "invalid";
                return false;
            }

            var cfg = GetLevelConfig(glm);
            var per = Math.Max(1, cfg?.DecomposeJingyuanPerResidue ?? 10);
            if (jingyuanAmount < per)
            {
                failReason = "too_few";
                return false;
            }

            var stored = GetStored(glm);
            var spend = Math.Min(jingyuanAmount, stored);
            spend = spend / per * per;
            if (spend <= 0)
            {
                failReason = "too_few";
                return false;
            }

            residueGained = spend / per;
            glm.worldPersistState.AddJingYuanPoolStored(-spend);
            glm.playerDataManager?.JingYuanEssenceSystem?.AddResidue(residueGained);
            OnPoolChanged?.Invoke();
            return true;
        }

        public static bool CanUpgrade(GameLogicManager glm, out string failReason)
        {
            failReason = null;
            var next = GetNextLevelConfig(glm);
            if (next == null)
            {
                failReason = "max_level";
                return false;
            }

            if (GetStored(glm) < next.UpgradeCostJingyuan)
            {
                failReason = "jingyuan_not_enough";
                return false;
            }

            if (!string.IsNullOrEmpty(next.UpgradeCostItemId) && next.UpgradeCostItemCount > 0)
            {
                if (glm?.playerDataManager == null
                    || !glm.playerDataManager.CheckHaveItem(next.UpgradeCostItemId, next.UpgradeCostItemCount))
                {
                    failReason = "item_not_enough";
                    return false;
                }
            }

            return true;
        }

        public static bool TryUpgrade(GameLogicManager glm, out string failReason)
        {
            if (!CanUpgrade(glm, out failReason)) return false;
            var next = GetNextLevelConfig(glm);
            if (next.UpgradeCostJingyuan > 0)
                glm.worldPersistState.AddJingYuanPoolStored(-next.UpgradeCostJingyuan);
            if (!string.IsNullOrEmpty(next.UpgradeCostItemId) && next.UpgradeCostItemCount > 0)
                glm.playerDataManager.CostItem(next.UpgradeCostItemId, next.UpgradeCostItemCount);

            glm.worldPersistState.SetSecretBaseFacilityLevel(FacilityId, next.Level);
            OnPoolChanged?.Invoke();
            return true;
        }

        public static string PickRitualId(GameLogicManager glm)
        {
            // 超额时优先批量倾泻，便于消化溢出
            if (GetOverflow(glm) > 0)
            {
                var bulk = CfgMgr.Cfgs?.TbJingYuanPoolRitual?.GetOrDefault(RitualBulkId);
                if (bulk != null && GetStored(glm) >= bulk.CostJingyuan)
                    return RitualBulkId;
            }
            return RitualBasicId;
        }

        public static bool TryStartRitual(GameLogicManager glm, string ritualId, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(ritualId))
                ritualId = PickRitualId(glm);

            var ritual = CfgMgr.Cfgs?.TbJingYuanPoolRitual?.GetOrDefault(ritualId);
            if (ritual == null)
            {
                failReason = "ritual_missing";
                return false;
            }

            if (glm?.worldPersistState == null || glm.playerDataManager == null)
            {
                failReason = "invalid";
                return false;
            }

            if (GetStored(glm) < ritual.CostJingyuan)
            {
                failReason = "jingyuan_not_enough";
                return false;
            }

            glm.worldPersistState.AddJingYuanPoolStored(-ritual.CostJingyuan);
            OnPoolChanged?.Invoke();

            var rewardCount = ritual.RewardItemCount;
            if (rewardCount <= 0 && ritual.JingyuanPerMaterial > 0)
                rewardCount = ritual.CostJingyuan / ritual.JingyuanPerMaterial;

            void GrantReward()
            {
                if (!string.IsNullOrEmpty(ritual.RewardItemId) && rewardCount > 0)
                    glm.playerDataManager.GiveItemToPlayer(ritual.RewardItemId, rewardCount);
                OnPoolChanged?.Invoke();
            }

            if (string.IsNullOrEmpty(ritual.DialogId) || MainGameManager.Instance == null)
            {
                GrantReward();
                return true;
            }

            var played = MainGameManager.Instance.PlayDialog(ritual.DialogId, null, false, GrantReward);
            if (!played)
            {
                Debug.LogWarning($"JingYuan pool ritual dialog missing: {ritual.DialogId}");
                GrantReward();
            }

            return true;
        }
    }
}
