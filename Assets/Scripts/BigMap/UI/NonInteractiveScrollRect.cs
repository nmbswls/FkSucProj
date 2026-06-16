using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI
{
    // 仅允许代码控制 content 位置，禁止玩家拖拽/滚轮滚动
    public class NonInteractiveScrollRect : ScrollRect
    {
        public override void OnBeginDrag(PointerEventData eventData)
        {
        }

        public override void OnDrag(PointerEventData eventData)
        {
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
        }

        public override void OnScroll(PointerEventData eventData)
        {
        }
    }
}
