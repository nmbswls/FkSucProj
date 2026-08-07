
using System;
using cfg.demo;
using My.Map;
using System.Collections.Generic;
using UnityEngine;
using My.Farm;
using My.Home;
using My.MiniGame.Dream;
using My.Player;
using My.UI;

namespace My
{

    public partial class GameLogicManager
    {
        public enum EDayPeriod
        {
            Day,
            Night,
        }

        /// <summary>
        /// 世界结算日；推进时触发垂钓点按配置补满等。
        /// </summary>
        public int SettlementDayIndex { get; private set; }
        public EDayPeriod DayPeriod;
        
        public int DayPeriodLeft = 2;

        public event Action EventOnNextDayPeriod;

        public class OneDayBalanceInfo
        {
            public long AddFallenAmount = 0;
            public long FromFallenAmount = 0;
            public long AddFallenBaseAmount = 0;
            public long AddFallenSpreadAmount = 0;
            public long FromFallenBaseAmount = 0;
            public long FromFallenSpreadAmount = 0;
            public long DesireShardAdded = 0;
            public Dictionary<string, long> TownFacilityOutputs = new();
            public Dictionary<string, long> TransportRecoveredOutputs = new();
            public int TransportRecoveredStackCount;
            public int TransportLostStackCount;
            public int CultControlledTownCount;
            public long CultTownDailyFaith;
            public long CultFaithAdded;
            public long CultTownFaithAdded;
            public long CultLinkerFaithAdded;
            public long CultRegionLinkersAdded;
            public long CultAnchorFaithAdded;
            public long CultAnchorLinkersAdded;
            public Dictionary<string, long> CultAnchorItemOutputs = new();
            public int CultSecretMissionCompleted;
            public long CultSecretMissionFaithAdded;
            public long CultSecretMissionLinkersAdded;
            public long CultSecretMissionPressureReduced;
            public int CultSecretMissionWatchCleared;
            public int CultSecretMissionDisableCleared;
            public Dictionary<string, long> CultSecretMissionItemOutputs = new();
            public long MarketGoldEarned;
            public int MarketSoldItemCount;
            public int MarketUnsoldItemCount;
            public int FarmHarvestCount;
            public int FarmAutoPlantCount;
            public long TavernGoldEarned;
            public int TavernInfluenceEarned;
            public int TavernSoldCount;
        }
        public event Action<OneDayBalanceInfo> EventOnOneDayBalance;


        public bool CheckPeriodEnough(int period)
        {
            if(period <= DayPeriodLeft)
            {
                return true;
            }
            return false;
        }

        public void TryCostPeriod(int period)
        {
            DayPeriodLeft -= period;
            if(DayPeriodLeft < 0)
            {
                DayPeriodLeft = 0;
            }

            //EventOnNextDayPeriod?.Invoke();
        }

        public void RequestAdvanceDayPeriod()
        {
            if (UIManager.Instance == null)
            {
                NextDayPeriod();
                ApplySceneMaskForCurrentPeriod();
                return;
            }

            bool crossingDay = DayPeriod == EDayPeriod.Night;
            OneDayBalanceInfo balanceInfo = null;
            void CaptureBalance(OneDayBalanceInfo info) => balanceInfo = info;

            UIManager.Instance.DoFadeInAndOut(0.25f, 0.25f, () =>
            {
                if (crossingDay)
                {
                    EventOnOneDayBalance += CaptureBalance;
                }

                try
                {
                    NextDayPeriod();
                }
                finally
                {
                    if (crossingDay)
                    {
                        EventOnOneDayBalance -= CaptureBalance;
                    }
                }

                ApplySceneMaskForCurrentPeriod();
            }, null, () =>
            {
                if (crossingDay && balanceInfo != null)
                {
                    DayPeriodSettlementPanel.Show(balanceInfo);
                }

                SecretBaseHudPanel.Instance?.RefreshUI();
            });
        }

        void ApplySceneMaskForCurrentPeriod()
        {
            if (DayPeriod == EDayPeriod.Night)
            {
                SceneMaskPanel.Instance?.ShowHunting();
            }
            else
            {
                SceneMaskPanel.Instance?.ShowDayTime();
            }
        }

        public void NextDayPeriod()
        {
            if(DayPeriod == EDayPeriod.Day)
            {
                DayPeriod = EDayPeriod.Night;
            }
            else
            {
                DayPeriod = EDayPeriod.Day;

                FinishOneDay();
            }

            EventOnNextDayPeriod?.Invoke();
        }

        public void FinishOneDay()
        {
            HandleOneDayBalance();
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleOneDayBalance()
        {
            OneDayBalanceInfo balanceInfo = new OneDayBalanceInfo();
            transportLootSystem?.DepositMarkerContentsToPending();
            //
            Debug.Log("Settlement day balance");
            //

            // 沉沦：基础人数 + 扩散人数（展示用合计）
            FallenPopulationService.ApplyDailySettlement(this, balanceInfo);

            // 结算每日固定欲望碎片（仍按合计）
            long addYuWang = (long)(Math.Pow(Math.Max(0, playerDataManager.TotalFallPeopleAmount), 0.5f));
            balanceInfo.DesireShardAdded = addYuWang;

            if(addYuWang > 0)
            {
                playerDataManager.GiveItemToPlayer("desire_shard", addYuWang);
            }

            var townOutputs = townFacilityDevelopmentSystem?.ApplyDailySettlement(playerDataManager);
            if (townOutputs?.MergedOutputs != null && townOutputs.MergedOutputs.Count > 0)
            {
                foreach (var kv in townOutputs.MergedOutputs)
                {
                    balanceInfo.TownFacilityOutputs[kv.Key] = kv.Value;
                }
            }

            var market = playerDataManager?.ProgressionSystem?.HumanCivilization?.SettleMarket();
            if (market != null)
            {
                balanceInfo.MarketGoldEarned = market.GoldEarned;
                balanceInfo.MarketSoldItemCount = market.SoldItemCount;
                balanceInfo.MarketUnsoldItemCount = market.UnsoldItemCount;
            }

            var transport = playerDataManager?.ProgressionSystem?.HumanCivilization?.SettleTransportRecovery();
            if (transport != null)
            {
                balanceInfo.TransportRecoveredStackCount = transport.RecoveredStackCount;
                balanceInfo.TransportLostStackCount = transport.LostStackCount;
                if (transport.RecoveredOutputs != null)
                {
                    foreach (var kv in transport.RecoveredOutputs)
                    {
                        balanceInfo.TransportRecoveredOutputs[kv.Key] = kv.Value;
                    }
                }
            }

            var cult = playerDataManager?.ProgressionSystem?.DemonCult;
            cult?.RefreshAutoUnlockedSeats();
            var controlledTownCount = worldPersistState?.GetControlledTownCount() ?? 0;
            var cultFaithPerTown = cult?.GetCultAttributeValue(ECultAttribute.TownDailyFaith) ?? 0;
            var townFaithAdded = controlledTownCount * cultFaithPerTown;
            if (townFaithAdded != 0)
            {
                cult?.AddFaith(townFaithAdded);
            }
            balanceInfo.CultControlledTownCount = controlledTownCount;
            balanceInfo.CultTownDailyFaith = cultFaithPerTown;
            balanceInfo.CultTownFaithAdded = townFaithAdded;

            // 灵感皈依先涨教徒，再进锚点效率与什一结算
            balanceInfo.CultRegionLinkersAdded = cult?.ApplyRegionDailyLinkerSettlement() ?? 0;

            // 日结开始先丢弃精元池超额，保留玩家在上一日内分解/转化的窗口
            My.SecretBase.JingYuanPoolService.ClampOverflowAtSettlement(this);

            var settlementDay = SettlementDayIndex + 1;
            // 先结算到期秘会（灭迹可在盯梢升级前生效）
            var secretMission = cult?.ApplySecretMissionSettlement(settlementDay);
            if (secretMission != null)
            {
                balanceInfo.CultSecretMissionCompleted = secretMission.CompletedCount;
                balanceInfo.CultSecretMissionFaithAdded = secretMission.FaithAdded;
                balanceInfo.CultSecretMissionLinkersAdded = secretMission.LinkersAdded;
                balanceInfo.CultSecretMissionPressureReduced = secretMission.PressureReduced;
                balanceInfo.CultSecretMissionWatchCleared = secretMission.WatchClearedCount;
                balanceInfo.CultSecretMissionDisableCleared = secretMission.DisableClearedCount;
                foreach (var pair in secretMission.ItemOutputs)
                {
                    balanceInfo.CultSecretMissionItemOutputs[pair.Key] = pair.Value;
                }
            }

            var anchorSettlement = cult?.ApplyAnchorSettlement(settlementDay);
            if (anchorSettlement != null)
            {
                balanceInfo.CultAnchorFaithAdded = anchorSettlement.FaithAdded;
                balanceInfo.CultAnchorLinkersAdded = anchorSettlement.LinkersAdded;
                foreach (var pair in anchorSettlement.ItemOutputs)
                {
                    balanceInfo.CultAnchorItemOutputs[pair.Key] = pair.Value;
                }
            }

            balanceInfo.CultLinkerFaithAdded = cult?.ApplyLinkerFaithSettlement() ?? 0;
            balanceInfo.CultFaithAdded = balanceInfo.CultTownFaithAdded
                + balanceInfo.CultLinkerFaithAdded
                + balanceInfo.CultAnchorFaithAdded
                + balanceInfo.CultSecretMissionFaithAdded;

            SettlementDayIndex++;
            worldPersistState?.ApplyFishingRestockForSettlement(SettlementDayIndex);
            playerDataManager?.RumorIntel?.PruneExpiredRumors(SettlementDayIndex);
            RumorIntelSpawn?.PruneExpiredEventsForCurrentMap();
            farmSystem?.ApplyDailySettlement(balanceInfo);
            AbstractGroupDreamService.OnSettlementDayBalance(this);
            DreamPasserbyService.OnSettlementDayBalance(this);

            EventOnOneDayBalance?.Invoke(balanceInfo);
        }
    }
}
