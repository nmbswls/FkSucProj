using DG.Tweening;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneSimpleBlockPresenter : ScenePresentationBase<LogicEntitySimpleBlock>
    {
        [SerializeField] private GameObject highlightFx;

        public Collider2D MainBlock;

        public LogicEntitySimpleBlock SimpleBlockEntity { get { return (LogicEntitySimpleBlock)_logic; } }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }


        protected override void OnEventEntityDestroyed(long entityId)
        {

        }
    }
}

