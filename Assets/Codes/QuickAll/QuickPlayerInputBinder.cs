using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerScenePresenter))]
public class QuickPlayerInputBinder : MonoBehaviour
{
    private PlayerScenePresenter playerPresenter;
    private MyInput actions;

    private void Awake()
    {
        playerPresenter = GetComponent<PlayerScenePresenter>();
        actions = new MyInput();
    }

    private void OnEnable()
    {
        actions.Player.Enable();
        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled += OnMove;
        actions.Player.Dash.performed += OnDash;
        //actions.Player.Dash.canceled += controller.OnSprint;
        actions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled -= OnMove;
        actions.Player.Dash.performed -=OnDash;
        //actions.Player.Dash.canceled -= controller.OnSprint;
        actions.Player.Interact.performed -= OnInteract;
        actions.Player.Disable();
    }


    public void OnMove(InputAction.CallbackContext ctx)
    {
        if(MainUIManager.Instance.IsLootingMode)
        {
            return;
        }

        playerPresenter.freeMoveDir = ctx.ReadValue<Vector2>();
        playerPresenter.freeMoveDir = Vector2.ClampMagnitude(playerPresenter.freeMoveDir, 1f);
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (MainUIManager.Instance.IsLootingMode)
        {
            return;
        }

        if (ctx.performed)
        {

            Vector2 dir = Vector2.one;
            if (playerPresenter.freeMoveDir.magnitude < 0.01f)
            {
                dir = playerPresenter.PlayerEntity.FaceDir;
            }
            else
            {
                dir = playerPresenter.freeMoveDir;
            }

            playerPresenter.PlayerEntity.PlayerAbilityController.TryDash(dir);
        }
    }


    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // TODO: ½»»¥¼ì²â£¨ÉäÏß/´¥·¢Æ÷£©
        }
    }
}
