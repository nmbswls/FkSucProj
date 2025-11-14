using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneAttractPointPresenter : ScenePresentationBase<AttractPointLogicEntity>
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;


        public AttractPointLogicEntity AttractPointEntity { get { return (AttractPointLogicEntity)_logic; } }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }
    }
}

