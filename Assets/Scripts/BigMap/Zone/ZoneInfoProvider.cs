using System;
using System.Collections;
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
        }

        public EZoneFlag ZoneType;

        public bool IsForbidden;
        public List<CommonCheckCond> EnableCondition = new();

        public int ZoneBusyValue;
    }
}


