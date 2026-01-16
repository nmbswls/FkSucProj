using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneDestroyObjPresenter : ScenePresentationBase<DestroyObjLogicEntity>
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;


        public DestroyObjLogicEntity DestroyObjEntity { get { return (DestroyObjLogicEntity)_logic; } }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            DestroyObjEntity.EventOnHit += OnEventDestroyObjHit;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();
            DestroyObjEntity.EventOnHit += OnEventDestroyObjHit;
        }

        protected virtual void OnEventDestroyObjHit(long entityId)
        {
            PresenterOnHit();
        }

        public SpriteWhiteFlasher MainFlasher;
        public void PresenterOnHit()
        {
            MainFlasher?.TriggerFlash();
        }
    }
}

