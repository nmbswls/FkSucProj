using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{

    /// <summary>
    /// 操作点 
    /// </summary>
    public class HomeActionSpot : MonoBehaviour
    {
        // 1. 融合 SpotType：定义这个点具体的行为类型
        public enum SpotType
        {
            Work,       // 产出型工作（锯木、打铁）
            Social,     // 社交/休息（坐椅子、喝酒）
            Worship,    // 宗教行为
            Queue,      // 排队点
            Gate        // 传送门/出入口
        }

        [System.Serializable]
        public class RuntimeSlot
        {
            public bool IsOccupied;
            public HomeSimpleNpc CurrentOwner;
            public Vector3 Offset;
        }

        [Header("核心配置")]
        public SpotType Type = SpotType.Work; // 这个点是干嘛的？
        public string AnimationTrigger = "Working"; // 在这里播什么动画？

        [Header("容量配置")]
        public int MaxCapacity = 1;
        public bool IsQueueMode = false;
        public float Spacing = 1.0f;

        // 运行时数据
        private List<RuntimeSlot> _slots = new List<RuntimeSlot>();

        // 所属设施（可选，如果是路边的野椅子可能没有 Facility）
        public HomeFacility ParentFacility { get; private set; }

        void Awake()
        {
            ParentFacility = GetComponentInParent<HomeFacility>();
            InitializeSlots();
        }

        void Start()
        {
            if (HomeSceneManager.Instance != null)
            {
                HomeSceneManager.Instance.RegisterGlobalSpot(this);
            }
        }

        void OnDestroy()
        {
            
        }

        void InitializeSlots()
        {
            for (int i = 0; i < MaxCapacity; i++)
            {
                _slots.Add(new RuntimeSlot
                {
                    IsOccupied = false,
                    Offset = IsQueueMode ? -transform.forward * (i * Spacing) : Vector3.zero
                });
            }
        }

        /// <summary>
        /// 尝试获取一个空槽位
        /// </summary>
        /// <returns>返回槽位索引，-1表示满了</returns>
        public int TryGetFreeSlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsOccupied) return i;
            }
            return -1;
        }

        /// <summary>
        /// 占用槽位
        /// </summary>
        public Vector3 OccupySlot(int index, HomeSimpleNpc npc)
        {
            if (index < 0 || index >= _slots.Count) return Vector3.zero;

            _slots[index].IsOccupied = true;
            _slots[index].CurrentOwner = npc;

            // 返回该槽位的世界坐标
            return transform.position + transform.rotation * _slots[index].Offset;
        }

        /// <summary>
        /// 释放槽位
        /// </summary>
        public void ReleaseSlot(int index)
        {
            if (index < 0 || index >= _slots.Count) return;
            _slots[index].IsOccupied = false;
            _slots[index].CurrentOwner = null;
        }

        public float QueueSpacing = 1f;

        // 可视化调试
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < MaxCapacity; i++)
            {
                Vector3 offset = IsQueueMode ? -transform.forward * (i * QueueSpacing) : Vector3.zero;
                Vector3 pos = transform.position + transform.rotation * offset;
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }
    }
}

