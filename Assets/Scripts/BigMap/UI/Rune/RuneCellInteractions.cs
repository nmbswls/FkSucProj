using cfg.demo;
using My;
using My.Config;
using UnityEngine.EventSystems;

namespace My.UI.Rune
{
    public static class RuneCellInteractions
    {
        public static readonly RuneOwnedCellInteractionPolicy OwnedCell = new RuneOwnedCellInteractionPolicy();
    }

    public sealed class RuneOwnedCellInteractionPolicy
        : IRuneCellClickBehaviour, IRuneCellDragSourceBehaviour, IRuneCellDropTargetBehaviour
    {
        public void OnRuneCellClick(RuneCellBase cell, PointerEventData eventData)
        {
            if (cell is RuneOwnedCell ownedCell)
            {
                ownedCell.Panel?.TryEquipOwnedRune(ownedCell.BoundDef);
            }
        }

        public bool TryBeginDrag(RuneCellBase cell, PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(cell.BoundRuneId))
            {
                return false;
            }

            var ctrl = RuneDragDropController.Instance;
            if (ctrl == null)
            {
                return false;
            }

            return ctrl.BeginDrag(new RuneDragPayload
            {
                RuneId = cell.BoundRuneId,
                SourceType = RuneDragSourceType.OwnedGrid,
                SourceIndex = cell.CellIndex,
            });
        }

        public void HandleDrop(RuneCellBase target, RuneDragPayload payload, RuneDragDropController controller)
        {
            if (payload == null || controller == null)
            {
                return;
            }

            if (payload.SourceType != RuneDragSourceType.EquipSlot)
            {
                return;
            }

            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null)
            {
                return;
            }

            if (pdm.TryUnequipRune(payload.SourceEquipSlot))
            {
                controller.MarkDropHandled();
                if (target is RuneOwnedCell ownedCell)
                {
                    ownedCell.Panel?.RefreshAll();
                }
            }
        }
    }

    public sealed class RuneEquipSlotDragSourcePolicy : IRuneCellDragSourceBehaviour
    {
        readonly ERuneEquipSlot _slot;
        readonly RunePanel _panel;

        public RuneEquipSlotDragSourcePolicy(ERuneEquipSlot slot, RunePanel panel)
        {
            _slot = slot;
            _panel = panel;
        }

        public bool TryBeginDrag(RuneCellBase cell, PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(cell.BoundRuneId))
            {
                return false;
            }

            var ctrl = RuneDragDropController.Instance;
            if (ctrl == null)
            {
                return false;
            }

            return ctrl.BeginDrag(new RuneDragPayload
            {
                RuneId = cell.BoundRuneId,
                SourceType = RuneDragSourceType.EquipSlot,
                SourceEquipSlot = _slot,
            });
        }
    }
}
