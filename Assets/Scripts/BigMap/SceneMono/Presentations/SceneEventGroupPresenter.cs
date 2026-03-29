using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneEventGroupPresenter : InteractPointPresenter
    {

        public EventGroupLogicEntity EventGroupEntity { get { return (EventGroupLogicEntity)_logic; } }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

    }
}

