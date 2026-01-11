

using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        public Transform BindEffectRoot;

        private float _pendingOffsetZ = 0;
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

            float targetOffsetZ = offsetZ;

            _pendingOffsetZ = Mathf.Lerp(_pendingOffsetZ, targetOffsetZ, 3f * LogicTime.deltaTime);

            this.AgentView.transform.localPosition = new(this.AgentView.transform.localPosition.x, _pendingOffsetZ, 0);
        }

        public SpriteWhiteFlasher MainFlasher;
        public void PresenterOnHit()
        {
            MainFlasher?.TriggerFlash();
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

    }
}