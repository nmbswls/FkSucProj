using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public static class JingYuanFacilityAccess
    {
        static bool _granted;

        public static bool CanOpenTune(GameLogicManager glm)
        {
            return _granted && glm != null && glm.IsInSecretBaseContext();
        }

        public static void Grant() => _granted = true;

        public static void Revoke() => _granted = false;
    }

    public sealed class JingYuanPoolPanel : PanelBase
    {
        public const string PanelIdConst = "JingYuanPoolPanel";

        [SerializeField] Button tuneButton;
        [SerializeField] Button warehouseButton;
        [SerializeField] Button closeButton;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            tuneButton?.onClick.AddListener(OpenTune);
            warehouseButton?.onClick.AddListener(OpenWarehouse);
            closeButton?.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
        }

        public override void Show()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                UIManager.Instance.HidePanel(PanelIdConst);
                return;
            }

            base.Show();
        }

        public override void Hide()
        {
            JingYuanFacilityAccess.Revoke();
            base.Hide();
        }

        void OpenTune()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext()) return;
            JingYuanFacilityAccess.Grant();
            PlayerProgressionHubPanel.OpenJingYuanTune();
        }

        void OpenWarehouse()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext()) return;
            UIManager.Instance.ShowPanel(JingYuanWarehousePanel.PanelIdConst);
        }
    }
}
