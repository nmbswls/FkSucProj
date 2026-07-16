using System.Collections.Generic;
using My.Map.Logic;
using My.Player.Bag;
using UnityEngine;

namespace My.Map.Entity
{
    // 玩家放置的运输标记：空容器、无搜索遮罩、可双向拖放
    public sealed class TransportLootPointLogicEntity : LootPointLogicEntity
    {
        public const string MarkerCfgId = "transport_marker";

        public bool IsTransportMarker => true;

        public TransportLootPointLogicEntity(
            GameLogicManager logicManager,
            long instId,
            string cfgId,
            Vector2 orgPos,
            LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            IsLocked = false;
        }

        public override void Initialize()
        {
            base.Initialize();
            IsLocked = false;
        }

        public new bool IsRevealed(int itemIdx) => true;

        public new int GetCurrUnrealed() => -1;

        public new void TickUnReveal(float dt)
        {
        }

        public bool HasAnyItem()
        {
            foreach (var stack in LootItems)
            {
                if (stack != null && stack.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
