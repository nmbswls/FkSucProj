using Map.Entity;
using My.Map;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DefaultSceneVisionSenser2D : IVisionSenser2D
{
    public LayerMask ObstacleMask;

    public bool CanSee(Vector2 selftPos, Vector2 selfFace, Vector2 targetPos, float range, float fov)
    {
        Vector2 to = targetPos - selftPos;
        if (to.magnitude > range) return false;
        float angle = Vector2.SignedAngle(selfFace, to);
        if (Mathf.Abs(angle) > fov * 0.5f) return false;
        var hit = Physics2D.Raycast(selftPos, to.normalized, to.magnitude, ObstacleMask);
        return !hit;
    }


    /// <summary>
    /// 选择一个离中心点指定距离的点 尽量离原始点较近
    /// </summary>
    /// <param name="orgPos"></param>
    /// <param name="centerPos"></param>
    /// <param name="awayDist"></param>
    /// <returns></returns>
    public Vector2 ChoosePointAwayFromTarget(Vector2 orgPos, Vector2 centerPos, float awayDist)
    {
        var dir = (orgPos - centerPos).normalized;
        return centerPos + dir * awayDist;
    }

    public Collider2D[] hits = new Collider2D[128];
    public List<ILogicEntity> OverlapBoxAllEntity(Vector2 orgPos, Vector2 dir, Vector2 size, EntityFilterParam? filter)
    {
        List<ILogicEntity> retList = new();
        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var hitCount = Physics2D.OverlapBoxNonAlloc(orgPos,size, angleDeg, hits, 1 << LayerMask.NameToLayer("MapTarget"));
        for(int i=0;i< hitCount;i++)
        {
            var trans = hits[i].transform;
            var comp = trans.GetComponent<IScenePresentation>();
            if (comp == null) continue;
            var entity = comp.GetLogicEntity();
            if (entity == null) continue;

            if(filter != null)
            {
                // 不满足
                if(filter.Value.FilterType != EEntityType.None && filter.Value.FilterType != entity.Type)
                {
                    continue;
                }

                if(filter.Value.FilterParamLists != null && !filter.Value.FilterParamLists.Contains(entity.Type))
                {
                    continue;
                }

                // 校验阵营相关
                if(filter.Value.CampFilterType != ECampFilterType.All)
                {
                    if(filter.Value.CampFilterType == ECampFilterType.NotSelf)
                    {
                        if(entity.FactionId == filter.Value.SelfCampId)
                        {
                            continue;
                        }
                    }
                }
            }
            
            retList.Add(comp.GetLogicEntity());
        }

        return retList;
    }

    public List<ILogicEntity> OverlapCircleAllEntity(Vector2 orgPos, float radius,  EntityFilterParam? filter)
    {
        List<ILogicEntity> retList = new();
        var hitCount = Physics2D.OverlapCircleNonAlloc(orgPos, radius, hits, 1 << LayerMask.NameToLayer("MapTarget"));
        for (int i = 0; i < hitCount; i++)
        {
            var trans = hits[i].transform;
            var comp = trans.GetComponent<IScenePresentation>();
            if (comp == null) continue;
            var entity = comp.GetLogicEntity();
            if (entity == null) continue;
            if (filter != null)
            {
                // 不满足
                if (filter.Value.FilterType != EEntityType.None && filter.Value.FilterType != entity.Type)
                {
                    continue;
                }

                if (filter.Value.FilterParamLists != null && !filter.Value.FilterParamLists.Contains(entity.Type))
                {
                    continue;
                }

                // 校验阵营相关
                if (filter.Value.CampFilterType != ECampFilterType.All)
                {
                    if (filter.Value.CampFilterType == ECampFilterType.NotSelf)
                    {
                        if (entity.FactionId == filter.Value.SelfCampId)
                        {
                            continue;
                        }
                    }
                }
            }
            retList.Add(comp.GetLogicEntity());
        }

        return retList;
    }
}

