
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;

namespace My
{

    public partial class GameLogicManager
    {
        public int CurrDay = 1;
        public int DayPeriodLeft = 2;

        public event Action EventOnNextDayPeriod;

        public class OneDayBalanceInfo
        {

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

            EventOnNextDayPeriod?.Invoke();
        }

        public void FinishOneDay()
        {
            CurrDay += 1;
            HandleOneDayBalance();
            DayPeriodLeft = 2;
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

            //

            EventOnOneDayBalance?.Invoke(balanceInfo);
        }
    }
}