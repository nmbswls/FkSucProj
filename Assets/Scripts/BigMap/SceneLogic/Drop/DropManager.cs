using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map;
using My.Player;
using System.Linq;

namespace My.Map.Drop
{
    public class GlobalMapDropCollection
    {
        public Dictionary<long, DropData> _drops = new Dictionary<long, DropData>();

        public event Action<DropData, Vector2?> EvOnDropAdd;
        public event Action<long> EvOnDropRemove;

        public Dictionary<string, long> LostItems = new Dictionary<string, long>();

        public GameLogicManager logicManager;

        public GlobalMapDropCollection(GameLogicManager logicManager)
        {
            _lastRecycleTime = LogicTime.time;
            this.logicManager = logicManager;
        }

        private float _lastRecycleTime;

        public void Tick(float dt)
        {
            if(LogicTime.time < _lastRecycleTime + 60.0f)
            {
                return;
            }

            _lastRecycleTime = LogicTime.time;

            foreach(var k in _drops.Keys.ToList())
            {
                if (_drops[k].CreateTime + 10 * 60f < LogicTime.time )
                {
                    RemoveDrop(k, true);
                }
            }
        }

        public void CreateDrop(string itemId, long amount, Vector2 position, bool autoPick, Vector2? sourcePos)
        {
            Debug.Log($"Create drop {itemId} {amount} {position}");
            var dropData = new DropData(
                GameLogicManager.LogicEntityIdInst++,
                itemId,
                (int)amount,
                position,
                createTime: LogicTime.time,
                autoPick);
            _drops.Add(dropData.Id, dropData);
            EvOnDropAdd?.Invoke(dropData, sourcePos);
        }

        public void CreateDrop(DropUtils.DropReward reward, Vector2 position, bool autoPick, Vector2? sourcePos)
        {
            if (reward == null) return;
            var dropData = new DropData(
                GameLogicManager.LogicEntityIdInst++, reward.ItemId, reward.Amount, position,
                LogicTime.time, autoPick, reward.PremiumEssence);
            _drops.Add(dropData.Id, dropData);
            EvOnDropAdd?.Invoke(dropData, sourcePos);
        }

        public void PickDrop(long id)
        {
            _drops.TryGetValue(id, out var dropData);
            if(dropData == null)
            {
                return;
            }

            RemoveDrop(id, isRecycle: false);
            Debug.Log("PickDrop " + id);
            if (dropData.PremiumEssence != null)
            {
                var stack = new ItemStack(dropData.ItemId, dropData.Amount)
                {
                    InstanceInfo = new ItemInstanceInfo()
                };
                var component = stack.InstanceInfo.GetOrAdd<ItemInstance4PremiumEssence>();
                component.InstanceId = dropData.PremiumEssence.InstanceId;
                component.TypeId = dropData.PremiumEssence.TypeId;
                component.Concentration = dropData.PremiumEssence.Concentration;
                component.DropLevel = dropData.PremiumEssence.DropLevel;
                component.QualityTier = dropData.PremiumEssence.QualityTier;
                logicManager.playerDataManager.JingYuanEssenceSystem.TryAddFromItemStack(stack);
            }
            else
            {
                logicManager.playerDataManager.GiveItemToPlayer(dropData.ItemId, dropData.Amount);
            }
        }

        public void RemoveDrop(long id, bool isRecycle)
        {
            _drops.Remove(id);
            EvOnDropRemove?.Invoke(id);
        }

        public DropData FindDrop(long id)
        {
            _drops.TryGetValue(id, out var dropData);
            return dropData;
        }

        public void Clear()
        {
            _drops.Clear();
        }
    }

    public class DropData
    {
        public long Id;
        public string ItemId;
        public int Amount;
        public Vector2 Position;
        public float CreateTime;
        public bool AutoPick;
        public ItemInstance4PremiumEssence PremiumEssence;

        public DropData(long id, string itemId, int amount, Vector2 position, float createTime, bool autoPick = true, ItemInstance4PremiumEssence premiumEssence = null)
        {
            Id = id;
            ItemId = itemId;
            Amount = amount;
            Position = position;
            CreateTime = createTime;
            AutoPick = autoPick;
            PremiumEssence = premiumEssence;
        }
    }
}
