



using Map.Logic.Events;
using My.Map;
using TMPro;
using UnityEngine;
using static My.Map.Entity.MapEntityAbilityController;

namespace My.UI
{
    public class BeFckedWindowPanel : PanelBase, IInputConsumer, IRefreshable
    {
        public TextMeshProUGUI LeftCatchValComp;
        public TextMeshProUGUI SelfKnockDownComp;


        public float CatchVal;
        public long FckingUnitId;

        public static BeFckedWindowPanel ShowFckedWindow(long fckingUnitId, float catchVal)
        {
            var panel = UIManager.Instance.ShowPanel("BeFckedWindow") as BeFckedWindowPanel;
            panel.Initialize(fckingUnitId, catchVal);

            return panel;
        }

        public void Initialize(long fckingUnitId, float catchVal)
        {
            this.FckingUnitId = fckingUnitId;
            this.CatchVal = catchVal;
        }

        public void Update()
        {
            LeftCatchValComp.text = CatchVal.ToString();

            if(FckingUnitId > 0 && CatchVal < 0)
            {
                OnClickkkSuccess();
            }
        }

        public override void Setup(object data = null)
        {

        }

        /// <summary>
        /// ≥…π¶’ıÕ—
        /// </summary>
        public void OnClickkkSuccess()
        {
            var srcEntity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(FckingUnitId, false);
            if (srcEntity != null && srcEntity is BaseUnitLogicEntity unitEntity) 
            {
                unitEntity.TryInterrupt(new InterruptRequest() {
                    source = InterruptSource.System,
                    priority = 999,
                });
            }

            UIManager.Instance.HidePanel("BeFckedWindow");
        }

        public override void Hide()
        {
            FckingUnitId = 0;
        }

        public bool OnCancel()
        {
            return false;
        }

        public bool OnConfirm()
        {
            return false;
        }

        public bool OnHotkey(int index)
        {
            return false;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return false;
        }

        public bool OnScroll(float deltaY)
        {
            return false;
        }

        public void Refresh()
        {
            
        }

        public bool OnSpace()
        {
            CatchVal -= 8;
            return true;
        }
    }
}