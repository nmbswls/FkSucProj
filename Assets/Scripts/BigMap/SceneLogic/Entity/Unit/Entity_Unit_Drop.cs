
using System;
using System.Collections.Generic;
using My.Map.Entity;
using My.Player.Bag;
using UnityEngine;
using static My.Map.LootPointLogicEntity;

namespace My.Map
{
    public partial class BaseUnitLogicEntity : ILootableObj
    {

        public void TryUseLootPoint()
        {
            LogicManager.viewer.ShowFakeFxEffect("หัฃก", Pos);
            LogicManager.viewer.StartLoot(this);
        }

        public List<ItemStack> LootItems
        {
            get
            {
                return dropBagContainer.InnerItems ?? new();
            }
        }

        public event Action<int> EnOnUnrealed;

        public EContainerType GetContainerType()
        {
            return EContainerType.LootPoint;
        }

        public IItemContainer GetLootItemContainer()
        {
            return dropBagContainer;
        }

        public int GetCurrUnrealed()
        {
            return -1;
        }

        public bool IsRevealed(int itemIdx)
        {
            return true;
        }

        public void RemoveFromIndex(int index, int count)
        {
            if(dropBagContainer == null)
            {
                Debug.LogError("unit drop invlid");
                return;
            }
            if (index < 0 || index >= dropBagContainer.InnerItems.Count) return;
            var s = dropBagContainer.InnerItems[index];
            if (s == null) return;
            s.RemoveFromStack(count);
            if (s.Count <= 0) dropBagContainer.InnerItems[index] = null;
        }


        public void TickUnReveal(float dt)
        {
            
        }
    }
}