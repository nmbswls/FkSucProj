using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技专用分支：读 ThrowContext.RunningVars[ResultVarKey] == TimelineHoldSession.OutcomeSuccess 走成功链，否则失败链
    [Serializable]
    public class MapAbilityEffectThrowTimedInputBranchCfg : MapFightEffectCfg
    {
        public string ResultVarKey = "ThrowTimedInput";

        [SerializeReference]
        public List<MapFightEffectCfg> SuccessBranchEffects = new();

        [SerializeReference]
        public List<MapFightEffectCfg> FailBranchEffects = new();
    }
}
