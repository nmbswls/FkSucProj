
using System.Drawing;
using Config.Unit;
using My.Input;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Config.Unit.EntitySkillCfg;


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
        public TextMeshProUGUI PlayerClothesText;
        public TextMeshProUGUI PlayerPleasureText;
        public TextMeshProUGUI PlayerHungerText;
        public TextMeshProUGUI PlayerSanText;

        public TextMeshProUGUI PlayerQueenStatusText;

        public RectTransform AlertHint;
        public TextMeshProUGUI AlertValText;

        public RectTransform RetreatHint;
        public Image RetreatHintBar;
        public TextMeshProUGUI RetreatHintText;


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

            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity != null)
            {
                PlayerHpText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.HP) * 0.001f)).ToString();
                PlayerClothesText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) * 0.001f)).ToString();
                PlayerPleasureText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerPleasure) * 0.001f)).ToString();

                PlayerHungerText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerHunger) * 0.001f)).ToString();
                PlayerSanText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerSan) * 0.001f)).ToString();

                PlayerQueenStatusText.text = MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsQueenMode ? "Queen" : "Normal";

                if(MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsRetreating)
                {
                    float pastTime = LogicTime.time - MainGameManager.Instance.gameLogicManager.playerLogicEntity.RetreatingStartTime;
                    float maxTime = PlayerLogicEntity.RetreatDuration;

                    float rate = pastTime / maxTime;
                    rate = Mathf.Clamp(rate, 0f, 1f);

                    RetreatHintBar.fillAmount = rate;
                    RetreatHintText.text = ((int)(rate * 100) * 0.01f).ToString();

                    RetreatHint.gameObject.SetActive(true);
                }
                else
                {
                    RetreatHint.gameObject.SetActive(false);
                }
            }

            if(HudMode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.TickPreviewState();
            }

            if (WorldAreaManager.Instance.cacheAreaInfo.IsHome && UnityEngine.Input.GetKeyDown(KeyCode.B))
            {
                if(HudMode == EHudMode.Normal)
                {
                    EnterBuildMode();
                }
            }

            AlertValText.text = MainGameManager.Instance.gameLogicManager.AreaManager.AreaAlertValue.ToString();

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
                homeBuildPanel.InitShow();
            }
        }


        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel()
        {

            if(HudMode == EHudMode.Build)
            {
                QuitBuildMode();
                return true;
            }

            return false;
        }

        public string GetSkillIdByKey(string keyName)
        {
            string skillId = string.Empty;
            if(MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsQueenMode)
            {
                if (keyName == QuickPlayerInputBinder.EInputKey.Num1.ToString())
                {
                    skillId = "crazy_fire";
                }
                else if (keyName == QuickPlayerInputBinder.EInputKey.Num2.ToString())
                {
                    skillId = "spawn_attract";
                }
                else if (keyName == QuickPlayerInputBinder.EInputKey.Num3.ToString())
                {
                    skillId = "queen_counter";
                }
                else if (keyName == QuickPlayerInputBinder.EInputKey.Num4.ToString())
                {
                    skillId = "queen_pull_all";
                }
                else if(keyName == QuickPlayerInputBinder.EInputKey.MouseLeft.ToString())
                {
                    skillId = "queen_attack";
                }
            }
            else
            {
                if (keyName == QuickPlayerInputBinder.EInputKey.MouseLeft.ToString())
                {
                    skillId = "default_push";
                }
                else if (keyName == QuickPlayerInputBinder.EInputKey.MouseRight.ToString())
                {
                    skillId = "player_normal_defend";
                }
                else if (keyName == QuickPlayerInputBinder.EInputKey.Q.ToString())
                {
                    skillId = "player_enter_queen";
                }
            }

            return skillId;
        }


        private bool TryRouteUseSkill(string keyName)
        {
            var skillId = GetSkillIdByKey(keyName);

            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            var skillConf = SkillLibrary.GetSkillConfig(skillId);
            if (skillConf.TargetType == ETargetType.Self)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill(skillId, target: MainGameManager.Instance.gameLogicManager.playerLogicEntity);
            }
            else if (skillConf.TargetType != ETargetType.NoTarget)
            {
                EnterSkillPreviewMode(skillId);
            }
            else
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill(skillId);
            }

            return true;
        }

        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(string keyName)
        {
            if(HudMode == EHudMode.Normal)
            {
                return TryRouteUseSkill(keyName);
            }
            
            return false;
        }

        public bool OnScroll(float deltaY)
        {
            return false;
        }


        public bool OnHoldUpdate(string holdKey)
        {
            if(HudMode == EHudMode.Normal)
            {
                var skillId = GetSkillIdByKey(holdKey);
                if(!string.IsNullOrEmpty(skillId))
                {
                    MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHold(skillId);
                }
            }

            return false;
        }


        public bool OnHoldingEnd(string holdKey)
        {
            return false;
        }



        public bool OnClick(int button, Vector2 mousePos)
        {
            if(HudMode == EHudMode.Normal)
            {
                if(button == 0)
                {
                    TryRouteUseSkill(QuickPlayerInputBinder.EInputKey.MouseLeft.ToString());
                }
                else if(button == 1)
                {
                    TryRouteUseSkill(QuickPlayerInputBinder.EInputKey.MouseRight.ToString());
                }
            }
            else if (HudMode == EHudMode.PreviewSkill)
            {
                Vector3 wp = Camera.main.ScreenToWorldPoint(mousePos);
                wp.z = 0; // 将 z 固定到你的世界平面（例如 0）

                // 左键
                if (button == 0)
                {
                    ConfirmSkillCast(overworldSkillPreviewUI.PreviewSkillName, wp, Vector2.zero);

                }
                else if (button == 1)
                {
                    CancelSkillCast();
                }
            }
            else if (HudMode == EHudMode.Build)
            {
                if(button == 1)
                {
                    QuitBuildMode();
                    return true;
                }
                else if(button == 0)
                {
                    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mousePos);
                    homeBuildPanel.TryConfirmPlace(mouseWorld);
                    QuitBuildMode();
                }
            }

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


        #endregion

        #region 技能预览

        protected void EnterSkillPreviewMode(string skillId)
        {
            UpdateHudMode(EHudMode.PreviewSkill);
            overworldSkillPreviewUI.Initialize(skillId);
        }


        public void ConfirmSkillCast(string skillName, Vector2 point1, Vector2 point2)
        {
            if (HudMode != EHudMode.PreviewSkill)
            {
                return;
            }

            var skillConfig = SkillLibrary.GetSkillConfig(skillName);
            switch (skillConfig.TargetType)
            {
                case EntitySkillCfg.ETargetType.Point:
                case EntitySkillCfg.ETargetType.Circle:
                    {
                        // 施法距离
                        var dist = skillConfig.Range1;
                        var playerPos = MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos;
                        if (dist < (point1 - playerPos).magnitude)
                        {
                            point1 = playerPos + (point1 - playerPos).normalized * dist;
                        }
                    }
                    break;

            }

            var truncatedPoint = 

            MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility(skillName, castDir: point1);
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
            homeBuildPanel.CancelBuildMode();
            UpdateHudMode(EHudMode.Normal);
        }


        #endregion

        public GameObject simpleFloatTextPrefab;
        public void DoPendingAlertReduce(long val)
        {
            // 1. 生成预制体
            GameObject go = Instantiate(simpleFloatTextPrefab, AlertHint.transform.position, Quaternion.identity, AlertHint.transform);
            go.SetActive(true);

            // 2. 获取脚本并初始化
            HudSimpleFloatingText popup = go.GetComponent<HudSimpleFloatingText>();
            popup.Setup("-"+val, AlertHint.transform.position, UnityEngine.Color.black);
        }
    }

}
