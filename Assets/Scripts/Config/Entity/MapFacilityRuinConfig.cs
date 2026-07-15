using System;
using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Map.Logic;
using TMPro;
using UnityEngine;

namespace Config.Map
{
    [CreateAssetMenu(menuName = "GP/Config/Entity/FixFacility")]
    [Serializable]
    public class MapFacilityRuinConfig : ScriptableObject
    {
        public string CfgId;

        public bool AutoRepair;
        public bool ShwoWhenLocked;
        public List<CommonCheckCond> OpenRepairCond;

        public SerializableDict<string, int> RepairMaterials;

        /// <summary>
        /// 修缮完成后对应的设施 id（新字段）
        /// </summary>
        public string TargetFacilityId;

        /// <summary>
        /// 旧资源字段，与 TargetFacilityId 同义
        /// </summary>
        public string PlacementId;

        public Vector2 PlaceOffset;

        public string ResolveTargetFacilityId()
        {
            return !string.IsNullOrEmpty(TargetFacilityId) ? TargetFacilityId : PlacementId;
        }
    }
}
