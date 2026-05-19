using My.UI;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 据点装修 UI 占位：后续接家具目录与摆放预览。
    public class SecretBaseBuildPanel : PanelWithInput
    {
        public const string PanelIdConst = "SecretBaseBuildPanel";

        [SerializeField] private Button btnClose;

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
