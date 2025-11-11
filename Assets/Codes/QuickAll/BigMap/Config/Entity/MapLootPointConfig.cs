using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Entity/LootPoint")]
    [Serializable]
    public  class MapLootPointConfig : ScriptableObject
    {
        public string CfgId;

        public bool DefaultLocked = false; // 是否默认带锁

        [Serializable]
        public class UnlockItemReq
        {
            public string ItemId;
            public int Count;
            public bool IsCost;
        }

        public List<UnlockItemReq> UnlockItemCost = new();

        public enum ELootReqType
        {
            None,
            HoldItem,
            TaskFinished,
        }

        [Serializable]
        public class CLootRequiment
        {
            public ELootReqType ReqType;
            public int Param1;
            public int Param2;
            public string Param3;
            public string Param4;
        }

        public float LootOpenTime = 0; // shifou xuyao shiijan kaiqi
        public string LootOverrideAnim = string.Empty;

        public CLootRequiment LootRequiment = null;

        /// <summary>
        /// 对应drop id
        /// </summary>
        public string DefaultDropId;
    }
}
