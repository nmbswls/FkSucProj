using System.Collections;
using System.Collections.Generic;
using Map.Entity;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class TestPatroler : MonoBehaviour
{
    public int WayPointIdx = 0;
    public float WayPointDistance = 0;
    public List<Transform> WayPointInfos = new();

    private Vector3? currMoveDir;
    private float? currMoveDist;

    public float MoveSpeed = 0.2f;

    public void Update()
    {
        Tick(Time.time, Time.deltaTime);
    }
    public void Tick(float now, float dt)
    {
        //foreach (var uid in PatrolUnitIds)
        //{
        //    var entity = LogicManager.AreaManager.GetLogicEntiy(uid, false);
        //    if (entity != null && entity is BaseUnitLogicEntity unitEntity)
        //    {
        //        if (unitEntity.IsInBattle)
        //        {
        //            return;
        //        }
        //    }
        //}

        if (currMoveDir == null)
        {
            int currIdx = WayPointIdx;
            int nextIdx = (currIdx + 1) % this.WayPointInfos.Count;

            currMoveDir = (WayPointInfos[nextIdx].position - WayPointInfos[currIdx].position).normalized;
            currMoveDist = (WayPointInfos[nextIdx].position - WayPointInfos[currIdx].position).magnitude;
        }

        WayPointDistance += MoveSpeed * dt;


        // µÖ´ï
        if (WayPointDistance >= currMoveDist)
        {
            WayPointIdx = (WayPointIdx + 1) % this.WayPointInfos.Count;

            transform.position = WayPointInfos[WayPointIdx].position;
            WayPointDistance = 0;
            currMoveDir = null;
            currMoveDist = null;
        }
        else
        {
            transform.position = WayPointInfos[WayPointIdx].position + (currMoveDir.Value * WayPointDistance);
        }
    }
}
