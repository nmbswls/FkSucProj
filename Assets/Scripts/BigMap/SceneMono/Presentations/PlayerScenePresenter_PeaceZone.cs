using System.Collections.Generic;
using My;
using My.Map.Fight;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        readonly HashSet<int> _peaceZoneInsideIds = new();
        readonly HashSet<int> _peaceZoneScanScratch = new();
        readonly List<int> _peaceZoneDiffScratch = new();

        static bool PeaceZoneCondEnabled(ZoneInfoProvider zone)
        {
            if (zone.EnableCondition == null || zone.EnableCondition.Count == 0)
            {
                return true;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return false;
            }

            for (var i = 0; i < zone.EnableCondition.Count; i++)
            {
                if (!glm.CheckCommonCond(zone.EnableCondition[i]))
                {
                    return false;
                }
            }

            return true;
        }

        void ResetPeaceZoneTracking()
        {
            _peaceZoneInsideIds.Clear();
            _peaceZoneScanScratch.Clear();
            _peaceZoneDiffScratch.Clear();
        }

        void TickPeaceZoneAtFoot()
        {
            if (PlayerEntity == null)
            {
                return;
            }

            var glm = PlayerEntity.LogicManager;
            if (glm?.playerLogicEntity == null)
            {
                return;
            }

            _peaceZoneScanScratch.Clear();
            int zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer < 0)
            {
                return;
            }

            int count = Physics2D.OverlapPointNonAlloc(
                PlayerEntity.Pos,
                zoneTriggerCache,
                1 << zoneLayer);

            for (int i = 0; i < count; i++)
            {
                var col = zoneTriggerCache[i];
                if (col == null)
                {
                    continue;
                }

                var zone = col.GetComponentInParent<ZoneInfoProvider>();
                if (zone == null || !zone.HasPeaceZone || zone.IsForbidden)
                {
                    continue;
                }

                if (!PeaceZoneCondEnabled(zone))
                {
                    continue;
                }

                _peaceZoneScanScratch.Add(zone.gameObject.GetInstanceID());
            }

            long playerId = PlayerEntity.Id;

            _peaceZoneDiffScratch.Clear();
            foreach (var zoneSourceId in _peaceZoneScanScratch)
            {
                if (!_peaceZoneInsideIds.Contains(zoneSourceId))
                {
                    _peaceZoneDiffScratch.Add(zoneSourceId);
                }
            }

            for (int i = 0; i < _peaceZoneDiffScratch.Count; i++)
            {
                PeaceCombatBuffRefresh.NotifyZonePeaceEnter(glm, playerId, _peaceZoneDiffScratch[i]);
            }

            _peaceZoneDiffScratch.Clear();
            foreach (var zoneSourceId in _peaceZoneInsideIds)
            {
                if (!_peaceZoneScanScratch.Contains(zoneSourceId))
                {
                    _peaceZoneDiffScratch.Add(zoneSourceId);
                }
            }

            for (int i = 0; i < _peaceZoneDiffScratch.Count; i++)
            {
                PeaceCombatBuffRefresh.NotifyZonePeaceExit(glm, playerId, _peaceZoneDiffScratch[i]);
            }

            _peaceZoneInsideIds.Clear();
            foreach (var zoneSourceId in _peaceZoneScanScratch)
            {
                _peaceZoneInsideIds.Add(zoneSourceId);
            }
        }
    }
}
