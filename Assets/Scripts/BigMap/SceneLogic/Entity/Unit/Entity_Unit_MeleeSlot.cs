
using System;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map
{
    public class CompFightMeleeSlot
    {
        public BaseUnitLogicEntity UnitEntity { get; protected set; }

        [Header("Settings")]
        public int maxSlots = 8;            // 最大槽位数 (例如6个，每60度一个)
        public float slotRadius = 2.0f;


        // 内部类：定义单个槽位
        [System.Serializable]
        public class Slot
        {
            public bool isOccupied;
            public ILogicEntity occupier;      // 谁占了这个坑
            public float angleDeg;          // 角度 (0-360)
            public Vector2 currentWorldPos; // 当前计算出的世界坐标（缓存用）
        }

        private List<Slot> _slots = new List<Slot>();

        public CompFightMeleeSlot(BaseUnitLogicEntity unitEntity)
        {
            this.UnitEntity = unitEntity;
        }
        /// <summary>
        /// 初始化槽位角度
        /// </summary>
        public void InitializeSlots()
        {
            _slots.Clear();
            float angleStep = 360f / maxSlots;
            for (int i = 0; i < maxSlots; i++)
            {
                _slots.Add(new Slot
                {
                    isOccupied = false,
                    occupier = null,
                    angleDeg = i * angleStep
                });
            }
        }

        /// <summary>
        /// 更新所有槽位的世界坐标 (基于玩家当前位置)
        /// </summary>
        public void UpdateSlotPositions()
        {
            Vector2 center = UnitEntity.Pos;
            for (int i = 0; i < _slots.Count; i++)
            {
                float rad = _slots[i].angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                _slots[i].currentWorldPos = center + (dir * slotRadius);
            }
        }

        public int ReserveBestSlot(ILogicEntity requester)
        {
            int bestSlotIndex = -1;
            float minAngleDiff = float.MaxValue;

            // 计算申请者相对于玩家的角度 (2D Atan2)
            Vector2 dirToEnemy = ((Vector2)requester.Pos - (Vector2)UnitEntity.Pos).normalized;
            float enemyAngle = Mathf.Atan2(dirToEnemy.y, dirToEnemy.x) * Mathf.Rad2Deg;
            // 归一化到 0-360
            if (enemyAngle < 0) enemyAngle += 360f;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].isOccupied)
                {
                    // 使用 DeltaAngle 计算最短角度差
                    float diff = Mathf.Abs(Mathf.DeltaAngle(enemyAngle, _slots[i].angleDeg));
                    if (diff < minAngleDiff)
                    {
                        minAngleDiff = diff;
                        bestSlotIndex = i;
                    }
                }
            }

            if (bestSlotIndex != -1)
            {
                _slots[bestSlotIndex].isOccupied = true;
                _slots[bestSlotIndex].occupier = requester;
            }

            return bestSlotIndex;
        }

        public void ReleaseSlot(int index)
        {
            if (index >= 0 && index < _slots.Count)
            {
                _slots[index].isOccupied = false;
                _slots[index].occupier = null;
            }
        }

        public Vector2 GetSlotPosition(int index)
        {
            if (index >= 0 && index < _slots.Count)
            {
                return _slots[index].currentWorldPos;
            }
            return UnitEntity.Pos;
        }
    }
}