using My.Config;
using My.Player.Bag;
using UnityEngine;

namespace My.UI
{
    // 背包格悬停：交给 UIHoverManager，详情由 ItemHoverTipPanel 展示
    public class ItemCellHoverProvider : BaseUIHoverProvider
    {
        ItemCellBase _cell;

        public ItemCellBase Cell => _cell;

        protected override void Awake()
        {
            base.Awake();
            _cell = GetComponent<ItemCellBase>();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.Item,
                BindPos = Vector3.zero,
            };
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            ItemStack stack = _cell?.GetBoundStack();
            if (stack == null || stack.IsEmpty)
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            ItemStack stack = _cell?.GetBoundStack();
            if (stack == null || stack.IsEmpty)
            {
                return string.Empty;
            }

            var def = ItemCatalog.GetItemDef(stack.ItemID);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return stack.ItemID;
        }
    }
}
