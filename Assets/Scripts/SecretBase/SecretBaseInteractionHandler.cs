using System.Collections.Generic;
using UnityEngine;

namespace My.SecretBase
{
    // 仅处理 SecretBaseSceneRoot 登记的 SecretBaseInteractable 列表。
    public class SecretBaseInteractionHandler
    {
        readonly List<SecretBaseInteractable> _items = new();
        SecretBaseInteractable _hovered;

        public SecretBaseInteractionHandler(IReadOnlyList<SecretBaseInteractable> items)
        {
            _items.Clear();
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    _items.Add(items[i]);
                }
            }
        }

        public void TickHover(Vector2 worldPos)
        {
            var hit = FindTopHit(worldPos);
            if (_hovered == hit)
            {
                return;
            }

            _hovered?.SetHighlighted(false);
            _hovered = hit;
            _hovered?.SetHighlighted(true);
        }

        public void TryClick(Vector2 worldPos)
        {
            FindTopHit(worldPos)?.TryOpenPanel();
        }

        public void ClearHover()
        {
            _hovered?.SetHighlighted(false);
            _hovered = null;
        }

        SecretBaseInteractable FindTopHit(Vector2 worldPos)
        {
            SecretBaseInteractable best = null;
            var bestOrder = int.MinValue;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null || !item.isActiveAndEnabled)
                {
                    continue;
                }

                if (!item.HitTest(worldPos))
                {
                    continue;
                }

                var order = item.HitSortOrder;
                if (best == null || order > bestOrder)
                {
                    best = item;
                    bestOrder = order;
                }
            }

            return best;
        }
    }
}
