using DG.Tweening;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneDestroyObjPresenter : ScenePresentationBase<DestroyObjLogicEntity>
    {
        [SerializeField] private GameObject highlightFx;

        public SpriteRenderer[] MainView;
        public SpriteRenderer ShadowView;

        public Collider2D MainCol;

        public DestroyObjLogicEntity DestroyObjEntity { get { return (DestroyObjLogicEntity)_logic; } }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            if (MainView != null)
            {
                foreach (var view in MainView)
                {
                    view.color = new Color(view.color.a, view.color.g, view.color.b, 1);
                    //view.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                }
            }

            if (ShadowView != null)
            {
                ShadowView.enabled = true;
            }
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            DestroyObjEntity.EventOnHit += OnEventDestroyObjHit;
            DestroyObjEntity.EventOnBrack += OnEventDestroyObjBrack;

            
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();
            DestroyObjEntity.EventOnHit += OnEventDestroyObjHit;
        }

        protected virtual void OnEventDestroyObjBrack(long entityId)
        {
            MainGameManager.Instance.ShowFakeFxEffect("ÆÆËé", this.transform.position);


            if (MainView != null)
            {
                foreach (var view in MainView)
                {
                    view.DOFade(0, 0.3f);
                    view.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
                }
            }

            if(ShadowView != null)
            {
                DOVirtual.DelayedCall(0.3f, () =>
                {
                    ShadowView.enabled = false ;
                });
            }

            if(MainCol != null)
            {
                MainCol.enabled = false;
            }
            //if (ViewRoot != null)
            //{
            //    ViewRoot.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            //}
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

