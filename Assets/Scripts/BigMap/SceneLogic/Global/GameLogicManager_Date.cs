
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;
using My.Home;
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
            public long DesireShardAdded = 0;
            public Dictionary<string, long> TownBuildingOutputs = new();
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
            //
            Debug.Log("Settlement day balance");
            //

            // 计算每日自然上涨沉沦人数
            float hotRate = 1.0f;
            float K = 0.1f;
            float pow = 0.5f; // 使用近二次公式增长

            long fixAddVal = playerDataManager.ProgressionSystem.GetFinalAttribute((int)EYCAttribute.FixFallenAdd);
            long bombAddVal = (long) (playerDataManager.TotalFallPeopleAmount * hotRate * K * Math.Pow(playerDataManager.TotalFallPeopleAmount, pow));

            balanceInfo.FromFallenAmount = playerDataManager.TotalFallPeopleAmount;
            balanceInfo.AddFallenAmount = fixAddVal + bombAddVal;

            playerDataManager.TotalFallPeopleAmount += fixAddVal + bombAddVal;

            // 结算每日固定欲望碎片
            long addYuWang = (long)(Math.Pow(playerDataManager.TotalFallPeopleAmount, 0.5f));
            balanceInfo.DesireShardAdded = addYuWang;

            if(addYuWang > 0)
            {
                playerDataManager.GiveItemToPlayer("desire_shard", addYuWang);
            }

            //  
            playerDataManager.ProgressionSystem.BaseStats.OnFallenAmountUpdate(playerDataManager.TotalFallPeopleAmount);

            var townOutputs = logicAreaHomesteadManager?.ApplyDailySettlement(playerDataManager);
            if (townOutputs?.MergedOutputs != null && townOutputs.MergedOutputs.Count > 0)
            {
                foreach (var kv in townOutputs.MergedOutputs)
                {
                    balanceInfo.TownBuildingOutputs[kv.Key] = kv.Value;
                }
            }

            SettlementDayIndex++;
            worldPersistState?.ApplyFishingRestockForSettlement(SettlementDayIndex);
            playerDataManager?.RumorIntel?.PruneExpiredRumors(SettlementDayIndex);

            EventOnOneDayBalance?.Invoke(balanceInfo);
        }
    }
}
