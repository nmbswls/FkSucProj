using Map.Entity;
using My.Map.Fight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapAbilityEffectHitBoxCfg : MapFightEffectCfg
    {
        public enum EShape
        {
            None,
            Square, // 矩形 原点在中心
            Direction, // 方向，原点在一边
            Circle,
        }
        public EShape Shape;

        public float Width;
        public float Length;
        public float Radius;

        public float CenterOffset; // 偏移量
        public bool IsDirRevert; // 是否反向
        public int CenterPosType = 0; // 0 happen 1 castVec

        public EEntityType TargetEntityType;

        public ECampFilterType CampFilterType;

        public bool IncludeEnmity;
        public bool IncludeFriendly;

        public float HitVal;

        public int MaxCatchCount = 50; // 

        public FightStruct.HitResult HitResult;

        [SerializeReference]
        [Obsolete]
        public List<MapFightEffectCfg> OnHitEffects = new();
    }
}


