using Map.Entity;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.WSA;
using static UnityEngine.GraphicsBuffer;

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

    public bool CanUnitSee(long selfEId, long targetEId)
    {
        var selfP = SceneAOIManager.Instance.GetActivePresentation(selfEId);
        if(selfP == null || selfP is not SceneUnitPresenter selfUnit)
        {
            return false;
        }
        var targetP = SceneAOIManager.Instance.GetActivePresentation(targetEId);
        if (targetP == null)
        {
            return false;
        }
        Vector2 eyePos = selfUnit.GetWorldPosition();
        Vector2 p1 = targetP.GetWorldPosition();
        // 方向与距离
        Vector2 toTarget = p1 - eyePos;
        float dist = toTarget.magnitude;
        if (dist <= 0.0001f) return true;

        if(dist > DefaultRange)
        {
            return false;
        }

        Vector2 dirToTarget = toTarget.normalized;
        float angle = Mathf.Abs(Vector2.SignedAngle(selfUnit.UnitEntity.FaceDir, toTarget));

        // 背向硬限制
        if (angle >= HardBackLimitDeg)
            return false;

        // 目标半径（来自 CapsuleCollider2D）
        float targetRadius = 0.2f;
        // todo 实现不同半径

        // 半径角容忍
        float radiusAngleDeg = Mathf.Atan2(targetRadius, Mathf.Max(dist, 0.0001f)) * Mathf.Rad2Deg;
        radiusAngleDeg = Mathf.Min(radiusAngleDeg, RadiusAngleMaxDeg);

        // 仅前半圆应用半径角容忍（避免影响背向）
        if (angle > 120f)
            radiusAngleDeg = 0f;

        // 近身策略性扩展（仅前向权重）
        float baseHalfFov = Mathf.Abs(DefaultFovAngle) * 0.5f;
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

    private bool IsLineClear2D(Vector2 origin, Vector2 dir, float dist)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, ObstacleMask);
        return hit.collider == null;
    }



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
}

