
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class SceneGroundLiquidStrampManager : MonoBehaviour
    {
        const string LiquidMaskLayerName = "LiquidMask";

        [Header("Liquid Mask")]
        public GameObject stampPrefab;
        public Transform liquidLayerContainer;

        readonly Queue<GameObject> _stampPool = new Queue<GameObject>();
        readonly Dictionary<Vector2Int, GameObject> _activeStamps = new Dictionary<Vector2Int, GameObject>();
        readonly List<EGroundLiquidType> _scratchTypes = new List<EGroundLiquidType>(4);

        int _liquidMaskLayer = -1;

        LogicGroundLiquidManager LiquidManager =>
            MainGameManager.Instance.gameLogicManager.GroundLiquidManager;

        void Awake()
        {
            _liquidMaskLayer = LayerMask.NameToLayer(LiquidMaskLayerName);
        }

        public void RegisterEvents()
        {
            LiquidManager.OnCellTypeAdded += HandleCellTypeAdded;
            LiquidManager.OnCellTypeRemoved += HandleCellTypeRemoved;
            LiquidManager.OnCellRemoved += HandleCellRemoved;
        }

        public void UnRegisterEvents()
        {
            if (MainGameManager.Instance?.gameLogicManager?.GroundLiquidManager == null)
            {
                return;
            }

            LiquidManager.OnCellTypeAdded -= HandleCellTypeAdded;
            LiquidManager.OnCellTypeRemoved -= HandleCellTypeRemoved;
            LiquidManager.OnCellRemoved -= HandleCellRemoved;
        }

        void HandleCellTypeAdded(Vector2Int gridPos, EGroundLiquidType type)
        {
            EnsureStamp(gridPos);
            RefreshStampColor(gridPos);
        }

        void HandleCellTypeRemoved(Vector2Int gridPos, EGroundLiquidType type)
        {
            if (!_activeStamps.ContainsKey(gridPos))
            {
                return;
            }

            LiquidManager.GetActiveTypes(gridPos, _scratchTypes);
            if (_scratchTypes.Count == 0)
            {
                RecycleStamp(gridPos);
                return;
            }

            RefreshStampColor(gridPos);
        }

        void HandleCellRemoved(Vector2Int gridPos)
        {
            RecycleStamp(gridPos);
        }

        public void ClearAllStamps()
        {
            foreach (var kvp in _activeStamps)
            {
                kvp.Value.SetActive(false);
                _stampPool.Enqueue(kvp.Value);
            }

            _activeStamps.Clear();
        }

        void EnsureStamp(Vector2Int gridPos)
        {
            if (stampPrefab == null)
            {
                Debug.LogError("SceneGroundLiquidStrampManager: stampPrefab is missing");
                return;
            }

            if (liquidLayerContainer == null)
            {
                Debug.LogError("SceneGroundLiquidStrampManager: liquidLayerContainer is missing");
                return;
            }

            if (_activeStamps.ContainsKey(gridPos))
            {
                return;
            }

            GameObject stamp = _stampPool.Count > 0 ? _stampPool.Dequeue() : Instantiate(stampPrefab, liquidLayerContainer);
            if (_liquidMaskLayer >= 0)
            {
                stamp.layer = _liquidMaskLayer;
            }

            stamp.SetActive(true);
            stamp.transform.position = LiquidManager.GridToWorld(gridPos);
            _activeStamps.Add(gridPos, stamp);
        }

        void RecycleStamp(Vector2Int gridPos)
        {
            if (!_activeStamps.TryGetValue(gridPos, out GameObject stamp))
            {
                return;
            }

            stamp.SetActive(false);
            _stampPool.Enqueue(stamp);
            _activeStamps.Remove(gridPos);
        }

        void RefreshStampColor(Vector2Int gridPos)
        {
            if (!_activeStamps.TryGetValue(gridPos, out GameObject stamp))
            {
                return;
            }

            LiquidManager.GetActiveTypes(gridPos, _scratchTypes);
            var sr = stamp.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                return;
            }

            sr.color = BuildLiquidMaskColor(_scratchTypes);
        }

        // R=GcLiquid£¬G=Milk
        static Color BuildLiquidMaskColor(List<EGroundLiquidType> types)
        {
            var color = Color.black;
            for (int i = 0; i < types.Count; i++)
            {
                switch (types[i])
                {
                    case EGroundLiquidType.GcLiquid:
                        color.r = 1f;
                        break;
                    case EGroundLiquidType.Milk:
                        color.g = 1f;
                        break;
                }
            }

            color.a = color.r > 0f || color.g > 0f ? 1f : 0f;
            return color;
        }
    }
}
