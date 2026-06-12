
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using My.Config;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.Hunting;
using My.Map.Scene;
using My.Player;
using My.Quest;
using My.UI.Bag;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using cfg.demo;
using static My.GameLogicManager;

namespace My.UI
{

    public class OverworldHudDayPeriodIndicator : MonoBehaviour
    {
        public TextMeshProUGUI PeriodText;

        public void RefreshView(GameLogicManager glm)
        {

        }
    }

    public class OverworldWantedIndicator : MonoBehaviour
    {
        public TextMeshProUGUI WantedValText;
        public Transform MarkContainer;
        public List<GameObject> WantedMarkList = new();

        public void BindView()
        {
            WantedValText = transform.Find("TxtLevel").GetComponent<TextMeshProUGUI>();

            MarkContainer = transform.Find("Marks");
            WantedMarkList.Clear();
            for (int i = 0; i < MarkContainer.childCount; i++)
            {
                WantedMarkList.Add(MarkContainer.GetChild(i).gameObject);
            }
        }

        public void RefreshView()
        {
            var wantedLevel = MainGameManager.Instance.gameLogicManager.WantedManager.GetWantedStarLevel();
            WantedValText.text = wantedLevel.ToString();

            for(int i=0;i< wantedLevel;i++)
            {
                WantedMarkList[i].gameObject.SetActive(true);
            }

            for(int i = wantedLevel; i< WantedMarkList.Count;i++)
            {
                WantedMarkList[i].gameObject.SetActive(false);
            }
        }
    }

    public class OverworldHudEstrusIndicator : MonoBehaviour
    {
        public ParticleSystem MainPs;

        private int cachedPlayerEstrusLevel = -1;
        public void Init()
        {
            MainPs = transform.Find("MainPs").GetComponent<ParticleSystem>();
        }

        public void CheckEstrusUpdate()
        {
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            int level = (int)(player.GetAttr(AttrIdConsts.PlayerEstrusProgrss) / 1000 / 20);
            if(cachedPlayerEstrusLevel == level)
            {
                return;
            }

            cachedPlayerEstrusLevel = level;

            switch (cachedPlayerEstrusLevel)
            {
                case 0:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 0, 2.0f);
                    }
                    break;
                case 1:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 2, 2.0f);
                    }
                    break;
                case 2:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 4, 2.0f);
                    }
                    break;
                case 3:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 6, 2.0f);
                    }
                    break;
                case 4:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 7, 2.0f);
                    }
                    break;
                case 5:
                    {
                        ModifyParticleStyle(null, 0.7f, 0, 10, 2.0f);
                    }
                    break;
            }
        }


        public void ModifyParticleStyle(Texture2D newTexture, float alpha, float startSpeed, float density, float duration)
        {
            if (MainPs == null)
            {
                Debug.LogError("未绑定 ParticleSystem!");
                return;
            }
            MainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // ================= 1. 修改 Main 模块 (主模块) =================
            var mainModule = MainPs.main;

            // 修改持续时间
            mainModule.duration = duration;

            // 修改发射速度
            mainModule.startSpeed = startSpeed;

            // 修改透明度 (通过修改 Start Color 的 Alpha 通道)
            // 注意：Start Color 可能是一个渐变或随机颜色，这里假设它是单一颜色 (Color)
            Color currentColor = mainModule.startColor.color;
            currentColor.a = Mathf.Clamp01(alpha); // 限制在 0-1 之间
            mainModule.startColor = currentColor;


            // ================= 2. 修改 Emission 模块 (发射模块) =================
            var emissionModule = MainPs.emission;

            // 修改密度 (Rate over Time：每秒发射的粒子数量)
            emissionModule.rateOverTime = density;


            // ================= 3. 修改图案 (Renderer 模块) =================
            if (newTexture != null)
            {
                // 图案通常由粒子系统的 Renderer 组件上的材质(Material)决定
                ParticleSystemRenderer psRenderer = MainPs.GetComponent<ParticleSystemRenderer>();

                if (psRenderer != null && psRenderer.material != null)
                {
                    // 修改材质的主贴图。
                    // 注意：如果是内置渲染管线(Standard)，通常是 "_MainTex"
                    // 如果是 URP/HDRP 管线，通常是 "_BaseMap"
                    psRenderer.material.SetTexture("_MainTex", newTexture);
                }
            }

            // ================= 4. 应用生效 =================
            // 对于 duration（持续时间）等某些属性的修改，需要重启粒子系统才能立即应用完美生效
            
            MainPs.Clear(); // 清除当前屏幕上已有的旧粒子
            MainPs.Play();  // 重新播放
        }
    }


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

        public OverworldHudDayPeriodIndicator PeriodIndicator;
        public OverworldHudEstrusIndicator EstrusIndicator;

        public BottomProgressPanel bottomProgressPanel;

        public MapHomeBuildPanel homeBuildPanel;
        public OverworldSkillPreviewUI overworldSkillPreviewUI;

        // 搬运提示根节点：屏幕中下，默认 Inactive；子物体可放 TMP「搬运中，按 X 放下」
        public GameObject carryBodyHintRoot;

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

        public OverworldPlayerBuffBar PlayerBuffBar;

        public Image PleasureBar;

        public OverworldWantedIndicator WantedIndicator;

        private LogicEntityBase _buffEventsPlayer;

        public Image zhaZhiSwitchOne;
        public Button BtnZhaZhiSwitch;

        public Button BtnHomeStorage;
        public Button BtnHomeNextPeriod;

        public ParticleSystem FaQingPS;

        public Image ExposeSkillPressHint;

        public class PlayerPropBall
        {
            public string AttrId;
            public RectTransform Root;
            public CanvasGroup CG;
            public Image BarValue;
        }

        public RectTransform PropLineContainer;
        private Dictionary<string, PlayerPropBall> PlayerBallMap = new();

        private bool isUIDisguiseMode = false;
        private Tween disguiseSwitchTween = null;

        public override void Setup(object data = null)
        {
            bottomProgressPanel.gameObject.SetActive(false);
            //BottomProgressPanel.Setup();

            SkilBar.InitSkills(this);
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public void SetCarryBodyHintVisible(bool visible)
        {
            if (carryBodyHintRoot != null)
            {
                carryBodyHintRoot.SetActive(visible);
            }
        }

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

            BtnHomeStorage.onClick.AddListener(() =>
            {
                UIOrchestrator.Instance.ToggleWarehousePanel();
            });

            InitializePropBalls();
            PlayerEventBus.Subscribe<PlayerTempSkillChangedEvent>(OnTempSkillChanged);

            var dayPeriodObj = transform.Find("DayPeriodIndicator");
            if (dayPeriodObj != null)
            {
                PeriodIndicator = dayPeriodObj.AddComponent<OverworldHudDayPeriodIndicator>();
                PeriodIndicator.PeriodText = PeriodIndicator.transform.Find("PeriodText").GetComponent<TextMeshProUGUI>();
            }

            var wantedObj = transform.Find("WantedView");
            if (wantedObj != null)
            {
                WantedIndicator = wantedObj.AddComponent<OverworldWantedIndicator>();
                WantedIndicator.BindView();
            }
            
            var estrusObj = transform.Find("EstrusIndicator");
            if (estrusObj != null)
            {
                EstrusIndicator = estrusObj.AddComponent<OverworldHudEstrusIndicator>();
                EstrusIndicator.Init();
            }

            if (PlayerBuffBar == null)
            {
                var buffBarTr = transform.Find("PlayerBuffBar");
                if (buffBarTr != null)
                {
                    PlayerBuffBar = buffBarTr.GetComponent<OverworldPlayerBuffBar>();
                }
            }

        }

        private void InitializePropBalls()
        {
            var hungerGo = PropLineContainer.Find("PlayerHunger");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerHunger;
                ball.Root = hungerGo as RectTransform;
                ball.CG = hungerGo.GetComponent<CanvasGroup>();
                ball.BarValue = hungerGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerHunger, ball);
                ball.Root.gameObject.SetActive(false);
            }
            var sanGo = PropLineContainer.Find("PlayerSan");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerSanity;
                ball.Root = sanGo as RectTransform;
                ball.CG = sanGo.GetComponent<CanvasGroup>();
                ball.BarValue = sanGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerSanity, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var clothesGo = PropLineContainer.Find("PlayerClothes");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerClothes;
                ball.Root = clothesGo as RectTransform;
                ball.CG = clothesGo.GetComponent<CanvasGroup>();
                ball.BarValue = clothesGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerClothes, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var cexposeGo = PropLineContainer.Find("PlayerExpose");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerOriginPower;
                ball.Root = cexposeGo as RectTransform;
                ball.CG = cexposeGo.GetComponent<CanvasGroup>();
                ball.BarValue = cexposeGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerOriginPower, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var jingyuGo = PropLineContainer.Find("PlayerJingYu");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerJingYu;
                ball.Root = jingyuGo as RectTransform;
                ball.CG = jingyuGo.GetComponent<CanvasGroup>();
                ball.BarValue = jingyuGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerJingYu, ball);

                ball.Root.gameObject.SetActive(false);
            }
            
        }

        public void Update()
        {

            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity != null)
            {
                PlayerHpText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.HP) * 0.001f)).ToString();
                //PlayerClothesText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) * 0.001f)).ToString();
                //PlayerPleasureText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerPleasure) * 0.001f)).ToString();

                PleasureBar.fillAmount = MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerPleasure) * 0.001f / 100;

                //PlayerHungerText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerHunger) * 0.001f)).ToString();
                //PlayerSanText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerSan) * 0.001f)).ToString();

                //PlayerQueenStatusText.text = MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsQueenMode ? "Queen" : "Normal";

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

                var jingyuVal = MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerJingYu);
                int layer = (int)(jingyuVal / 1000);
                if (layer > 0)
                {
                    PlayerBallMap[AttrIdConsts.PlayerJingYu].Root.gameObject.SetActive(true);
                }
                else
                {
                    PlayerBallMap[AttrIdConsts.PlayerJingYu].Root.gameObject.SetActive(false);
                }
            }

            if(WantedIndicator != null)
            {
                if (MainGameManager.Instance.gameLogicManager.GameSession.IsInfiltrationRun && MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapOverlayCfg.IsCivilArea)
                {
                    WantedIndicator.gameObject.SetActive(true);
                    WantedIndicator.RefreshView();
                }
                else
                {
                    WantedIndicator.gameObject.SetActive(false);
                }
            }
            

            if (HudMode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.TickPreviewState();
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

            UpdateEstrusStateHint();

            EstrusIndicator?.CheckEstrusUpdate();

            
        }

        /// <summary>
        /// 检查
        /// </summary>
        private void CheckDisguiseState()
        {
            if (disguiseSwitchTween != null) return;
            bool disguising = false;
            var lgm = MainGameManager.Instance.gameLogicManager;

            // 只有真身形态 才有伪装概念
            if (!lgm.PlayerHumanMode)
            {
                if (lgm.AreaManager.cacheMapOverlayCfg != null
                    && lgm.AreaManager.cacheMapOverlayCfg.IsCivilArea
                    && !lgm.playerLogicEntity.IsExposed)
                {
                    disguising = true;
                }
            }
            
            
            if(isUIDisguiseMode == disguising)
            {
                return;
            }

            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(false);
            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(false);

            if (!MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Clothes))
            {
                return;
            }
            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(true);
            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(true);


            if (disguising)
            {
                PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(true);
                PlayerBallMap[AttrIdConsts.PlayerClothes].CG.alpha = 0;

                disguiseSwitchTween = DOTween.Sequence()
                        .Append(PlayerBallMap[AttrIdConsts.PlayerClothes].CG.DOFade(1, 0.3f))
                        .Append(PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(false);
                            isUIDisguiseMode = disguising;

                        }).SetLink(gameObject);

            }
            else
            {
                PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(true);
                PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.alpha = 0;

                disguiseSwitchTween = DOTween.Sequence()
                        .Append(PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.DOFade(1, 0.3f))
                        .Append(PlayerBallMap[AttrIdConsts.PlayerClothes].CG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(false);
                            isUIDisguiseMode = disguising;

                        }).SetLink(gameObject);

            }
        }

        private void ShowBallAppearEffect(string attrId)
        {
            if(PlayerBallMap.TryGetValue(attrId, out var ball))
            {
                ball.CG.alpha = 0;
                ball.CG.DOFade(1.0f, 1.0f);
            }
        }


        public override void Show()
        {
            base.Show();

            UpdateHudMode(EHudMode.Normal);

            PlayerEventBus.Subscribe<PlayerFuncUnlockEvent>(HandleOnPlayerFuncOpen);

            MainGameManager.Instance.gameLogicManager.EventOnSwitchStageUpdate += HandleSwitchStageUpdate;

            RefreshUILayout();
        }

        public override void Hide()
        {
            HuntingModeManager.Instance?.Exit();

            base.Hide();

            UnsubscribePlayerBuffEvents();
            PlayerBuffBar?.RefreshFromPlayer();

            PlayerEventBus.Unsubscribe<PlayerFuncUnlockEvent>(HandleOnPlayerFuncOpen);

            MainGameManager.Instance.gameLogicManager.EventOnSwitchStageUpdate -= HandleSwitchStageUpdate;
        }
        private void HandleSwitchStageUpdate(EMapSwitchStep step)
        {
            if(step >= EMapSwitchStep.Loaded)
            {
                RefreshUILayout();
            }
        }


        private void OnPlayerBuffRegister(BuffInstance _)
        {
            PlayerBuffBar?.RefreshFromPlayer();
        }

        private void OnPlayerBuffUnregister(long _)
        {
            PlayerBuffBar?.RefreshFromPlayer();
        }

        private void TrySubscribePlayerBuffEvents()
        {
            UnsubscribePlayerBuffEvents();
            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                PlayerBuffBar?.RefreshFromPlayer();
                return;
            }

            player.EventOnBuffRegister += OnPlayerBuffRegister;
            player.EventOnBuffUnregister += OnPlayerBuffUnregister;
            _buffEventsPlayer = player;
            PlayerBuffBar?.RefreshFromPlayer();
        }

        private void UnsubscribePlayerBuffEvents()
        {
            if (_buffEventsPlayer == null)
            {
                return;
            }

            _buffEventsPlayer.EventOnBuffRegister -= OnPlayerBuffRegister;
            _buffEventsPlayer.EventOnBuffUnregister -= OnPlayerBuffUnregister;
            _buffEventsPlayer = null;
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

            if (homeBuildPanel != null)
            {
                homeBuildPanel.gameObject.SetActive(false);
            }

            if (mode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.gameObject.SetActive(true);
            }
            else if (mode == EHudMode.Build)
            {
                mode = EHudMode.Normal;
            }

            RefreshUILayout();
        }

        private void RefreshUILayout()
        {

            if (MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Hunger))
            {
                PlayerBallMap[AttrIdConsts.PlayerHunger].Root.gameObject.SetActive(true);
            }
            else
            {
                PlayerBallMap[AttrIdConsts.PlayerHunger].Root.gameObject.SetActive(false);
            }

            if (MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Desire))
            {
                PlayerBallMap[AttrIdConsts.PlayerSanity].Root.gameObject.SetActive(true);
            }
            else
            {
                PlayerBallMap[AttrIdConsts.PlayerSanity].Root.gameObject.SetActive(false);
            }

            CheckDisguiseState();

            /*
            if (MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapOverlayCfg.IsHome)
            {
                BtnHomeStorage.gameObject.SetActive(true);
                BtnHomeNextPeriod.gameObject.SetActive(true);
            }
            else
            {
                BtnHomeStorage.gameObject.SetActive(false);
                BtnHomeNextPeriod.gameObject.SetActive(false);
            }
            */
            BtnHomeStorage.gameObject.SetActive(false);
            BtnHomeNextPeriod.gameObject.SetActive(false);

            TrySubscribePlayerBuffEvents();

            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel()
        {

            return false;
        }


        static bool IsWeaponHotkey(string keyName)
        {
            return keyName == EInputKey.Num1.ToString() || keyName == EInputKey.Num2.ToString();
        }

        static int KeyNameToWeaponSlotIndex(string keyName)
        {
            if (keyName == EInputKey.Num1.ToString())
            {
                return 0;
            }

            if (keyName == EInputKey.Num2.ToString())
            {
                return 1;
            }

            return -1;
        }

        bool TryHandleHumanQuickBarHotkey(string keyName)
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return false;
            }

            var qb = lgm.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return false;
            }

            if (keyName == EInputKey.UseQuickItem.ToString())
            {
                OnClickUseConsumable();
                return true;
            }

            if (IsWeaponHotkey(keyName))
            {
                int wIdx = KeyNameToWeaponSlotIndex(keyName);
                if (wIdx >= 0)
                {
                    qb.SelectWeaponSlot(wIdx);
                    PlayerHumanItemBarPanel.RefreshFromGame();
                }

                return true;
            }

            return false;
        }

        public string GetSkillIdByKey(string keyName)
        {
            string skillId = string.Empty;
            var showSkills = MainGameManager.Instance.gameLogicManager.playerDataManager.GetSkillSlotsByState();

            bool isSkillSlot = false;
            int skillSLotIdx = -1;

            if (keyName == EInputKey.MouseLeft.ToString())
            {
                var pdm = MainGameManager.Instance.gameLogicManager.playerDataManager;
                var leftClick = pdm?.HumanQuickBar?.ResolveLeftClickSkillId();
                if (!string.IsNullOrEmpty(leftClick))
                {
                    return leftClick;
                }

                skillSLotIdx = 0;
                isSkillSlot = true;
            }
            else if(keyName == EInputKey.MouseRight.ToString())
            {
                skillSLotIdx = 1;
                isSkillSlot = true;
            }
            if (keyName == EInputKey.Space.ToString())
            {
                skillSLotIdx = 2;
                isSkillSlot = true;
            }
            if (keyName == EInputKey.Num1.ToString())
            {
                skillSLotIdx = 3;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num2.ToString())
            {
                skillSLotIdx = 4;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num3.ToString())
            {
                skillSLotIdx = 5;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num4.ToString())
            {
                skillSLotIdx = 6;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.Num5.ToString())
            {
                skillSLotIdx = 7;
                isSkillSlot = true;
            }
            else if (keyName == EInputKey.EnterExpose.ToString())
            {
                return "player_enter_expose";
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

            if (TryHandleHumanQuickBarHotkey(keyName))
            {
                return true;
            }

            if (keyName == EInputKey.EnterExpose.ToString())
            {
                var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
                if (player == null
                    || player.LogicManager.PlayerHumanMode
                    || player.IsExposed
                    || !player.DisguiseIfPossible)
                {
                    return false;
                }
            }

            var skillId = GetSkillIdByKey(keyName);
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            OnClickUseSkill(skillId);
            return true;
        }


        public void OnClickUseSkill(string skillId, Action<bool> onConfirm = null, bool isExtend = false)
        {
            var skillConf = SkillLibrary.GetSkillConfig(skillId);
            if (skillConf == null)
            {
                NotifySkillUseConfirmed(skillId, false, onConfirm);
                return;
            }

            var castOverrides = ResolveHumanWeaponCastOverrides(skillId);
            if(skillConf.IsCombo)
            {
                bool ok = TryCastPlayerSkill(skillId, null, null, null, castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }

            var mainAbilityCfg = AbilityLibrary.GetAbilityConfig(skillConf.MainAbilityId);
            if(mainAbilityCfg == null)
            {
                Debug.LogError($"skill not found main ability:{skillConf.MainAbilityId}");
                NotifySkillUseConfirmed(skillId, false, onConfirm);
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
                bool ok = TryCastPlayerSkill(skillId, dir, null, null, castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }
            else if(mainAbilityCfg.CastType == MapAbilitySpecConfig.ECastType.ToFace)
            {
                var player = MainGameManager.Instance.playerScenePresenter.PlayerEntity;
                bool ok = TryCastPlayerSkill(
                    skillId,
                    dir,
                    player.Pos + player.CurrentLook * 1.0f,
                    null,
                    castOverrides);
                NotifySkillUseConfirmed(skillId, ok, onConfirm);
                return;
            }

            EnterSkillPreviewMode(skillId, (ret) => NotifySkillUseConfirmed(skillId, ret, onConfirm));
        }

        static bool TryCastPlayerSkill(
            string skillId,
            Vector2? inputVec,
            Vector2? castVec,
            My.Map.ILogicEntity target,
            Dictionary<string, string> castOverrides)
        {
            var player = MainGameManager.Instance?.playerScenePresenter?.PlayerEntity;
            var skillSystem = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            if (player?.ablilityManager == null)
            {
                return false;
            }

            if (skillSystem != null && skillSystem.IsTempSkill(skillId))
            {
                return player.ablilityManager.TryUseSkillFromConfig(skillId, inputVec, castVec, target, castOverrides);
            }

            return player.ablilityManager.UseSkill(skillId, inputVec, castVec, target, castOverrides);
        }

        void OnTempSkillChanged(PlayerTempSkillChangedEvent _)
        {
            PlayerHumanItemBarPanel.RefreshFromGame();
            SkilBar?.Refresh();
        }

        void OnDestroy()
        {
            PlayerEventBus.Unsubscribe<PlayerTempSkillChangedEvent>(OnTempSkillChanged);
        }

        void NotifySkillUseConfirmed(string skillId, bool success, Action<bool> onConfirm)
        {
            if (success)
            {
                TryConsumeTempSkill(skillId);
            }

            onConfirm?.Invoke(success);
        }

        static void TryConsumeTempSkill(string skillId)
        {
            var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (pdm == null || !pdm.ConsumeTempSkillIfMatch(skillId))
            {
                return;
            }

            PlayerHumanItemBarPanel.RefreshFromGame();
            Instance?.SkilBar?.Refresh();
        }

        static Dictionary<string, string> ResolveHumanWeaponCastOverrides(string skillId)
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return null;
            }

            var qb = lgm.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return null;
            }

            var activeWeaponSkill = qb.GetActiveWeaponSkillId();
            if (string.IsNullOrEmpty(activeWeaponSkill) || activeWeaponSkill != skillId)
            {
                return null;
            }

            return qb.BuildCastParamsForActiveWeapon();
        }

        public void OnClickUseConsumable()
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable())
            {
                return;
            }

            var binding = lgm.playerDataManager.HumanQuickBar.GetActiveConsumableBinding();
            if (binding.IsEmpty)
            {
                return;
            }

            UseQuickBarBinding(binding);
        }

        void UseQuickBarBinding(My.Player.QuickSlotBinding binding)
        {
            var pdm = MainGameManager.Instance.gameLogicManager.playerDataManager;
            var inv = pdm.InventorySystem;
            if (inv == null || !inv.CheckQuickSlotBindingAvailable(binding))
            {
                return;
            }

            var itemId = binding.ItemId;
            var itemUseCfg = ItemCatalog.GetPrimaryUse(itemId);
            if (itemUseCfg == null || !itemUseCfg.Usable)
            {
                return;
            }

            if (itemUseCfg.UseType == cfg.demo.EItemUseType.UseSkill)
            {
                var skillId = itemUseCfg.S1;
                bool usedEnchant = false;
                if (pdm.ItemEnchant != null && pdm.ItemEnchant.TryGetRemapSkill(itemId, out var enchantSkill))
                {
                    skillId = enchantSkill;
                    usedEnchant = true;
                }

                OnClickUseSkill(skillId, (ret) =>
                {
                    if (ret && usedEnchant)
                    {
                        pdm.ItemEnchant?.ConsumeEnchant(itemId);
                        PlayerHumanItemBarPanel.RefreshFromGame();
                    }

                    if (ret && itemUseCfg.CostOnUse)
                    {
                        inv.CostQuickSlotBinding(binding, 1);
                        pdm.HumanQuickBar?.PruneInvalidSlots();
                    }
                });
            }
            else
            {
                TryUseConsumableFromInventoryBag(binding);
            }

            pdm.HumanQuickBar?.PruneInvalidSlots();
            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        static void TryUseConsumableFromInventoryBag(My.Player.QuickSlotBinding binding)
        {
            var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
            int bagId = (int)EPlayerBagId.Default;

            if (inv.TryFindCarriedStack(binding, out var flatIndex, out _))
            {
                PlayerBagUIPanel.Instance?.UseItem(bagId, flatIndex);
            }
        }

        public bool OnNavigate(Vector2 dir) => false;

        public bool CapturesNavigateAxisForWorld => false;
        public bool OnHotkey(string keyName)
        {
            if(HudMode == EHudMode.Normal)
            {
                return PeeviewUseSkillByKey(keyName);
            }
            
            return false;
        }

        public bool OnScroll(float deltaY)
        {
            if (HudMode != EHudMode.Normal)
            {
                return false;
            }

            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null || !lgm.IsHumanQuickBarAvailable() || Mathf.Abs(deltaY) < 0.01f)
            {
                return false;
            }

            lgm.playerDataManager.HumanQuickBar.CycleConsumableSelection(deltaY > 0f ? 1 : -1);
            return true;
        }

        public bool OnHoldStart(string holdKey)
        {
            //if (HudMode == EHudMode.Normal)
            //{
            //    var skillId = GetSkillIdByKey(holdKey);
            //    if (!string.IsNullOrEmpty(skillId))
            //    {
            //        MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHold(skillId);
            //    }

            //}
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
            if (HudMode == EHudMode.Normal)
            {
                var skillId = GetSkillIdByKey(holdKey);
                if (!string.IsNullOrEmpty(skillId))
                {
                    MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHoldEnd(skillId);
                }

            }
            return false;
        }



        public bool OnClick(int button, Vector2 mousePos)
        {
            if(HudMode == EHudMode.Normal)
            {
                if(button == 0)
                {
                    PeeviewUseSkillByKey(EInputKey.MouseLeft.ToString());
                }
                else if(button == 1)
                {
                    PeeviewUseSkillByKey(EInputKey.MouseRight.ToString());
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

        public void QuitBuildMode()
        {
            if (HudMode == EHudMode.Build)
            {
                homeBuildPanel?.CancelBuildMode();
                UpdateHudMode(EHudMode.Normal);
            }
        }

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

        #region 监听玩家事件


        /// <summary>
        /// 事件监听
        /// </summary>
        /// <param name="e"></param>
        private void HandleOnPlayerFuncOpen(PlayerFuncUnlockEvent e)
        {
            if(e.OpenType == EFuncOpenType.Hunger)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerHunger);
            }
            else if (e.OpenType == EFuncOpenType.Desire)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerSanity);
            }
            else if(e.OpenType == EFuncOpenType.Clothes)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerClothes);
            }
        }


        #endregion


        #region 发情效果

        private void UpdateEstrusStateHint()
        {
            // 
        }

        #endregion
    }


}
