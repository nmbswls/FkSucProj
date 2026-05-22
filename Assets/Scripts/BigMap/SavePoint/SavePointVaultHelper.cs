namespace My.Map
{
    public static class SavePointVaultHelper
    {
        public const string DesireShardItemId = "desire_shard";
        public const long DesireShardRunCap = 50;

        public static long GetDepositedThisRun(GameLogicManager glm) =>
            glm?.SavePointVaultDesireShardDepositedThisRun ?? 0;

        public static long GetRemainingQuota(GameLogicManager glm)
        {
            var remaining = DesireShardRunCap - GetDepositedThisRun(glm);
            return remaining > 0 ? remaining : 0;
        }

        public static long GetCarriedDesireShard(GameLogicManager glm)
        {
            var inv = glm?.playerDataManager?.InventorySystem;
            return inv == null ? 0 : inv.GetCarriedItemTotalExcludingWarehouse(DesireShardItemId);
        }

        public static long GetDepositableAmount(GameLogicManager glm)
        {
            var carried = GetCarriedDesireShard(glm);
            var quota = GetRemainingQuota(glm);
            return carried < quota ? carried : quota;
        }

        public static bool TryDepositAllAvailable(GameLogicManager glm, out long deposited, out string failReason)
        {
            deposited = 0;
            failReason = null;

            if (glm?.playerDataManager?.InventorySystem == null)
            {
                failReason = "no_inventory";
                return false;
            }

            var amount = GetDepositableAmount(glm);
            if (amount <= 0)
            {
                failReason = "nothing_to_deposit";
                return false;
            }

            var inv = glm.playerDataManager.InventorySystem;
            var left = inv.CostCarriedItem(DesireShardItemId, amount);
            var actuallyCost = amount - left;
            if (actuallyCost <= 0)
            {
                failReason = "cost_failed";
                return false;
            }

            var warehouse = inv.WarehouseBag;
            if (warehouse == null)
            {
                inv.GiveItemToPlayer(DesireShardItemId, actuallyCost);
                failReason = "no_warehouse";
                return false;
            }

            var put = warehouse.TryGiveItem(DesireShardItemId, actuallyCost);
            if (put < actuallyCost)
            {
                var rollback = actuallyCost - put;
                if (rollback > 0)
                {
                    inv.GiveItemToPlayer(DesireShardItemId, rollback);
                }

                if (put <= 0)
                {
                    failReason = "warehouse_full";
                    return false;
                }
            }

            glm.AddSavePointVaultDesireShardDeposited(put);
            deposited = put;
            return true;
        }
    }
}
