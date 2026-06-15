using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.Hunting;
using My.Map.Scene;
using My.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using cfg.demo;
using static My.GameLogicManager;
using Unity.VisualScripting;

namespace My.UI
{
    public partial class OverworldHUDPanel : PanelBase, IInputConsumer, IRefreshable
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
        public OverworldHudAlertHintIndicator AlertHintIndicator;
        public OverworldHudControlDegreeIndicator ControlDegreeIndicator;
        public OverworldHudRetreatHintIndicator RetreatHintIndicator;
        public OverworldHudExposeSkillIndicator ExposeSkillIndicator;

        public BottomProgressPanel bottomProgressPanel;

        public OverworldSkillPreviewUI overworldSkillPreviewUI;

        // 搬运提示根节点：屏幕中下，默认 Inactive；子物体可放 TMP「搬运中，按 X 放下」
        public GameObject carryBodyHintRoot;
        // 可 SceneCancel 打断的蓄力提示：屏幕底部，默认 Inactive
        public GameObject holdCancelHintRoot;


        public OverworldMainBottomBar MainBottomBar;

        public OverworldPlayerBuffBar PlayerBuffBar;

        public Image HeadProfile;

        public TextMeshProUGUI PlayerHpText;
        // 血量进度条（PlayerCoreCircle/HPBar/bar），需在 Inspector 重新绑定
        public Image HpBar;
        // 高潮进度条（PlayerCoreCircle/PleasureBar/bar），需在 Inspector 重新绑定
        public Image PleasureBar;
        // 发情值进度条（PlayerCoreCircle/HeadProfile/DesireBar），需在 Inspector 重新绑定
        public Image DesireBar;

        // HUD 内的位置锚点，供 PlayerHumanItemBarPanel 折叠/展开定位使用
        public RectTransform ItemAnchor;
        public RectTransform ItemAnchor2;

        public OverworldWantedIndicator WantedIndicator;

        LogicEntityBase _buffEventsPlayer;

        public Image zhaZhiSwitchOne;

        public Button BtnZhaZhiSwitch;

        public enum EHudMode
        {
            None,
            Normal,
            PreviewSkill,
            Build,
        }

        public EHudMode HudMode = EHudMode.None;
        public Texture2D cursorTexSkill;

        public override void Setup(object data = null)
        {
            bottomProgressPanel.gameObject.SetActive(false);

            MainBottomBar.InitBar(this);
        }

        public void Refresh() { }

        public void SetCarryBodyHintVisible(bool visible)
        {
            if (carryBodyHintRoot != null)
            {
                carryBodyHintRoot.SetActive(visible);
            }
        }

        public void RefreshHoldCancelHint()
        {
            if (holdCancelHintRoot == null)
            {
                return;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            bool show = player?.ablilityManager != null
                && player.ablilityManager.TryGetActiveHoldViewState(out var holdState)
                && holdState.IsActive
                && holdState.CancelableBySceneCancel;

            holdCancelHintRoot.SetActive(show);
        }

        void Awake()
        {
            BtnZhaZhiSwitch.onClick.AddListener(() =>
            {
                DoSwitchZhaZhiMode();
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
                // 将爱心粒子移到 HeadProfile 附近，使效果浮现在头像圆区域上
                var headProfileTr = transform.Find("PlayerCoreCircle/HeadProfile");
                if (headProfileTr != null)
                {
                    estrusObj.SetParent(headProfileTr, false);
                    estrusObj.localPosition = new Vector3(0, 15, 0);
                }
                EstrusIndicator = estrusObj.AddComponent<OverworldHudEstrusIndicator>();
                EstrusIndicator.Init();
            }

            ItemAnchor = transform.Find("ItemAnchor") as RectTransform;
            ItemAnchor2 = transform.Find("ItemAnchor2") as RectTransform;

            var alertObj = transform.Find("AlertProgress");
            if (alertObj != null)
            {
                AlertHintIndicator = alertObj.AddComponent<OverworldHudAlertHintIndicator>();
                AlertHintIndicator.BindView();
            }

            ControlDegreeIndicator?.BindView();

            var retreatObj = transform.Find("RetreatHint");
            if (retreatObj != null)
            {
                RetreatHintIndicator = retreatObj.AddComponent<OverworldHudRetreatHintIndicator>();
                RetreatHintIndicator.BindView();
            }

            var exposeSkillObj = transform.Find("ExposeSkill");
            if (exposeSkillObj != null)
            {
                ExposeSkillIndicator = exposeSkillObj.AddComponent<OverworldHudExposeSkillIndicator>();
                ExposeSkillIndicator.BindView();
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

        void HandleSwitchStageUpdate(EMapSwitchStep step)
        {
            if (step >= EMapSwitchStep.Loaded)
            {
                RefreshUILayout();
            }
        }

        void OnPlayerBuffRegister(BuffInstance _)
        {
            PlayerBuffBar?.RefreshFromPlayer();
        }

        void OnPlayerBuffUnregister(long _)
        {
            PlayerBuffBar?.RefreshFromPlayer();
        }

        void OnPlayerFaQingStateChange()
        {
            RefreshMainBottomBarForPlayerState();
        }

        void OnPlayerExposeStateChange(bool isBroken)
        {
            RefreshMainBottomBarForPlayerState();
        }

        void RefreshMainBottomBarForPlayerState()
        {
            MainBottomBar?.Refresh(true, forceLayoutRebuild: true);
        }

        void TrySubscribePlayerBuffEvents()
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
            player.EventOnFaQingStateChange += OnPlayerFaQingStateChange;
            player.EventOnExposeStateChange += OnPlayerExposeStateChange;
            _buffEventsPlayer = player;
            PlayerBuffBar?.RefreshFromPlayer();
        }

        void UnsubscribePlayerBuffEvents()
        {
            if (_buffEventsPlayer is not PlayerLogicEntity player)
            {
                return;
            }

            player.EventOnBuffRegister -= OnPlayerBuffRegister;
            player.EventOnBuffUnregister -= OnPlayerBuffUnregister;
            player.EventOnFaQingStateChange -= OnPlayerFaQingStateChange;
            player.EventOnExposeStateChange -= OnPlayerExposeStateChange;
            _buffEventsPlayer = null;
        }

        public void UpdateHudMode(EHudMode mode)
        {
            if (HudMode == mode)
            {
                return;
            }

            HudMode = mode;

            if (mode == EHudMode.Normal)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else if (mode == EHudMode.PreviewSkill)
            {
                Vector2 hotspot = new Vector2(cursorTexSkill.width / 2, cursorTexSkill.height / 2);
                Cursor.SetCursor(cursorTexSkill, hotspot, CursorMode.Auto);
            }

            overworldSkillPreviewUI.Clear();
            overworldSkillPreviewUI.gameObject.SetActive(false);

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

        void RefreshUILayout()
        {
            bool hungerOpen = MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Hunger);
            if (PlayerBallMap.TryGetValue(AttrIdConsts.PlayerHunger, out var hungerBall) && hungerBall.Root != null)
            {
                hungerBall.Root.gameObject.SetActive(hungerOpen);
            }

            //bool desireOpen = MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Desire);
            //if (PlayerBallMap.TryGetValue(AttrIdConsts.PlayerSanity, out var sanityBall) && sanityBall.Root != null)
            //{
            //    sanityBall.Root.gameObject.SetActive(desireOpen);
            //}

            CheckDisguiseState();

            TrySubscribePlayerBuffEvents();

            PlayerHumanItemBarPanel.RefreshFromGame();
        }


        public void DoPendingAlertReduce(long val)
        {
            AlertHintIndicator?.DoPendingAlertReduce(val);
        }

        public void DoSwitchZhaZhiMode()
        {
            bool isOn = MainGameManager.Instance.gameLogicManager.playerLogicEntity.SwitchZhaZHiMode();

            if (isOn)
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

        void OnDestroy()
        {
            PlayerEventBus.Unsubscribe<PlayerTempSkillChangedEvent>(OnTempSkillChanged);
        }
    }
}
