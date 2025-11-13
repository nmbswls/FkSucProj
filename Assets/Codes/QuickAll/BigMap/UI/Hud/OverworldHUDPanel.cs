
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace My.UI
{

    public class OverworldHUDPanel : PanelBase, IInputConsumer, IRefreshable
    {
        public static OverworldHUDPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("OverworldHUD");
                if (panel != null && panel is OverworldHUDPanel hudPanel)
                {
                    return hudPanel;
                }
                return null;
            }
        }

        public BottomProgressPanel bottomProgressPanel;

        public TextMeshProUGUI PlayerHpText;
        public override void Setup(object data = null)
        {
            bottomProgressPanel = GetComponentInChildren<BottomProgressPanel>();
            bottomProgressPanel.gameObject.SetActive(false);
            //BottomProgressPanel.Setup();
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel() => false;
        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(int index)
        {

            if (index == 1)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("fix_clothes");
            }
            else if (index == 2)
            {

            }

            return false;
        }

        public bool OnScroll(float deltaY)
        {
            return false;
        }

        public void Update()
        {
            if(MainGameManager.Instance.playerScenePresenter != null)
            {
                PlayerHpText.text = MainGameManager.Instance.playerScenePresenter.PlayerEntity.GetAttr(AttrIdConsts.HP).ToString();
            }
        }

        #region bottom hud

        public long ShowBottomProgress(string hintText, float targetProgress)
        {
            var showId = ++BottomProgressPanel.ShowInstIdCounter;
            bottomProgressPanel.Setup(showId, hintText, targetProgress);
            return showId;
        }

        public void HideBottomProgress(long showId)
        {
            bottomProgressPanel.HideProgress(showId);
        }

        public void TryCancelProgressComplete(long showId)
        {
            bottomProgressPanel.TryCancelProgressComplete(showId);
        }

        public bool OnSpace()
        {
            return false;
        }

        #endregion
    }

}
