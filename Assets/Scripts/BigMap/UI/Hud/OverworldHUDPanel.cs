
using System;
using DG.Tweening;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
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
        public Image FilledAlertBar;
        public Image TempAlertBar;

        public RectTransform RetreatHint;
        public Image RetreatHintBar;
        public TextMeshProUGUI RetreatHintText;

        public GameObject botHintTextPrefab;
        public OverworldSkillBar SkilBar;
        //public OverworldSkillBar ItemBar;

        public bool IsHunterMode = false;

        public Image zhaZhiSwitchOne;
        public Button BtnZhaZhiSwitch;

        public RectTransform PlayerClothesRoot;
        public CanvasGroup PlayerClothesCG;
        public Image ClothesBar;

        public RectTransform PlayerExposeRoot;
        public CanvasGroup PlayerExposeCG;
        public Image ExposeBar;

        private bool isUIDisguiseMode = false;
        private Tween disguiseSwitchTween = null;

        public override void Setup(object data = null)
        {
            bottomProgressPanel.gameObject.SetActive(false);
            //BottomProgressPanel.Setup();

            SkilBar.InitSkills(this);


            bool disguising = false;

            if (MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapCfg.DefaultDisguise)
            {
                if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) > 0)
                {
                    disguising = true;
                }
            }
            if (disguising)
            {
                PlayerClothesRoot.gameObject.SetActive(true);
                PlayerExposeRoot.gameObject.SetActive(false);
            }
            else
            {
                PlayerClothesRoot.gameObject.SetActive(false);
                PlayerExposeRoot.gameObject.SetActive(true);
            }

            PlayerClothesCG.alpha = 1;
            PlayerExposeCG.alpha = 1;
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
            BtnZhaZhiSwitch.onClick.AddListener(() =>
            {
                DoSwitchZhaZhiMode();
            });
        }

        public void Update()
        {

            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity != null)
            {
                PlayerHpText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.HP) * 0.001f)).ToString();
                //PlayerClothesText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) * 0.001f)).ToString();
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

            var tempVal = MainGameManager.Instance.gameLogicManager.AreaManager.GetTempAlertValue();
            var filledVal = MainGameManager.Instance.gameLogicManager.AreaManager.AreaAlertValue;

            var filledRate = filledVal * 1.0f / MainGameManager.Instance.gameLogicManager.AreaManager.MaxAlertValue;
            filledRate = Mathf.Clamp(filledRate, 0, 1);

            var totalRate = (filledVal + tempVal) * 1.0f / MainGameManager.Instance.gameLogicManager.AreaManager.MaxAlertValue;
            totalRate = Mathf.Clamp(totalRate, 0, 1);

            FilledAlertBar.fillAmount = filledRate;
            TempAlertBar.fillAmount = totalRate;

            CheckDisguiseState();
        }

        /// <summary>
        /// 检查
        /// </summary>
        private void CheckDisguiseState()
        {
            bool disguising = false;

            if (MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapCfg.DefaultDisguise)
            {
                if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) > 0)
                {
                    disguising = true;
                }
            }
            
            if(isUIDisguiseMode == disguising)
            {
                return;
            }

            if(isUIDisguiseMode)
            {
                PlayerClothesRoot.gameObject.SetActive(true);
                PlayerClothesCG.alpha = 0;
                disguiseSwitchTween = DOTween.Sequence()
                        .Append(PlayerClothesCG.DOFade(1, 0.3f))
                        .Append(PlayerExposeCG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerExposeRoot.gameObject.SetActive(false);
                        }).SetLink(gameObject);
            }
            else
            {
                PlayerExposeRoot.gameObject.SetActive(true);
                PlayerExposeCG.alpha = 0;
                disguiseSwitchTween = DOTween.Sequence()
                        .Append(PlayerExposeCG.DOFade(1, 0.3f))
                        .Append(PlayerClothesCG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerClothesRoot.gameObject.SetActive(false);
                        }).SetLink(gameObject);
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
            var playerData = MainGameManager.Instance.gameLogicManager.playerDataManager.NormalSkillSlots;

            var showSkills = MainGameManager.Instance.gameLogicManager.playerDataManager.GetSkillSlotsByState();

            bool isSkillSlot = false;
            int skillSLotIdx = -1;

            if (keyName == QuickPlayerInputBinder.EInputKey.MouseLeft.ToString())
            {
                skillSLotIdx = 0;
                isSkillSlot = true;
            }
            else if(keyName == QuickPlayerInputBinder.EInputKey.MouseRight.ToString())
            {
                skillSLotIdx = 1;
                isSkillSlot = true;
            }
            if (keyName == QuickPlayerInputBinder.EInputKey.Space.ToString())
            {
                skillSLotIdx = 2;
                isSkillSlot = true;
            }
            if (keyName == QuickPlayerInputBinder.EInputKey.Num1.ToString())
            {
                skillSLotIdx = 3;
                isSkillSlot = true;
            }
            else if (keyName == QuickPlayerInputBinder.EInputKey.Num2.ToString())
            {
                skillSLotIdx = 4;
                isSkillSlot = true;
            }
            else if (keyName == QuickPlayerInputBinder.EInputKey.Num3.ToString())
            {
                skillSLotIdx = 5;
                isSkillSlot = true;
            }
            else if (keyName == QuickPlayerInputBinder.EInputKey.Num4.ToString())
            {
                skillSLotIdx = 6;
                isSkillSlot = true;
            }
            else if (keyName == QuickPlayerInputBinder.EInputKey.Num5.ToString())
            {
                skillSLotIdx = 7;
                isSkillSlot = true;
            }
            if (isSkillSlot)
            {
                return showSkills[skillSLotIdx];
            }

            return skillId;
        }


        public bool PeeviewUseSkillByKey(string keyName)
        {
            if(MainGameManager.Instance.gameLogicManager.IsDialogPlayering)
            {
                return false;
            }
            var skillId = GetSkillIdByKey(keyName);

            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            OnClickUseSkill(skillId);

            return true;
        }


        public void OnClickUseSkill(string skillId, Action<bool> onConfirm = null)
        {
            var skillConf = SkillLibrary.GetSkillConfig(skillId);
            if(skillConf.IsCombo)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill(skillId, castVec:null, target:null);
                onConfirm?.Invoke(true);
                return;
            }

            var mainAbilityCfg = AbilityLibrary.GetAbilityConfig(skillConf.MainAbilityId);
            if(mainAbilityCfg == null)
            {
                Debug.LogError($"skill not found main ability:{skillConf.MainAbilityId}");
                onConfirm?.Invoke(false);
                return;
            }

            Vector2 dir = Vector2.one;
            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput.magnitude < 0.01f)
            {
                dir = MainGameManager.Instance.playerScenePresenter.PlayerEntity.FinalLook;
            }
            else
            {
                dir = MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput;
            }


            if (mainAbilityCfg.CastType == MapAbilitySpecConfig.ECastType.NoTarget)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill(skillId, castVec: null, target: null, inputVec: dir);
                onConfirm?.Invoke(true);
                return;
            }
            else if(mainAbilityCfg.CastType == MapAbilitySpecConfig.ECastType.ToFace)
            {
                var player = MainGameManager.Instance.playerScenePresenter.PlayerEntity;
                player.ablilityManager.UseSkill(skillId, castVec: player.Pos + player.CurrentLook * 1.0f, target: null, inputVec: dir);
                onConfirm?.Invoke(true);
                return;
            }

            EnterSkillPreviewMode(skillId);
        }


        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(string keyName)
        {
            if(HudMode == EHudMode.Normal)
            {
                if(keyName == "Ctrl")
                {
                    SwitchHunterMode();
                    return true;
                }
                return PeeviewUseSkillByKey(keyName);
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
                    PeeviewUseSkillByKey(QuickPlayerInputBinder.EInputKey.MouseLeft.ToString());
                }
                else if(button == 1)
                {
                    PeeviewUseSkillByKey(QuickPlayerInputBinder.EInputKey.MouseRight.ToString());
                }
            }
            else if (HudMode == EHudMode.PreviewSkill)
            {
                // 左键
                if (button == 0)
                {
                    overworldSkillPreviewUI.ConfirmSkillCast(mousePos);
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

        protected void EnterSkillPreviewMode(string skillId, Action<bool> onConfirm = null)
        {
            UpdateHudMode(EHudMode.PreviewSkill);
            overworldSkillPreviewUI.Initialize(skillId, onConfirm);
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

        public void ShowBottomHintText(long val)
        {
            //// 1. 生成预制体
            //GameObject go = Instantiate(simpleFloatTextPrefab, AlertHint.transform.position, Quaternion.identity, AlertHint.transform);
            //go.SetActive(true);

            //// 2. 获取脚本并初始化
            //HudSimpleFloatingText popup = go.GetComponent<HudSimpleFloatingText>();
            //popup.Setup("-" + val, AlertHint.transform.position, UnityEngine.Color.black);
        }


        public void SwitchHunterMode()
        {
            
            if(IsHunterMode)
            {
                SceneVolumnManager.Instance.EnterHuntingMode(false);

                //SceneSmallIconLayerPanel.Instance?.Switch();
                IsHunterMode = false;
            }
            else
            {
                SceneVolumnManager.Instance.EnterHuntingMode(true);
                IsHunterMode = true;
            }
        }

        public void DoSwitchZhaZhiMode()
        {
            MainGameManager.Instance.gameLogicManager.playerLogicEntity.SwitchZhaZHiMode();

            if(MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsZhaZhiMode)
            {
                var sprite = SimpleResManager.Load<Sprite>("Sprites/red_tip_01");
                zhaZhiSwitchOne.sprite = sprite;
            }
            else
            {
                var sprite = SimpleResManager.Load<Sprite>("Sprites/red_tip_01_diable");
                zhaZhiSwitchOne.sprite = sprite;
            }
        }
    }

}
