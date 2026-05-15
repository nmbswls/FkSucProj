using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 挂在 ItemBar 根：用子级单个 Template（QuickSlotItemCell）克隆到 GridSlots，刷新 QuickSlot 数据。
    public class OverworldItemQuickBarController : MonoBehaviour
    {
        const int DefaultSlotCount = 5;
        const string SlotInstancePrefix = "QuickItemSlot_";

        [SerializeField]
        RectTransform _slotsParent;

        [SerializeField]
        QuickSlotItemCell _slotTemplate;

        bool _initialized;

        public void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            if (_slotsParent == null)
            {
                var t = transform.Find("GridSlots");
                if (t != null)
                {
                    _slotsParent = t as RectTransform;
                }
            }

            if (_slotTemplate == null)
            {
                var tplTr = transform.Find("ItemQuickSlotTemplate") ?? transform.Find("SkillSlotTemplate");
                if (tplTr != null)
                {
                    _slotTemplate = tplTr.GetComponent<QuickSlotItemCell>();
                }
            }

            if (_slotTemplate == null || _slotsParent == null)
            {
                Debug.LogError("OverworldItemQuickBarController: missing slot template or GridSlots. Assign on ItemBar.");
                return;
            }

            EnsureRootRaycastTarget(_slotTemplate.gameObject);
            foreach (var btn in _slotTemplate.GetComponentsInChildren<Button>(true))
            {
                btn.enabled = false;
            }

            _slotTemplate.gameObject.SetActive(false);
            _slotTemplate.EnsureQuickBarComponents();
            _slotTemplate.RebuildBehaviourCache();
            _initialized = true;
        }

        static void EnsureRootRaycastTarget(GameObject root)
        {
            var img = root.GetComponent<Image>();
            if (img == null)
            {
                img = root.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.02f);
            }

            img.raycastTarget = true;
            var rt = root.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        public void EnsureSlots()
        {
            InitializeIfNeeded();
            if (!_initialized || _slotTemplate == null || _slotsParent == null)
            {
                return;
            }

            int existing = 0;
            for (int i = 0; i < _slotsParent.childCount; i++)
            {
                var c = _slotsParent.GetChild(i);
                if (c.GetComponent<QuickSlotItemCell>() != null && c.name.StartsWith(SlotInstancePrefix, System.StringComparison.Ordinal))
                {
                    existing++;
                }
            }

            if (existing >= DefaultSlotCount)
            {
                return;
            }

            for (int i = _slotsParent.childCount - 1; i >= 0; i--)
            {
                var ch = _slotsParent.GetChild(i);
                if (ch.name.StartsWith(SlotInstancePrefix, System.StringComparison.Ordinal))
                {
                    Destroy(ch.gameObject);
                }
            }

            int layer = gameObject.layer;
            if (layer == 0)
            {
                layer = 5;
            }

            for (int i = 0; i < DefaultSlotCount; i++)
            {
                var go = Instantiate(_slotTemplate.gameObject, _slotsParent);
                go.name = SlotInstancePrefix + i;
                go.SetActive(true);
                SetLayerRecursively(go, layer);
                EnsureRootRaycastTarget(go);

                var cell = go.GetComponent<QuickSlotItemCell>();
                if (cell != null)
                {
                    var btn = go.GetComponentInChildren<Button>(true);
                    if (btn != null)
                    {
                        btn.enabled = false;
                    }

                    cell.EnsureQuickBarComponents();
                    cell.RebuildBehaviourCache();
                }
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int c = 0; c < t.childCount; c++)
            {
                SetLayerRecursively(t.GetChild(c).gameObject, layer);
            }
        }

        public void RefreshFromPlayerData()
        {
            InitializeIfNeeded();
            if (!_initialized || _slotsParent == null)
            {
                return;
            }

            if (MainGameManager.Instance?.gameLogicManager?.playerDataManager == null)
            {
                return;
            }

            for (int s = 0; s < DefaultSlotCount; s++)
            {
                var tr = _slotsParent.Find(SlotInstancePrefix + s);
                if (tr == null)
                {
                    continue;
                }

                tr.GetComponent<QuickSlotItemCell>()?.BindSlot(s);
            }
        }
    }
}
