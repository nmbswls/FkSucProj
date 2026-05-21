using System.Collections;
using System.Collections.Generic;
using Map.Logic;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using My.MiniGame;
using My.MiniGame.Dream;
using My.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugManager;

namespace My.Input
{
    public interface IUiRouter
    {
        // 返回是否已消费该输入（true 表示不再转发到场景）
        bool DispatchConfirm();
        bool DispatchCancel();
        bool DispatchNavigate(Vector2 dir);

        /// 是否存在占用导航轴的 UI（原始查询，与 OnNavigate 返回值无关）
        bool IsNavigateAxisCapturedByUi() => false;

        bool DispatchScroll(float deltaY);

        bool DispatchHotkey(string keyName);

        bool DispatchClick(int button, Vector2 mousePos);

        bool DispatchHoldingStart(string holdingKey);
        bool DispatchHoldingUpdate(string holdingKey);
        bool DispatchHoldingEnd(string holdingKey);
    }

    public enum EInputKey
    {
        MouseLeft,
        MouseRight,

        Tab,
        Space,

        Skill_01,
        Skill_02,
        Skill_03,
        Skill_04,

        Num1,
        Num2,
        Num3,
        Num4,
        Num5,

        Num6,
        Num7,
        Num8,
        Num9,
        Num10,

        HView,
        Crouch,

        Bag,
        Skill,
        Map,

        UseQuickItem,
    }


    //public interface ISceneRouter
    //{
    //    void OnMove(Vector2 dir);
    //    void OnDash();
    //    void OnInteract();
    //    void OnHotkey(int index);

    //    void OnMouseScroll(float deltaY);
    //}

    public class QuickPlayerInputBinder : MonoBehaviour
    {
        private MyInput actions;

        public IUiRouter uiRouter;       

        [SerializeField] private string overworldMapName = "OverworldMap";
        [SerializeField] private string battleMapName = "BattleMap";
        [SerializeField] private string uiMenuMapName = "UIMenuMap";

        public enum InputMode 
        { 
            None,
            Overworld, 
            Battle, 
            Menu, 
            Dialog 
        }

        private InputMode mode;

        public Vector2 LastPos;

        

        //public static string MouseRight = "MouseRight";
        //public static string Tab = "Tab";


        //private Dictionary<string, bool> keyHoldingStatus = new();

        public bool GlobalLock { get; set; }

        private const float OverworldNavigateDirEpsSq = 0.0001f;
        private Vector2? _overworldNavLastDispatchedDir;
        private bool _overworldNavStickyBlockWorld;

        private void ResetOverworldNavigateRoutingState()
        {
            _overworldNavLastDispatchedDir = null;
            _overworldNavStickyBlockWorld = false;
        }

        private bool OverworldNavigateSampleShouldDispatch(Vector2 dir)
        {
            if (_overworldNavLastDispatchedDir == null)
                return dir.sqrMagnitude > OverworldNavigateDirEpsSq;
            return (dir - _overworldNavLastDispatchedDir.Value).sqrMagnitude > OverworldNavigateDirEpsSq;
        }

        private void Awake()
        {
            actions = new MyInput();

            // 获取路由器实例：可以注入、查找或赋值
            //uiRouter = FindObjectOfType<UIManagerFacade>(); // 示例：一个实现 IUiRouter 的组件/服务
            //sceneRouter = FindObjectOfType<PlayerInputAdapter>(); // 示例：把玩家操作转为场景行为
            ApplyInputMode(InputMode.Menu);
        }

        private void Start()
        {
            uiRouter = UIManager.Instance;

            //keyHoldingStatus[EInputKey.MouseRight.ToString()] = false;
            //keyHoldingStatus[EInputKey.Tab.ToString()] = false;
        }

        private readonly string keyMouseRight = EInputKey.MouseRight.ToString();
        private readonly string keyTab = EInputKey.Tab.ToString();
        private void Update()
        {
            

            if (GlobalLock) return;

            // 2. 持续输入（Hold）的每帧 Update，直接问询 Input System
            // 前提：actions.OverworldMap.RightClickHold / Tab 没有被 Disable
            if (actions.OverworldMap.enabled)
            {
                if (actions.OverworldMap.RightClickHold.IsPressed())
                {
                    OnKeyHoldingUpdate(keyMouseRight);
                }

                if (actions.OverworldMap.Tab.IsPressed())
                {
                    OnKeyHoldingUpdate(keyTab);
                }
            }

            // 移动：每帧读当前向量（键按住不重复 performed 时也能对齐；传送锁等由 DoPlayerMove 内统一门控）
            PollOverworldMoveSample();
        }

        private void ApplyOverworldNavigateAndMove(Vector2 dir)
        {
            if (GlobalLock)
                return;
            if (!actions.OverworldMap.enabled)
                return;
            if (MainGameManager.Instance == null)
                return;

            if (MainGameManager.Instance.playerScenePresenter != null)
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.FreeMoveInput = Vector2.zero;

            if (dir.sqrMagnitude <= OverworldNavigateDirEpsSq)
                _overworldNavStickyBlockWorld = false;

            bool axisCaptured = uiRouter != null && uiRouter.IsNavigateAxisCapturedByUi();
            if (uiRouter != null && OverworldNavigateSampleShouldDispatch(dir))
            {
                bool consumed = uiRouter.DispatchNavigate(dir);
                _overworldNavLastDispatchedDir = dir;
                if (!axisCaptured)
                    _overworldNavStickyBlockWorld = consumed;
            }

            bool allowWorldMove = (uiRouter == null || !axisCaptured) && !_overworldNavStickyBlockWorld;
            if (allowWorldMove)
                DoPlayerMove(dir);
        }

        private void PollOverworldMoveSample()
        {
            if (!actions.OverworldMap.enabled)
                return;
            ApplyOverworldNavigateAndMove(actions.OverworldMap.Move.ReadValue<Vector2>());
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            ApplyOverworldNavigateAndMove(ctx.ReadValue<Vector2>());
        }

        private void ForceReleaseActiveHolds()
        {
            // 如果 OverworldMap 正在激活，检查哪些键还按着
            if (actions.OverworldMap.enabled)
            {
                if (actions.OverworldMap.RightClickHold.IsPressed())
                {
                    OnKeyHoldEnd(keyMouseRight);
                }
                if (actions.OverworldMap.Tab.IsPressed())
                {
                    OnKeyHoldEnd(keyTab);
                }
            }

            ReleaseHuntingModeIfNeeded();
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 强制告诉业务层停止一切长按行为
                OnKeyHoldEnd(keyMouseRight);
                OnKeyHoldEnd(keyTab);
                ReleaseHuntingModeIfNeeded();

                // 也可以顺便把移动方向清零
                DoPlayerMove(Vector2.zero);
                ResetOverworldNavigateRoutingState();
            }
        }


        // 底层执行输入模式切换（由组织层调用）
        public void ApplyInputMode(InputMode mode)
        {
            this.mode = mode;
            // 基本策略：只启用当前模式的 Map；或按需并存
            switch (mode)
            {
                case InputMode.Menu:
                    actions.UIMenuMap.Enable();
                    actions.BattleMap.Disable();   // UI 模式下屏蔽玩家行动
                    actions.OverworldMap.Disable();
                    break;
                case InputMode.Overworld:
                    actions.OverworldMap.Enable();
                    actions.BattleMap.Disable(); 
                    actions.UIMenuMap.Disable();
                    break;
                case InputMode.Battle:
                    actions.BattleMap.Enable();
                    actions.OverworldMap.Disable();
                    actions.UIMenuMap.Disable();
                    break;
            }

            ResetOverworldNavigateRoutingState();
        }

        private void OnEnable()
        {
            ApplyInputMode(InputMode.Overworld);

            actions.OverworldMap.Move.started += OnMove;
            actions.OverworldMap.Move.performed += OnMove;
            actions.OverworldMap.Move.canceled += OnMove;

            actions.OverworldMap.Space.performed += OnHotKeySpace;

            actions.OverworldMap.Confirm.performed += OnConfirm;
            actions.OverworldMap.Cancel.performed += OnCancel;

            actions.OverworldMap.Scroll.performed += OnMouseScroll;

            actions.OverworldMap.HView.started += OnHotKeyHViewStarted;
            actions.OverworldMap.HView.canceled += OnHotKeyHViewCanceled;

            actions.OverworldMap.Crouch.performed += OnHotKeyCrouch;
            

            actions.OverworldMap.HotKey1.performed += OnHotKey1;
            actions.OverworldMap.HotKey2.performed += OnHotKey2;
            actions.OverworldMap.HotKey3.performed += OnHotKey3;
            actions.OverworldMap.HotKey4.performed += OnHotKey4;

            actions.OverworldMap.Click.performed += OnLeftDown;
            //actions.OverworldMap.RightClick.performed += OnRightDown;
            actions.OverworldMap.RightClick.started += OnRightDown;
            actions.OverworldMap.RightClick.canceled += OnMouseRightHoldEnd;

            //actions.OverworldMap.RightClickHold.started += OnMouseRightHoldStart;
            //actions.OverworldMap.RightClickHold.canceled += OnMouseRightHoldEnd;

            actions.OverworldMap.Tab.started += OnHotKeyTab;
            actions.OverworldMap.Tab.performed += OnHotKeyTab;
            actions.OverworldMap.Tab.canceled += OnHotKeyTabEnd;

            actions.OverworldMap.S01.performed += OnHotKeySkill01;
            actions.OverworldMap.S02.performed += OnHotKeySkill02;
            actions.OverworldMap.S03.performed += OnHotKeySkill03;
            actions.OverworldMap.S04.performed += OnHotKeySkill04;

            actions.OverworldMap.Skill.performed += OnHotKeySkill;
            actions.OverworldMap.Bag.performed += OnHotKeyBag;
            actions.OverworldMap.Map.performed += OnHotKeyMap;

            actions.OverworldMap.QuickItem.performed += OnHotKeyQuickItem;

            actions.OverworldMap.PointerPos.performed += OnPointerMove;
        }

        private void OnDisable()
        {
            actions.OverworldMap.Move.started -= OnMove;
            actions.OverworldMap.Move.performed -= OnMove;
            actions.OverworldMap.Move.canceled -= OnMove;

            actions.OverworldMap.Space.performed -= OnHotKeySpace;


            actions.OverworldMap.Confirm.performed -= OnConfirm;
            actions.OverworldMap.Cancel.performed -= OnCancel;

            actions.OverworldMap.Scroll.performed -= OnMouseScroll;

            actions.OverworldMap.HView.started -= OnHotKeyHViewStarted;
            actions.OverworldMap.HView.canceled -= OnHotKeyHViewCanceled;

            actions.OverworldMap.HotKey1.performed -= OnHotKey1;
            actions.OverworldMap.HotKey2.performed -= OnHotKey2;
            actions.OverworldMap.HotKey3.performed -= OnHotKey3;
            actions.OverworldMap.HotKey4.performed -= OnHotKey4;

            actions.OverworldMap.Click.performed -= OnLeftDown;
            actions.OverworldMap.RightClick.performed -= OnRightDown;

            actions.OverworldMap.RightClick.canceled -= OnMouseRightHoldEnd;
            
            //actions.OverworldMap.RightClickHold.started -= OnMouseRightHoldStart;
            // actions.OverworldMap.RightClickHold.canceled -= OnMouseRightHoldEnd;

            actions.OverworldMap.PointerPos.performed -= OnPointerMove;

            actions.OverworldMap.Tab.started -= OnHotKeyTab;
            actions.OverworldMap.Tab.performed -= OnHotKeyTab;
            actions.OverworldMap.Tab.canceled -= OnHotKeyTabEnd;

            actions.OverworldMap.S01.performed -= OnHotKeySkill01;
            actions.OverworldMap.S02.performed -= OnHotKeySkill02;
            actions.OverworldMap.S03.performed -= OnHotKeySkill03;
            actions.OverworldMap.S04.performed -= OnHotKeySkill04;

            actions.OverworldMap.Skill.performed -= OnHotKeySkill;
            actions.OverworldMap.Bag.performed -= OnHotKeyBag;
            actions.OverworldMap.Map.performed -= OnHotKeyMap;

            actions.OverworldMap.QuickItem.performed -= OnHotKeyQuickItem;

            actions.OverworldMap.Disable();
            actions.BattleMap.Disable();
            actions.UIMenuMap.Disable();

            ForceReleaseActiveHolds();
        }

        public void OnMouseScroll(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }

            var delta = ctx.ReadValue<Vector2>().y; // 鼠标滚轮
            if (uiRouter == null || !uiRouter.DispatchScroll(delta))
            {
                OnSceneMouseScroll(delta);
            }
        }

        void OnLeftDown(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null && glm.IsInSecretBase)
            {
                var screen = My.SecretBase.SecretBaseSceneRoot.GetPointerScreenPosition();
                glm.SecretBase.OnScreenPointer(screen, true);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (uiRouter == null || !uiRouter.DispatchClick(0, LastPos))
            {
                OnSceneLeftClick();
            }
        }

        void OnRightDown(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }


            if (uiRouter == null || !uiRouter.DispatchClick(1, LastPos))
            {
                OnSceneRightClick();
            }
        }

        
        public void OnPointerMove(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }

            LastPos = ctx.ReadValue<Vector2>();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            OnScenePointMove();
        }

        public void OnConfirm(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }

            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchConfirm())
                {
                }
            }
        }



        public void OnCancel(InputAction.CallbackContext ctx)
        {
            if (GlobalLock)
            {
                return;
            }


            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchCancel())
                {
                    DoPauseMenu();
                }
            }
        }



        public void OnHotKey1(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Num1.ToString());
        public void OnHotKey2(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Num2.ToString());
        public void OnHotKey3(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Num3.ToString());
        public void OnHotKey4(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Num4.ToString());
        public void OnHotKey5(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Num5.ToString());

        public void OnHotKeySkill01(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Skill_01.ToString());
        public void OnHotKeySkill02(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Skill_02.ToString());
        public void OnHotKeySkill03(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Skill_03.ToString());

        public void OnHotKeySkill04(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Skill_04.ToString());

        public void OnHotKeySpace(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Space.ToString());
        public void OnHotKeyTab(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Tab.ToString());
        public void OnHotKeyTabEnd(InputAction.CallbackContext ctx) => OnKeyHoldEnd(EInputKey.Tab.ToString());

        public void OnHotKeyHViewStarted(InputAction.CallbackContext ctx)
        {
            if (!ctx.started || GlobalLock)
            {
                return;
            }

            My.Map.Hunting.HuntingModeManager.Instance?.Enter();
        }

        public void OnHotKeyHViewCanceled(InputAction.CallbackContext ctx)
        {
            if (!ctx.canceled)
            {
                return;
            }

            var hm = My.Map.Hunting.HuntingModeManager.Instance;
            hm?.Exit();
            hm?.NotifyHViewCanceledAfterExit();
        }

        private void ReleaseHuntingModeIfNeeded()
        {
            var hm = My.Map.Hunting.HuntingModeManager.Instance;
            if (hm == null || !hm.Active)
            {
                return;
            }

            hm.Exit();
            hm.NotifyHViewCanceledAfterExit();
        }

        //public void OnMouseRightHoldStart(InputAction.CallbackContext ctx) => OnKeyHoldStart(EInputKey.MouseRight.ToString());
        public void OnMouseRightHoldEnd(InputAction.CallbackContext ctx) => OnKeyHoldEnd(EInputKey.MouseRight.ToString());

        public void OnHotKeyCrouch(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Crouch.ToString());

        public void OnHotKeyBag(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Bag.ToString());

        public void OnHotKeySkill(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Skill.ToString());

        public void OnHotKeyMap(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Map.ToString());
        public void OnHotKeyQuickItem(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.UseQuickItem.ToString());


        public void OnKeyPress(InputAction.CallbackContext ctx, string keyName)
        {
            if (GlobalLock)
            {
                return;
            }

            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchHotkey(keyName))
                {
                    OnSceneKeyPress(keyName);
                }
            }
        }

        private void OnKeyHoldingUpdate(string holdKey)
        {
            if (GlobalLock)
            {
                return;
            }

            if (uiRouter == null || !uiRouter.DispatchHoldingUpdate(holdKey))
            {
                OnSceneHolding(holdKey);
            }
        }


        //void OnRightHoldStart(InputAction.CallbackContext ctx)
        //{
        //    keyHoldingStatus[EInputKey.MouseRight.ToString()] = true;
        //}

        //void OnRightHoldEnd(InputAction.CallbackContext ctx)
        //{
        //    keyHoldingStatus[EInputKey.MouseRight.ToString()] = false;

        //    if (GlobalLock)
        //    {
        //        return;
        //    }

        //    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //        return;

        //    OnSceneHoldEnd(EInputKey.MouseRight.ToString());
        //}


        void OnKeyHoldStart(string keyName)
        {
            //keyHoldingStatus[keyName] = true;

            if (GlobalLock)
            {
                return;
            }

            if (uiRouter == null || !uiRouter.DispatchHoldingStart(keyName))
            {
            }
        }

        void OnKeyHoldEnd(string keyName)
        {
            if (uiRouter == null || !uiRouter.DispatchHoldingEnd(keyName))
            {
                // 监听scene里的结束
                OnSceneHoldEnd(keyName);
            }
        }


        

        #region scene ops

        private void OnSceneKeyPress(string keyName)
        {
            if(keyName == EInputKey.Space.ToString())
            {
                //if (MainGameManager.Instance.playerScenePresenter != null)
                //{
                //    Vector2 dir = Vector2.one;
                //    if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput.magnitude < 0.01f)
                //    {
                //        dir = MainGameManager.Instance.playerScenePresenter.PlayerEntity.FinalLook;
                //    }
                //    else
                //    {
                //        dir = MainGameManager.Instance.gameLogicManager.playerLogicEntity.FreeMoveInput;
                //    }

                //    MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill("default_dash", inputVec : dir);
                //}
            }
            else if(keyName == EInputKey.Tab.ToString())
            {
                MapPlayerRadialMenu.ShowMenu();
            }
            else if (keyName == EInputKey.Crouch.ToString())
            {
                var p = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
                if (p == null || MainGameManager.Instance.dialoguePlayer.IsPlaying)
                {
                    return;
                }

                if (PlayerNpcCarryService.IsCarrying)
                {
                    return;
                }

                p.SetSpecialCrouchStance(!p.IsSpecialCrouchStance);
            }
            else if(keyName == EInputKey.Bag.ToString())
            {
                UIOrchestrator.Instance.EnsurePlayerBag();
            }
            else if(keyName == EInputKey.Map.ToString())
            {
                WorldMapRuntime.TryToggle();
            }
        }


        public void DoPlayerMove(Vector2 dir)
        {
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }

            if (LogicTime.paused)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.FreeMoveInput = Vector2.zero;
                return;
            }

            bool doMove = false;

            do
            {
                if (MainGameManager.Instance.gameLogicManager.IsBalancing)
                {
                    break;
                }

                if (MainGameManager.Instance.gameLogicManager.IsLocalRoomTeleportLocked)
                {
                    break;
                }

                // 切图：PreparePlayerSwitchArea 已写入 SwitchAreaIntent，但 MainStage 要等下一次 Tick 才变为 SwitchingMap，空窗内仍会 Running
                if (MainGameManager.Instance.gameLogicManager.SwitchAreaIntent != null)
                {
                    break;
                }

                if (MainGameManager.Instance.gameLogicManager.MainStage != GameLogicManager.EMainGameStage.Running)
                {
                    break;
                }

                if (MainGameManager.Instance.playerScenePresenter.ForbidPhase != PlayerScenePresenter.EForbidPhase.Idle)
                {
                    break;
                }


                if (LootPointUIPanel.Instance != null)
                {
                    break;
                }
                if (MiniStaticAbsorbPanel.Instance != null)
                {
                    break;
                }

                if (MainGameManager.Instance.dialoguePlayer.IsPlaying)
                {
                    break;
                }

                doMove = true;
            }
            while (false);

            var playerEntity = MainGameManager.Instance.playerScenePresenter.PlayerEntity;
            if (playerEntity.HasSimulatedMoveInput)
            {
                playerEntity.FreeMoveInput = Vector2.zero;
                return;
            }

            if(doMove)
            {
                playerEntity.FreeMoveInput = Vector2.ClampMagnitude(dir, 1f);
            }
            else
            {
                playerEntity.FreeMoveInput = Vector2.zero;
            }
        }


        public void OnScenePointMove()
        {
            var player = MainGameManager.Instance.playerScenePresenter;
            if (player == null || !player.CheckValid())
            {
                return;
            }

            if (player.ForbidPhase != PlayerScenePresenter.EForbidPhase.Idle)
            {
                return;
            }

            if (!LogicTime.paused)
            //if (!LogicTime.paused && !player.PlayerEntity.CheckHasState(AttrIdConsts.LockFace))
            {
                Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);
                var castDir = (LastPos - playerScreenPos).normalized;

                if ((playerScreenPos - LastPos).magnitude < 1e-1)
                {
                    return;
                }

                player.PlayerEntity.ForceSetFaceTarget(castDir, true);
            }
        }

        // 放入conroller里 不放在binder里

        public void OnSceneLeftClick()
        {
            if (LogicTime.paused)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null && glm.IsInSecretBase)
            {
                var screen = My.SecretBase.SecretBaseSceneRoot.GetPointerScreenPosition();
                glm.SecretBase.OnScreenPointer(screen, true);
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;


            var hunting = My.Map.Hunting.HuntingModeManager.Instance;
            if (hunting != null && hunting.Active && hunting.TryExecuteHoveredTarget())
            {
                return;
            }

            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            //Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);
            //var castDir = (LastPos - playerScreenPos).normalized;

            ////  get skill name from data
            //string skillName;
            //if (player.PlayerEntity.IsQueenMode)
            //{
            //    skillName = "queen_attack";
            //}
            //else
            //{
            //    skillName = "default_push";
            //}

            //player.ablilityManager.UseSkill(skillName, null, target: null);
        }

        public void OnSceneRightClick()
        {
            if (!LogicTime.paused)
            {
                //var player = MainGameManager.Instance.playerScenePresenter;
                //Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);
                //var castDir = (LastPos - playerScreenPos).normalized;

                ////player.PlayerEntity.ablilityManager.UseSkill("player_normal_defend", castDir.normalized + player.PlayerEntity.Pos);
                //player.PlayerEntity.ablilityManager.UseSkill("player_normal_defend", castDir.normalized + player.PlayerEntity.Pos);
            }
        }



        

        #endregion




        public void OnSceneMouseScroll(float deltaY)
        {

        }


        private void OnSceneHolding(string key)
        {
            //if(key == EInputKey.MouseRight.ToString())
            //{
            //    MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.TrySkillHold("player_normal_defend");
            //}
        }

        public void OnSceneHoldEnd(string keyName)
        {
            if (keyName != EInputKey.MouseRight.ToString())
            {
                return;
            }

            if (LogicTime.paused)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || glm.IsInSecretBase)
            {
                return;
            }

            var player = glm.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            player.ablilityManager.CheckSkillCanceled("player_normal_defend");
        }


        public void DoPauseMenu()
        {
            if(!LogicTime.paused)
            {
                LogicTime.RequestPause("Menu");
            }
            else
            {
                LogicTime.ClearAllPauses();
            }
        }

    }
}

