using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace My.UI.BodyPart
{
    // 装备点数栏：NotchRoot 下按 cap 生成格，前 used 格标记为占用
    public sealed class GearEquipNotchBarView : MonoBehaviour
    {
        [SerializeField] Transform notchRoot;
        [SerializeField] GearEquipNotchCellView notchCellTemplate;
        [SerializeField] TextMeshProUGUI summaryText;

        readonly List<GearEquipNotchCellView> _cells = new();

        public void Refresh(int used, int cap)
        {
            used = Mathf.Max(0, used);
            cap = Mathf.Max(0, cap);

            if (summaryText != null)
            {
                summaryText.text = $"装备点数 {used}/{cap}";
            }

            EnsureCellCount(cap);

            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell == null)
                {
                    continue;
                }

                if (i >= cap)
                {
                    cell.gameObject.SetActive(false);
                    continue;
                }

                cell.gameObject.SetActive(true);
                if (i < used)
                {
                    cell.BindOccupied();
                }
                else
                {
                    cell.BindFree();
                }
            }
        }

        void EnsureCellCount(int cap)
        {
            if (notchRoot == null || notchCellTemplate == null)
            {
                return;
            }

            while (_cells.Count < cap)
            {
                var view = Instantiate(notchCellTemplate, notchRoot);
                view.gameObject.SetActive(true);
                _cells.Add(view);
            }
        }

        void OnDestroy()
        {
            ClearSpawnedExceptTemplate();
        }

        void ClearSpawnedExceptTemplate()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null && _cells[i] != notchCellTemplate)
                {
                    Destroy(_cells[i].gameObject);
                }
            }

            _cells.Clear();

            if (notchRoot == null || notchCellTemplate == null)
            {
                return;
            }

            for (int i = notchRoot.childCount - 1; i >= 0; i--)
            {
                var child = notchRoot.GetChild(i);
                if (child == notchCellTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }
    }
}
