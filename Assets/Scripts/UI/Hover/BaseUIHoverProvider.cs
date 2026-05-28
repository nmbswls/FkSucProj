


using UnityEngine;

namespace My.UI
{
    public class BaseUIHoverProvider : MonoBehaviour, IHoverInfoProvider
    {
        public RectTransform boxTr;

        protected virtual void Awake()
        {
            boxTr = transform as RectTransform;
        }
        public Vector2 TooltipPosition
        {
            get
            {
                return transform.position;
            }
        }



        public RectTransform GetHoverUIRange() 
        {
            return boxTr;
        }

        public virtual void OnEnterHovered()
        {

        }


        public virtual void OnLeaveHovered()
        {

        }

        public virtual Vector2? GetCustomScreenPos(Camera uiCamera)
        {
            Vector3[] corners = new Vector3[4];
            boxTr.GetWorldCorners(corners);
            Vector3 worldTopLeft = corners[2];

            Camera cam = Camera.main;

            // 将世界坐标转屏幕坐标（像素）
            Vector3 screenPos = cam ? cam.WorldToScreenPoint(worldTopLeft)
                                    : new Vector3(worldTopLeft.x, worldTopLeft.y, 0f);

            return new Vector2(screenPos.x, screenPos.y);
        }

        public HoverTipParams InnerParams;

        public virtual HoverTipParams? GetSimpleTipInfo()
        {
            return InnerParams;
        }

        void OnDisable()
        {
            UIHoverManager.Instance?.NotifyProviderDisabled(this);
        }
    }
}