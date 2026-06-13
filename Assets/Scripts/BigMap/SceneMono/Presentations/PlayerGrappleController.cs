using My;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using static My.Map.BaseUnitLogicEntity;

// 钩爪独立控制器：挂在 PlayerScenePresenter 上，管理钩爪发射后玩家自身贴近的全流程
[RequireComponent(typeof(PlayerScenePresenter))]
public class PlayerGrappleController : MonoBehaviour
{
    [SerializeField] float stopOffset = 0.5f;
    // originSpeed = dist * pullSpeedMul；与 AbilityEffectExecutor4ControlledMove 保持一致
    [SerializeField] float pullSpeedMul = 8f;
    [SerializeField] float minEndSpeed = 0.1f;

    PlayerScenePresenter _presenter;

    GrappleHookLineCtrl _activeHook;

    enum EState { Idle, Firing, Stuck, Retracting }
    EState _state = EState.Idle;

    void Awake()
    {
        _presenter = GetComponent<PlayerScenePresenter>();
    }

    void OnEnable()
    {
        MapProjectile.GrappleHookFired += OnGrappleHookFired;
    }

    void OnDisable()
    {
        MapProjectile.GrappleHookFired -= OnGrappleHookFired;
        Cleanup();
    }

    void OnGrappleHookFired(MapProjectile proj, long casterId)
    {
        if (_presenter == null || casterId != _presenter.Id) return;

        Cleanup();

        _activeHook = proj.GetComponent<GrappleHookLineCtrl>();
        if (_activeHook == null) return;

        _activeHook.HookHit += OnHookHit;
        _state = EState.Firing;
    }

    void OnHookHit(Vector2 hitWorldPos)
    {
        if (_state != EState.Firing) return;

        _state = EState.Stuck;

        var playerEntity = _presenter?.PlayerEntity;
        if (playerEntity == null)
        {
            StartRetract();
            return;
        }

        var toHook = hitWorldPos - playerEntity.Pos;
        float dist = toHook.magnitude;
        float targetDist = dist - stopOffset;

        if (targetDist > 0.05f && toHook.sqrMagnitude > 1e-6f)
        {
            // 计算目标点并发起 Pull 类型受控移动
            var targetPos = playerEntity.Pos + toHook.normalized * targetDist;
            var diff = targetPos - playerEntity.Pos;
            playerEntity.ApplyControlledMove(
                ControlledMoveCtx.EType.Pull,
                diff.normalized,
                originSpeed: diff.magnitude * pullSpeedMul,
                minEndSpeed: minEndSpeed
            );
            playerEntity.controlledMoveCtx.onMoveEnd = _ => StartRetract();
        }
        else
        {
            // 已经足够近，直接收回
            StartRetract();
        }
    }

    void StartRetract()
    {
        _state = EState.Retracting;
        _activeHook?.BeginRetract();
    }

    void LateUpdate()
    {
        // 收回动画播完后清理
        if (_state != EState.Idle && _activeHook != null && _activeHook.IsDone)
        {
            Cleanup();
        }
    }

    void Cleanup()
    {
        if (_activeHook != null)
        {
            _activeHook.HookHit -= OnHookHit;
            _activeHook = null;
        }
        _state = EState.Idle;
    }
}
