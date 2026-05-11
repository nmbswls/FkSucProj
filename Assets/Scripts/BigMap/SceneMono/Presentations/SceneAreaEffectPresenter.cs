using Map.Entity;
using My.Map;
using My.Map.Entity;
using My.Map.Fight;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public class SceneAreaEffectPresenter : ScenePresentationBase<AreaEffectLogicEntity>
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;

        public event Action<bool> EventOnInteractStateChanged;

        public string ShowName => gameObject.name;

        public AreaEffectLogicEntity AreaEffectEntity { get { return (AreaEffectLogicEntity)_logic; } }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            TryTriggerActivate(dt);
        }

        private float _checkTriggerTimer;
        private Collider2D[] hits = new Collider2D[16];
        private List<ILogicEntity> _candidates = new();

        public void TryTriggerActivate(float dt)
        {
            if (LogicTime.time < _checkTriggerTimer)
            {
                return;
            }

            _checkTriggerTimer = LogicTime.time + 0.5f;

            var row = AreaEffectEntity.CfgRow;
            if (row == null)
            {
                return;
            }

            var shape = MapAreaEffectBind.ToShape(row);
            _candidates.Clear();
            int count = 0;
            int layerMask = 1 << LayerMask.NameToLayer("MapTarget");
            switch (shape.Type)
            {
                case FightStruct.EShapeType.Square:
                    count = Physics2D.OverlapBoxNonAlloc(
                        transform.position,
                        new Vector2(shape.Width, shape.Length),
                        0,
                        hits,
                        layerMask);
                    break;
                case FightStruct.EShapeType.Circle:
                    count = Physics2D.OverlapCircleNonAlloc(transform.position, shape.Radius, hits, layerMask);
                    break;
                default:
                    {
                        float r = Mathf.Max(0.08f, shape.Radius);
                        count = Physics2D.OverlapCircleNonAlloc(transform.position, r, hits, layerMask);
                    }
                    break;
            }

            var campFilter = MapAreaEffectBind.ToCampFilter(row);
            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                if (col == null)
                {
                    continue;
                }

                var presentation = col.GetComponentInParent<IScenePresentation>();
                if (presentation == null)
                {
                    continue;
                }

                var logic = presentation.GetLogicEntity();
                if (logic == null)
                {
                    continue;
                }

                if (logic is not BaseUnitLogicEntity unitEntity)
                {
                    continue;
                }

                switch (campFilter)
                {
                    case ECampFilterType.OnlySelf:
                        {
                            if (unitEntity.FactionId != AreaEffectEntity.FactionId)
                            {
                                continue;
                            }

                            break;
                        }
                    case ECampFilterType.NotSelf:
                        {
                            if (unitEntity.FactionId == AreaEffectEntity.FactionId)
                            {
                                continue;
                            }

                            break;
                        }
                }

                _candidates.Add(logic);
            }

            AreaEffectEntity.UpdateAffectedLogics(_candidates);
        }
    }
}
