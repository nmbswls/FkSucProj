using System;
using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyMaterialPickerCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] Button clickButton;
        [SerializeField] Button unequipButton;

        string _itemId;
        Action<string> _onPick;
        Action<string> _onUnequip;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }

            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveAllListeners();
                unequipButton.onClick.AddListener(OnUnequipClicked);
            }
        }

        public void Bind(string itemId, long count, long placedCount, Action<string> onPick, Action<string> onUnequip)
        {
            _itemId = itemId;
            _onPick = onPick;
            _onUnequip = onUnequip;
            var def = ItemCatalog.GetItemDef(itemId);
            if (nameText != null)
            {
                nameText.text = def?.DisplayName ?? itemId;
            }

            if (countText != null)
            {
                if (placedCount > 0)
                {
                    countText.text = $"x{count} 已投入{placedCount}";
                }
                else
                {
                    countText.text = count > 0 ? "x" + count : string.Empty;
                }
            }

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(placedCount > 0 && onUnequip != null);
            }

            ApplyIcon(def?.SpriteName);
        }

        void ApplyIcon(string spriteName)
        {
            if (icon == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                icon.enabled = false;
                return;
            }

            var sp = SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
            icon.sprite = sp;
            icon.enabled = sp != null;
        }

        void OnClicked()
        {
            if (!string.IsNullOrEmpty(_itemId))
            {
                _onPick?.Invoke(_itemId);
            }
        }

        void OnUnequipClicked()
        {
            if (!string.IsNullOrEmpty(_itemId))
            {
                _onUnequip?.Invoke(_itemId);
            }
        }
    }
}
