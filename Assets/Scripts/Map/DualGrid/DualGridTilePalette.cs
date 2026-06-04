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
                if (mask != 0)
                {
                    return GetSprite(0, randomSeed);
                }

                return null;
            }

            int validCount = 0;
            for (int i = 0; i < slot.Variants.Length; i++)
            {
                if (slot.Variants[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return mask != 0 ? GetSprite(0, randomSeed) : null;
            }

            if (validCount == 1)
            {
                for (int i = 0; i < slot.Variants.Length; i++)
                {
                    if (slot.Variants[i] != null)
                    {
                        return slot.Variants[i];
                    }
                }
            }

            int idx = Mathf.Abs(randomSeed) % validCount;
            for (int i = 0; i < slot.Variants.Length; i++)
            {
                if (slot.Variants[i] == null)
                {
                    continue;
                }

                if (idx == 0)
                {
                    return slot.Variants[i];
                }

                idx--;
            }

            return null;
        }
    }
}
