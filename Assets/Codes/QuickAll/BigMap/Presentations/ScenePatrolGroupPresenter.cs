using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class ScenePatrolGroupPresenter : ScenePresentationBase<PatrolGroupLogicEntity>
{
    [SerializeField] private SpriteRenderer icon;


    public PatrolGroupLogicEntity RealEntity { get { return (PatrolGroupLogicEntity)_logic; } }


    private void Update()
    {
        transform.position = RealEntity.Pos;
    }

    private void OnDrawGizmos()
    {
        var pos = RealEntity.WayPointInfos[RealEntity.WayPointIdx];
        Gizmos.DrawSphere(new Vector3(pos.x, pos.y, 0), 0.1f); // 固定世界坐标
    }

}
