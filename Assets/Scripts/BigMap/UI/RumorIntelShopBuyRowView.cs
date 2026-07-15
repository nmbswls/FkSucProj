using System;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class RumorIntelShopBuyRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text thumbLabel;

        [SerializeField] TMP_Text priceLabel;

        [SerializeField] Button buyButton;

        string _rumorId;

        Action<string> _onBuy;

        bool _clickHooked;

        void EnsureClickHooked()
        {
            if (_clickHooked || buyButton == null)
            {
                return;
            }

            buyButton.onClick.AddListener(OnBuyClicked);
            _clickHooked = true;
        }

        void Awake()
        {
            EnsureClickHooked();
        }

        void OnBuyClicked()
        {
            _onBuy?.Invoke(_rumorId);
        }

        public void Apply(string thumb, string costId, long cost, string rumorId, Action<string> onBuy)
        {
            EnsureClickHooked();
            if (thumbLabel != null)
            {
                thumbLabel.text = thumb;
            }

            if (priceLabel != null)
            {
                var costDef = ItemCatalog.GetItemDef(costId);
                var costName = costDef != null && !string.IsNullOrEmpty(costDef.DisplayName)
                    ? costDef.DisplayName
                    : costId;
                priceLabel.text = $"{costName} x{cost}";
            }

            _rumorId = rumorId;
            _onBuy = onBuy;
        }
    }
}
