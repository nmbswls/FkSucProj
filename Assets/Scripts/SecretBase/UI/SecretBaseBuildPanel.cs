using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseBuildPanel : PanelWithInput
    {
        public const string PanelIdConst = "SecretBaseBuildPanel";

        [SerializeField] Button btnClose;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
            }
        }

        public override bool OnCancel()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
            return true;
        }
    }
}
