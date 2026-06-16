using System;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My
{
    public class ZoneInfoProvider : MonoBehaviour
    {
        [Flags]
        public enum EZoneFlag
        {
            None = 0,
            Alert = 1 << 0,
            BusyZone = 1 << 1,
            TallGrass = 1 << 2,
            PeaceZone = 1 << 3,
        }

        public EZoneFlag ZoneType;

        public bool IsForbidden;
        public List<CommonCheckCond> EnableCondition = new();

        public int ZoneBusyValue;

        [Range(0f, 1f)]
        public float TallGrassCoverStrength = 1f;

        public bool HasTallGrass =>
            (ZoneType & EZoneFlag.TallGrass) != 0;

        public bool HasPeaceZone =>
            (ZoneType & EZoneFlag.PeaceZone) != 0;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (HasPeaceZone && GetComponent<ZonePeaceTrigger>() == null)
            {
                Debug.LogWarning(
                    $"[ZoneInfoProvider] Peace zone '{name}' needs ZonePeaceTrigger component.",
                    this);
            }

            if (!HasTallGrass)
            {
                return;
            }

            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                return;
            }

            collider.isTrigger = true;

            int zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer >= 0 && gameObject.layer != zoneLayer)
            {
                Debug.LogWarning(
                    $"[ZoneInfoProvider] TallGrass zone '{name}' should use Layer 'Zone'.",
                    this);
            }
        }
#endif
    }
}
