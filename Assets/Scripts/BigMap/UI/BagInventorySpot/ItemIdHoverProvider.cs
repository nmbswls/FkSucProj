using My.Config;
using UnityEngine;

namespace My.UI
{
    // 按 itemId 展示悬停详情（养成面板装备行/护符格等）
    public sealed class ItemIdHoverProvider : BaseUIHoverProvider
    {
        string _itemId;
        long _stackCount = 1;
        bool _nameOnlyTip;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.Item,
                BindPos = Vector3.zero,
            };
        }

        public void SetItem(string itemId, long stackCount = 1, bool nameOnlyTip = false)
        {
            _itemId = itemId;
            _stackCount = stackCount > 0 ? stackCount : 1;
            _nameOnlyTip = nameOnlyTip;
        }

        public void SetNameOnlyTip(bool nameOnly)
        {
            _nameOnlyTip = nameOnly;
        }

        public bool IsNameOnlyTip => _nameOnlyTip;

        public void ClearItem()
        {
            _itemId = null;
            _stackCount = 1;
            _nameOnlyTip = false;
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (string.IsNullOrEmpty(_itemId))
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(_itemId))
            {
                return string.Empty;
            }

            var def = ItemCatalog.GetItemDef(_itemId);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return _itemId;
        }

        public string GetDetailText() => ItemHoverDetailUtil.BuildDetailText(_itemId, _stackCount);
    }
}
