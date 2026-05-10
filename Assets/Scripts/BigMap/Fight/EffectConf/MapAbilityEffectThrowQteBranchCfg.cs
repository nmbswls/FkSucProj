using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技专用分支：读 ThrowContext.RunningVars[ResultVarKey]，与 SuccessValue 一致走成功链，否则走失败链
    [Serializable]
    public class MapAbilityEffectThrowQteBranchCfg : MapFightEffectCfg
    {
        public string ResultVarKey = "ThrowQte";
        public string SuccessValue = "1";

        [SerializeReference]
        public List<MapFightEffectCfg> SuccessBranchEffects = new();

        [SerializeReference]
        public List<MapFightEffectCfg> FailBranchEffects = new();
    }
}
