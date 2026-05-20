using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectAddBuffCfg : MapFightEffectCfg
    {
        public string BuffId;
        public int Layer;
        public float Duration = -1;
        //public int TargetType; // 0 target 1 self 2 other

        public bool SelfAdd;
        public bool RevertAdd; // ÈÃtarget ¼Ó¸ø src

        public List<AttrKvPair> ExtraAttrInfos;
    }
}

