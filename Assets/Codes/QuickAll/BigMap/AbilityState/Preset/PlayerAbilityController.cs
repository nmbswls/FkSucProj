using Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerAbilityController : MapEntityAbilityController
{
    // 预设程序生成行为
    //private MapAbilitySpecConfig _openDoor;
    //private MapAbilitySpecConfig _useItem;
    //private MapAbilitySpecConfig _shoot;
    //private MapAbilitySpecConfig _slash;
    //private MapAbilitySpecConfig _dash;

    //private MapAbilitySpecConfig _zhaqu;

    public PlayerAbilityController(BaseUnitLogicEntity owner) : base(owner)
    {
        {
            var _openDoor = AbilityLibrary.CreateDefaultUnlockLootPoint();
            RegisterAbility(_openDoor);
        }

        {
            var _useItem = AbilityLibrary.CreateDefaultUseItem();
            RegisterAbility(_useItem);
        }

        {

            var _shoot = AbilityLibrary.CreateDefaultShootAbility();
            RegisterAbility(_shoot);

            var _slash = AbilityLibrary.CreateDefaultUseWeaponAbility();
            RegisterAbility(_slash);
            var _dash = AbilityLibrary.CreateDefaultDash();
            RegisterAbility(_dash);
            var _zhaqu = AbilityLibrary.CreateOrRefreshZhaQu();
            RegisterAbility(_zhaqu);
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