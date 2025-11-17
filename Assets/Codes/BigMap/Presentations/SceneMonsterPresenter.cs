using Config;
using Map.Entity;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace My.Map.Scene
{
    public class SceneMonsterPresenter : SceneUnitPresenter
    {

        public MonsterUnitLogicEntity MonsterEntity
        {
            get
            {
                return (MonsterUnitLogicEntity)_logic;
            }
        }


        protected override void Awake()
        {
            base.Awake();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }
    }
}

/// <summary>
/// 场景单位 基类
/// </summary>

