

using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Logic
{

    public class UniformGridIndex<TKey> where TKey : IEquatable<TKey>
    {
        private readonly float cellSize;
        private readonly Dictionary<(int x, int y), List<TKey>> cellToIds = new();
        private readonly Dictionary<TKey, (int x, int y)> idToCell = new();

        public UniformGridIndex(float cellSize) { this.cellSize = Mathf.Max(1f, cellSize); }

        public static (int x, int y) PosToCell(Vector2 p, float cellSize)
        {
            int x = Mathf.FloorToInt(p.x / cellSize);
            int y = Mathf.FloorToInt(p.y / cellSize);
            return (x, y);
        }

        public void AddOrMove(TKey id, Vector2 pos)
        {
            var cell = PosToCell(pos, cellSize);
            if (idToCell.TryGetValue(id, out var old) && old.Equals(cell)) return;

            if (idToCell.TryGetValue(id, out var oldCell))
            {
                if (cellToIds.TryGetValue(oldCell, out var lst))
                    lst.Remove(id);
            }

            idToCell[id] = cell;
            if (!cellToIds.TryGetValue(cell, out var list))
                cellToIds[cell] = list = new List<TKey>(8);
            if (!list.Contains(id)) list.Add(id);
        }

        public void Remove(TKey id)
        {
            if (idToCell.TryGetValue(id, out var cell))
            {
                if (cellToIds.TryGetValue(cell, out var lst)) lst.Remove(id);
                idToCell.Remove(id);
            }
        }

        // 简易范围查询（方形近似）
        public void Query(Vector2 center, float radius, List<TKey> result)
        {
            if (result == null)
            {
                ;
            }
            result.Clear();
            int r = Mathf.CeilToInt(radius / cellSize);
            var c0 = PosToCell(center, cellSize);
            for (int y = c0.y - r; y <= c0.y + r; y++)
                for (int x = c0.x - r; x <= c0.x + r; x++)
                {
                    if (!cellToIds.TryGetValue((x, y), out var lst)) continue;
                    foreach (var id in lst) result.Add(id);
                }
        }

        public void Clear()
        {
            cellToIds.Clear();
            idToCell.Clear();
        }
    }


    // InterestPoint：兴趣点（玩家、本地AI、相机锚点等）
    public class InterestPoint
    {
        public int Id;            // 唯一ID
        public Func<Vector3> Pos; // 实时位置获取委托
        public float LogicRadius; // 逻辑活跃半径（进入即唤醒）
        public float WarmupRadius;// 预热半径（在更远处预加载，进入Active半径更近）
    }


    public partial class GameLogicAreaManager
    {

    }
}

