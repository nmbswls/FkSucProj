using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // 背包可装备候选列表（主背包 → 点击装备）
    public sealed class GearEquipBagGridView : MonoBehaviour
    {
        [SerializeField] Transform gridContent;
        [SerializeField] AnyContainerItemCell itemCellTemplate;
        [SerializeField] TextMeshProUGUI emptyHint;

        readonly List<AnyContainerItemCell> _cells = new();
        GearEquipBagCellClickPolicy _clickPolicy;

        public void Refresh(
            PlayerEquipmentManager equipment,
            EBodyPart part,
            Action onEquipChanged)
        {
            ClearCells();
            if (equipment == null || gridContent == null || itemCellTemplate == null)
            {
                SetEmptyHintVisible(true);
                return;
            }

            _clickPolicy ??= new GearEquipBagCellClickPolicy();
            _clickPolicy.Configure(equipment, () => part, onEquipChanged);

            var list = equipment.ListMainBagCandidates(part);
            bool any = false;
            foreach (var pair in list)
            {
                any = true;
                int flatIdx = pair.bagFlatIndex;
                var stack = pair.stack;
                bool canEquip = equipment.CanEquipFromMainBag(part, flatIdx, out _);
                var style = canEquip ? ItemCellBase.EStyleType.Normal : ItemCellBase.EStyleType.Masked;

                var cell = SpawnCell();
                cell.Bind(stack, flatIdx, EContainerType.Inventory, 0, null, style);
                cell.SetItemCellInteractions(_clickPolicy, null, null);
            }

            SetEmptyHintVisible(!any);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent as RectTransform);
        }

        AnyContainerItemCell SpawnCell()
        {
            var cell = Instantiate(itemCellTemplate, gridContent);
            cell.gameObject.SetActive(true);
            _cells.Add(cell);
            return cell;
        }

        void SetEmptyHintVisible(bool visible)
        {
            if (emptyHint != null)
            {
                emptyHint.gameObject.SetActive(visible);
            }
        }

        void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                {
                    Destroy(_cells[i].gameObject);
                }
            }

            _cells.Clear();

            if (gridContent == null || itemCellTemplate == null)
            {
                return;
            }

            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                var child = gridContent.GetChild(i);
                if (child == itemCellTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        sealed class GearEquipBagCellClickPolicy : IItemCellClickBehaviour
        {
            PlayerEquipmentManager _equipment;
            Func<EBodyPart> _getPart;
            Action _onChanged;

            public void Configure(
                PlayerEquipmentManager equipment,
                Func<EBodyPart> getPart,
                Action onChanged)
            {
                _equipment = equipment;
                _getPart = getPart;
                _onChanged = onChanged;
            }

            public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData)
            {
                if (_equipment == null || _getPart == null || cell == null)
                {
                    return;
                }

                var part = _getPart();
                if (!_equipment.CanEquipFromMainBag(part, cell.Index, out _))
                {
                    return;
                }

                if (_equipment.TryEquipFromMainBag(part, cell.Index, out _))
                {
                    _onChanged?.Invoke();
                }
            }
        }
    }
}
