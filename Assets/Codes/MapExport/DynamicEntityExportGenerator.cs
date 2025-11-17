using Map.Entity;
using My.Map;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;


namespace My.MapExport
{
    public class DynamicEntityExportGenerator : MonoBehaviour
    {
        public EEntityType EntityType;
        public string CfgId;

        public string BindRoomId;
        public CommonCheckCond? AppearCond;
    }
}

