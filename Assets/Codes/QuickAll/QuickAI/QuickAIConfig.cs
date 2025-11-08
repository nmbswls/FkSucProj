using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/QuickAIConfig")]
public class QuickAIConfig : ScriptableObject
{
    [Header("Perception")]
    public float visionRange = 10f;
    [Range(0, 180)] public float visionAngle = 120f;
    public float hearingRadius = 6f;
    public float loseTargetTime = 2f;
    public LayerMask obstacleMask;
    public LayerMask targetMask;

    [Header("Movement")]
    //public float moveSpeed = 3.5f;
    public float chaseSpeed = 4.5f;
    public float rotationSpeed = 720f;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public float attackWindup = 0.15f;
    public float attackWinddown = 0.15f;
    public GameObject projectilePrefab;

    public enum PatrolType { Loop, PingPong, Random }

    [Header("Patrol")]
    public PatrolType patrolType = PatrolType.Loop;
    public Transform[] waypoints;
    public float patrolWaitTime = 1.0f;
    public float randomPatrolRadius = 5f;

    [Header("Search")]
    public float searchDuration = 3f;
    public float searchRadius = 2f;
    public int searchPoints = 3;

    [Header("Control")]
    public float stunnedDuration = 0.5f;

    [Header("Stats")]
    public int maxHealth = 100;
    public bool canFlee = false;
    [Range(0f, 1f)] public float fleeHealthThreshold = 0.2f;
}
