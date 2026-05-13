using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.MapExport
{
    // 由导出 Portal 路网构建巡逻折线：多点环上相邻点之间最短路拼接；边权为 0 时用两端节点世界坐标距离。
    public static class PortalPatrolPathBuilder
    {
        public static bool TryBuildCycleWorldPath(
            MapExportDatabase db,
            string patrolNetworkIdOrEmpty,
            IReadOnlyList<string> cycleNodeIds,
            List<Vector2> outWorldPoints)
        {
            outWorldPoints ??= new List<Vector2>();
            outWorldPoints.Clear();

            if (db == null || cycleNodeIds == null || cycleNodeIds.Count < 2)
            {
                return false;
            }

            var net = ResolveNetwork(db, patrolNetworkIdOrEmpty);
            if (net == null)
            {
                Debug.LogError("[PortalPatrolPathBuilder] No portal network found for patrol.");
                return false;
            }

            var graph = new PortalNetworkGraph(net);
            if (!graph.IsValid)
            {
                return false;
            }

            var segment = new List<string>();
            int n = cycleNodeIds.Count;
            for (int i = 0; i < n; i++)
            {
                var a = cycleNodeIds[i];
                var b = cycleNodeIds[(i + 1) % n];
                if (!graph.TryGetShortestNodePath(a, b, segment))
                {
                    Debug.LogError($"[PortalPatrolPathBuilder] Unreachable segment: '{a}' -> '{b}' in network '{net.NetworkId}'.");
                    return false;
                }

                AppendSegmentWorldPoints(outWorldPoints, graph, segment);
            }

            return outWorldPoints.Count > 0;
        }

        static void AppendSegmentWorldPoints(List<Vector2> acc, PortalNetworkGraph graph, List<string> nodePath)
        {
            foreach (var nidTrim in nodePath)
            {
                var nid = nidTrim;
                if (string.IsNullOrEmpty(nid))
                {
                    continue;
                }

                var p = graph.GetPosition2(nid);
                if (acc.Count > 0 && (acc[acc.Count - 1] - p).sqrMagnitude < 1e-4f)
                {
                    continue;
                }

                acc.Add(p);
            }
        }

        static PortalNetworkExport ResolveNetwork(MapExportDatabase db, string patrolNetworkIdOrEmpty)
        {
            var list = db.PortalNetworks;
            if (list == null || list.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(patrolNetworkIdOrEmpty))
            {
                foreach (var p in list)
                {
                    if (p != null && string.Equals(p.NetworkId, patrolNetworkIdOrEmpty, StringComparison.Ordinal))
                    {
                        return p;
                    }
                }

                return null;
            }

            if (list.Count == 1)
            {
                return list[0];
            }

            Debug.LogError("[PortalPatrolPathBuilder] PatrolPortalNetworkId is empty and multiple portal networks exist; specify network id.");
            return null;
        }

        sealed class PortalNetworkGraph
        {
            readonly Dictionary<string, int> _idToIndex = new(StringComparer.Ordinal);
            readonly List<string> _indexToId = new();
            readonly List<Vector3> _pos = new();
            readonly List<List<(int to, float w)>> _adj = new();

            public bool IsValid => _indexToId.Count > 0;

            public PortalNetworkGraph(PortalNetworkExport export)
            {
                if (export?.Nodes == null)
                {
                    return;
                }

                foreach (var node in export.Nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.NodeId))
                    {
                        continue;
                    }

                    if (_idToIndex.ContainsKey(node.NodeId))
                    {
                        continue;
                    }

                    int idx = _indexToId.Count;
                    _idToIndex[node.NodeId] = idx;
                    _indexToId.Add(node.NodeId);
                    _pos.Add(node.Position);
                    _adj.Add(new List<(int, float)>());
                }

                if (export.Edges == null)
                {
                    return;
                }

                void AddUndirected(string na, string nb, float w)
                {
                    if (!_idToIndex.TryGetValue(na, out int ia) || !_idToIndex.TryGetValue(nb, out int ib))
                    {
                        return;
                    }

                    if (w <= 0f)
                    {
                        w = Vector3.Distance(_pos[ia], _pos[ib]);
                    }

                    _adj[ia].Add((ib, w));
                    _adj[ib].Add((ia, w));
                }

                foreach (var e in export.Edges)
                {
                    if (e == null || string.IsNullOrEmpty(e.NodeA) || string.IsNullOrEmpty(e.NodeB))
                    {
                        continue;
                    }

                    if (string.Equals(e.NodeA, e.NodeB, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    AddUndirected(e.NodeA, e.NodeB, e.Weight);
                }
            }

            public Vector2 GetPosition2(string nodeId)
            {
                if (nodeId == null || !_idToIndex.TryGetValue(nodeId, out int i))
                {
                    return Vector2.zero;
                }

                return _pos[i];
            }

            public bool TryGetShortestNodePath(string fromId, string toId, List<string> pathNodes)
            {
                pathNodes.Clear();
                if (!_idToIndex.TryGetValue(fromId, out int s) || !_idToIndex.TryGetValue(toId, out int t))
                {
                    return false;
                }

                if (s == t)
                {
                    pathNodes.Add(_indexToId[s]);
                    return true;
                }

                int n = _indexToId.Count;
                var dist = new float[n];
                var prev = new int[n];
                var visited = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    dist[i] = float.PositiveInfinity;
                    prev[i] = -1;
                }

                dist[s] = 0f;

                for (int iter = 0; iter < n; iter++)
                {
                    int u = -1;
                    float best = float.PositiveInfinity;
                    for (int i = 0; i < n; i++)
                    {
                        if (!visited[i] && dist[i] < best)
                        {
                            best = dist[i];
                            u = i;
                        }
                    }

                    if (u < 0 || float.IsPositiveInfinity(dist[u]))
                    {
                        break;
                    }

                    visited[u] = true;
                    if (u == t)
                    {
                        break;
                    }

                    foreach (var edge in _adj[u])
                    {
                        int v = edge.to;
                        float w = edge.w;
                        float nd = dist[u] + w;
                        if (nd < dist[v])
                        {
                            dist[v] = nd;
                            prev[v] = u;
                        }
                    }
                }

                if (float.IsPositiveInfinity(dist[t]))
                {
                    return false;
                }

                int c = t;
                while (true)
                {
                    pathNodes.Add(_indexToId[c]);
                    if (c == s)
                    {
                        break;
                    }

                    c = prev[c];
                    if (c < 0)
                    {
                        pathNodes.Clear();
                        return false;
                    }
                }

                pathNodes.Reverse();
                return true;
            }
        }
    }
}
