using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My
{
    public partial class GameLogicManager
    {
        public long LatestPoisonLacedInteractInstId { get; private set; }

        public void RegisterLatestPoisonLacedInteract(long interactInstId)
        {
            LatestPoisonLacedInteractInstId = interactInstId;
        }

        public void ClearLatestPoisonLacedIfMatch(long interactInstId)
        {
            if (LatestPoisonLacedInteractInstId == interactInstId)
            {
                LatestPoisonLacedInteractInstId = 0;
            }
        }

        public bool TryGetPoisonBaitTargetForNpc(NpcUnitLogicEntity npc, out long interactInstId)
        {
            interactInstId = 0;
            if (npc == null || npc.MarkDestroyed)
            {
                return false;
            }

            long id = LatestPoisonLacedInteractInstId;
            if (id == 0)
            {
                return false;
            }

            var ip = GetLogicEntity(id, false) as LogicEntityInteractPoint;
            if (ip == null || ip.MarkDestroyed)
            {
                ClearLatestPoisonLacedIfMatch(id);
                return false;
            }

            if (!ip.IsPoisonBaitWindowActive())
            {
                return false;
            }

            var ps = ip.cacheCfg?.PoisonSettings;
            if (ps == null || !ps.Enable)
            {
                return false;
            }

            float r = Mathf.Max(0.5f, ps.NpcSeekRadius);
            if (Vector2.Distance(npc.Pos, ip.Pos) > r)
            {
                return false;
            }

            interactInstId = id;
            return true;
        }
    }
}
