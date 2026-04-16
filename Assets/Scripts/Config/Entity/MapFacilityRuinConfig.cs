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
        public List<CommonCheckCond> AutoRepairCond;

        public List<CommonCheckCond> OpenRepairCond;
        public SerializableDict<string, int> RepairMaterials;

        /// <summary>
        /// 完成后的placement
        /// </summary>
        public string PlacementId;
        public Vector2 PlaceOffset;
    }
}
