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

    void SetFadeAlpha(float fadeAlpha);
}

public interface ISceneTargettable
{
    Collider2D GetTargetCol();
}

public abstract class ScenePresentationBase<TLogic> : MonoBehaviour, IScenePresentation
    where TLogic : class, ILogicEntity
{
    public long Id => _logic?.Id ?? 0;
    protected TLogic _logic;

    public Transform MainViewRt;
    protected SpriteRenderer[] _mainSpriteArr;

    private bool _visible;


    protected virtual void Awake()
    { 
        if(MainViewRt != null)
        {
            _mainSpriteArr = MainViewRt.GetComponentsInChildren<SpriteRenderer>();
        }
    }


    public void Update()
    {
        if (_logic == null) return;

        Tick(LogicTime.deltaTime);
    }

    public virtual void Bind(ILogicEntity logic)
    {
        _logic = logic as TLogic;

        // 初始状态可能需要主动拉取或由逻辑层在 Bind 后立即推送
        
        transform.localPosition = MainGameManager.Instance.GetWorldPosFromLogicPos(_logic.Pos);
        _logic.EventOnEntityMove += OnEntityMove;

        // vieweventbus
        SceneSmallIconLayerPanel.Instance?.OnScenePresentationBinded(this);
    }

    public virtual void Unbind()
    {
        _logic.EventOnEntityMove -= OnEntityMove;

        SceneSmallIconLayerPanel.Instance?.OnScenePresentationUbbind(this);

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

    public virtual void Tick(float dt) 
    {
        RefreshFadeState();
    }

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

    public virtual Collider2D GetMainCol()
    {
        return null;
    }

    protected float _targetFadeAlpha = 1;
    protected float _currFadeAlpha = 1;
    public void SetFadeAlpha(float alpha)
    {
        _targetFadeAlpha = alpha;
    }

    protected virtual void RefreshFadeState()
    {
        _currFadeAlpha = Mathf.Lerp(_currFadeAlpha, _targetFadeAlpha, 2 * LogicTime.deltaTime);
    }
}