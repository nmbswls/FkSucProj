using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuickAISensor : MonoBehaviour
{
    public QuickAIConfig config;
    public QuickAIBlackboard bb;

    public bool CanSeeTarget()
    {
        if (bb.target == null) return false;

        Vector2 dir = (bb.target.position - transform.position);
        float dist = dir.magnitude;
        if (dist > config.visionRange) return false;

        float angle = Vector2.Angle(transform.right, dir.normalized);
        if (angle > config.visionAngle * 0.5f) return false;

        // …‰œﬂ’⁄µ≤ºÏ≤‚
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dist, config.obstacleMask | config.targetMask);
        if (hit && hit.transform == bb.target) return true;

        return false;
    }

    public bool CanHearTarget()
    {
        if (bb.target == null) return false;
        float dist = Vector2.Distance(transform.position, bb.target.position);
        return dist <= config.hearingRadius;
    }
}
