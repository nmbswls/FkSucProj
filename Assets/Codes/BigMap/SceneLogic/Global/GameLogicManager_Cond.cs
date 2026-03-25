
using System;
using cfg.demo;
using UnityEngine;

namespace My

{
    ///// <summary>
    ///// 所有条件归一化
    ///// </summary>
    //public enum ECommonCheckType
    //{
    //    None,
    //    TaskFinish,
    //    CheckVariable,
    //    HasPlacement,

    //    OwnItem, // p5 itemid p1 count
    //}


    //[Serializable]
    //public class CommonCheckCond
    //{
    //    public cfg.demo.ECommonCheckType Type;
    //    public long Param1;
    //    public long Param2;
    //    public long Param3;
    //    public long Param4;
    //    public string Param5;
    //    public string Param6;

    //    public static CommonCheckCond ConverFromCfg(cfg.demo.CommonCheckCond cfgCond)
    //    {
    //        CommonCheckCond ret = new CommonCheckCond();
    //        ret.Type = cfgCond.Type;
    //        ret.Param1 = cfgCond.Param1;
    //        ret.Param2 = cfgCond.Param2;
    //        ret.Param3 = cfgCond.Param3;
    //        ret.Param4 = cfgCond.Param4;
    //        ret.Param5 = cfgCond.Param5;
    //        ret.Param6 = cfgCond.Param6;
    //        return ret;
    //    }
    //}



    public partial class GameLogicManager
    {

        public bool CheckCommonCond(CommonCheckCond cond)
        {
            switch(cond.Type)
            {
                case cfg.demo.ECommonCheckType.None:
                    {
                        return true;
                    }
                    break;
                case cfg.demo.ECommonCheckType.CheckVariable:
                    {
                        bool checkHas = false;
                        if(cond.Param1 > 0)
                        {
                            checkHas = true;
                        }
                        if(checkHas && playerDataManager.CheckHasParam(cond.Param5))
                        {
                            return true;
                        }
                        if (!checkHas && !playerDataManager.CheckHasParam(cond.Param5))
                        {
                            return true;
                        }
                    }
                    break;
                case cfg.demo.ECommonCheckType.OwnItem:
                    {
                        string itemId = cond.Param5;
                        long itemCnt = cond.Param1;

                        if(playerDataManager.CheckHaveItem(itemId, itemCnt))
                        {
                            return true;
                        }
                    }
                    break;

            }
            return false;
        }
    }
}