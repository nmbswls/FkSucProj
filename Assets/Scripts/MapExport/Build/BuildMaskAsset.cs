
using UnityEngine;

namespace My
{
    [CreateAssetMenu(fileName = "BuildMask", menuName = "GameData/BuildMask")]
    public class BuildMaskAsset : ScriptableObject
    {
        [Header("Grid bounds (cell-space)")]
        public int width;
        public int height;
        public int originX;
        public int originY;

        [Tooltip("Bit-packed buildable mask: 1=buildable, 0=not")]
        public byte[] buildableBits;

        [Tooltip("Bit-packed initial occupancy: 1=occupied, 0=free")]
        public byte[] occupancyBits; // ©ин╙©у
    }
}