using System;
using My.Config;
using My.Quest;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    public sealed class PlayerDirectDeathRewardSystem : IPlayerSystem
    {
        const string LogTag = "[PlayerDirectDeathRewardSystem]";

        readonly PlayerSystemManager _owner;
        bool _eventsBound;

        public PlayerDirectDeathRewardSystem(PlayerSystemManager owner)
        {
            _owner = owner;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            if (_eventsBound)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerKillUnitEvent>(OnUnitKilled);
            _eventsBound = true;
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        public void Tick(float dt)
        {
        }

        void OnUnitKilled(PlayerKillUnitEvent evt)
        {
            if (string.IsNullOrEmpty(evt.KilledCfgId))
            {
                return;
            }

            var npc = CfgMgr.Cfgs?.TbUnitNpc?.GetOrDefault(evt.KilledCfgId);
            if (npc == null
                || (npc.DirectRewardRequirePlayerKill && !evt.KilledByPlayer))
            {
                return;
            }

            if (npc.DirectDropId > 0)
            {
                var items = DropUtils.GetBundleDropItems(npc.DirectDropId);
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    long gained = _owner.GiveItemToPlayer(item.Item1, item.Item2);
                    if (gained < item.Item2)
                    {
                        Debug.LogError(
                            $"{LogTag} Direct reward inventory overflow: npc={evt.KilledCfgId}, item={item.Item1}, requested={item.Item2}, gained={gained}.");
                    }
                }
            }

            if (!string.IsNullOrEmpty(npc.DirectRuneId)
                && !_owner.RuneSystem.OwnsRune(npc.DirectRuneId))
            {
                _owner.TryGrantRune(npc.DirectRuneId);
            }
        }
    }
}
