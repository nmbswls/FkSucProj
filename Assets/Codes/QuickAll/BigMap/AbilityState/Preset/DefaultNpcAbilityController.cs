using DG.Tweening.Core.Easing;
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{
    public class DefaultNpcAbilityController : MapEntityAbilityController
    {
        // 预设程序生成行为
        public List<string> DefaultSkillList = new List<string>()
        {
            "player_shoot",
            "player_weapon",
            "default_enemy_qinfan",
        };

        public DefaultNpcAbilityController(BaseUnitLogicEntity owner) : base(owner)
        {
            foreach (var skill in DefaultSkillList)
            {
                var conf = AbilityLibrary.GetAbilityConfig(skill);
                RegisterAbility(conf);
            }
        }

    }

}
