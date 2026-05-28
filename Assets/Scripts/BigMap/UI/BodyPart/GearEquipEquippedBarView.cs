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
        readonly List<int> _filledEquippedIndices = new();
        HorizontalLayoutGroup _layoutGroup;
        RectTransform _contentRect;

        void Awake()
        {
            EnsureTemplateHidden();
            CacheLayout();
        }

        void CacheLayout()
        {
            if (gridContent == null)
            {
                return;
            }

            _contentRect = gridContent as RectTransform;
            _layoutGroup = gridContent.GetComponent<HorizontalLayoutGroup>();
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
            Action<int> onUnequipRequested)
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
                _filledEquippedIndices.Add(equippedIndex);
                cell.BindEquipped(slot.ItemId, cost, name, () => onUnequipRequested?.Invoke(equippedIndex));
            }

            if (!any)
            {
                SpawnCell().BindEmpty();
            }

            SetEmptyHintVisible(false);
            RebuildLayout();
        }

        public bool TryGetFilledCell(
            int equippedIndex,
            out GearEquipEquippedCellView cell,
            out RectTransform iconRect,
            out string itemId)
        {
            cell = null;
            iconRect = null;
            itemId = null;

            for (int i = 0; i < _cells.Count; i++)
            {
                if (_filledEquippedIndices.Count <= i || _filledEquippedIndices[i] != equippedIndex)
                {
                    continue;
                }

                cell = _cells[i];
                iconRect = cell.IconRect;
                itemId = cell.GetItemId();
                return cell != null && iconRect != null && !string.IsNullOrEmpty(itemId);
            }

            return false;
        }

        public float GetCellStep()
        {
            CacheLayout();
            if (_layoutGroup == null)
            {
                return 60f;
            }

            float width = cellTemplate != null
                ? (cellTemplate.transform as RectTransform).rect.width
                : 60f;
            return width + _layoutGroup.spacing;
        }

        public List<RectTransform> CollectSlideTargetsAfter(int equippedIndex)
        {
            var result = new List<RectTransform>();
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_filledEquippedIndices.Count <= i || _filledEquippedIndices[i] <= equippedIndex)
                {
                    continue;
                }

                var rt = _cells[i].transform as RectTransform;
                if (rt != null)
                {
                    result.Add(rt);
                }
            }

            return result;
        }

        public void BeginManualLayout()
        {
            CacheLayout();
            if (_layoutGroup != null)
            {
                _layoutGroup.enabled = false;
            }
        }

        public void EndManualLayoutAndRemoveAt(int equippedIndex)
        {
            int removeVisualIndex = -1;
            for (int i = 0; i < _filledEquippedIndices.Count; i++)
            {
                if (_filledEquippedIndices[i] == equippedIndex)
                {
                    removeVisualIndex = i;
                    break;
                }
            }

            if (removeVisualIndex >= 0)
            {
                var cell = _cells[removeVisualIndex];
                _cells.RemoveAt(removeVisualIndex);
                _filledEquippedIndices.RemoveAt(removeVisualIndex);
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            if (_layoutGroup != null)
            {
                _layoutGroup.enabled = true;
            }

            RebuildLayout();
        }

        public bool TryGetAppendSlotWorldPos(out Vector3 worldPos)
        {
            worldPos = default;
            CacheLayout();
            if (_contentRect == null)
            {
                return false;
            }

            if (_filledEquippedIndices.Count == 0)
            {
                if (_cells.Count > 0 && _cells[0].IconRect != null)
                {
                    worldPos = _cells[0].IconRect.position;
                    return true;
                }

                if (cellTemplate != null)
                {
                    worldPos = cellTemplate.IconRect.position;
                    return true;
                }

                worldPos = _contentRect.position;
                return true;
            }

            var lastCell = _cells[_cells.Count - 1];
            if (lastCell == null || lastCell.IconRect == null)
            {
                return false;
            }

            float step = GetCellStep();
            var local = _contentRect.InverseTransformPoint(lastCell.IconRect.position);
            local.x += step;
            worldPos = _contentRect.TransformPoint(local);
            return true;
        }

        GearEquipEquippedCellView SpawnCell()
        {
            var go = Instantiate(cellTemplate.gameObject, gridContent, false);
            var cell = go.GetComponent<GearEquipEquippedCellView>();
            go.SetActive(true);
            cell.SetVisualHidden(false);
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

        void RebuildLayout()
        {
            if (_contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
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
            _filledEquippedIndices.Clear();

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
