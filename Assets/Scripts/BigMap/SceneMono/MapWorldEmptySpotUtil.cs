using UnityEngine;

namespace My.Map.Scene
{
    // 在大世界中搜索半径 searchRadius 内第一个净空点（无 Wall/DynamicObs/MapTarget 碰撞体）
    public static class MapWorldEmptySpotUtil
    {
        private static readonly Collider2D[] _hits = new Collider2D[32];

        // 在 center 附近以环状方式搜索空地落点
        // ignoreIdA / ignoreIdB：需要跳过的实体 Id（例如被搬运 NPC 和玩家自身）
        public static bool TryFindEmptySpotNear(
            Vector2 center,
            float searchRadius,
            float clearanceRadius,
            long? ignoreIdA,
            long? ignoreIdB,
            out Vector2 spot)
        {
            spot = center;

            int wallLayer      = LayerMask.NameToLayer("Wall");
            int dynObsLayer    = LayerMask.NameToLayer("DynamicObs");
            int mapTargetLayer = LayerMask.NameToLayer("MapTarget");

            if (wallLayer < 0 || dynObsLayer < 0 || mapTargetLayer < 0)
            {
                Debug.LogError("MapWorldEmptySpotUtil: missing layer Wall / DynamicObs / MapTarget");
                return false;
            }

            // 以 step 为格间距，从中心向外按环扩展搜索
            float step = Mathf.Max(0.22f, clearanceRadius * 0.7f);
            int maxRing = Mathf.CeilToInt(searchRadius / step);

            for (int ring = 0; ring <= maxRing; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dy = -ring; dy <= ring; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring)
                        {
                            continue;
                        }

                        Vector2 cand = center + new Vector2(dx * step, dy * step);
                        if ((cand - center).sqrMagnitude > searchRadius * searchRadius)
                        {
                            continue;
                        }

                        if (IsClear(cand, clearanceRadius, wallLayer, dynObsLayer, mapTargetLayer, ignoreIdA, ignoreIdB))
                        {
                            spot = cand;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsClear(
            Vector2 pos,
            float radius,
            int wallLayer,
            int dynObsLayer,
            int mapTargetLayer,
            long? ignoreIdA,
            long? ignoreIdB)
        {
            int mask = (1 << wallLayer) | (1 << dynObsLayer) | (1 << mapTargetLayer);
            int n = Physics2D.OverlapCircleNonAlloc(pos, radius, _hits, mask);

            for (int i = 0; i < n; i++)
            {
                var col = _hits[i];
                if (col == null)
                {
                    continue;
                }

                int lyr = col.gameObject.layer;

                if (lyr == wallLayer || lyr == dynObsLayer)
                {
                    return false;
                }

                if (lyr == mapTargetLayer)
                {
                    var targettable = col.GetComponentInParent<SceneTargettable>();
                    if (targettable?.BelongPresenter != null)
                    {
                        var ent = targettable.BelongPresenter.GetLogicEntity();
                        if (ent != null
                            && ((ignoreIdA.HasValue && ent.Id == ignoreIdA.Value)
                             || (ignoreIdB.HasValue && ent.Id == ignoreIdB.Value)))
                        {
                            continue;
                        }
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
