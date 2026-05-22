
using My.Map;
using My.UI;

namespace My
{
    /// <summary>
    /// session????
    /// </summary>
    public class PlayerGameSession
    {
        public bool IsFreeBigMap = true;
        public bool IsInfiltrationRun;

        public bool IsPeaceful = true;

        // true = 人类形态；false = 真身形态（仅真身下维持衣装/暴露等玩法）
        public bool PlayerHumanMode = true;

        public long SPDesireShardDeposited;
    }

    public partial class GameLogicManager
    {
        public bool PlayerHumanMode => GameSession.PlayerHumanMode;

        public bool PlayerPeaceMode => GameSession.IsPeaceful;

        public bool IsInfiltrationRun => GameSession.IsInfiltrationRun;

        public bool IsFreeBigMap => GameSession.IsFreeBigMap;

        public void BeginInfiltrationRunSession()
        {
            GameSession.IsInfiltrationRun = true;
            GameSession.IsFreeBigMap = false;
        }

        public void BeginFreeBigMapSession()
        {
            GameSession.IsInfiltrationRun = false;
            GameSession.IsFreeBigMap = true;
        }

        public bool CanInteractSavePoint => GameSession.PlayerHumanMode || GameSession.IsPeaceful;

        private float _peaceModeTimer = 0;

        private void TickPeaceMode()
        {
            if (LogicTime.time - _peaceModeTimer < 3.0f)
            {
                return;
            }

            _peaceModeTimer = LogicTime.time;
            RefreshPlayerPeaceMode();
        }

        public void RefreshPlayerPeaceMode()
        {
            if (playerLogicEntity == null || AreaManager == null)
            {
                GameSession.IsPeaceful = true;
                return;
            }

            foreach (var one in AreaManager.FindEntityInRange(playerLogicEntity.Pos, SavePointPeaceScanRadius))
            {
                if (one is NpcUnitLogicEntity npcUnit && npcUnit.IsInCombat)
                {
                    GameSession.IsPeaceful = false;
                    return;
                }
            }

            GameSession.IsPeaceful = true;
        }


        // ?????????????????谢?????/?????????Home ???????
        public bool TrySetPlayerHumanMode(bool wantHuman)
        {
            /*
            if (AreaManager?.cacheMapOverlayCfg == null || !AreaManager.cacheMapOverlayCfg.IsHome)
            {
                return false;
            }

            ForcePlayerHumanMode(wantHuman, refreshDespitePendingSwitch: true);
            return true;
            */
            return false;
        }

        // ????????????????????????????????????谢??????????????? PostNewAreaLoaded ??????????
        public void ForcePlayerHumanMode(bool human, bool refreshDespitePendingSwitch = false)
        {
            if (GameSession.PlayerHumanMode == human)
            {
                return;
            }

            GameSession.PlayerHumanMode = human;
            ResetSavePointVaultDepositedThisRun();
            if (SwitchAreaIntent != null && !refreshDespitePendingSwitch)
            {
                return;
            }

            RefreshPlayerMagicClothesAndExposeForCurrentMode();
            NotifyHumanQuickBarStateChanged();
        }

        public const string SavePointDesireShardItemId = "desire_shard";
        public const long SavePointDesireShardRunCap = 50;

        public long SavePointVaultDepositedThisRun => GameSession.SPDesireShardDeposited;

        public void ResetSavePointVaultDepositedThisRun()
        {
            GameSession.SPDesireShardDeposited = 0;
        }

        public void AddSavePointVaultDesireShardDeposited(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            GameSession.SPDesireShardDeposited += amount;
        }

        public long GetSavePointVaultRemainingQuota()
        {
            var remaining = SavePointDesireShardRunCap - SavePointVaultDepositedThisRun;
            return remaining > 0 ? remaining : 0;
        }

        public long GetSavePointVaultCarriedDesireShard()
        {
            var inv = playerDataManager?.InventorySystem;
            return inv == null ? 0 : inv.GetCarriedItemTotalExcludingWarehouse(SavePointDesireShardItemId);
        }

        public long GetSavePointVaultDepositableAmount()
        {
            var carried = GetSavePointVaultCarriedDesireShard();
            var quota = GetSavePointVaultRemainingQuota();
            return carried < quota ? carried : quota;
        }

        public bool TryDepositSavePointVaultAllAvailable(out long deposited, out string failReason)
        {
            deposited = 0;
            failReason = null;

            var inv = playerDataManager?.InventorySystem;
            if (inv == null)
            {
                failReason = "no_inventory";
                return false;
            }

            var amount = GetSavePointVaultDepositableAmount();
            if (amount <= 0)
            {
                failReason = "nothing_to_deposit";
                return false;
            }

            var left = inv.CostCarriedItem(SavePointDesireShardItemId, amount);
            var actuallyCost = amount - left;
            if (actuallyCost <= 0)
            {
                failReason = "cost_failed";
                return false;
            }

            var warehouse = inv.WarehouseBag;
            if (warehouse == null)
            {
                inv.GiveItemToPlayer(SavePointDesireShardItemId, actuallyCost);
                failReason = "no_warehouse";
                return false;
            }

            var put = warehouse.TryGiveItem(SavePointDesireShardItemId, actuallyCost);
            if (put < actuallyCost)
            {
                var rollback = actuallyCost - put;
                if (rollback > 0)
                {
                    inv.GiveItemToPlayer(SavePointDesireShardItemId, rollback);
                }

                if (put <= 0)
                {
                    failReason = "warehouse_full";
                    return false;
                }
            }

            AddSavePointVaultDesireShardDeposited(put);
            deposited = put;
            return true;
        }

        // ?????????????未???????伪??? ?? ????未?????
        public bool IsHumanQuickBarAvailable()
        {
            if (playerLogicEntity == null)
            {
                return false;
            }

            if (playerLogicEntity.IsFaQing)
            {
                return false;
            }

            if (GameSession.PlayerHumanMode)
            {
                return true;
            }

            return !playerLogicEntity.IsExposed;
        }


        public bool CanEditQuickSlotBar()
        {
            if (IsHumanQuickBarAvailable())
            {
                return true;
            }

            // 基地开背包编辑快捷栏：显示不受 IsHumanQuickBarAvailable 约束，拖拽也应可用
            if (IsInSecretBaseContext()
                && UIManager.Instance != null
                && UIManager.Instance.IsPanelVisible("PlayerBag"))
            {
                return true;
            }

            return My.UI.PlayerHumanItemBarPanel.IsBagCompanionEditing();
        }

        public void NotifyHumanQuickBarStateChanged()
        {
            if (!IsHumanQuickBarAvailable())
            {
                playerDataManager?.HumanQuickBar?.ClearActiveWeapon();
            }
            else
            {
                playerDataManager?.HumanQuickBar?.ApplyWeaponToRuntime();
            }

            playerDataManager?.SyncLearnedSkillsToPlayerEntity();
            My.UI.OverworldHUDPanel.Instance?.SkilBar?.Refresh(true);
            My.UI.PlayerHumanItemBarPanel.RefreshFromGame();
        }
    }
}