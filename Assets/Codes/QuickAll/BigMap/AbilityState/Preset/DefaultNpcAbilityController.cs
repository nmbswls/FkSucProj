using DG.Tweening.Core.Easing;
using Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class DefaultNpcAbilityController : MapEntityAbilityController
{
    // 预设程序生成行为

    public DefaultNpcAbilityController(BaseUnitLogicEntity owner) : base(owner)
    {
        {
            var _shoot = AbilityLibrary.CreateDefaultShootAbility();
            RegisterAbility(_shoot);
        }

        {
            var _slash = AbilityLibrary.CreateDefaultUseWeaponAbility();
            RegisterAbility(_slash);
        }
        {
            var qinfan = AbilityLibrary.CreateDefaultEnemyQinfan();
            RegisterAbility(qinfan);
        }
    }

}