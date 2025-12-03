using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using TMPro;
using UnityEngine;

namespace Config.Unit
{

    [Serializable]
    public class EntitySkillCfg
    {
        public string SkillId;

        public bool IsPassive;
        public string PassiveBuffId;

        public bool NeedHMode;

        public bool IsFinisher;

        public bool IsDerived;

        public float CoolDown = 5.0f;
        public int StackCount = 0;

        public int Priority = 10;

        public float DesiredUseAngle;
        public float DesiredUseDistance;
        public enum ESelectPolicy
        {
            None,
            PrimaryTarget,
            Self,
            LowHpAlly,
            LowHpEnmity,
            Random,
        }
        public ESelectPolicy SelectPolicy; // 1-敌人主目标 2-自身 3-血量最低友方 4 血量最低敌方 5 随机 
    }


    [Serializable]
    public abstract class AbsMapUnitConfig : ScriptableObject
    {
        public string UnitName;
        public Sprite ViewSprite;

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

        public string AITemplateMode = string.Empty;
        public string AITemplateName = string.Empty;

        public EFactionId DefaultFactionId;

        [Header("属性模板")]

        public int Hp = 100;

        public List<string> SkillList = new();

        public string DropId;
    }
}
