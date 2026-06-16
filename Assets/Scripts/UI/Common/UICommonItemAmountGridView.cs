using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 固定列数 Grid，按 itemId 排序刷新通用物品格
    public class UICommonItemAmountGridView : MonoBehaviour
    {
        [SerializeField] RectTransform gridRoot;
        [SerializeField] UICommonItemAmountCell cellTemplate;
        [SerializeField] GameObject sectionRoot;
        [SerializeField] int fixedColumnCount = 4;

        readonly List<UICommonItemAmountCell> _spawned = new();

        void Awake()
        {
            gridRoot ??= transform as RectTransform;
            if (cellTemplate != null)
            {
                cellTemplate.gameObject.SetActive(false);
            }

            EnsureGridLayout();
        }

        void EnsureGridLayout()
        {
            if (gridRoot == null)
            {
                return;
            }

            var grid = gridRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, fixedColumnCount);
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        public void Refresh(IReadOnlyDictionary<string, long> outputs)
        {
            ClearSpawned();

            var hasOutput = outputs != null && outputs.Count > 0;
            if (sectionRoot != null)
            {
                sectionRoot.SetActive(hasOutput);
            }
            else
            {
                gameObject.SetActive(hasOutput);
            }

            if (!hasOutput || cellTemplate == null || gridRoot == null)
            {
                return;
            }

            var keys = new List<string>(outputs.Keys);
            keys.Sort(string.CompareOrdinal);

            foreach (var itemId in keys)
            {
                if (!outputs.TryGetValue(itemId, out var count) || count <= 0)
                {
                    continue;
                }

                var cell = Instantiate(cellTemplate, gridRoot);
                cell.gameObject.SetActive(true);
                cell.Bind(itemId, count);
                _spawned.Add(cell);
            }
        }

        void ClearSpawned()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var cell = _spawned[i];
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _spawned.Clear();
        }
    }
}
