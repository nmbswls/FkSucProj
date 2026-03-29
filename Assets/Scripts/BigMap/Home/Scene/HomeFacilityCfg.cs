
using System;
using System.Collections.Generic;
using System.Linq;
using My.Config;
using My.MapExport;
using UnityEngine;

namespace My
{
    public enum EPlacementRotation { R0, R90, R180, R270 }

    [CreateAssetMenu(menuName = "GP/Build/HomeFacilityCfg")]
    public class HomeFacilityCfg : ScriptableObject
    {
        [Header("Meta")]
        public string CfgId;
        public string Name;

        public Sprite sprite;
        //public GameObject prefab; // 实际放置的 Prefab
        public Sprite previewSprite; // 预览用图标（可选）

        public bool IsFixed = false; // 固定设施 

        [Header("Footprint Config")]
        // pivot 为占格的参考点（相对左下角为 0,0）
        public Vector2Int pivot = new Vector2Int(0, 0);

        // 基础 footprint（R0），相对 pivot 的格子坐标偏移
        public List<Vector2Int> footprintR0 = new List<Vector2Int>() { new Vector2Int(0, 0) };

        // 是否在导入时预计算各旋转的 footprint
        public bool precomputeRotations = true;

        [HideInInspector] public List<Vector2Int> footprintR90;
        [HideInInspector] public List<Vector2Int> footprintR180;
        [HideInInspector] public List<Vector2Int> footprintR270;


        public enum EFacilityFuncType
        { 
            None,
        }

        [Serializable]
        public class SubFuncStruct
        {
            public int SubHandleIdx;
            public EFacilityFuncType FuncType;
            public int FuncParam1;
            public int FuncParam2;
        }

        public List<SubFuncStruct> SubFuncInfos = new();


        /// <summary>
        /// 每个放置物，都可以对应一些交互物
        /// </summary>
        [Serializable]
        public class BindingEntityInfo
        {
            public int MemberId = 0;
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }
        public List<BindingEntityInfo> BindingEntityInfoList = new();


        private void OnValidate()
        {
            if (precomputeRotations)
            {
                footprintR90 = RotateFootprint(footprintR0, EPlacementRotation.R90);
                footprintR180 = RotateFootprint(footprintR0, EPlacementRotation.R180);
                footprintR270 = RotateFootprint(footprintR0, EPlacementRotation.R270);
            }
        }

        public IEnumerable<Vector2Int> GetFootprint(EPlacementRotation rot)
        {
            if (!precomputeRotations)
            {
                return RotateFootprint(footprintR0, rot);
            }
            return rot switch
            {
                EPlacementRotation.R0 => footprintR0,
                EPlacementRotation.R90 => footprintR90,
                EPlacementRotation.R180 => footprintR180,
                EPlacementRotation.R270 => footprintR270,
                _ => footprintR0
            };
        }

        public static List<Vector2Int> RotateFootprint(IEnumerable<Vector2Int> baseOffsets, EPlacementRotation rot)
        {
            // 以 pivot 为原点的 90° 旋转矩阵
            return baseOffsets.Select(o => rot switch {
                EPlacementRotation.R0 => new Vector2Int(o.x, o.y),
                EPlacementRotation.R90 => new Vector2Int(-o.y, o.x),
                EPlacementRotation.R180 => new Vector2Int(-o.x, -o.y),
                EPlacementRotation.R270 => new Vector2Int(o.y, -o.x),
                _ => o
            }).ToList();
        }
    }
}