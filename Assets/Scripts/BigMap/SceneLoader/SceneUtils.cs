using Map.Entity;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.WSA;

public class DefaultSceneVisionSenser2D : IVisionSenser2D
{
    public LayerMask ObstacleMask;

    public float DefaultRange = 6.0f;
    public float DefaultFovAngle = 90;

    private const float HardBackLimitDeg = 150f;   // 背向硬限制
    private const float RadiusAngleMaxDeg = 20f;   // 半径角容忍上限
    private const float CloseDistance = 1.0f;      // 贴身距离阈值
    private const float ExtraAngleNearDeg = 20f;   // 近身策略性扩展最大值
    private const float MaxExpandedHalfFovDeg = 120f;
    private const float RayLateralSpread = 0.2f;   // 近身多射线左右偏移（米）

    /// <summary>
    /// 针对单位间的可见性
    ///   假设单位半径不至于太大
    /// </summary>
    /// <param name="selfEntityId"></param>
    /// <param name="targetEntityId"></param>
    /// <returns></returns>
    public bool CanUnitSee(long selfEntityId, long targetEntityId)
    {
        var selfPresenter = SceneAOIManager.Instance.GetActivePresentation(selfEntityId);
        if(selfPresenter == null || selfPresenter is not SceneUnitPresenter selfUnit)
        {
            return false;
        }
        var targetPresenter = SceneAOIManager.Instance.GetActivePresentation(targetEntityId);
        if (targetPresenter == null)
        {
            return false;
        }
        Vector2 eyePos = selfUnit.GetWorldPosition();
        Vector2 p1 = targetPresenter.GetWorldPosition();
        // 方向与距离
        Vector2 toTarget = p1 - eyePos;
        float dist = toTarget.magnitude;
        // 贴身 必定看见
        if (dist <= 0.05f) return true;

        var seeParams = selfUnit.UnitEntity.GetViewRangeAndAngle();
        float range = seeParams.Item1;
        float fovAngle = seeParams.Item2;
        if (dist > range)
        {
            return false;
        }

        Vector2 dirToTarget = toTarget.normalized;
        float angle = Mathf.Abs(Vector2.SignedAngle(selfUnit.UnitEntity.CurrentLook, toTarget));

        float baseHalfFov = Mathf.Abs(fovAngle) * 0.5f;

        // 背向硬限制
        if (angle >= HardBackLimitDeg)
            return false;

        // 目标半径（来自 CapsuleCollider2D）
        float targetRadius = 0.25f;
        // todo 实现不同半径

        // 半径角容忍
        float radiusAngleDeg = Mathf.Atan2(targetRadius, Mathf.Max(dist, 0.05f)) * Mathf.Rad2Deg;
        radiusAngleDeg = Mathf.Min(radiusAngleDeg, RadiusAngleMaxDeg);

        // 仅前半圆应用半径角容忍（避免影响背向）
        if (angle > 120f)
            radiusAngleDeg = 0f;

        // 近身策略性扩展（仅前向权重）  
        bool nearMode = dist <= CloseDistance;
        float frontWeight = Mathf.Max(0f, Mathf.Cos(angle * Mathf.Deg2Rad)); // 前方≈1，侧向≈0，后方≈0
        float extraNear = nearMode ? (ExtraAngleNearDeg * frontWeight * Mathf.Clamp01((CloseDistance - dist) / Mathf.Max(CloseDistance, 0.0001f))) : 0f;

        // 有效半角
        float effectiveHalfFov = Mathf.Min(baseHalfFov + radiusAngleDeg + extraNear, MaxExpandedHalfFovDeg);

        // 角度判定
        if (angle > effectiveHalfFov)
            return false;

        // 遮挡检测：主射线 + 近身左右偏移两条（任意一条通则可见）
        if (IsLineClear2D(eyePos, dirToTarget, dist))
            return true;

        if (nearMode)
        {
            // 构造左右偏移起点（2D中与视线垂直的右向）
            Vector2 right = new Vector2(-dirToTarget.y, dirToTarget.x); // 旋转90°
            Vector2 leftOrigin = eyePos - right * RayLateralSpread;
            Vector2 rightOrigin = eyePos + right * RayLateralSpread;

            if (IsLineClear2D(leftOrigin, dirToTarget, dist)) return true;
            if (IsLineClear2D(rightOrigin, dirToTarget, dist)) return true;
        }

        return false;
    }

    public bool IsLineClear2D(Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, ObstacleMask);
        return hit.collider == null;
    }

    public bool SimpleCanSee(Vector2 selftPos, Vector2 selfFace, Vector2 targetPos, float range, float fov)
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
            var comp = trans.GetComponentInParent<IScenePresentation>();
            if (comp == null) continue;
            var entity = comp.GetLogicEntity();
            if (entity == null) continue;

            if (entity.MarkDestroyed) continue;

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
            var comp = trans.GetComponentInParent<IScenePresentation>();
            if (comp == null) continue;
            var entity = comp.GetLogicEntity();
            if (entity == null) continue;
            if (entity.MarkDestroyed) continue;

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

    public void OverlapCheckDynamicObs(Vector2 orgPos, float radius, List<(Vector2, Vector2)> retList)
    {
        retList.Clear();
        var hitCount = Physics2D.OverlapCapsuleNonAlloc(orgPos, new Vector2(0.46f, 0.2f), CapsuleDirection2D.Horizontal, 0, hits, 1 << LayerMask.NameToLayer("DynamicObs"));
        for (int i = 0; i < hitCount; i++)
        {
            var col = hits[i];
            var closestP = col.ClosestPoint(orgPos);
            retList.Add((col.transform.position, closestP));
        }
    }


    /// <summary>
    /// 检查是否与alert area洛
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool CheckIsInAlertArea(Vector2 pos)
    {
        var hitCol = Physics2D.OverlapPoint(pos, 1 << LayerMask.NameToLayer("AlertArea"));
        if (hitCol == null) return false;
        return true;
    }
}

