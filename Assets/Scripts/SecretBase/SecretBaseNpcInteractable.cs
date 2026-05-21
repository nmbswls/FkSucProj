using cfg.demo;
using My.UI;
using UnityEngine;

namespace My.SecretBase
{
    public class SecretBaseNpcInteractable : SecretBaseClickTargetBase
    {
        SecretBaseCharacter _row;

        public SecretBaseCharacter Row => _row;

        public void Setup(SecretBaseCharacter row)
        {
            _row = row;
            CacheRefs();
            if (row != null)
            {
                ApplySortOrder(row.SortOrder);
            }
        }

        void Awake()
        {
            CacheRefs();
        }

        public override void OnClick()
        {
            if (_row == null)
            {
                return;
            }

            UIManager.Instance.ShowPanel(
                SecretBaseNpcHubPanel.PanelIdConst,
                new SecretBaseNpcHubPanel.Payload { CharacterRow = _row });
        }
    }
}
