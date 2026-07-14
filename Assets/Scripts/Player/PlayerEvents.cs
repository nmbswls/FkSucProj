using System;
using cfg.demo;
using My.Map;
using My.Player;
using UnityEngine;

namespace My.Quest
{
    [Serializable]
    public enum EPlayerEventType
    { 
        Inlivad = 0,

        PlayerKillUnit,
        PlayerKilled,
        EntityInteractionCompleted,

        ItemChange,

        OpenFunc,
    }

    // 符文获得
    public struct PlayerRuneGrantedEvent
    {
        public string RuneId;
    }

    // 符文升级项解锁
    public struct PlayerRuneUpgradeUnlockedEvent
    {
        public string UpgradeId;
        public string BaseRuneId;
    }

    // 功能解锁
    public struct PlayerFuncUnlockEvent
    {
        public EFuncOpenType OpenType;

        public EPlayerEventType EventType { get { return EPlayerEventType.OpenFunc; } }
    }

    // 玩家击杀单位事件
    public struct PlayerKillUnitEvent
    {
        public string KilledCfgId;
        public EEntityType UnitType;
        public bool KilledByPlayer;

        public EPlayerEventType EventType { get { return EPlayerEventType.PlayerKillUnit; } }
    }

    public struct PlayerEntityInteractionCompletedEvent
    {
        public string CfgId;
        public string UniqName;
        public int InteractId;

        public EPlayerEventType EventType { get { return EPlayerEventType.EntityInteractionCompleted; } }
    }

    // 玩家死亡事件
    public struct PlayerKilledEvent
    {
        public Vector3 DeathPosition;

        public EPlayerEventType EventType { get { return EPlayerEventType.PlayerKilled; } }
    }

    // 
    public struct PlayerItemChangeEvent
    {
        public string ItemId;
        public int ChangeAmount;
    }

    public struct PlayerTempSkillChangedEvent
    {
    }

    public struct PlayerJingYuanCodexProgressEvent
    {
        public string CodexId;
        public int ExtractCount;
        public long TotalAmount;
        public int Level;
        public EJingYuanProgressSource Source;
    }

    public struct PlayerJingYuanCodexLevelUpEvent
    {
        public string CodexId;
        public int OldLevel;
        public int NewLevel;
    }

    public struct PlayerJingYuanTuneEquipChangedEvent
    {
        public int Slot;
        public string CodexId;
    }

    // 玩家吸收 NPC 射精（内射/近距离吸收）
    public struct PlayerJingYuanBlurtAbsorbedEvent
    {
        public string JingyuanTag;
        public float SjAmount;
    }

    // 玩家逻辑实体已就绪（区域加载完成）
    public struct PlayerEntityReadyEvent
    {
    }

    // 玩家逻辑实体即将回收（切图 / despawn）
    public struct PlayerEntityDespawnEvent
    {
    }

    // 任务最终完成
    public struct PlayerQuestCompleteEvent
    {
        public int QuestId;
    }

    // 道具使用成功（含 auto-use）
    public struct PlayerItemUsedEvent
    {
        public string ItemId;
        public long Count;
    }

    // 进入地图 overlay（切图完成）
    public struct PlayerEnterOverlayEvent
    {
        public string OverlayId;
    }

    // Statistic 计数变化
    public struct PlayerStatisticChangedEvent
    {
        public cfg.demo.EStatType StatType;
        public string Key;
        public long NewValue;
        public long Delta;
        public string Arg0;
        public string Arg1;
    }

    public static class PlayerEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            EventBusInternal<T>.OnEvent += handler;
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            EventBusInternal<T>.OnEvent -= handler;
        }

        /// <summary>
        /// 发布（触发）事件
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            EventBusInternal<T>.OnEvent?.Invoke(eventData);
        }

        /// <summary>
        /// 内部泛型静态类，利用 C# 泛型特性，每种事件类型会自动生成独立的类和委托实例。
        /// 这样做省去了 Dictionary 的查表开销，也没有任何装箱拆箱（0 GC）。
        /// </summary>
        private static class EventBusInternal<T> where T : struct
        {
            public static Action<T> OnEvent;
        }
    }
}
