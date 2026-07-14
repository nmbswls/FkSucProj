using System;
using System.Collections.Generic;
using cfg.demo;
using My.Map;
using My.Quest;
using My.Saving;

namespace My.Player
{
    // 共性统计：只写事实，供 EventGrant / 成就回溯读取
    public sealed class PlayerStatisticSystem : IPlayerSystem
    {
        readonly PlayerSystemManager _owner;
        readonly Dictionary<string, long> _counters = new(StringComparer.Ordinal);
        bool _eventsBound;

        public event Action<EStatType, string, long> OnStatChanged;

        public PlayerStatisticSystem(PlayerSystemManager owner)
        {
            _owner = owner;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            _counters.Clear();
            var src = savingData?.PlayerData?.StatCounters;
            if (src != null)
            {
                foreach (var kv in src)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value == 0)
                    {
                        continue;
                    }

                    _counters[kv.Key] = kv.Value;
                }
            }

            BindEvents();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.StatCounters ??= new Dictionary<string, long>(StringComparer.Ordinal);
            pd.StatCounters.Clear();
            foreach (var kv in _counters)
            {
                if (kv.Value == 0)
                {
                    continue;
                }

                pd.StatCounters[kv.Key] = kv.Value;
            }
        }

        public long Get(EStatType type, string arg0 = null, string arg1 = null)
        {
            var key = PlayerStatisticKeys.MakeKey(type, arg0, arg1);
            return _counters.TryGetValue(key, out var v) ? v : 0;
        }

        public long GetByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            return _counters.TryGetValue(key, out var v) ? v : 0;
        }

        public long Add(EStatType type, long delta, string arg0 = null, string arg1 = null)
        {
            if (type == EStatType.None || delta == 0)
            {
                return Get(type, arg0, arg1);
            }

            var key = PlayerStatisticKeys.MakeKey(type, arg0, arg1);
            _counters.TryGetValue(key, out var cur);
            var next = cur + delta;
            if (next < 0)
            {
                next = 0;
            }

            _counters[key] = next;
            OnStatChanged?.Invoke(type, key, next);
            PlayerEventBus.Publish(new PlayerStatisticChangedEvent
            {
                StatType = type,
                Key = key,
                NewValue = next,
                Delta = delta,
                Arg0 = arg0 ?? string.Empty,
                Arg1 = arg1 ?? string.Empty,
            });
            return next;
        }

        void BindEvents()
        {
            if (_eventsBound)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerKillUnitEvent>(OnKillUnit);
            PlayerEventBus.Subscribe<PlayerKilledEvent>(OnPlayerKilled);
            PlayerEventBus.Subscribe<PlayerEntityInteractionCompletedEvent>(OnInteract);
            PlayerEventBus.Subscribe<PlayerQuestCompleteEvent>(OnQuestComplete);
            PlayerEventBus.Subscribe<PlayerItemUsedEvent>(OnItemUsed);
            PlayerEventBus.Subscribe<PlayerEnterOverlayEvent>(OnEnterOverlay);
            PlayerEventBus.Subscribe<PlayerJingYuanBlurtAbsorbedEvent>(OnBlurtAbsorbed);
            PlayerEventBus.Subscribe<PlayerNpcUnsensoredEvent>(OnNpcUnsensored);
            _eventsBound = true;
        }

        void OnKillUnit(PlayerKillUnitEvent e)
        {
            if (!e.KilledByPlayer || string.IsNullOrEmpty(e.KilledCfgId))
            {
                return;
            }

            if (e.UnitType != EEntityType.Npc)
            {
                return;
            }

            Add(EStatType.KillNpcCfg, 1, e.KilledCfgId);
        }

        void OnPlayerKilled(PlayerKilledEvent _)
        {
            Add(EStatType.PlayerDeath, 1);
        }

        void OnInteract(PlayerEntityInteractionCompletedEvent e)
        {
            if (!string.IsNullOrEmpty(e.UniqName))
            {
                Add(EStatType.InteractUniq, 1, e.UniqName);
            }
        }

        void OnQuestComplete(PlayerQuestCompleteEvent e)
        {
            if (e.QuestId <= 0)
            {
                return;
            }

            Add(EStatType.QuestComplete, 1, e.QuestId.ToString());
        }

        void OnItemUsed(PlayerItemUsedEvent e)
        {
            if (string.IsNullOrEmpty(e.ItemId) || e.Count <= 0)
            {
                return;
            }

            Add(EStatType.UseItem, e.Count, e.ItemId);
        }

        void OnEnterOverlay(PlayerEnterOverlayEvent e)
        {
            if (string.IsNullOrEmpty(e.OverlayId))
            {
                return;
            }

            Add(EStatType.EnterOverlay, 1, e.OverlayId);
        }

        void OnBlurtAbsorbed(PlayerJingYuanBlurtAbsorbedEvent e)
        {
            if (e.SjAmount <= 0f)
            {
                return;
            }

            // 全局吸收次数（与 per-codex ExtractCodex 并存，供档位 EventGrant 回溯）
            Add(EStatType.BlurtAbsorb, 1);
        }

        void OnNpcUnsensored(PlayerNpcUnsensoredEvent e)
        {
            if (!e.ByPlayer || string.IsNullOrEmpty(e.RaceId))
            {
                return;
            }

            Add(EStatType.UnsensoredRace, 1, e.RaceId);
        }
    }
}
