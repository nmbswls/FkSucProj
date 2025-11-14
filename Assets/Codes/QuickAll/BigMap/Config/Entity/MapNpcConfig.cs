using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Config.Unit
{
    [CreateAssetMenu(menuName = "GP/Config/Entity/Npc")]
    [Serializable]
    public class MapNpcConfig : AbsMapUnitConfig
    {
        public string NpcTag = string.Empty;
    }
}
