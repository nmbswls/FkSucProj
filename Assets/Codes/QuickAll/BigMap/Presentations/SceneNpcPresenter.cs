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
        if(NpcEntity.IsInBattle)
        {
            return false;
        }

        var diff = transform.position - MainGameManager.Instance.playerScenePresenter.transform.position;
        if(diff.magnitude > 2f)
        {
            return false;
        }

        if (MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, MainGameManager.Instance.playerScenePresenter.transform.position, NpcEntity.FaceDir, 1.0f, 60f))
        {
            return false;
        }

        //if(NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) > 0)
        //{
        //    return true;
        //}

        return true;
    }

    public void TriggerInteract(string interactSelection)
    {
        if (NpcEntity.IsInBattle)
        {
            return;
        }

        if (MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, MainGameManager.Instance.playerScenePresenter.transform.position, NpcEntity.FaceDir, 1.0f, 60f))
        {
            return;
        }

        if (NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) == 0)
        {
            //return;
        }

        // 显示层事件
        MainGameManager.Instance.gameLogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
        {
            Name = "AbsorbDizzy",
            Param3 = this.Id,
        });

        MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("zhaqu", target:NpcEntity);
    }

    public Vector3 GetHintAnchorPosition()
    {
        return transform.position + new Vector3(0, 0.25f, 0);
    }

    public List<string> GetInteractSelections()
    {
        return new List<string>() { "吸!" };
    }
}
