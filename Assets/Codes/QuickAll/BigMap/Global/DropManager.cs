using System.Collections.Generic;
using System;
using UnityEngine;

namespace Map.Drop
{
    public class GlobalMapDropCollection
    {
        private int _dropCounter;
        public Dictionary<long, DropData> _drops = new Dictionary<long, DropData>();

        public event Action<DropData> EvOnDropAdd;
        public event Action<long> EvOnDropRemove;

        public GlobalMapDropCollection(GameLogicManager logicManager)
        {
        }


        public void CreateDrop(string itemId, int amount, Vector2 position, bool autoPick)
        {
            var dropData = new DropData(_dropCounter++, itemId, amount, position, autoPick);
            _drops.Add(dropData.Id, dropData);
            EvOnDropAdd?.Invoke(dropData);

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
        public bool AutoPick;

        public DropData(int id, string itemId, int amount, Vector2 position, bool autoPick = true)
        {
            Id = id;
            ItemId = itemId;
            Amount = amount;
            Position = position;
            AutoPick = autoPick;
        }
    }
}