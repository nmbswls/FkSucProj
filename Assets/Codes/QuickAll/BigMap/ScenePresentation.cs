using Map.Entity;
using Map.Logic;
using My.Map;
using My.Map.Entity;
using My.UI;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public interface IScenePresentation
{
    long Id { get; }
    void Bind(ILogicEntity logic);
    void Unbind();
    void ApplyState(object state);
    void SetVisible(bool visible);
    void Tick(float dt);

    ILogicEntity GetLogicEntity();

    Vector3 GetWorldPosition();

    bool CheckValid();
}

public abstract class ScenePresentationBase<TLogic> : MonoBehaviour, IScenePresentation
    where TLogic : class, ILogicEntity
{
    public long Id => _logic?.Id ?? 0;
    protected TLogic _logic;

    private bool _visible;


    protected virtual void Awake()
    { 
    }


    public virtual void Bind(ILogicEntity logic)
    {
        _logic = logic as TLogic;

        // 初始状态可能需要主动拉取或由逻辑层在 Bind 后立即推送
        
        transform.localPosition = MainGameManager.Instance.GetWorldPosFromLogicPos(_logic.Pos);
        _logic.EventOnEntityMove += OnEntityMove;

        UIOrchestrator.Instance.OnScenePresentationBinded(this);
    }

    public virtual void Unbind()
    {
        _logic.EventOnEntityMove -= OnEntityMove;

        UIOrchestrator.Instance.OnScenePresentationUbbind(this);

        _logic = null;
    }

    public virtual void ApplyState(object state) { }

    protected virtual void OnLogicStateChanged(object payload) => ApplyState(payload);

    protected virtual void OnLogicAOIExit(object _)
    {
        SetVisible(false);
    }

    public virtual void SetVisible(bool visible)
    {
        _visible = visible;
        gameObject.SetActive(visible);
    }

    public virtual void Tick(float dt) { }

    public void OnEntityMove(Vector2 oldPos, Vector2 newPos)
    {
        transform.position = newPos;
        SceneAOIManager.Instance.MoveEntity(_logic, oldPos, newPos);
    }

    public ILogicEntity GetLogicEntity()
    {
        return _logic;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    protected virtual void LateUpdate()
    {

    }

    public bool CheckValid()
    {
        return _logic != null;
    }


    public void OnLogicUpdate(float logicDeltaTime)
    {
        Tick(logicDeltaTime);
    }
}