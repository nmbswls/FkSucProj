using System.Collections;
using System.Collections.Generic;
using Map.Logic;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using My.MiniGame;
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
        bool DispatchScroll(float deltaY);

        bool DispatchHotkey(string keyName);

        bool DispatchClick(int button, Vector2 mousePos);

        bool DispatchHoldingStart(string holdingKey);
        bool DispatchHoldingUpdate(string holdingKey);
        bool DispatchHoldingEnd(string holdingKey);
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

        public enum EInputKey
        {
            MouseLeft,
            MouseRight,

            Tab,
            Space,

            Q,
            E,
            R,

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

            Ctrl,
        }

        //public static string MouseRight = "MouseRight";
        //public static string Tab = "Tab";


        //private Dictionary<string, bool> keyHoldingStatus = new();

        public bool GlobalLock { get; set; }

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
            // 1. 普通按键检测
            if (UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                UIOrchestrator.Instance.EnsurePlayerBag();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                WorldMapRuntime.TryToggle();
            }

            if (GlobalLock) return;

            // 2. 持续输入（Hold）的每帧 Update，直接问询 Input System
            // 前提：actions.OverworldMap.RightClickHold 没有被 Disable
            if (actions.OverworldMap.enabled)
            {
                if (actions.OverworldMap.RightClickHold.IsPressed())
                {
                    OnKeyHoldingUpdate(keyMouseRight);
                }

                if (actions.OverworldMap.TabHold.IsPressed())
                {
                    OnKeyHoldingUpdate(keyTab);
                }
            }
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
                if (actions.OverworldMap.TabHold.IsPressed())
                {
                    OnKeyHoldEnd(keyTab);
                }
            }
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 强制告诉业务层停止一切长按行为
                OnKeyHoldEnd(keyMouseRight);
                OnKeyHoldEnd(keyTab);

                // 也可以顺便把移动方向清零
                DoPlayerMove(Vector2.zero);
            }
        }

        //private void Update()
        //{
        //    if(UnityEngine.Input.GetKeyDown(KeyCode.I))
        //    {
        //        UIOrchestrator.Instance.EnsurePlayerBag();
        //    }

        //    foreach(var kv in keyHoldingStatus)
        //    {
        //        if(kv.Value)
        //        {
        //            OnKeyHoldingUpdate(kv.Key);
        //        }
        //    }
        //}


        // 底层执行输入模式切换（由组织层调用）
        public void ApplyInputMode(InputMode mode)
        {
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
        }

        private void OnEnable()
        {
            ApplyInputMode(InputMode.Overworld);

            actions.OverworldMap.Move.performed += OnMove;
            actions.OverworldMap.Move.canceled += OnMove;

            actions.OverworldMap.Space.performed += OnHotKeySpace;

            actions.OverworldMap.Confirm.performed += OnConfirm;
            actions.OverworldMap.Cancel.performed += OnCancel;

            actions.OverworldMap.Scroll.performed += OnMouseScroll;

            actions.OverworldMap.Ctrl.performed += OnHotKeyCtrl;

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
            actions.OverworldMap.Tab.canceled += OnHotKeyTabEnd;
            
            //actions.OverworldMap.TabHold.started += OnTabHoldStart;
            //actions.OverworldMap.TabHold.canceled += OnTabHoldEnd;

            actions.OverworldMap.Q.performed += OnHotKeyQ;
            actions.OverworldMap.E.performed += OnHotKeyE;
            actions.OverworldMap.R.performed += OnHotKeyR;

            actions.OverworldMap.PointerPos.performed += OnPointerMove;
        }

        private void OnDisable()
        {
            actions.OverworldMap.Move.performed -= OnMove;
            actions.OverworldMap.Move.canceled -= OnMove;

            actions.OverworldMap.Space.performed -= OnHotKeySpace;


            actions.OverworldMap.Confirm.performed -= OnConfirm;
            actions.OverworldMap.Cancel.performed -= OnCancel;

            actions.OverworldMap.Scroll.performed -= OnMouseScroll;

            actions.OverworldMap.Ctrl.performed -= OnHotKeyCtrl;

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
            actions.OverworldMap.Tab.canceled -= OnHotKeyTabEnd;
            //actions.OverworldMap.TabHold.started -= OnTabHoldStart;
            //actions.OverworldMap.TabHold.canceled -= OnTabHoldEnd;

            actions.OverworldMap.Q.performed -= OnHotKeyQ;
            actions.OverworldMap.E.performed -= OnHotKeyE;
            actions.OverworldMap.R.performed -= OnHotKeyR;

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

            //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            //    return;


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

        public void OnMove(InputAction.CallbackContext ctx)
        {
            if(GlobalLock)
            {
                return;
            }

            var dir = ctx.ReadValue<Vector2>();
            if (uiRouter == null || !uiRouter.DispatchNavigate(dir))
            {
                // 未消费：可用于切换武器槽、翻页等
                // sceneRouter?.OnNavigateInWorld(dir); // 如有需要
                DoPlayerMove(dir);
            }
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

        public void OnHotKeyQ(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Q.ToString());
        public void OnHotKeyE(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.E.ToString());
        public void OnHotKeyR(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.R.ToString());

        public void OnHotKeySpace(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Space.ToString());
        public void OnHotKeyTab(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Tab.ToString());
        public void OnHotKeyTabEnd(InputAction.CallbackContext ctx) => OnKeyHoldEnd(EInputKey.Tab.ToString());

        //public void OnTabHoldStart(InputAction.CallbackContext ctx) => OnKeyHoldStart(EInputKey.Tab.ToString());
        //public void OnTabHoldEnd(InputAction.CallbackContext ctx) => OnKeyHoldEnd(EInputKey.Tab.ToString());

        public void OnHotKeyCtrl(InputAction.CallbackContext ctx) => OnKeyPress(ctx, EInputKey.Ctrl.ToString());

        //public void OnMouseRightHoldStart(InputAction.CallbackContext ctx) => OnKeyHoldStart(EInputKey.MouseRight.ToString());
        public void OnMouseRightHoldEnd(InputAction.CallbackContext ctx) => OnKeyHoldEnd(EInputKey.MouseRight.ToString());

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
        }


        public void DoPlayerMove(Vector2 dir)
        {
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }


            bool doMove = false;
            do
            {
                

                if (MainGameManager.Instance.gameLogicManager.IsBalancing)
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
                if (DeepAbsorbPanel.Instance != null)
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

            if(doMove)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.FreeMoveInput = Vector2.ClampMagnitude(dir, 1f);
            }
            else
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.FreeMoveInput = Vector2.zero;
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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;


            if (LogicTime.paused)
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

            if(keyName == EInputKey.MouseRight.ToString())
            {
                if (!LogicTime.paused)
                {
                    var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                    //player.abilityController.CheckSkillCanceled("queen_shoot", castDir.normalized + player.PlayerEntity.Pos);
                    player.ablilityManager.CheckSkillCanceled("player_normal_defend");
                }
            }
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

