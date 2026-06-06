using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.UI.Rune
{
    public class RuneOwnedCell : RuneCellBase
    {
        RuneData _def;
        RunePanel _panel;

        public RuneData BoundDef => _def;
        public RunePanel Panel => _panel;

        public void Bind(RunePanel panel, RuneData def, bool isEquipped, bool selected, int index)
        {
            _panel = panel;
            _def = def;
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
            RefreshCellStyle(selected ? EStyleType.Selected : EStyleType.Normal);

            if (equippedMark != null)
            {
                equippedMark.gameObject.SetActive(isEquipped);
            }
        }
    }
}
