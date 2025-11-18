
using My.Map.Entity;
using TMPro;
using UnityEngine;


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

        public MapHomeBuildPanel homeBuildPanel;
        public OverworldSkillPreviewUI overworldSkillPreviewUI;


        public TextMeshProUGUI PlayerHpText;
        public override void Setup(object data = null)
        {
            bottomProgressPanel.gameObject.SetActive(false);
            //BottomProgressPanel.Setup();
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public enum EHudMode
        { 
            None,
            Normal,
            PreviewSkill,
            Build,
        }

        public EHudMode HudMode = EHudMode.None;
        public Texture2D cursorTexSkill;

        void Awake()
        {
        }

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

            if (WorldAreaManager.Instance.currentWorld.worldName == "home" && UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                if(HudMode == EHudMode.Normal)
                {
                    EnterBuildMode();
                }
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

            this.HudMode = mode;

            if(mode == EHudMode.Normal)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else if (mode == EHudMode.PreviewSkill)
            {
                Vector2 hotspot = new Vector2(cursorTexSkill.width / 2, cursorTexSkill.height / 2); // 或箭头尖端像素
                Cursor.SetCursor(cursorTexSkill, hotspot, CursorMode.Auto);
            }

            overworldSkillPreviewUI.Clear();
            overworldSkillPreviewUI.gameObject.SetActive(false);

            homeBuildPanel.gameObject.SetActive(false);

            if (mode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.gameObject.SetActive(true);
            }
            else if (mode == EHudMode.Build)
            {
                homeBuildPanel.gameObject.SetActive(true);
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


        public bool OnClick(int button, Vector2 mousePos)
        {

            if(HudMode == EHudMode.PreviewSkill)
            {
                Vector3 wp = Camera.main.ScreenToWorldPoint(mousePos);
                wp.z = 0; // 将 z 固定到你的世界平面（例如 0）

                // 左键
                if (button == 0)
                {
                    ConfirmSkillCast(overworldSkillPreviewUI.PreviewAbilityName, wp, Vector2.zero);

                }
                else if(button == 1)
                {
                    CancelSkillCast();
                }
            }

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

        #region 建造

        protected void EnterBuildMode()
        {



            UpdateHudMode(EHudMode.Build);
            
        }

        public void QuitBuildMode()
        {
            if (HudMode != EHudMode.Build)
            {
                return;
            }
            UpdateHudMode(EHudMode.Normal);
        }


        #endregion
    }

}
