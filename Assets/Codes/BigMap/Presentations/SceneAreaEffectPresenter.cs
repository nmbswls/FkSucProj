using Config.Map;
using Map.Entity;
using My.Map.Entity;
using My.Map.Fight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

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
            if (LogicTime.time  < _checkTriggerTimer)
            {
                return;
            }

            _checkTriggerTimer = LogicTime.time + 0.5f;

            _candidates.Clear();
            int count = 0;
            switch (AreaEffectEntity.cacheCfg.ShapeInfo.Type)
            {
                case FightStruct.EShapeType.Square:
                    {
                        count = Physics2D.OverlapBoxNonAlloc(transform.position, new Vector2(AreaEffectEntity.cacheCfg.ShapeInfo.Width, AreaEffectEntity.cacheCfg.ShapeInfo.Length), 0, hits, 1 << LayerMask.NameToLayer("MapTarget"));
                    }
                    break;
                case FightStruct.EShapeType.Circle:
                    {
                        count = Physics2D.OverlapCircleNonAlloc(transform.position, AreaEffectEntity.cacheCfg.ShapeInfo.Radius, hits, 1 << LayerMask.NameToLayer("MapTarget"));
                    }
                    break;
            }

            // 遍历命中，筛选实现了接口的对象
            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                if (col == null) continue;

                // 在 Collider 或其父节点上寻找接口
                // 注意：GetComponentInParent 会产生少量 GC，若极致无 GC，可预缓存或自定义映射
                var presentation = col.GetComponentInParent<IScenePresentation>();
                if (presentation == null)
                {
                    continue;
                }

                var logic = presentation.GetLogicEntity();
                if(logic  == null) continue;
                if (logic is not BaseUnitLogicEntity unitEntity)
                {
                    continue;
                }

                switch(AreaEffectEntity.cacheCfg.CampFilterType)
                {
                    case ECampFilterType.OnlySelf:
                        {
                            if(unitEntity.FactionId != AreaEffectEntity.FactionId)
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
