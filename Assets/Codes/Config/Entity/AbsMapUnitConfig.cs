using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using My.Map.Fight;
using TMPro;
using UnityEngine;

namespace Config.Unit
{

    [Serializable]
    public class EntitySkillCfg
    {
        public string SkillId;

        public string MainAbilityId;
        public bool IsPassive;
        public string PassiveBuffId;

        public bool IsCombo;

        public bool NeedHMode;

        public bool InterruptCombo = true;

        public bool IsDerived;

        public float CoolDown = 5.0f;
        public int StackCount = 0;

        public string IconPath;


        public int Priority = 10;
        public float DesiredUseAngle;
        public float DesiredUseDistance;

        public float BufferCacheTime = 0.1f;


        public enum ECastConditionType
        {
            None,
            HMode,

            QueenMode,
            NoQueenMode,
        }

        [Serializable]
        public class CastCondition
        {
            public ECastConditionType Type;
            public long Param1;
            public long Param2;
            public string Param3;
            public string Param4;
        }

        public List<CastCondition> CastConditions = new();

        //public enum ETargetType
        //{
        //    NoTarget,
        //    Point,
        //    Circle,
        //    Rect,
        //    LockTarget,
        //    Self,
        //}
        //public ETargetType TargetType;
        //public float Range1;
        //public float Range2;
    }


    [Serializable]
    public abstract class AbsMapUnitConfig : ScriptableObject
    {
        public string UnitName;
        public Sprite ViewSprite;
        public string PrefabName;
        public string ShowName = "?";

        public enum EMapUnitMoveStyle
        {
            NoMove,
            Normal,
            Fly,
            Ghost,
        }

        public EMapUnitMoveStyle MoveStyle;
        public float MoveSpeed = 1.0f;

        public bool IsPeace = false;
        public string CombatStretegyTemplateId;

        public float BattleBoundary = 10.0f;
        public bool RecoverReturn = true;

        public bool HasHMode = true;
        public bool AlwaysHMode = false;




        public EFactionId DefaultFactionId;
        public string EnmityCfgId;

        [Header(" Ù–‘ƒ£∞Â")]

        public int Hp = 100;

        public List<string> SkillList = new();

        public int DefaultDropId;


    }
}
