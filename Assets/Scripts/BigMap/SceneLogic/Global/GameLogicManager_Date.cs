
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;

namespace My

{
    public class GameDateInfo
    {
        public int CurrDay = 1;

        public enum EDayPeriod
        {
            Day,
            Dawn,
            Night,
        }

        public EDayPeriod DayPeriod;

        public void NextPeriod()
        {

            if(DayPeriod == EDayPeriod.Day)
            {
                DayPeriod = EDayPeriod.Dawn;
            }
            else if(DayPeriod == EDayPeriod.Dawn)
            {
                DayPeriod = EDayPeriod.Night;
            }
            else if (DayPeriod == EDayPeriod.Night)
            {
                DayPeriod = EDayPeriod.Day;

                CurrDay += 1;
            }
        }
    }


    public partial class GameLogicManager
    {

        public GameDateInfo DateInfo = new();

        public class OneDayBalanceInfo
        {

        }
        public event Action<OneDayBalanceInfo> EventOnOneDayBalance;

        public void GoToNextPeriod()
        {
            int currDay = DateInfo.CurrDay;

            DateInfo.NextPeriod();

            if(DateInfo.CurrDay != currDay)
            {
                HandleOneDayBalance();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void HandleOneDayBalance()
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