using My.Config;
using My.Player.Bag;
using UnityEngine;

namespace My.UI
{
    // 背包格悬停：交给 UIHoverManager，详情由 ItemHoverTipPanel 展示
    public class ItemCellHoverProvider : BaseUIHoverProvider
    {
        ItemCellBase _cell;
        bool _nameOnlyTip;

        public ItemCellBase Cell => _cell;

        public void SetNameOnlyTip(bool nameOnly)
        {
            _nameOnlyTip = nameOnly;
        }

        public bool IsNameOnlyTip => _nameOnlyTip;

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

        public virtual string GetDisplayName()
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

        public virtual string GetDetailText()
        {
            ItemStack stack = _cell?.GetBoundStack();
            if (stack == null || stack.IsEmpty)
            {
                return string.Empty;
            }

            return ItemHoverDetailUtil.BuildDetailText(stack);
        }
    }
}
