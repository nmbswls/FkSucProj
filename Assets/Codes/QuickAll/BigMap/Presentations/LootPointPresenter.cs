using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class LootPointPresenter : ScenePresentationBase<LootPointLogicEntity>, ISceneInteractable
{
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private GameObject highlightFx;

    public event Action<bool> EventOnInteractStateChanged;

    public LootPointLogicEntity LootEntity { get { return (LootPointLogicEntity)_logic; } }
    public override void ApplyState(object state)
    {
        if (state is InteractPointState s)
        {
            transform.position = s.Position;
            if (icon != null) icon.enabled = s.IsEnabled;
            //if (highlightFx != null) highlightFx.SetActive(s.IsEnabled && _logic.IsInAOI);
        }
    }

    public void SetInteractExpandStatus(bool expanded)
    {
        EventOnInteractStateChanged?.Invoke(expanded);
    }

    public Vector3 GetHintAnchorPosition()
    {
        return GetWorldPosition();
    }

    /// <summary>
    /// 触发交互
    /// </summary>
    /// <param name="triggerIdx"></param>
    public void TriggerInteract(string interactSelection)
    {
        // 只有一个触发点
        if(interactSelection == "unlock")
        {
            // 尝试解锁
            if(!LootEntity.IsLocked)
            {
                MainGameManager.Instance.ShowFakeFxEffect("没锁呀", _logic.Pos);
                return;
            }
            MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("unlock_loot_point", target:LootEntity); ;
        }
        else if(interactSelection == "loot")
        {
            // 尝试解锁
            if (LootEntity.IsLocked)
            {
                MainGameManager.Instance.ShowFakeFxEffect("locked", _logic.Pos);
                return;
            }

            float useTime = LootEntity.cacheConfig.LootOpenTime;
            MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("use_loot_point", target: LootEntity, 
                overrideParams: new Dictionary<string, string>()
            {
                ["PhaseExecutingTime"] = useTime.ToString()
            }, phaseOverrideAnims: new Dictionary<string, string>()
            {
                ["Executing"] = LootEntity.cacheConfig.LootOverrideAnim
            });
        }
    }

    public List<string> GetInteractSelections()
    {
        if (LootEntity.IsLocked)
        {
            return new() { "unlock" };
        }
        else
        {
            return new() { "loot" };
        }
    }

    public bool CanInteractEnable()
    {
        return true;
    }
}
