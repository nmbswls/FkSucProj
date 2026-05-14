

using My.Map.Entity;
using System.Collections.Generic;
using System;
using UnityEngine;
using My.Player.Bag;

namespace My.Map.Logic
{



    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord
    {
        public long Id;               // 全局唯一ID
        public EEntityType EntityType;
        public string CfgId;
        public Vector2 Position;
        public Vector2 FaceDir = Vector2.right;

        public EFactionId FactionId;

        public float LifeTime;

        public string BelongRoomId;
        public bool Activated = true;
        public long LifeBindEntityId;
        //public bool AlwaysActive;
        public string SrcUniqName = string.Empty;
        public bool IsFixed;

        public List<string> LocalSwitches = null;
        public bool MarkDestroyed = false; // 逻辑死亡/销毁标记，由 RefreshEntityRecordInfo 等与运行时对齐
    }

    [Serializable]
    public class LogicEntityRecord4InteractPoint : LogicEntityRecord
    {
        public int Status;

        public Dictionary<string, string> DynamicVariables = new();
    }


    [Serializable]
    public class LogicEntityRecord4LootPoint : LogicEntityRecord
    {
        public bool ItemInitialized = false;
        public int DynamicDropId;
        public List<ItemStack> InnerItems = new();
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4UnitBase : LogicEntityRecord
    {
        // 仅保存特殊状态 buff丢弃
        public bool Unsensored;
        public bool MarkDefeated; // 击败标记
        public bool MarkAttaching;

        public bool MarkNoLogic; // 非逻辑状态
    }

    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4Player : LogicEntityRecord4UnitBase
    {
        public class PlayerAttachment
        {
            public long AttachSrcId;
            public string AttachTypeId;
        }
        public List<PlayerAttachment> AttachUnits = new();
    }


    [Serializable]
    public class LogicEntityRecord4Npc : LogicEntityRecord4UnitBase
    {
        // 叙事/档案主键（导出填表）。有 CharacterKey 时 LocalSwitches 在 Spawn 时由 WorldNpcCharacterPersistRegistry 注入；运行时随 SetLocalSwitch 只写 Registry 不写回本 Record 的存盘周期。
        // MarkDefeated / MarkAttaching / Unsensored 等为当前地图 Record 范畴，不由 CharacterKey 全局档案同步。
        public string CharacterKey = string.Empty;

        public bool IsPeace;

        public UnitMoveBehaveInfo.EMoveBehaveType MoveBehaveType;

        public string EnmityConfId;
        public List<string> MoveWayPoints;

        public long PatrolFollowId;
        public Vector2 PatrolGroupRelativePos;
        public bool DisappearOnArrive;
        public string MovePath = null;
        public int CurrPathIdx = 0;
        public float CurrPathProgress = 0;

        public bool IsForeigner;

        // 欲望结晶类型 id（TbDesireCrystalDef）；空表示当前未附着；在刷新/生成时已决定，非死亡掉落时再算
        public string AttachedDesireCrystalTypeId = string.Empty;

        // 路网巡逻：对应导出的 PortalNetworkProvider.networkId；空且仅一张网时自动用该网
        public string PatrolPortalNetworkId = string.Empty;

        // 有序闭环节点 id（与路网节点 GameObject 名一致）；相邻及首尾段分别最短路拼接
        public List<string> PatrolCycleNodeIds = new();

        /// <summary>
        /// Search 张望阶段结束后的通用收尾策略（与通缉/守卫域无关）：0=与默认一致走 Return；1=MoveToDespawn；2=驻留 Idle；3=路网巡逻
        /// </summary>
        public int PostInvestigationResolveKind;

        /// <summary>
        /// ResolveKind=3 时路网随机巡逻采样规模（至少 2）
        /// </summary>
        public int PostInvestigationPatrolPickN = 3;

        /// <summary>
        /// 生成后是否立刻以疑点进入 Search（与收尾策略独立配置）
        /// </summary>
        public bool SpawnWithImmediateInvestigation;
    }


    // 逻辑实体的轻量描述（可存持久化）
    [Serializable]
    public class LogicEntityRecord4PatrolGroup : LogicEntityRecord
    {
        public float MoveSpeed;
        public int WayPointIdx = 0;
        public float WayPointDistance;
        /// <summary>
        /// 旧版：NamedPoint 名称序列，直线连接各点。
        /// </summary>
        public List<string> WayPointList = new();

        /// <summary>
        /// 路网巡逻：PortalNetworkProvider.networkId；空且地图仅一张网时由运行时解析。
        /// </summary>
        public string PatrolPortalNetworkId = string.Empty;

        /// <summary>
        /// 闭环节点 id（与路网节点名一致）；≥2 时优先走路网最短路展开，忽略 WayPointList。
        /// </summary>
        public List<string> PatrolCycleNodeIds = new();

        public bool IsBack = false;

        public List<long> PatrolUnitIds = new();
    }

    /// <summary>
    /// EventGroup
    /// </summary>
    [Serializable]
    public class LogicEntityRecord4EventGroup : LogicEntityRecord4InteractPoint
    {
        /// <summary>
        /// 已创建的集合
        /// </summary>
        public Dictionary<int, long> MemberId2EntityMap = new();

        public List<int> CurrActiveMembers = new();
        
        //public List<int> SleepMemberIds = new();
        //public List<int> DestroyedMemberIds = new();
    }

    /// <summary>
    /// 设施
    /// </summary>
    [Serializable]
    public class LogicEntityRecord4HomeFacility : LogicEntityRecord4InteractPoint
    {
        public long BindingFacilityId;

        // 城镇岗位分配人数（与 HomeFacilityInstance.ArrangePeopleNum 同步）
        public int ArrangePeopleNum;
    }

    // 
    [Serializable]
    public class LogicEntityRecord4DynamicSpawner : LogicEntityRecord
    {
        
    }

    [Serializable]
    public class LogicEntityRecord4Teleporter : LogicEntityRecord
    {
        public string TargetMap;
        public string TargetNamedPoint;
    }

    [Serializable]
    public class LogicEntityRecord4SimpleBlock : LogicEntityRecord
    {
        public float SizeX;
        public float SizeY;
    }

    [Serializable]
    public class LogicEntityRecord4FishingSpot : LogicEntityRecord
    {
    }

    [Serializable]
    public class LogicEntityRecord4Trap : LogicEntityRecord
    {
        public bool Armed = true;

        // LogicTime.time，0 表示未在沉睡；非 0 且大于当前逻辑时间则仍处于沉睡
        public float SleepWakeAtLogicTime;
    }
}

