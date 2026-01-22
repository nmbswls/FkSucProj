using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class HomeLandmarkSpot : MonoBehaviour
    {
        public enum ESpotType
        {
            Work,       // 工作点（农田、铁匠铺）
            Worship,    // 朝拜点（神像前、教堂）
            Loiter,     // 游荡/社交点（酒馆椅子、广场长椅）
            Gate        // 出入口
        }

        [Header("配置")]
        public ESpotType Type;

        // 是否已被占用
        public bool IsOccupied { get; private set; }

        //// 当前占用者（方便调试或交互）
        //public MobNPC CurrentOccupant { get; private set; }

        //void OnEnable()
        //{
        //    // 自动注册到管理器
        //    LandmarkManager.Instance.RegisterSpot(this);
        //}

        //void OnDisable()
        //{
        //    // 自动注销（如果建筑被销毁，点位也应该消失）
        //    LandmarkManager.Instance.UnregisterSpot(this);
        //}

        //// 尝试占用该点
        //public bool TryOccupy(MobNPC npc)
        //{
        //    if (IsOccupied) return false;

        //    IsOccupied = true;
        //    CurrentOccupant = npc;
        //    return true;
        //}

        //// 释放该点
        //public void Release()
        //{
        //    IsOccupied = false;
        //    CurrentOccupant = null;
        //}

        //// 可视化调试
        //void OnDrawGizmos()
        //{
        //    Gizmos.color = IsOccupied ? Color.red : Color.green;
        //    Gizmos.DrawWireSphere(transform.position, 0.5f);

        //    // 简单的图标颜色区分
        //    switch (Type)
        //    {
        //        case ESpotType.Work: Gizmos.color = Color.yellow; break;
        //        case ESpotType.Worship: Gizmos.color = Color.magenta; break;
        //        case ESpotType.Loiter: Gizmos.color = Color.cyan; break;
        //    }
        //    Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.2f);
        //}
    }
}

