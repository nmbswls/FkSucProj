

using System.Collections.Generic;
using UnityEngine;

namespace My
{

    public class SceneGroundLiquidStrampManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public GameObject stampPrefab; // 挂载了SpriteRenderer的Prefab，材质必须是Additive
        public Transform liquidLayerContainer; // 专门放液体印花的父节点，Layer设为LiquidLayer

        // 对象池，避免频繁Instantiate
        private Queue<GameObject> _stampPool = new Queue<GameObject>();
        // 记录每个网格坐标对应的印花对象
        private Dictionary<Vector2Int, GameObject> _activeStamps = new Dictionary<Vector2Int, GameObject>();

        public void RegisterEvents()
        {
            MainGameManager.Instance.gameLogicManager.GroundOverManager.OnCellAdded += HandleCellAdded;
            MainGameManager.Instance.gameLogicManager.GroundOverManager.OnCellRemoved += HandleCellRemoved;
        }

        public void UnRegisterEvents()
        {
            MainGameManager.Instance.gameLogicManager.GroundOverManager.OnCellAdded -= HandleCellAdded;
            MainGameManager.Instance.gameLogicManager.GroundOverManager.OnCellRemoved -= HandleCellRemoved;
        }

        private void HandleCellAdded(Vector2Int gridPos, EGroundElementType type)
        {
            if(stampPrefab == null)
            {
                Debug.LogError("HandleCellAdded no prefab");
                return;
            }
            if (_activeStamps.ContainsKey(gridPos))
            {
                // 如果已有印花，只需改颜色（互斥覆盖）
                SetStampColor(_activeStamps[gridPos], type);
                return;
            }

            // 从对象池取印花
            GameObject stamp = _stampPool.Count > 0 ? _stampPool.Dequeue() : Instantiate(stampPrefab, liquidLayerContainer);
            stamp.SetActive(true);

            // 设置位置和颜色
            stamp.transform.position = MainGameManager.Instance.gameLogicManager.GroundOverManager.GridToWorld(gridPos);
            SetStampColor(stamp, type);

            _activeStamps.Add(gridPos, stamp);
        }

        private void HandleCellRemoved(Vector2Int gridPos)
        {
            if (_activeStamps.TryGetValue(gridPos, out GameObject stamp))
            {
                stamp.SetActive(false);
                _stampPool.Enqueue(stamp); // 回收到池
                _activeStamps.Remove(gridPos);
            }
        }

        // 根据逻辑类型映射到 RGBA 通道
        private void SetStampColor(GameObject stamp, EGroundElementType type)
        {
            SpriteRenderer sr = stamp.GetComponent<SpriteRenderer>();
            switch (type)
            {
                case EGroundElementType.GcLiquid: sr.color = new Color(1, 0, 0, 1); break; // 写入 R 通道
                case EGroundElementType.Milk: sr.color = new Color(0, 1, 0, 1); break; // 写入 G 通道
            }
        }
    }
}