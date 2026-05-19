using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My
{
    public partial class GameLogicManager
    {
        // 同时为多个交互点下过毒时需全部追踪，不设「仅最新一个」上限。
        readonly HashSet<long> _poisonLacedInteractInstIds = new HashSet<long>();
        const float NpcSeekRadius = 2.0f;

        public void RegisterPoisonLacedInteract(long interactInstId)
        {
            if (interactInstId == 0)
            {
                return;
            }

            _poisonLacedInteractInstIds.Add(interactInstId);
        }

        public void UnregisterPoisonLacedInteract(long interactInstId)
        {
            if (interactInstId == 0)
            {
                return;
            }

            _poisonLacedInteractInstIds.Remove(interactInstId);
        }

        /// <summary>
        /// 在给定半径内选一个仍有效的诱饵点（最近的优先）。
        /// </summary>
        public bool TryGetPoisonBaitTargetForNpc(NpcUnitLogicEntity npc, out long interactInstId)
        {
            interactInstId = 0;
            if (npc == null || npc.MarkDestroyed || _poisonLacedInteractInstIds.Count == 0)
            {
                return false;
            }

            long[] snapshot;
            snapshot = new long[_poisonLacedInteractInstIds.Count];
            _poisonLacedInteractInstIds.CopyTo(snapshot);

            float bestSq = float.MaxValue;
            long bestId = 0;
            List<long> staleIds = null;

            for (int i = 0; i < snapshot.Length; i++)
            {
                long id = snapshot[i];
                var ip = GetLogicEntity(id, false) as LogicEntityInteractPoint;
                if (ip == null || ip.MarkDestroyed)
                {
                    staleIds ??= new List<long>(4);
                    staleIds.Add(id);
                    continue;
                }

                if (!ip.IsPoisonBaitWindowActive())
                {
                    staleIds ??= new List<long>(4);
                    staleIds.Add(id);
                    continue;
                }

                var ps = ip.cacheCfg?.PoisonSettings;
                if (ps == null || !ps.Enable)
                {
                    continue;
                }

                float r = Mathf.Max(0.5f, NpcSeekRadius);
                float sqDist = Vector2.SqrMagnitude(npc.Pos - ip.Pos);
                if (sqDist > r * r)
                {
                    continue;
                }

                if (sqDist < bestSq)
                {
                    bestSq = sqDist;
                    bestId = id;
                }
            }

            if (staleIds != null && staleIds.Count > 0)
            {
                for (int si = 0; si < staleIds.Count; si++)
                {
                    _poisonLacedInteractInstIds.Remove(staleIds[si]);
                }
            }

            if (bestId == 0)
            {
                return false;
            }

            interactInstId = bestId;
            return true;
        }
    }
}
