using System;
using System.Collections.Generic;
using My;
using My.Farm;
using My.Map.Entity;
using My.Player;
using My.Player.Bag;
using My.UI;
using UnityEngine;

namespace My.Map.Scene
{
    // 种子篮表现：整理种子 / 进入播种模式；库存走 FarmSystem
    public class SceneSeedBasketPresenter : ScenePresentationBase<SeedBasketLogicEntity>, ISceneInteractable, ILootableObj
    {
        [SerializeField] Transform hintPivot;

        public event Action<int> EnOnUnrealed;

        public string ShowName => "种子篮";
        public Vector2 Pos => transform.position;
        public bool WithInteractDetail => true;
        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }

        SeedBasketLogicEntity Basket => (SeedBasketLogicEntity)_logic;

        FarmSystem Farm => MainGameManager.Instance?.gameLogicManager?.farmSystem;

        string LogicAreaId =>
            Basket != null && !string.IsNullOrEmpty(Basket.LogicAreaId)
                ? Basket.LogicAreaId
                : FarmCatalog.DefaultLogicAreaId;

        public bool CanInteractEnable() => Farm != null && Basket != null;

        public Vector3 GetHintAnchorPosition() => hintPivot != null ? hintPivot.position : transform.position;

        public float GetHintOffsetInfos() => 0.4f;

        public bool IsAutoInteract() => false;

        public List<SceneInteractSelection> GetInteractSelections()
        {
            return new List<SceneInteractSelection>
            {
                new() { SelectId = 1, SelectContent = "整理种子" },
                new() { SelectId = 2, SelectContent = "开始播种" },
            };
        }

        public bool TriggerInteract(int selectionId, int playerId)
        {
            var farm = Farm;
            if (farm == null)
            {
                return false;
            }

            if (selectionId == 1)
            {
                UIOrchestrator.Instance?.TryEnterLootDetailMode(this);
                return true;
            }

            if (selectionId == 2)
            {
                return farm.EnterPlantingMode(LogicAreaId);
            }

            return false;
        }

        public List<ItemStack> LootItems
        {
            get
            {
                var bag = Farm?.GetSeedBasket(LogicAreaId);
                return bag != null ? bag.NormalSlots : new List<ItemStack>();
            }
        }

        public bool IsRevealed(int itemIdx) => true;

        public void TickUnReveal(float dt)
        {
        }

        public int GetCurrUnrealed() => -1;

        public void RemoveFromIndex(int index, int count)
        {
            var bag = Farm?.GetSeedBasket(LogicAreaId);
            if (bag == null || index < 0 || index >= bag.NormalSlots.Count)
            {
                return;
            }

            var stack = bag.NormalSlots[index];
            if (stack == null || stack.IsEmpty)
            {
                return;
            }

            stack.Count -= count;
            if (stack.Count <= 0)
            {
                bag.NormalSlots[index] = null;
            }

            bag.CompactPackPrimary();
            Farm?.NotifyChanged();
        }

        public EContainerType GetContainerType() => EContainerType.LootPoint;

        public IItemContainer GetLootItemContainer() => Farm?.GetSeedBasket(LogicAreaId);

        public void TryUseLootPoint()
        {
        }
    }
}
