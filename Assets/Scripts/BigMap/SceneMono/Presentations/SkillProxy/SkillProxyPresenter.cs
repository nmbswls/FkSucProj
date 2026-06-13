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
        public event Action<string, int, int> EvResourceChanged;
        public event Action EvPeriodicCast;

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            if (_logic == null)
            {
                return;
            }

            _logic.EventOnResourceChanged += OnResourceChanged;
            _logic.EventOnPeriodicCast += OnPeriodicCast;

            EvBound?.Invoke(_logic);
        }

        public override void Unbind()
        {
            if (_logic != null)
            {
                _logic.EventOnResourceChanged -= OnResourceChanged;
                _logic.EventOnPeriodicCast -= OnPeriodicCast;
            }

            EvUnbound?.Invoke();
            base.Unbind();
        }

        // SkillProxy 在逻辑层通过 SetPosition 每帧更新位置（轨道/跟随），
        // 但 SetPosition 不触发 EventOnEntityMove，需在 Tick 中主动同步 Transform。
        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (_logic != null)
            {
                transform.position = MapLogicPosition.LogicToWorld(_logic);
            }
        }

        void OnResourceChanged(string resourceId, int current, int max) =>
            EvResourceChanged?.Invoke(resourceId, current, max);

        void OnPeriodicCast() => EvPeriodicCast?.Invoke();
    }
}
