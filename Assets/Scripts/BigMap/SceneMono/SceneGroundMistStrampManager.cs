
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class SceneGroundMistStrampManager : MonoBehaviour
    {
        const string MistMaskLayerName = "MistMask";

        [Header("Mist Mask")]
        public GameObject stampPrefab;
        public Transform mistLayerContainer;

        readonly Queue<GameObject> _stampPool = new Queue<GameObject>();
        readonly Dictionary<Vector2Int, GameObject> _activeStamps = new Dictionary<Vector2Int, GameObject>();
        readonly List<EGroundMistType> _scratchTypes = new List<EGroundMistType>(2);

        int _mistMaskLayer = -1;

        LogicGroundMistManager MistManager =>
            MainGameManager.Instance.gameLogicManager.GroundMistManager;

        void Awake()
        {
            _mistMaskLayer = LayerMask.NameToLayer(MistMaskLayerName);
            EnsureMistLayerContainer();
        }

        public void RegisterEvents()
        {
            MistManager.OnCellTypeAdded += HandleCellTypeAdded;
            MistManager.OnCellTypeRemoved += HandleCellTypeRemoved;
            MistManager.OnCellRemoved += HandleCellRemoved;
        }

        public void UnRegisterEvents()
        {
            if (MainGameManager.Instance?.gameLogicManager?.GroundMistManager == null)
            {
                return;
            }

            MistManager.OnCellTypeAdded -= HandleCellTypeAdded;
            MistManager.OnCellTypeRemoved -= HandleCellTypeRemoved;
            MistManager.OnCellRemoved -= HandleCellRemoved;
        }

        void HandleCellTypeAdded(Vector2Int gridPos, EGroundMistType type)
        {
            EnsureStamp(gridPos);
            RefreshStampColor(gridPos);
        }

        void HandleCellTypeRemoved(Vector2Int gridPos, EGroundMistType type)
        {
            if (!_activeStamps.ContainsKey(gridPos))
            {
                return;
            }

            MistManager.GetActiveTypes(gridPos, _scratchTypes);
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

        void EnsureMistLayerContainer()
        {
            if (mistLayerContainer != null)
            {
                return;
            }

            var go = new GameObject("MistLayer");
            go.transform.SetParent(transform, false);
            mistLayerContainer = go.transform;
        }

        void EnsureStamp(Vector2Int gridPos)
        {
            if (stampPrefab == null)
            {
                Debug.LogError("SceneGroundMistStrampManager: stampPrefab is missing");
                return;
            }

            if (mistLayerContainer == null)
            {
                Debug.LogError("SceneGroundMistStrampManager: mistLayerContainer is missing");
                return;
            }

            if (_activeStamps.ContainsKey(gridPos))
            {
                return;
            }

            GameObject stamp = _stampPool.Count > 0 ? _stampPool.Dequeue() : Instantiate(stampPrefab, mistLayerContainer);
            if (_mistMaskLayer >= 0)
            {
                stamp.layer = _mistMaskLayer;
            }

            stamp.SetActive(true);
            stamp.transform.position = MistManager.GridToWorld(gridPos);
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

            var sr = stamp.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                return;
            }

            sr.color = MistManager.HasActiveType(gridPos, EGroundMistType.PinkMist) ? Color.white : Color.clear;
        }
    }
}
