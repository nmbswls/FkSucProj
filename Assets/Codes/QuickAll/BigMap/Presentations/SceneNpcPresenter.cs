using Map.Entity;
using Map.Entity.Attr;
using Map.Logic.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.RuleTile.TilingRuleOutput;

/// <summary>
/// 场景单位 基类
/// </summary>
public class SceneNpcPresenter : SceneUnitPresenter, ISceneInteractable
{

    public NpcUnitLogicEntity NpcEntity { get
        {
            return (NpcUnitLogicEntity)_logic;
        } }


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

    public bool CanInteractEnable()
    {
        if(NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) > 0)
        {
            return true;
        }

        return false;
    }

    public void TriggerInteract(string interactSelection)
    {
        if (NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) == 0)
        {
            return;
        }

        // 显示层事件
        MainGameManager.Instance.gameLogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
        {
            Name = "AbsorbDizzy",
            Param3 = this.Id,
        });
    }

    public Vector3 GetHintAnchorPosition()
    {
        return transform.position;
    }

    public List<string> GetInteractSelections()
    {
        return new List<string>() { "吸!" };
    }
}
