using Map.Entity;
using Map.Scene;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerScenePresenter : SceneUnitPresenter
{
   
    protected override void Awake()
    {
        base.Awake();
    }

    public PlayerLogicEntity PlayerEntity { get
        {
            return (PlayerLogicEntity)_logic;
        } }




    public override void Tick(float dt)
    {
        base.Tick(dt);

        TryUpdateInRoomStatus(dt);

        TickMoveNoiseEffect(Time.time, Time.deltaTime);
    }


    public override void ApplyState(object state)
    {
        if (state is InteractPointState s)
        {
            transform.position = s.Position;
            if (icon != null) icon.enabled = s.IsEnabled;
            //if (highlightFx != null) highlightFx.SetActive(s.IsEnabled && _logic.IsInAOI);
        }
    }

    public override void Bind(ILogicEntity logic)
    {
        base.Bind(logic);

        
    }

    private float _updateRoomStatusTimer = 0;

    private string? currRoomId = null;
    private string? lastRoomId = null;
    private float lastChangeRoomTime = 0;
    public void TryUpdateInRoomStatus(float dt)
    {
        _updateRoomStatusTimer -= dt;
        if(_updateRoomStatusTimer < 0)
        {
            var collides = Physics2D.OverlapPointAll(transform.position, 1 << LayerMask.NameToLayer("MapRoom"));
            if(collides.Length > 0)
            {
                var infoProvider = collides.First().transform.GetComponentInParent<MapRoomProvider>();
                if(currRoomId != infoProvider.RoomId)
                {
                    lastRoomId = currRoomId;
                    currRoomId = infoProvider.RoomId;
                    OnRoomStatusChange();
                }
            }
            else
            {
                if (currRoomId != null)
                {
                    lastRoomId = currRoomId;
                    currRoomId = null;
                    OnRoomStatusChange();
                }
            }
            _updateRoomStatusTimer = 0.3f;
        }
    }

    public void OnRoomStatusChange()
    {
        MainGameManager.Instance.SceneFadeManager.RefreshCeilFadeEffect(currRoomId);
    }

    protected float lastHasSpeedTs = 0;
    protected float lastEmitNoiseTs = 0;

    /// <summary>
    /// 移动噪音
    /// </summary>
    /// <param name="now"></param>
    /// <param name="dt"></param>
    protected void TickMoveNoiseEffect(float now, float dt)
    {
        // 有速度时记录时间戳
        if (rb.velocity.magnitude > 0.1f)
        {
            lastHasSpeedTs = now;
        }

        if (lastHasSpeedTs > 0.1f)
        {
            float interval = 0.5f;
            float intensity = 0.5f;
            if (now - lastEmitNoiseTs > interval)
            {
                MainGameManager.Instance.ShowNoiseEffect(intensity, transform.position);
                lastEmitNoiseTs = now;
            }
        }
    }
}
