
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;
using My.Player;

namespace My
{

    public partial class GameLogicManager
    {
        public enum EDayPeriod
        {
            Day,
            Night,
        }

        public EDayPeriod DayPeriod;
        
        public int DayPeriodLeft = 2;

        public event Action EventOnNextDayPeriod;

        public class OneDayBalanceInfo
        {
            public long AddFallenAmount = 0;
            public long FromFallenAmount = 0;
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
            AdvanceSettlementDayAndApplyFishingRules();
            HandleOneDayBalance();
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleOneDayBalance()
        {
            OneDayBalanceInfo balanceInfo = new OneDayBalanceInfo();
            //
            Debug.Log("结算");
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

            if(addYuWang > 0)
            {
                playerDataManager.GiveItemToPlayer("desire_shard", addYuWang);
            }

            //  
            playerDataManager.ProgressionSystem.BaseStats.OnFallenAmountUpdate(playerDataManager.TotalFallPeopleAmount);


            EventOnOneDayBalance?.Invoke(balanceInfo);
        }
    }
}