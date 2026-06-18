using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public partial class PlayerScenePresenter
    {
        readonly HashSet<int> _dialogZoneInsideIds = new();
        readonly HashSet<int> _dialogZoneScanScratch = new();
        readonly List<DialogTriggerZone> _dialogZoneEnterScratch = new();

        void ResetDialogZoneTracking()
        {
            _dialogZoneInsideIds.Clear();
            _dialogZoneScanScratch.Clear();
            _dialogZoneEnterScratch.Clear();
        }

        void TickDialogTriggerZonesAtFoot()
        {
            if (PlayerEntity == null)
            {
                return;
            }

            int zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer < 0)
            {
                return;
            }

            var worldPos = PlayerEntity.Pos;
            _dialogZoneScanScratch.Clear();
            _dialogZoneEnterScratch.Clear();

            int count = Physics2D.OverlapPointNonAlloc(
                worldPos,
                zoneTriggerCache,
                1 << zoneLayer);

            for (int i = 0; i < count; i++)
            {
                var col = zoneTriggerCache[i];
                if (col == null)
                {
                    continue;
                }

                var zone = col.GetComponentInParent<DialogTriggerZone>();
                if (zone == null || !zone.ContainsPoint(worldPos))
                {
                    continue;
                }

                var zoneId = zone.gameObject.GetInstanceID();
                _dialogZoneScanScratch.Add(zoneId);
                if (!_dialogZoneInsideIds.Contains(zoneId))
                {
                    _dialogZoneEnterScratch.Add(zone);
                }
            }

            _dialogZoneInsideIds.Clear();
            foreach (var zoneId in _dialogZoneScanScratch)
            {
                _dialogZoneInsideIds.Add(zoneId);
            }

            for (int i = 0; i < _dialogZoneEnterScratch.Count; i++)
            {
                if (_dialogZoneEnterScratch[i].TryTriggerDialog())
                {
                    break;
                }
            }
        }
    }
}
