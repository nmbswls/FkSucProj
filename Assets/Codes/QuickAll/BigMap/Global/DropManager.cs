using System.Collections.Generic;
using System;
using UnityEngine;
using My.Map;

namespace Map.Drop
{
    public class GlobalMapDropCollection
    {
        //private int _dropCounter;
        public Dictionary<long, DropData> _drops = new Dictionary<long, DropData>();

        public event Action<DropData, Vector2?> EvOnDropAdd;
        public event Action<long> EvOnDropRemove;

        public GlobalMapDropCollection(GameLogicManager logicManager)
        {
        }


        public void CreateDrop(string itemId, int amount, Vector2 position, bool autoPick, Vector2? sourcePos)
        {
            var dropData = new DropData(GameLogicManager.LogicEntityIdInst++, itemId, amount, position, createTime: LogicTime.time,  autoPick);
            _drops.Add(dropData.Id, dropData);
            EvOnDropAdd?.Invoke(dropData, sourcePos);
        }

        public void RemoveDrop(long id)
        {
            _drops.Remove(id);
            EvOnDropRemove?.Invoke(id);
        }

        public DropData FindDrop(long id)
        {
            _drops.TryGetValue(id, out var dropData);
            return dropData;
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

        public DropData(long id, string itemId, int amount, Vector2 position, float createTime, bool autoPick = true)
        {
            Id = id;
            ItemId = itemId;
            Amount = amount;
            Position = position;
            this.CreateTime = createTime;
            AutoPick = autoPick;
        }
    }
}