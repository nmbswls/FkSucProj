using My.Config;
using My.Player.Bag;

namespace My.UI
{
    // 武器快捷槽专用 hover 提供者。
    // 与 ItemCellHoverProvider 的区别：即使槽位为空也返回 TipInfo，显示"暂无装备"。
    public class WeaponCellHoverProvider : ItemCellHoverProvider
    {
        public override HoverTipParams? GetSimpleTipInfo()
        {
            return InnerParams;
        }

        public override string GetDisplayName()
        {
            ItemStack stack = Cell?.GetBoundStack();
            if (stack == null || stack.IsEmpty)
            {
                return "暂无装备";
            }

            var def = ItemCatalog.GetItemDef(stack.ItemID);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return stack.ItemID;
        }

        public override string GetDetailText()
        {
            ItemStack stack = Cell?.GetBoundStack();
            if (stack == null || stack.IsEmpty)
            {
                return "空";
            }

            return ItemHoverDetailUtil.BuildDetailText(stack.ItemID, stack.Count);
        }
    }
}
