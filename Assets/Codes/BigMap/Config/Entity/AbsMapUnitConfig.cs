using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using TMPro;
using UnityEngine;

namespace Config.Unit
{




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

        public bool HasHMode = true;
        public bool AlwaysHMode = false;

        public string AITemplateMode = string.Empty;
        public string AITemplateName = string.Empty;

        public EFactionId DefaultFactionId;

        [Header(" Ù–‘ƒ£∞Â")]

        public int Hp = 100;

        public List<string> SkillList = new();

        public string DropId;
    }
}
