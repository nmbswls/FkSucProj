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
        public OverworldHudRetreatHintIndicator RetreatHintIndicator;
        public OverworldHudExposeSkillIndicator ExposeSkillIndicator;

        public BottomProgressPanel bottomProgressPanel;

        public OverworldSkillPreviewUI overworldSkillPreviewUI;

        // 搬运提示根节点：屏幕中下，默认 Inactive；子物体可放 TMP「搬运中，按 X 放下」
        public GameObject carryBodyHintRoot;
        // 可 SceneCancel 打断的蓄力提示：屏幕底部，默认 Inactive
        public GameObject holdCancelHintRoot;


        public OverworldSkillBar SkilBar;

        public OverworldPlayerBuffBar PlayerBuffBar;

        public TextMeshProUGUI PlayerHpText;
        public Image PleasureBar;

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

            SkilBar.InitSkills(this);
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
                EstrusIndicator = estrusObj.AddComponent<OverworldHudEstrusIndicator>();
                EstrusIndicator.Init();
            }

            var alertObj = transform.Find("AlertProgress");
            if (alertObj != null)
            {
                AlertHintIndicator = alertObj.AddComponent<OverworldHudAlertHintIndicator>();
                AlertHintIndicator.BindView();
            }

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
            _buffEventsPlayer = player;
            PlayerBuffBar?.RefreshFromPlayer();
        }

        void UnsubscribePlayerBuffEvents()
        {
            if (_buffEventsPlayer == null)
            {
                return;
            }

            _buffEventsPlayer.EventOnBuffRegister -= OnPlayerBuffRegister;
            _buffEventsPlayer.EventOnBuffUnregister -= OnPlayerBuffUnregister;
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

            TrySubscribePlayerBuffEvents();

            PlayerHumanItemBarPanel.RefreshFromGame();
        }


        public void DoPendingAlertReduce(long val)
        {
            AlertHintIndicator?.DoPendingAlertReduce(val);
        }

        public void DoSwitchZhaZhiMode()
        {
            MainGameManager.Instance.gameLogicManager.playerLogicEntity.SwitchZhaZHiMode();

            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsZhaZhiMode)
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
