
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;

namespace My

{
    public class GlobalLogicDateManager
    {
        public GameLogicManager LogicManager { get; set; }

        public int CurrDay;
        public void OnEnterNextDay()
        {
            CurrDay += 1;

            Debug.Log("OnEnterNextDay next day");

            // npc 刷新交互次数

            //foreach(var p in LogicManager.homeDataManager)
            //{

            //}
        }
    }


    public partial class GameLogicManager
    {

        
    }
}