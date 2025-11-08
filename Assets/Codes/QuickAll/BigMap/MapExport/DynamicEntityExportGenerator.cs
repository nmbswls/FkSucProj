using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChunkMapExportDatabase;


public class DynamicEntityExportGenerator : MonoBehaviour
{
    public EEntityType EntityType;
    public string CfgId;

    public string BindRoomId;
    public DynamicEntityAppearCond? AppearCond;
}