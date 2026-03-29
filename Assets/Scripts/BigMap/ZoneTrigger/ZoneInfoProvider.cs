using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My
{

    public class ZoneInfoProvider : MonoBehaviour
    {
        public enum EZoneType
        {
            Invalid,
            Alert,
            BusyZone,
        }

        public EZoneType ZoneType;
    }
}


