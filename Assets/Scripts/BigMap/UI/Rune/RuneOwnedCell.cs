using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.UI.Rune
{
    public class RuneOwnedCell : RuneCellBase
    {
        RuneData _def;
        RunePanel _panel;
        bool _canEquipToSelectedSlot;

        public RuneData BoundDef => _def;
        public RunePanel Panel => _panel;
        public bool CanEquipToSelectedSlot => _canEquipToSelectedSlot;

        public void Bind(RunePanel panel, RuneData def, bool isEquipped, bool selected, int index, bool canEquipToSelectedSlot)
        {
            _panel = panel;
            _def = def;
            _canEquipToSelectedSlot = canEquipToSelectedSlot;
            SetBoundRune(def?.RuneId, index);
            SetRuneCellInteractions(
                RuneCellInteractions.OwnedCell,
                RuneCellInteractions.OwnedCell,
                RuneCellInteractions.OwnedCell);

            if (nameText != null)
            {
                nameText.text = def?.Name ?? string.Empty;
            }

            ApplyRuneIcon(def);

            if (!canEquipToSelectedSlot)
            {
                RefreshCellStyle(EStyleType.Locked);
            }
            else
            {
                RefreshCellStyle(selected ? EStyleType.Selected : EStyleType.Normal);
            }

            if (equippedMark != null)
            {
                equippedMark.gameObject.SetActive(isEquipped);
            }

            if (maskOverlay != null)
            {
                maskOverlay.gameObject.SetActive(!canEquipToSelectedSlot);
            }
        }
    }
}
