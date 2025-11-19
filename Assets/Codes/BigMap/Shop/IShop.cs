
using System.Collections.Generic;
using My.Player.Bag;

namespace My
{
    public interface IShopProvider
    { 
        int ShopId { get; }

        List<(ItemStack, bool)> ShopItems { get; }
    }

}