using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SceneDestroyObjPresenter : ScenePresentationBase<DestroyObjLogicEntity>
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private GameObject highlightFx;


    public DestroyObjLogicEntity DestroyObjEntity { get { return (DestroyObjLogicEntity)_logic; } }


    public override void Tick(float dt)
    {
        base.Tick(dt);
    }
    
}
