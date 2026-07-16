
using My.Map;
using My.Map.Entity;
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

        // 潜入会话中玩家放置的运输标记实体（按当前 overlay 关联）
        public long TransportMarkerEntityId;
        public string TransportMarkerOverlayId = string.Empty;

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
            GameSession.TransportMarkerEntityId = 0;
            GameSession.TransportMarkerOverlayId = string.Empty;
        }

        public void BeginFreeBigMapSession()
        {
            GameSession.IsInfiltrationRun = false;
            GameSession.IsFreeBigMap = true;
        }

        public bool CanInteractSavePoint => GameSession.PlayerHumanMode || GameSession.IsPeaceful;

        public bool IsSavePointVaultAvailable => IsInfiltrationRun;

        public bool IsSavePointFormSwitchVisible()
        {
            if (IsInfiltrationRun)
            {
                return false;
            }

            return HasSavePointFormSwitchAreaTags();
        }

        public bool CanToggleSavePointForm(out string failReason)
        {
            failReason = null;
            if (IsInfiltrationRun)
            {
                failReason = "infiltration";
                return false;
            }

            if (!HasSavePointFormSwitchAreaTags())
            {
                failReason = "area_not_supported";
                return false;
            }

            var cfg = AreaManager?.cacheMapOverlayCfg;
            if (cfg == null)
            {
                failReason = "no_area";
                return false;
            }

            bool wantHuman = !GameSession.PlayerHumanMode;
            if (wantHuman)
            {
                if (!cfg.IsCivilArea)
                {
                    failReason = "not_civil";
                    return false;
                }
            }
            else if (!cfg.IsDangerArea)
            {
                failReason = "not_danger";
                return false;
            }

            return true;
        }

        public bool TryToggleSavePointForm(out string failReason)
        {
            if (!CanToggleSavePointForm(out failReason))
            {
                return false;
            }

            ForcePlayerHumanMode(!GameSession.PlayerHumanMode, refreshDespitePendingSwitch: true);
            return true;
        }

        bool HasSavePointFormSwitchAreaTags()
        {
            var cfg = AreaManager?.cacheMapOverlayCfg;
            if (cfg == null)
            {
                return false;
            }

            return cfg.IsCivilArea || cfg.IsDangerArea;
        }

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

            if (playerLogicEntity.AggroSystem != null && playerLogicEntity.AggroSystem.CombatEngaged)
            {
                GameSession.IsPeaceful = false;
                return;
            }

            foreach (var one in AreaManager.FindEntityInRange(playerLogicEntity.Pos, SavePointPeaceScanRadius))
            {
                if (one is NpcUnitLogicEntity npcUnit && !npcUnit.IsDead && npcUnit.IsInCombat)
                {
                    GameSession.IsPeaceful = false;
                    return;
                }

                if (one is NpcUnitLogicEntity partyAlly
                    && partyAlly.FactionId == EFactionId.Ally
                    && partyAlly.AggroSystem != null
                    && partyAlly.AggroSystem.CombatEngaged)
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

            // 快捷栏不可用时，开背包仍可拖拽装配
            if (UIManager.Instance != null && UIManager.Instance.IsPanelVisible("PlayerBag"))
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
            My.UI.OverworldHUDPanel.Instance?.MainBottomBar?.Refresh(true, forceLayoutRebuild: true);
            My.UI.PlayerHumanItemBarPanel.RefreshFromGame();
        }
    }
}