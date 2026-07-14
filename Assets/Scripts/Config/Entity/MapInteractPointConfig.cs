using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using cfg.demo;
using My;
using My.Config;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/InteractPoint")]
    [Serializable]
    public  class MapInteractPointConfig : ScriptableObject
    {
        public string CfgId;
        public string ShowName;
        public string PrefabName;

        public float NameOffset = -1f;

        public bool IsAlwaysActive = false;

        [Tooltip("需要地图刷新项 UniqName；为 true 时 LocalSwitch 写入 PlayerData 稀疏存档")]
        public bool PersistByUniqName = false;

        [Tooltip("在大地图（M）上显示为重要地标")]
        public bool ShowOnWorldMap = false;

        [Tooltip("大地图标记旁的可选短标签，空则用 ShowName 或 CfgId")]
        public string WorldMapMarkerLabel = "";

        [Serializable]
        public class StatusInfo
        {
            public int StatusId;
            public string Desp;
            public List<MapInteractInfo> InteractInfos = new();

            public bool HasBlock = false;
            public bool AutoTriggerCollide = false;

            public bool AutoTrigger = false;
            public int AutoTriggerInteractId = 1;
            public float AutoTriggerCheckInterval = 0.25f;
        }

        public StatusInfo MainStatusInfo;
        public List<StatusInfo> ExtraStatusInfos;

        public int InitState;

        [Serializable]
        public class StateChangeView
        {
            public float ChangingDuration = 0;
            public string ChangingAnimName;
            public string ChangingEffect;
        }



        [Serializable]
        public class StateChangeRule
        {
            public int FromStatus;
            public List<CommonCheckCond> CommonConds = new();
            public List<string> NeedSelfFlag = new();
            public List<LocalIntValueCondition> LocalIntConditions = new();
            public List<TimedRefreshCondition> TimedRefreshConditions = new();
            public int ToStatus;

            public StateChangeView ChangeView;
        }

        /// <summary>
        /// 状态切换规则
        /// </summary>
        public List<StateChangeRule> StateChangeRules = new();

        [Header("Poison bait (optional)")]
        public InteractPointPoisonSettings PoisonSettings = new();

        [Header("Dormant / GcLiquid reveal (optional)")]
        public InteractPointDormantRevealSettings DormantRevealSettings = new();

    }

    [Serializable]
    public class LocalIntValueCondition
    {
        public string Key = "";
        public ELocalIntCompare Compare = ELocalIntCompare.Equal;
        public ELocalIntValueSource RightValueSource = ELocalIntValueSource.Constant;
        public int RightValue;
    }

    public enum ELocalIntCompare
    {
        Equal = 0,
        NotEqual = 1,
        Greater = 2,
        GreaterOrEqual = 3,
        Less = 4,
        LessOrEqual = 5,
    }

    public enum ELocalIntValueSource
    {
        Constant = 0,
        SettlementDayIndex = 1,
    }

    [Serializable]
    public class TimedRefreshCondition
    {
        public ERefreshTimeExistence Existence = ERefreshTimeExistence.Exists;
        public ELocalIntCompare Compare = ELocalIntCompare.GreaterOrEqual;
        public int ElapsedDays;
    }

    public enum ERefreshTimeExistence
    {
        Exists = 0,
        Missing = 1,
    }

    [Serializable]
    public class InteractPointDormantRevealSettings
    {
        [Tooltip("开启后初始逻辑隐身且不可交互，被 GcLiquid 触及后短暂显形")]
        public bool Enable;

        [Tooltip("GcLiquid 触发的显形持续时间（秒）")]
        public float RevealDurationSeconds = 8f;

        [Tooltip("与 GroundOverlay 液体检测半径一致")]
        public float GcLiquidCheckRadius = 0.3f;
    }

    [Serializable]
    public class InteractPointPoisonSettings
    {
        [Tooltip("开启后玩家可对该点下毒；Idle NPC 可被吸引前来假交互并吃 buff")]
        public bool Enable;

        public List<CommonCheckCond> ApplyPoisonConds = new();

        [Tooltip("NPC 假交互触发后对 NPC 施加的主 buff（buffId）")]
        public string NpcTriggerBuffId = "";

        [Tooltip("诱饵持续时间（秒）；过期后开始重下毒 CD")]
        public float BaitDurationSeconds = 30f;

        [Tooltip("诱饵被 NPC 触发或过期后，再次允许下毒的间隔（秒）")]
        public float ReapplyCooldownSeconds = 20f;

        public string ApplyPoisonLabel = "下毒";

        [Tooltip("NPC 开始靠近前头顶飘字")]
        public string NpcFloatText = "...";

        public float NpcApproachStopDistance = 0.4f;
    }
}
