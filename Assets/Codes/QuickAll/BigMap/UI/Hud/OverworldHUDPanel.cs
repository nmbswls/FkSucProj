
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

        #region 技能释放预览

        public OverworldSkillPreviewUI overworldSkillPreviewUI;


        #endregion

        public BottomProgressPanel bottomProgressPanel;

        public TextMeshProUGUI PlayerHpText;
        public override void Setup(object data = null)
        {
            bottomProgressPanel = GetComponentInChildren<BottomProgressPanel>();
            bottomProgressPanel.gameObject.SetActive(false);
            //BottomProgressPanel.Setup();
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public enum EHudMode
        { 
            Normal,
            PreviewSkill,
        }

        public EHudMode HudMode;
        public Texture2D cursorTexSkill;


        public void Update()
        {

            if (MainGameManager.Instance.playerScenePresenter != null)
            {
                PlayerHpText.text = MainGameManager.Instance.playerScenePresenter.PlayerEntity.GetAttr(AttrIdConsts.HP).ToString();
            }

            if(HudMode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.TickPreviewState();
            }
        }

        public override void Show()
        {
            base.Show();

            UpdateHudMode(EHudMode.Normal);
        }

        /// <summary>
        /// 更新hud模式
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateHudMode(EHudMode mode)
        {
            if(HudMode == mode)
            {
                return;
            }

            if(mode == EHudMode.Normal)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else if (mode == EHudMode.PreviewSkill)
            {
                Vector2 hotspot = new Vector2(cursorTexSkill.width / 2, cursorTexSkill.height / 2); // 或箭头尖端像素
                Cursor.SetCursor(cursorTexSkill, hotspot, CursorMode.Auto);
            }

            overworldSkillPreviewUI.gameObject.SetActive(false);

            if(mode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.gameObject.SetActive(true);
            }
        }


        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel() => false;
        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(int index)
        {
            string abilityName = string.Empty;
            
            if (index == 1)
            {
                abilityName = "fix_clothes";
            }
            else if (index == 2)
            {
                abilityName = "spawn_attract";
            }

            if(string.IsNullOrEmpty(abilityName))
            {
                return false;
            }

            var abConf = AbilityLibrary.GetAbilityConfig(abilityName);
            if(abConf.TargetType != MapAbilitySpecConfig.ETargetType.NoTarget)
            {
                EnterSkillPreviewMode(abilityName);
            }
            else
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility(abilityName);
            }


            return false;
        }

        public bool OnScroll(float deltaY)
        {
            return false;
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

        #region 技能预览

        protected void EnterSkillPreviewMode(string abName)
        {
            UpdateHudMode(EHudMode.PreviewSkill);
            overworldSkillPreviewUI.Initialize(abName);
        }


        public void ConfirmSkillCast(string abName, Vector2 point1, Vector2 point2)
        {
            if (HudMode != EHudMode.PreviewSkill)
            {
                return;
            }

            MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility(abName, castDir: point1);
            UpdateHudMode(EHudMode.Normal);
        }

        public void CancelSkillCast()
        {
            if(HudMode != EHudMode.PreviewSkill)
            {
                return;
            }
            UpdateHudMode(EHudMode.Normal);
        }

        #endregion
    }

}
