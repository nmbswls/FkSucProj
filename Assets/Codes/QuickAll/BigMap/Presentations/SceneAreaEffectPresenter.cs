using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SceneAreaEffectPresenter : ScenePresentationBase<AreaEffectLogicEntity>, ISceneInteractable
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private GameObject highlightFx;

    public event Action<bool> EventOnInteractStateChanged;

    public AreaEffectLogicEntity AreaEffectEntity { get { return (AreaEffectLogicEntity)_logic; } }


    public override void Tick(float dt)
    {
        base.Tick(dt);

        TryTriggerActivate(dt);
    }

    private float _checkTriggerTimer;
    private Collider2D[] hits = new Collider2D[16];

    public void TryTriggerActivate(float dt)
    {
        if(!AreaEffectEntity.cacheCfg.HastriggerArea)
        {
            return;
        }

        if(_checkTriggerTimer > 0)
        {
            _checkTriggerTimer -= dt;
        }

        if(_checkTriggerTimer > 0)
        {
            return;
        }

        ILogicEntity activator = null;

        switch(AreaEffectEntity.cacheCfg.Shape)
        {
            case Config.Map.MapAreaEffectConfig.EShape.Square:
                {
                    int count = Physics2D.OverlapBoxNonAlloc(transform.position, new Vector2(AreaEffectEntity.cacheCfg.Radius, AreaEffectEntity.cacheCfg.Radius), 0, hits, 1 << LayerMask.NameToLayer("Units"));
                    // 遍历命中，筛选实现了接口的对象
                    for (int i = 0; i < count; i++)
                    {
                        var col = hits[i];
                        if (col == null) continue;

                        var scenePresenter = col.GetComponentInParent<IScenePresentation>();
                        if (scenePresenter == null) continue;

                        // chek
                        activator = scenePresenter.GetLogicEntity();
                    }
                }
                break;
            case Config.Map.MapAreaEffectConfig.EShape.Circle:
                {
                    int count = Physics2D.OverlapCircleNonAlloc(transform.position, AreaEffectEntity.cacheCfg.Radius, hits, 1 << LayerMask.NameToLayer("Units"));

                    // 遍历命中，筛选实现了接口的对象
                    for (int i = 0; i < count; i++)
                    {
                        var col = hits[i];
                        if (col == null) continue;

                        var scenePresenter = col.GetComponentInParent<IScenePresentation>();
                        if (scenePresenter == null) continue;

                        // chek
                        activator = scenePresenter.GetLogicEntity();
                    }
                }
                break;
        }

        if(activator != null)
        {
            AreaEffectEntity.OnTriggerAreaTriggered(activator);
        }
    }




    public Vector3 GetHintAnchorPosition()
    {
        return GetWorldPosition();
    }

    public bool CanInteractEnable()
    {
        return false;
    }

    public void SetInteractExpandStatus(bool expanded)
    {
        throw new System.NotImplementedException();
    }

    public void TriggerInteract(string interactSelection)
    {
        throw new System.NotImplementedException();
    }

    public List<string> GetInteractSelections()
    {
        return new() { "Int" };
    }
}
