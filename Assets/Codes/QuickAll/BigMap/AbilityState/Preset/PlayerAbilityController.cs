using Map.Entity;
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity
{
    public class PlayerAbilityController : MapEntityAbilityController
    {
        public List<string> PlayerDefaultSkilld = new()
        {
            "unlock_loot_point",
            "use_loot_point",
            "use_item",

            "player_shoot",
            "player_weapon",
            "default_dash",
            "fix_clothes",

            "zhaqu",
            "deep_zhaqu",
        };

        public PlayerAbilityController(BaseUnitLogicEntity owner) : base(owner)
        {
            foreach(var skill in PlayerDefaultSkilld)
            {
                var conf = AbilityLibrary.GetAbilityConfig(skill);
                RegisterAbility(conf);
            }
        }

        public void UseUnlockLootPoint(ILogicEntity entity)
        {
            Debug.Log("PlayerAbilityController " + entity.Id);
            TryUseAbility("unlock_loot_point", target: entity, overrideParams: new Dictionary<string, string>()
            {
                ["PhaseExecutingTime"] = "0.9",
            }); ;
        }

        public void TryUseItem(string itemId)
        {
            Debug.Log($"PlayerAbilityController TryUseItem {itemId}");
            TryUseAbility("use_item", overrideParams: new Dictionary<string, string>()
            {
                ["PhaseExecutingTime"] = "0.5",
                ["ItemId"] = itemId,
            }); ;
        }

        public void TryShoot(Vector2 shootDir)
        {
            //Debug.Log("PlayerAbilityController TryShoot " + shootDir);
            TryUseAbility("player_shoot", castDir: shootDir, overrideParams: new Dictionary<string, string>()
            {
            }); ;
        }

        public void TrySlash(Vector2 shootDir)
        {
            //Debug.Log("PlayerAbilityController TryShoot " + shootDir);
            TryUseAbility("player_weapon", castDir: shootDir, overrideParams: new Dictionary<string, string>()
            {
            }); ;
        }

        public void TryDash(Vector2 shootDir)
        {
            //Debug.Log("PlayerAbilityController TryShoot " + shootDir);
            TryUseAbility("default_dash", castDir: shootDir, overrideParams: new Dictionary<string, string>()
            {
            }); ;
        }

        public void TryZhaQu(BaseUnitLogicEntity entity, float duration)
        {
            //Debug.Log("PlayerAbilityController TryShoot " + shootDir);
            TryUseAbility("zhaqu", target: entity, overrideParams: new Dictionary<string, string>()
            {
                ["ExecutingTime"] = duration.ToString(),
            }); ;
        }
    }
}

