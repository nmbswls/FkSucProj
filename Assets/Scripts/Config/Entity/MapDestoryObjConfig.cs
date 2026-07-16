using System;
using System.Collections;
using System.Collections.Generic;
using My.Config;
using TMPro;
using UnityEngine;

namespace Config.Unit
{
    [CreateAssetMenu(menuName = "GP/Config/Entity/DestoryObj")]
    [Serializable]
    public class MapDestoryObjConfig : ScriptableObject
    {
        public string CfgId;

        public bool IsHitCountMode = true; // 是否是攻击次数模式
        public int HitCount = 3;
        public int DropBundleId;

        public bool IsPrecious; // 破坏珍贵物会引发通缉
        public bool HasOwner; // 是否有主

        [Tooltip("破坏完成后由逻辑层执行的配置化效果")]
        public List<LogicInteractOutput> BreakOutputs = new();
    }
}
