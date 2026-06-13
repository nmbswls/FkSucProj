using System;
using My;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // 逻辑桥接架子：只负责将 SkillProxyLogicEntity 的事件转发为自身的 C# 事件，
    // 不持有任何视觉对象引用，不关心显示细节。
    public class SkillProxyPresenter : ScenePresentationBase<SkillProxyLogicEntity>
    {
        public event Action<SkillProxyLogicEntity> EvBound;
        public event Action EvUnbound;
        public event Action<int, int> EvAmmoChanged;
        public event Action EvPeriodicCast;

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            if (_logic == null)
            {
                return;
            }

            _logic.EventOnAmmoResourceChanged += OnAmmoChanged;
            _logic.EventOnPeriodicCast += OnPeriodicCast;

            EvBound?.Invoke(_logic);
        }

        public override void Unbind()
        {
            if (_logic != null)
            {
                _logic.EventOnAmmoResourceChanged -= OnAmmoChanged;
                _logic.EventOnPeriodicCast -= OnPeriodicCast;
            }

            EvUnbound?.Invoke();
            base.Unbind();
        }

        void OnAmmoChanged(int current, int max) => EvAmmoChanged?.Invoke(current, max);

        void OnPeriodicCast() => EvPeriodicCast?.Invoke();
    }
}
