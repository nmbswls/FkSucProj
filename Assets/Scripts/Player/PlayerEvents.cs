
using System;
using My.Map;
using UnityEngine;

namespace My.Quest
{
    [Serializable]
    public enum EPlayerEventType
    { 
        Inlivad = 0,

        PlayerKillUnit,
        PlayerKilled,

        ItemChange,
    }

    // 玩家击杀单位事件
    public struct PlayerKillUnitEvent
    {
        public string KilledCfgId;
        public EEntityType UnitType;

        public EPlayerEventType EventType { get { return EPlayerEventType.PlayerKillUnit; } }
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
