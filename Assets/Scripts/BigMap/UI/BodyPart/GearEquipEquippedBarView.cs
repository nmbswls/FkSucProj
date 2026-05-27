using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // EquippedBar：当前部位已装备物品列表
    public sealed class GearEquipEquippedBarView : MonoBehaviour
    {
        [SerializeField] Transform gridContent;
        [SerializeField] GearEquipEquippedCellView cellTemplate;
        [SerializeField] TextMeshProUGUI emptyHint;

        readonly List<GearEquipEquippedCellView> _cells = new();

        void Awake()
        {
            EnsureTemplateHidden();
        }

        void EnsureTemplateHidden()
        {
            if (cellTemplate != null)
            {
                cellTemplate.gameObject.SetActive(false);
            }
        }

        public void Refresh(
            PlayerEquipmentManager equipment,
            EBodyPart part,
            Action onEquipChanged)
        {
            ClearCells();
            EnsureTemplateHidden();
            if (equipment == null || gridContent == null || cellTemplate == null)
            {
                SetEmptyHintVisible(true);
                return;
            }

            var list = equipment.GetEquippedOnPart(part);
            bool any = false;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                {
                    continue;
                }

                any = true;
                var itemDef = ItemCatalog.GetItemDef(slot.ItemId);
                int cost = ItemGearRules.GetSlotCost(itemDef);
                string name = itemDef != null ? itemDef.DisplayName : slot.ItemId;
                int equippedIndex = i;

                var cell = SpawnCell();
                cell.BindEquipped(slot.ItemId, cost, name, () =>
                {
                    equipment.TryUnequip(part, equippedIndex, out _);
                    onEquipChanged?.Invoke();
                });
            }

            if (!any)
            {
                SpawnCell().BindEmpty();
            }

            SetEmptyHintVisible(false);
            if (gridContent is RectTransform rt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        GearEquipEquippedCellView SpawnCell()
        {
            var go = Instantiate(cellTemplate.gameObject, gridContent, false);
            var cell = go.GetComponent<GearEquipEquippedCellView>();
            go.SetActive(true);
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

            if (gridContent == null || cellTemplate == null)
            {
                return;
            }

            for (int i = gridContent.childCount - 1; i >= 0; i--)
            {
                var child = gridContent.GetChild(i);
                if (cellTemplate != null && child == cellTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }

            EnsureTemplateHidden();
        }
    }
}
