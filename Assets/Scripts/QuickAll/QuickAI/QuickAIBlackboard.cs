using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickAIBlackboard
{
    public Transform target;                 // 玩家
    public Vector2 lastKnownTargetPos;       // 最后目击点
    public float timeSinceSeen;              // 与 loseTargetTime 配合
    public int currentWaypointIndex;         // 巡逻索引
    public int patrolDirection = 1;          // PingPong
    public float stateTimer;                 // 通用计时器
    public bool inAttackCooldown;
}
