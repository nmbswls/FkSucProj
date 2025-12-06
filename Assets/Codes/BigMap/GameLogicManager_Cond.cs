
using System;
using UnityEngine;

namespace My

{
    /// <summary>
    /// 所有条件归一化
    /// </summary>
    public enum ECommonCheckType
    {
        None,
        TaskFinish,
        CheckVariable,
        HasPlacement,
    }


    [Serializable]
    public class CommonCheckCond
    {
        public ECommonCheckType Type;
        public long Param1;
        public long Param2;
        public long Param3;
        public long Param4;
        public string Param5;
        public string Param6;
    }


    public partial class GameLogicManager
    {

        public bool CheckCommonCond(CommonCheckCond cond)
        {
            switch(cond.Type)
            {
                case ECommonCheckType.None:
                    {
                        return true;
                    }
                    break;
                case ECommonCheckType.CheckVariable:
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
            }
            return false;
        }
    }
}