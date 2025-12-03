using DG.Tweening.Core.Easing;
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{
    public class DefaultNpcAbilityController : MapEntityAbilityExecutor
    {
        

        public DefaultNpcAbilityController(BaseUnitLogicEntity owner) : base(owner)
        {
            //if(owner.unitCfg.SkillList.Count > 0)
            //{
            //    foreach (var skill in owner.unitCfg.SkillList)
            //    {
            //        var conf = AbilityLibrary.GetAbilityConfig(skill);
            //        RegisterAbility(conf);
            //    }
            //}
            //else
            //{
            //    foreach (var skill in DefaultSkillList)
            //    {
            //        var conf = AbilityLibrary.GetAbilityConfig(skill);
            //        RegisterAbility(conf);
            //    }
            //}
        }

    }

}
