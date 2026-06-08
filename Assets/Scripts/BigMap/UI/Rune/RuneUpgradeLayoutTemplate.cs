using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    // 挂在布局模板根节点；子节点 Slot_1、Slot_2… 为升级孔锚点
    public sealed class RuneUpgradeLayoutTemplate : MonoBehaviour
    {
        static readonly Regex SlotNamePattern = new(@"^Slot_(\d+)$", RegexOptions.Compiled);

        [SerializeField] Image baseArt;

        readonly Dictionary<int, RectTransform> _slotAnchors = new();

        public Image BaseArt => baseArt;
        public IReadOnlyDictionary<int, RectTransform> SlotAnchors => _slotAnchors;

        void Awake()
        {
            CollectSlots();
        }

        public void CollectSlots()
        {
            _slotAnchors.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                var match = SlotNamePattern.Match(child.name);
                if (!match.Success)
                {
                    continue;
                }

                if (!int.TryParse(match.Groups[1].Value, out int slotIndex) || slotIndex <= 0)
                {
                    continue;
                }

                if (_slotAnchors.ContainsKey(slotIndex))
                {
                    Debug.LogWarning($"[RuneUpgradeLayoutTemplate] Duplicate slot index: {child.name}");
                    continue;
                }

                _slotAnchors[slotIndex] = child;
            }
        }
    }
}
