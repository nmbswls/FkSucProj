using System.Collections;
using System.Collections.Generic;
using Map.Logic;
using My.Map;
using My.UI;
using UnityEngine;
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

        bool DispatchHotkey(int index);

        bool DispatchSpace();
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

        public enum InputMode { Overworld, Battle, Menu, Dialog }
        private InputMode mode;

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
        }


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

            actions.OverworldMap.Space.performed += OnSpace;

            actions.OverworldMap.Confirm.performed += OnConfirm;
            actions.OverworldMap.Cancel.performed += OnCancel;

            actions.OverworldMap.Scroll.performed += OnMouseScroll;


            actions.OverworldMap.HotKey1.performed += OnHotKey1;
            actions.OverworldMap.HotKey2.performed += OnHotKey2;
        }

        private void OnDisable()
        {
            actions.OverworldMap.Move.performed -= OnMove;
            actions.OverworldMap.Move.canceled -= OnMove;

            actions.OverworldMap.Space.performed -= OnSpace;


            actions.OverworldMap.Confirm.performed += OnConfirm;
            actions.OverworldMap.Cancel.performed += OnCancel;

            actions.OverworldMap.Scroll.performed -= OnMouseScroll;

            actions.OverworldMap.HotKey1.performed -= OnHotKey1;
            actions.OverworldMap.HotKey2.performed -= OnHotKey2;

            actions.OverworldMap.Disable();
            actions.BattleMap.Disable();
            actions.UIMenuMap.Disable();
        }

        public void OnMouseScroll(InputAction.CallbackContext ctx)
        {
            var delta = ctx.ReadValue<Vector2>().y; // 鼠标滚轮
            if (uiRouter == null || !uiRouter.DispatchScroll(delta))
            {
                OnMouseScroll(delta);
            }
        }


        public void OnMove(InputAction.CallbackContext ctx)
        {
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
            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchConfirm())
                {
                }
            }
        }

        public void OnHotKey1(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchHotkey(1))
                {
                }
            }
        }

        public void OnHotKey2(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchHotkey(2))
                {
                }
            }
        }

        public void OnCancel(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchCancel())
                {
                    DoPauseMenu();
                }
            }
        }

        public void DoPlayerMove(Vector2 dir)
        {
            if(MainGameManager.Instance.playerScenePresenter != null)
            {
                MainGameManager.Instance.playerScenePresenter.freeMoveDir = dir;
                MainGameManager.Instance.playerScenePresenter.freeMoveDir = Vector2.ClampMagnitude(dir, 1f);
            }
            
        }



        public void OnMouseScroll(float deltaY)
        {

        }


        public void OnSpace(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                if (uiRouter == null || !uiRouter.DispatchCancel())
                {
                    if (MainGameManager.Instance.playerScenePresenter != null)
                    {
                        Vector2 dir = Vector2.one;
                        if (MainGameManager.Instance.playerScenePresenter.freeMoveDir.magnitude < 0.01f)
                        {
                            dir = MainGameManager.Instance.playerScenePresenter.PlayerEntity.FaceDir;
                        }
                        else
                        {
                            dir = MainGameManager.Instance.playerScenePresenter.freeMoveDir;
                        }

                        MainGameManager.Instance.playerScenePresenter.PlayerEntity.PlayerAbilityController.TryDash(dir);
                    }
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

