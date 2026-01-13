

using My.Map.View;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class BigMapFinishPanel : PanelWithInput, IInputConsumer
    {
        public static BigMapFinishPanel Create()
        {

            var panel = UIManager.Instance.ShowPanel("BigMapFinishPanel") as BigMapFinishPanel;
            if (panel == null)
            {
                Debug.LogError("BigMapFinishPanel err");
                return null;
            }

            //panel.RefreshData(showName, duration);
            return panel;
        }

        public Button ConfirmBtn;

        public void Awake()
        {
            ConfirmBtn.onClick.AddListener(DoConfirm);
        }

        private void DoConfirm()
        {
            int tpAreaId = 1;
            MainGameManager.Instance.gameLogicManager.PlayerSwitchArea(tpAreaId, true);
        }
    }
}