

using System.Collections.Generic;
using Animancer;
using My.Map;
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
        private AnimationClip _moveClip;
        private bool _locomotionVisualMove;

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

                var moveInfo = AnimHolder.AnimClips.Find(item => item.Name == "move");
                if (moveInfo == null)
                {
                    moveInfo = AnimHolder.AnimClips.Find(item => item.Name == "walk");
                }
                if (moveInfo != null)
                {
                    _moveClip = moveInfo.Clip;
                }

                if (_Idle != null && EnsureAnimancerReady())
                {
                    var state = MainAgentAnimator.Layers[0].Play(_Idle);
                    state.Events.Clear();
                }
            }
        }

        // Animancer 在首次访问 Layers[n] 前 Count 可能为 0；访问 Layers[0] 会 SetMinCount 创建默认层。
        private bool EnsureAnimancerReady()
        {
            if (MainAgentAnimator == null)
            {
                return false;
            }

            MainAgentAnimator.InitializePlayable();
            _ = MainAgentAnimator.Layers[0];
            return true;
        }

        // 不依赖预制体上是否已「加层」：未配置的更高逻辑层一律落到 Base（0），仍能播放。
        private AnimancerLayer ResolveAnimancerLayer(int layerIndex)
        {
            if (!EnsureAnimancerReady())
            {
                return null;
            }

            if (layerIndex < 0)
            {
                layerIndex = 0;
            }

            if (layerIndex >= MainAgentAnimator.Layers.Count)
            {
                return MainAgentAnimator.Layers[0];
            }

            return MainAgentAnimator.Layers[layerIndex];
        }

        protected override void OnAnimLayerRefreshed(object sender, AnimLayerRefreshEventArgs e)
        {
            ApplyAnimLayerRefresh(e);
        }

        private void ApplyAnimLayerRefresh(AnimLayerRefreshEventArgs e)
        {
            if (AnimHolder == null || MainAgentAnimator == null || UnitEntity == null)
            {
                return;
            }

            var lyr = ResolveAnimancerLayer(e.Layer);
            if (lyr == null)
            {
                return;
            }

            if (e.Top == null || e.Top.Value.IsEmpty)
            {
                if (e.Layer == 0)
                {
                    PlayLocomotionClipOnLayer0();
                }
                return;
            }

            var top = e.Top.Value;
            var clipInfo = AnimHolder.AnimClips.Find(item => item.Name == top.AnimName);

            if (clipInfo == null)
            {
                Debug.LogError("ApplyAnimLayerRefresh no clip " + top.AnimName);
                return;
            }

            var state = lyr.Play(clipInfo.Clip, 0.08f, FadeMode.FromStart);
            state.Speed = clipInfo.Speed;
            state.Events.OnEnd = null;

            if (top.ReleasePolicy.HasFlag(EAnimReleasePolicy.OnClipEnd) && !clipInfo.Clip.isLooping)
            {
                var capturedHandle = top.Handle;
                state.Events.OnEnd = () =>
                {
                    if (UnitEntity is LogicEntityBase le)
                    {
                        le.ReleaseAnimRequest(capturedHandle, EAnimReleaseReason.ClipEnded);
                    }
                };
            }
        }

        private AnimationClip ResolveLocomotionClipWithOverride(string baseName, AnimationClip fallback)
        {
            if (AnimHolder == null)
            {
                return fallback;
            }

            string resolved = baseName;
            if (UnitEntity is LogicEntityBase le)
            {
                resolved = le.GetAnimOverride(baseName);
            }

            if (resolved != baseName)
            {
                var overrideInfo = AnimHolder.AnimClips.Find(c => c.Name == resolved);
                if (overrideInfo != null)
                {
                    return overrideInfo.Clip;
                }
            }

            return fallback;
        }

        private void PlayLocomotionClipOnLayer0()
        {
            if (!EnsureAnimancerReady())
            {
                return;
            }

            var lyr = MainAgentAnimator.Layers[0];
            bool locomotionAllowed = CheckCanActiveMove();
            bool wantMove = locomotionAllowed && _moveClip != null && UnitEntity != null
                && UnitEntity.GetDesiredVelocity().sqrMagnitude > 0.02f;

            AnimationClip clip;
            if (wantMove)
            {
                // 优先用 "move" 的覆盖，若未配置则再查 "walk" 覆盖，最后回退原始 move clip
                clip = ResolveLocomotionClipWithOverride("move", null)
                    ?? ResolveLocomotionClipWithOverride("walk", _moveClip);
            }
            else
            {
                clip = ResolveLocomotionClipWithOverride("idle", _Idle);
            }

            if (clip == null)
            {
                return;
            }

            lyr.Play(clip, 0.12f, FadeMode.FixedSpeed);
            _locomotionVisualMove = wantMove;
        }

        // 外部调用：当 AnimOverride buff 变化时，若层 0 栈为空，立即刷新当前 locomotion 表现
        public void RefreshLocomotionAnimIfNoStack()
        {
            if (UnitEntity is LogicEntityBase le && le.TryPeekAnimStackTop(0, out var top) && !top.IsEmpty)
            {
                return;
            }

            PlayLocomotionClipOnLayer0();
        }

        public void TickLocomotionAnim(float dt)
        {
            if (AnimHolder == null || UnitEntity == null || MainAgentAnimator == null)
            {
                return;
            }

            if (UnitEntity is LogicEntityBase le && le.TryPeekAnimStackTop(0, out var top) && !top.IsEmpty)
            {
                return;
            }

            bool locomotionAllowed = CheckCanActiveMove();
            bool wantMove = locomotionAllowed && _moveClip != null && UnitEntity.GetDesiredVelocity().sqrMagnitude > 0.02f;
            if (wantMove == _locomotionVisualMove)
            {
                return;
            }

            PlayLocomotionClipOnLayer0();
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
                        existCtx = MapSceneEffectManager.Instance.ShowSceneEffect(UnitEntity.Pos, buff.Def.DurationEffect.ParamFloat1, buff.Def.DurationEffect.ParamStr1, this.Id);
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

    }
}