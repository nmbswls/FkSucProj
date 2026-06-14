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

        // 逻辑 Pos 与 Owner 脚底同步；FollowOffset 仅在此叠加为表现位置。
        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (_logic != null && _logic.Cfg != null)
            {
                var worldPos = MapLogicPosition.LogicToWorld(_logic);
                worldPos += (Vector3)_logic.Cfg.FollowOffset;
                transform.position = worldPos;
            }
        }

        void OnResourceChanged(string resourceId, int current, int max) =>
            EvResourceChanged?.Invoke(resourceId, current, max);

        void OnPeriodicCast() => EvPeriodicCast?.Invoke();
    }
}
