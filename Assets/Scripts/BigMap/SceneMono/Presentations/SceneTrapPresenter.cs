using Map.Entity;
using My.Map;
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public class SceneTrapPresenter : ScenePresentationBase<TrapLogicEntity>
    {
        TrapLogicEntity TrapEntity => (TrapLogicEntity)_logic;

        float _checkTriggerTimer;
        readonly Collider2D[] _hits = new Collider2D[16];
        readonly List<ILogicEntity> _scratch = new();

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            if (TrapEntity != null && !TrapEntity.IsArmedForScan)
            {
                SetVisible(false);
            }
            else
            {
                SetVisible(true);
            }
        }

        protected override void OnEventEntityDestroyed(long entityId)
        {
            SetVisible(false);
        }

        public override void OnEntityMove(long entityId, Vector2 oldPos, Vector2 newPos)
        {
            if (MainGameManager.Instance != null)
            {
                transform.localPosition = MainGameManager.Instance.GetWorldPosFromLogicPos(newPos);
            }
            else
            {
                transform.position = newPos;
            }

            if (SceneAOIManager.Instance != null)
            {
                SceneAOIManager.Instance.MoveEntity(_logic, oldPos, newPos);
            }
        }

        public override void Tick(float dt)
        {
            if (_logic is TrapLogicEntity trap && trap.MarkDestroyed)
            {
                SetVisible(false);
                return;
            }

            base.Tick(dt);

            if (TrapEntity == null)
            {
                return;
            }

            TrapEntity.TryWakeFromSleep();

            if (MainGameManager.Instance != null)
            {
                transform.localPosition = MainGameManager.Instance.GetWorldPosFromLogicPos(TrapEntity.Pos);
            }

            if (!TrapEntity.IsArmedForScan)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            var spec = TrapEntity.Spec;
            if (spec == null)
            {
                return;
            }

            if (LogicTime.time < _checkTriggerTimer)
            {
                return;
            }

            _checkTriggerTimer = LogicTime.time + 0.5f;

            _scratch.Clear();
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, spec.TriggerRadius, _hits, 1 << LayerMask.NameToLayer("MapTarget"));
            var campFilter = spec.CampFilter;

            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null)
                {
                    continue;
                }

                var presentation = col.GetComponentInParent<IScenePresentation>();
                if (presentation == null)
                {
                    continue;
                }

                var ent = presentation.GetLogicEntity();
                if (ent == null || ent is not BaseUnitLogicEntity unitEntity)
                {
                    continue;
                }

                switch (campFilter)
                {
                    case ECampFilterType.OnlySelf:
                        if (unitEntity.FactionId != TrapEntity.FactionId)
                        {
                            continue;
                        }

                        break;
                    case ECampFilterType.NotSelf:
                        if (unitEntity.FactionId == TrapEntity.FactionId)
                        {
                            continue;
                        }

                        break;
                }

                _scratch.Add(ent);
            }

            foreach (var ent in _scratch)
            {
                if (ent is BaseUnitLogicEntity unit && TrapEntity.TryTrigger(unit))
                {
                    break;
                }
            }
        }
    }
}
