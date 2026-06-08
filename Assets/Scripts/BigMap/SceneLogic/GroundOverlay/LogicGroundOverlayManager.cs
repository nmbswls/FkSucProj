
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public enum EGroundLiquidType
    {
        None = 0,
        GcLiquid = 1,
        Milk = 2,
    }

    public enum EGroundMistType
    {
        None = 0,
        PinkMist = 1,
    }

    class GroundOverlayElementSlot
    {
        public int RefCount;
        public float ExpireTime;

        public bool IsPresent(float now)
        {
            return RefCount > 0 || now < ExpireTime;
        }

        public void Clear()
        {
            RefCount = 0;
            ExpireTime = 0f;
        }
    }

    class GroundOverlayCellData<TEnum> where TEnum : struct, Enum
    {
        readonly GroundOverlayElementSlot[] _slots;
        readonly TEnum[] _activeTypes;

        public GroundOverlayCellData(TEnum[] activeTypes)
        {
            _activeTypes = activeTypes;
            int maxIndex = 0;
            for (int i = 0; i < activeTypes.Length; i++)
            {
                maxIndex = Math.Max(maxIndex, Convert.ToInt32(activeTypes[i]));
            }

            _slots = new GroundOverlayElementSlot[maxIndex + 1];
        }

        public GroundOverlayElementSlot GetSlot(TEnum type)
        {
            int idx = Convert.ToInt32(type);
            if (idx <= 0 || idx >= _slots.Length)
            {
                return null;
            }

            return _slots[idx] ??= new GroundOverlayElementSlot();
        }

        public bool IsEmpty(float now)
        {
            for (int i = 0; i < _activeTypes.Length; i++)
            {
                var slot = GetSlot(_activeTypes[i]);
                if (slot != null && slot.IsPresent(now))
                {
                    return false;
                }
            }

            return true;
        }

        public void CollectActiveTypes(List<TEnum> result, float now)
        {
            result.Clear();
            for (int i = 0; i < _activeTypes.Length; i++)
            {
                var type = _activeTypes[i];
                var slot = GetSlot(type);
                if (slot != null && slot.IsPresent(now))
                {
                    result.Add(type);
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i]?.Clear();
            }
        }
    }

    public abstract class LogicGroundOverlayGridManager<TEnum> where TEnum : struct, Enum
    {
        protected GameLogicManager logicManager { get; private set; }

        protected abstract TEnum[] ActiveTypes { get; }
        protected abstract TEnum NoneType { get; }

        readonly Dictionary<Vector2Int, GroundOverlayCellData<TEnum>> _gridData =
            new Dictionary<Vector2Int, GroundOverlayCellData<TEnum>>();
        readonly List<TEnum> _scratchTypes = new List<TEnum>(4);
        readonly List<TEnum> _expiredTypes = new List<TEnum>(4);

        const float GridSize = 0.2f;
        const float SourceFadeDelaySeconds = 2f;

        public Action<Vector2Int, TEnum> OnCellTypeAdded;
        public Action<Vector2Int, TEnum> OnCellTypeRemoved;
        public Action<Vector2Int> OnCellRemoved;

        protected LogicGroundOverlayGridManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }

        public void Tick()
        {
            ProcessExpirations();
        }

        void ProcessExpirations()
        {
            float now = Time.time;
            var emptyCells = new List<Vector2Int>();

            foreach (var kvp in _gridData)
            {
                var cell = kvp.Value;
                _expiredTypes.Clear();

                for (int i = 0; i < ActiveTypes.Length; i++)
                {
                    var type = ActiveTypes[i];
                    var slot = cell.GetSlot(type);
                    if (slot == null)
                    {
                        continue;
                    }

                    if (slot.RefCount <= 0 && slot.ExpireTime > 0f && now >= slot.ExpireTime)
                    {
                        slot.Clear();
                        _expiredTypes.Add(type);
                    }
                }

                for (int i = 0; i < _expiredTypes.Count; i++)
                {
                    OnCellTypeRemoved?.Invoke(kvp.Key, _expiredTypes[i]);
                }

                if (cell.IsEmpty(now))
                {
                    emptyCells.Add(kvp.Key);
                }
            }

            for (int i = 0; i < emptyCells.Count; i++)
            {
                RemoveCellImmediate(emptyCells[i]);
            }
        }

        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / GridSize),
                Mathf.FloorToInt(worldPos.y / GridSize)
            );
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * GridSize + (GridSize / 2f), gridPos.y * GridSize + (GridSize / 2f), 0);
        }

        public void GetActiveTypes(Vector2Int gridPos, List<TEnum> result)
        {
            result.Clear();
            if (_gridData.TryGetValue(gridPos, out var cell))
            {
                cell.CollectActiveTypes(result, Time.time);
            }
        }

        public bool HasActiveType(Vector2Int gridPos, TEnum type)
        {
            if (!_gridData.TryGetValue(gridPos, out var cell))
            {
                return false;
            }

            var slot = cell.GetSlot(type);
            return slot != null && slot.IsPresent(Time.time);
        }

        public bool TryFindNearestActivePosition(Vector2 center, float radius, out Vector2 nearestPos)
        {
            nearestPos = center;
            float bestSqrDist = float.MaxValue;
            bool found = false;
            float now = Time.time;
            float sqrRadius = radius * radius;

            foreach (var kvp in _gridData)
            {
                var cell = kvp.Value;
                _scratchTypes.Clear();
                cell.CollectActiveTypes(_scratchTypes, now);
                if (_scratchTypes.Count == 0)
                {
                    continue;
                }

                Vector3 cellWorld = GridToWorld(kvp.Key);
                float dx = cellWorld.x - center.x;
                float dy = cellWorld.y - center.y;
                float sqrDist = dx * dx + dy * dy;
                if (sqrDist > sqrRadius)
                {
                    continue;
                }

                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    nearestPos = new Vector2(cellWorld.x, cellWorld.y);
                    found = true;
                }
            }

            return found;
        }

        public void AddElement(Vector2Int gridPos, TEnum type, float duration)
        {
            if (Convert.ToInt32(type) == Convert.ToInt32(NoneType))
            {
                return;
            }

            if (!_gridData.TryGetValue(gridPos, out GroundOverlayCellData<TEnum> cell))
            {
                cell = new GroundOverlayCellData<TEnum>(ActiveTypes);
                _gridData[gridPos] = cell;
            }

            var slot = cell.GetSlot(type);
            bool wasPresent = slot.IsPresent(Time.time);

            if (duration < 0f)
            {
                slot.RefCount++;
            }
            else
            {
                slot.ExpireTime = Mathf.Max(slot.ExpireTime, Time.time + duration);
            }

            if (!wasPresent)
            {
                OnCellTypeAdded?.Invoke(gridPos, type);
            }
        }

        public void AddElementCircle(Vector2 worldCenter, float radius, TEnum type, float duration)
        {
            // 圆心对齐格心，与 stamp 落点同一套网格相位，避免半格偏移导致占格形状畸变
            Vector2Int centerGrid = WorldToGrid(worldCenter);
            Vector2 circleCenter = GridToWorld(centerGrid);

            float sqrRadius = radius * radius;

            Vector2Int minGrid = WorldToGrid(new Vector2(circleCenter.x - radius, circleCenter.y - radius));
            Vector2Int maxGrid = WorldToGrid(new Vector2(circleCenter.x + radius, circleCenter.y + radius));

            minGrid.x -= 1;
            minGrid.y -= 1;
            maxGrid.x += 1;
            maxGrid.y += 1;

            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    Vector2Int currentGridPos = new Vector2Int(x, y);
                    Vector3 cellWorldPos = GridToWorld(currentGridPos);

                    float dx = cellWorldPos.x - circleCenter.x;
                    float dy = cellWorldPos.y - circleCenter.y;
                    if ((dx * dx + dy * dy) <= sqrRadius)
                    {
                        AddElement(currentGridPos, type, duration);
                    }
                }
            }
        }

        public HashSet<TEnum> CheckAllUnderUnit(Vector3 unitPos, float unitRadius)
        {
            var touchedTypes = new HashSet<TEnum>();
            float now = Time.time;

            Vector2Int minGrid = WorldToGrid(new Vector3(unitPos.x - unitRadius, unitPos.y - unitRadius, 0));
            Vector2Int maxGrid = WorldToGrid(new Vector3(unitPos.x + unitRadius, unitPos.y + unitRadius, 0));

            float sqrRadius = unitRadius * unitRadius;
            float halfGrid = GridSize / 2f;

            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);
                    if (!_gridData.TryGetValue(currentPos, out GroundOverlayCellData<TEnum> cell))
                    {
                        continue;
                    }

                    _scratchTypes.Clear();
                    cell.CollectActiveTypes(_scratchTypes, now);
                    if (_scratchTypes.Count == 0)
                    {
                        continue;
                    }

                    Vector3 cellCenter = GridToWorld(currentPos);
                    float closestX = Mathf.Clamp(unitPos.x, cellCenter.x - halfGrid, cellCenter.x + halfGrid);
                    float closestY = Mathf.Clamp(unitPos.y, cellCenter.y - halfGrid, cellCenter.y + halfGrid);
                    float dx = unitPos.x - closestX;
                    float dy = unitPos.y - closestY;
                    if ((dx * dx + dy * dy) > sqrRadius)
                    {
                        continue;
                    }

                    for (int i = 0; i < _scratchTypes.Count; i++)
                    {
                        touchedTypes.Add(_scratchTypes[i]);
                    }
                }
            }

            return touchedTypes;
        }

        public void RemoveElementSource(Vector2Int gridPos, TEnum type)
        {
            if (Convert.ToInt32(type) == Convert.ToInt32(NoneType))
            {
                return;
            }

            if (!_gridData.TryGetValue(gridPos, out GroundOverlayCellData<TEnum> cell))
            {
                return;
            }

            var slot = cell.GetSlot(type);
            if (slot == null || !slot.IsPresent(Time.time))
            {
                return;
            }

            if (slot.RefCount > 0)
            {
                slot.RefCount--;
            }

            if (slot.RefCount <= 0)
            {
                slot.ExpireTime = Time.time + SourceFadeDelaySeconds;
            }
        }

        public void ClearAll()
        {
            _gridData.Clear();
        }

        void RemoveCellImmediate(Vector2Int gridPos)
        {
            _gridData.Remove(gridPos);
            OnCellRemoved?.Invoke(gridPos);
        }
    }

    public class LogicGroundLiquidManager : LogicGroundOverlayGridManager<EGroundLiquidType>
    {
        static readonly EGroundLiquidType[] s_activeTypes =
        {
            EGroundLiquidType.GcLiquid,
            EGroundLiquidType.Milk,
        };

        LogicGroundLiquidFieldManager _fieldManager;

        protected override EGroundLiquidType[] ActiveTypes => s_activeTypes;
        protected override EGroundLiquidType NoneType => EGroundLiquidType.None;

        public LogicGroundLiquidManager(GameLogicManager logicManager) : base(logicManager)
        {
        }

        internal void BindFieldManager(LogicGroundLiquidFieldManager fieldManager)
        {
            _fieldManager = fieldManager;
        }

        public new void AddElementCircle(Vector2 worldCenter, float radius, EGroundLiquidType type, float duration)
        {
            _fieldManager?.AddElementCircle(worldCenter, radius, type, duration);
        }

        public HashSet<EGroundLiquidType> CheckAllLiquidsUnderUnit(Vector3 unitPos, float unitRadius)
        {
            if (_fieldManager != null)
            {
                return _fieldManager.CheckAllLiquidsUnderUnit(unitPos, unitRadius);
            }

            return CheckAllUnderUnit(unitPos, unitRadius);
        }

        public new void ClearAll()
        {
            _fieldManager?.ClearAll();
            base.ClearAll();
        }
    }

    public class LogicGroundMistManager : LogicGroundOverlayGridManager<EGroundMistType>
    {
        static readonly EGroundMistType[] s_activeTypes =
        {
            EGroundMistType.PinkMist,
        };

        protected override EGroundMistType[] ActiveTypes => s_activeTypes;
        protected override EGroundMistType NoneType => EGroundMistType.None;

        public LogicGroundMistManager(GameLogicManager logicManager) : base(logicManager)
        {
        }

        public HashSet<EGroundMistType> CheckAllMistsUnderUnit(Vector3 unitPos, float unitRadius)
        {
            return CheckAllUnderUnit(unitPos, unitRadius);
        }
    }
}
