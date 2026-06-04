using System;
using UnityEngine;

namespace My.Map.DualGrid
{
    [CreateAssetMenu(fileName = "DualGridTilePalette", menuName = "Map/Dual Grid/Palette", order = 2)]
    public class DualGridTilePalette : ScriptableObject
    {
        public const int SlotCount = DualGridCore.CornerMaskCount;

        [Serializable]
        public class CornerSlot
        {
            public Sprite[] Variants = Array.Empty<Sprite>();
        }

        public byte TerrainId;
        public CornerSlot[] Corners = new CornerSlot[SlotCount];

        void OnEnable()
        {
            EnsureCornerSlots();
        }

        void EnsureCornerSlots()
        {
            if (Corners == null || Corners.Length != SlotCount)
            {
                var old = Corners;
                Corners = new CornerSlot[SlotCount];
                if (old != null)
                {
                    for (int i = 0; i < Mathf.Min(old.Length, SlotCount); i++)
                    {
                        Corners[i] = old[i];
                    }
                }
            }

            for (int i = 0; i < SlotCount; i++)
            {
                Corners[i] ??= new CornerSlot();
            }
        }

        public Sprite GetSprite(int mask, int randomSeed)
        {
            EnsureCornerSlots();
            mask = Mathf.Clamp(mask, 0, SlotCount - 1);
            var slot = Corners[mask];
            if (slot?.Variants == null || slot.Variants.Length == 0)
            {
                return null;
            }

            if (slot.Variants.Length == 1)
            {
                return slot.Variants[0];
            }

            int idx = Mathf.Abs(randomSeed) % slot.Variants.Length;
            return slot.Variants[idx];
        }
    }
}
