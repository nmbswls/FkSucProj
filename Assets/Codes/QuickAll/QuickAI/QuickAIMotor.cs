//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


//public enum EMotorStyle { Normal, Ranged}
//[RequireComponent(typeof(Rigidbody2D))]
//public class QuickAIMotor : MonoBehaviour
//{
//    public QuickAIConfig config;
//    private Rigidbody2D rb;


//    private QuickNpcController NpcController;

//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        NpcController = GetComponent<QuickNpcController>();
//    }

//    public void MoveToFixPoint(Vector2 point)
//    {
//        //// 如果没有路径或已到达
//        //if (!agent.hasPath || agent.pathPending)
//        //    return;


//        //NpcController
//    }


//    public void MoveTowards(Transform target)
//    {
//        //NpcController.aiMoveDir();
//        //Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
//        //rb.velocity = dir * speed;
//        //if (dir.sqrMagnitude > 0.0001f)
//        //{
//        //    // 2D 朝向：以 transform.right 作为朝向
//        //    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//        //    rb.rotation = angle;
//        //}
//    }

//    public void Stop()
//    {
//        rb.velocity = Vector2.zero;
//    }
//}