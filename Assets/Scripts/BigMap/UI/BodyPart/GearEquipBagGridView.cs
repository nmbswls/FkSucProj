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
        RectTransform _contentRect;

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
            _clickPolicy.Configure(equipment, () => part, onEquipChanged, null);

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
                cell.SetVisualHidden(false);
            }

            SetEmptyHintVisible(!any);
            RebuildLayout();
        }

        public void ConfigureEquipAnim(GearEquipTransferAnimView transferAnim, Func<EBodyPart> getPart, Action onFinished)
        {
            _clickPolicy ??= new GearEquipBagCellClickPolicy();
            _clickPolicy.SetTransferAnim(transferAnim, getPart, onFinished);
        }

        public bool TryFindCellIcon(long itemInstanceId, string itemId, out RectTransform iconRect)
        {
            iconRect = null;
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var stack = cell?.GetBoundStack();
                if (stack == null || stack.IsEmpty)
                {
                    continue;
                }

                if (itemInstanceId != 0 && stack.ItemInstanceId != itemInstanceId)
                {
                    continue;
                }

                if (itemInstanceId == 0 && !string.IsNullOrEmpty(itemId) && stack.ItemID != itemId)
                {
                    continue;
                }

                if (cell.TryGetIconRect(out iconRect))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryFindCellIconByItemId(string itemId, out RectTransform iconRect)
        {
            return TryFindCellIcon(0, itemId, out iconRect);
        }

        public bool TryGetCandidatesAreaCenter(out RectTransform iconRect)
        {
            iconRect = null;
            _contentRect ??= gridContent as RectTransform;
            if (_contentRect == null)
            {
                return false;
            }

            iconRect = _contentRect;
            return true;
        }

        public void SetIconVisible(RectTransform iconRect, bool visible)
        {
            if (iconRect == null)
            {
                return;
            }

            var cell = iconRect.GetComponentInParent<AnyContainerItemCell>();
            if (cell != null)
            {
                cell.SetVisualHidden(!visible);
                return;
            }

            var cg = iconRect.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = iconRect.gameObject.AddComponent<CanvasGroup>();
            }

            cg.alpha = visible ? 1f : 0f;
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

        void RebuildLayout()
        {
            _contentRect ??= gridContent as RectTransform;
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
            GearEquipTransferAnimView _transferAnim;
            Action _onAnimFinished;

            public void Configure(
                PlayerEquipmentManager equipment,
                Func<EBodyPart> getPart,
                Action onChanged,
                GearEquipTransferAnimView transferAnim)
            {
                _equipment = equipment;
                _getPart = getPart;
                _onChanged = onChanged;
                _transferAnim = transferAnim;
            }

            public void SetTransferAnim(
                GearEquipTransferAnimView transferAnim,
                Func<EBodyPart> getPart,
                Action onAnimFinished)
            {
                _transferAnim = transferAnim;
                _getPart = getPart;
                _onAnimFinished = onAnimFinished;
            }

            public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData)
            {
                if (_equipment == null || _getPart == null || cell == null)
                {
                    return;
                }

                if (_transferAnim != null && _transferAnim.IsBusy)
                {
                    return;
                }

                var part = _getPart();
                if (!_equipment.CanEquipFromMainBag(part, cell.Index, out _))
                {
                    return;
                }

                if (_transferAnim != null
                    && cell is AnyContainerItemCell bagCell
                    && _transferAnim.TryPlayEquip(_equipment, part, cell.Index, bagCell, _onAnimFinished))
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
