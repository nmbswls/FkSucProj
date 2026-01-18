

using System.Collections.Generic;
using System.Numerics;
using Animancer;
using Unity.VisualScripting;
using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        public Transform BindEffectRoot;

        private float _pendingOffsetZ = 0;

        public AnimancerComponent MainAgentAnimator;
        public UnitAnimHolder AnimHolder;

        private AnimationClip _Idle;
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

            float offsetZ = 0;

            foreach(var buffInst in UnitEntity.BuffContainer.Values)
            {
                if(buffInst.Def.ZOffsetOverride > 0)
                {
                    offsetZ = buffInst.Def.ZOffsetOverride;
                }
            }

            float targetOffsetZ = offsetZ + this.AgentView.transform.localPosition.y;

            _pendingOffsetZ = Mathf.Lerp(_pendingOffsetZ, targetOffsetZ, 3f * LogicTime.deltaTime);

            this.AgentView.transform.localPosition = new(this.AgentView.transform.localPosition.x, _pendingOffsetZ, 0);
        }

        public SpriteWhiteFlasher MainFlasher;
        public void PresenterOnHit()
        {
            MainFlasher?.TriggerFlash();

            if(HitPivot != null)
            {
            }
        }

        protected override void OnFadeStateUpdate()
        {
            //_currFadeAlpha = Mathf.Lerp(_currFadeAlpha, _targetFadeAlpha, 2 * LogicTime.deltaTime);

            if(srs != null)
            {
                foreach(var sr in srs)
                {
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, _currFadeAlpha);
                }
            }
        }


        protected virtual void UpdateBindingEffect()
        {

        }



        protected virtual void OnEventAnimPlay(string animName, int layer)
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
                //var animancer = AgentView.GetComponent<AnimancerComponent>();
                //List<AnimationClip> clips = new();
                //animancer.GetAnimationClips(clips);

                var state = MainAgentAnimator.Play(clipInfo.Clip, 0, FadeMode.FromStart);
                state.Speed = clipInfo.Speed;

                state.Events.OnEnd = () => MainAgentAnimator.Play(_Idle);
            }

        }


    }
}