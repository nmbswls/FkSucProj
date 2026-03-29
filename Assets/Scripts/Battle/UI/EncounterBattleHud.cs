
using My.Encounter;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class EncounterBattleHud : PanelBase, IInputConsumer
    {
        public Button EndBtn;

        public void Awake()
        {
            EndBtn.onClick.RemoveAllListeners();

            EndBtn.onClick.AddListener(() =>
            {
                EncounterBattleManager.Instance.FinishBattle();
            });
        }



        public bool OnCancel()
        {
            return false;
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            return false;
        }

        public bool OnConfirm()
        {
            return false;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            return false;
        }

        public bool OnHoldUpdate(int holdKey)
        {
            return false;
        }

        public bool OnHoldUpdate(string holdKey)
        {
            return false;
        }

        public bool OnHotkey(string keyName)
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
    }
}