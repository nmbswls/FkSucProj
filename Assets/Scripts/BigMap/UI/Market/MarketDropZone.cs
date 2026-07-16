using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI.Market
{
    public sealed class MarketDropZone : MonoBehaviour, IDropHandler
    {
        public MarketPanel Panel;

        public void OnDrop(PointerEventData eventData)
        {
            var controller = My.UI.ItemDragDropController.Instance;
            if (Panel != null && controller != null)
            {
                Panel.TryAcceptDrop(controller);
            }
        }
    }
}
