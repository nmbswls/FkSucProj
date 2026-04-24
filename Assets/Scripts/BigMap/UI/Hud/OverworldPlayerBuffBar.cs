using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 位于 ItemBar 上方，展示玩家自身 buff 小图标（悬停见 PlayerBuffHoverTipPanel）
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class OverworldPlayerBuffBar : MonoBehaviour
    {
        public const int MaxSlots = 16;

        [SerializeField]
        private RectTransform slotContainer;

        [SerializeField]
        private GameObject slotPrefab;

        private readonly List<PlayerBuffIconSlot> _slots = new();

        private void Awake()
        {
            if (slotContainer == null)
            {
                slotContainer = transform as RectTransform;
            }

            EnsureSlots();
        }

        private void EnsureSlots()
        {
            if (_slots.Count > 0)
            {
                return;
            }

            if (slotPrefab == null)
            {
                Debug.LogError("OverworldPlayerBuffBar: slotPrefab is not assigned (use PlayerBuffSlot prefab).");
                return;
            }

            for (int i = 0; i < MaxSlots; i++)
            {
                var go = Instantiate(slotPrefab, slotContainer);
                var slot = go.GetComponent<PlayerBuffIconSlot>();
                if (slot == null)
                {
                    Debug.LogError("OverworldPlayerBuffBar: slotPrefab must have PlayerBuffIconSlot on root.");
                    Destroy(go);
                    return;
                }

                slot.gameObject.SetActive(false);
                _slots.Add(slot);
            }
        }

        public void RefreshFromPlayer()
        {
            EnsureSlots();
            var player = MainGameManager.Instance != null
                ? MainGameManager.Instance.gameLogicManager?.playerLogicEntity
                : null;
            if (player == null)
            {
                foreach (var s in _slots)
                {
                    s.ClearSlot();
                }

                return;
            }

            int writeIdx = 0;
            foreach (var kv in player.BuffContainer)
            {
                var buff = kv.Value;
                if (buff == null || buff.MarkedForRemove)
                {
                    continue;
                }

                if (buff.Def != null && buff.Def.IsHidden)
                {
                    continue;
                }

                if (writeIdx >= _slots.Count)
                {
                    break;
                }

                var sp = TryResolveBuffIcon(buff);
                _slots[writeIdx].BindBuff(buff, sp);
                writeIdx++;
            }

            for (int i = writeIdx; i < _slots.Count; i++)
            {
                _slots[i].ClearSlot();
            }
        }

        private static Sprite TryResolveBuffIcon(BuffInstance buff)
        {
            if (buff?.Def == null || string.IsNullOrEmpty(buff.Def.EffectId))
            {
                return null;
            }

            return Resources.Load<Sprite>($"Sprites/BuffIcons/{buff.Def.EffectId}");
        }
    }
}
