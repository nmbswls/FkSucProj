

using System.Collections.Generic;
using Animancer;
using UnityEngine;
using static MapSceneEffectManager;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        public Transform BindEffectRoot;


        public AnimancerComponent MainAgentAnimator;
        public UnitAnimHolder AnimHolder;

        private AnimationClip _Idle;

        private float _pendingOffsetZ = 0;
        private void InitAnimComps()
        {
            if(AnimHolder != null)
            {
                var clipInfo = AnimHolder.AnimClips.Find(item => item.Name == "idle");

                if (clipInfo != null)
                {
                    _Idle = clipInfo.Clip;
                }
                var state = MainAgentAnimator.Play(_Idle);
                state.Events.Clear();
            }
            
            //OnEventAnimPlay("attack_01", 0);
        }

        


        public void UpdateOffsetZView()
        {
            if (AgentView == null) return;

            float targetOffsetZ = UnitEntity.OffsetZ + this.AgentView.transform.localPosition.y;

            _pendingOffsetZ = Mathf.Lerp(_pendingOffsetZ, targetOffsetZ, 3f * LogicTime.deltaTime);

            this.AgentView.transform.localPosition = new(this.AgentView.transform.localPosition.x, _pendingOffsetZ, 0);
        }

        public SpriteWhiteFlasher MainFlasher;
        private int lastHitOverrideCtxId = 0;
        public void PresenterOnHit(long? srcId)
        {
            bool overrideHit = false;
            foreach(var buff in UnitEntity.BuffContainer.Values)
            {
                if (buff.Def.DurationEffect == null) continue;
                if(buff.Def.DurationEffect.DurationType == Entity.EBuffDurationType.HitEffect)
                {
                    var existCtx = MapSceneEffectManager.Instance.FindSceneEffect(lastHitOverrideCtxId);

                    if(existCtx == null)
                    {
                        existCtx = MapSceneEffectManager.Instance.ShowSceneEffect(UnitEntity.Pos, buff.Def.DurationEffect.ParamFloat1, buff.Def.DurationEffect.ParamStr, this.Id);
                        if (existCtx != null)
                        {
                            existCtx.BindingUnitVec = new Vector2(0, 0.555f);
                        }
                    }
                    else
                    {
                        existCtx.EffectCtrl.Show();
                    }
                }
            }
            
            if (!overrideHit)
            {
                var ctx = MapSceneEffectManager.Instance.ShowSceneEffect(UnitEntity.Pos, 0.5f, "Hit/Style01", this.Id);
                if (ctx != null)
                {
                    ctx.BindingUnitVec = new Vector2(0, 0.05f);
                    var dir = UnityEngine.Random.insideUnitCircle.normalized;
                    if (srcId != null)
                    {
                        var pres = SceneAOIManager.Instance.GetActivePresentation(srcId.Value);
                        dir = pres.GetWorldPosition() - this.GetWorldPosition();
                    }

                    ctx.EffectGo.transform.right = -dir;
                }

                MainFlasher?.TriggerFlash();
            }


            if (HitPivot != null)
            {
            }
        }

        protected override void OnFadeStateUpdate()
        {
            //_currFadeAlpha = Mathf.Lerp(_currFadeAlpha, _targetFadeAlpha, 2 * LogicTime.deltaTime);
            base.OnFadeStateUpdate();

            //if (srs != null)
            //{
            //    foreach(var sr in srs)
            //    {
            //        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, _currFadeAlpha);
            //    }
            //}
        }


        protected virtual void UpdateBindingEffect()
        {

        }



        protected virtual void OnEventAnimPlay(string animName, int layer, bool clearAll)
        {
            if (layer == 0)
            {
                if (AnimHolder == null)
                {
                    return;
                }

                var clipInfo = AnimHolder.AnimClips.Find(item=>item.Name == animName);

                if(clipInfo == null)
                {
                    Debug.LogError("OnEventAnimPlay no clip " + animName);
                    return;
                }

                var state = MainAgentAnimator.Play(clipInfo.Clip, 0, FadeMode.FromStart);
                state.Speed = clipInfo.Speed;

                state.Events.OnEnd = () => MainAgentAnimator.Play(_Idle);
            }
        }


    }
}