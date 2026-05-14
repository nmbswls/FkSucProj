using System.Collections.Generic;
using My.Map.Logic;
using My.MapExport;
using UnityEngine;

namespace My
{
    /// <summary>
    /// 动态压力守卫：退场命名点、路网随机巡逻环采样
    /// </summary>
    public static class DynamicPressureGuardUtil
    {
        public static bool TryPickRandomGuardSpawnerLogicPos(MapExportDatabase db, out Vector2 pos)
        {
            pos = default;
            if (db?.NamedPoints == null || db.NamedPoints.Count == 0)
            {
                return false;
            }

            List<NamedPoint> candidates = null;
            foreach (var p in db.NamedPoints)
            {
                if (p.PointType != ENamedPointType.GuardSpawner)
                {
                    continue;
                }

                candidates ??= new List<NamedPoint>();
                candidates.Add(p);
            }

            if (candidates == null || candidates.Count == 0)
            {
                return false;
            }

            var pick = candidates[Random.Range(0, candidates.Count)];
            pos = new Vector2(pick.Position.x, pick.Position.y);
            return true;
        }

        /// <summary>
        /// 在路网上取一条随机边的两端形成 2 点闭合环（与 Policy_GraphPatrol 一致）；desiredCount 暂保留供后续扩展。
        /// </summary>
        public static bool TrySamplePatrolCycleIds(
            MapExportDatabase db,
            string patrolNetworkIdOrEmpty,
            int desiredCount,
            List<string> outCycleNodeIds,
            out string resolvedNetworkId)
        {
            resolvedNetworkId = string.Empty;
            outCycleNodeIds ??= new List<string>();
            outCycleNodeIds.Clear();

            if (db?.PortalNetworks == null || db.PortalNetworks.Count == 0)
            {
                return false;
            }

            PortalNetworkExport net = null;
            if (!string.IsNullOrEmpty(patrolNetworkIdOrEmpty))
            {
                foreach (var p in db.PortalNetworks)
                {
                    if (p != null && p.NetworkId == patrolNetworkIdOrEmpty)
                    {
                        net = p;
                        break;
                    }
                }
            }
            else if (db.PortalNetworks.Count == 1)
            {
                net = db.PortalNetworks[0];
            }

            if (net == null || net.Edges == null || net.Edges.Count == 0)
            {
                return false;
            }

            resolvedNetworkId = net.NetworkId;
            _ = Mathf.Max(2, desiredCount);

            var e = net.Edges[Random.Range(0, net.Edges.Count)];
            if (string.IsNullOrEmpty(e.NodeA) || string.IsNullOrEmpty(e.NodeB))
            {
                return false;
            }

            outCycleNodeIds.Add(e.NodeA);
            outCycleNodeIds.Add(e.NodeB);
            return true;
        }
    }
}
