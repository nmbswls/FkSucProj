using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{

    /// <summary>
    /// 将自身转变为指定目标的attach
    /// </summary>
    [Serializable]
    public class MapAbilityEffectConvertAttachCfg : MapFightEffectCfg
    {
        public string AttachId;
    }
}

