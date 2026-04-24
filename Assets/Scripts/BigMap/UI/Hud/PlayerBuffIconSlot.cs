using My.Map.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // HUD 单个 buff 图标：悬停交给 UIHoverManager，详情由 PlayerBuffHoverTipPanel 展示
    public class PlayerBuffIconSlot : BaseUIHoverProvider
    {
        public Image IconImage;
        public BuffInstance BoundBuff;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.PlayerBuff,
                BindPos = Vector3.zero,
            };
            if (IconImage == null)
            {
                IconImage = GetComponent<Image>();
            }
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (BoundBuff == null || BoundBuff.MarkedForRemove)
            {
                return null;
            }

            return InnerParams;
        }

        public void ClearSlot()
        {
            BoundBuff = null;
            if (IconImage != null)
            {
                IconImage.sprite = null;
                IconImage.enabled = false;
            }
            gameObject.SetActive(false);
        }

        public void BindBuff(BuffInstance buff, Sprite iconSprite)
        {
            BoundBuff = buff;
            gameObject.SetActive(true);
            if (IconImage != null)
            {
                IconImage.enabled = iconSprite != null;
                IconImage.sprite = iconSprite;
            }
        }
    }
}
