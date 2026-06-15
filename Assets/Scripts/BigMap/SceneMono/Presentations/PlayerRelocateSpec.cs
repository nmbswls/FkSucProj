using System;
using UnityEngine;

namespace My.Map.Scene
{
    // 脚本_relocate 的表现方式（与 ControlledMove 战斗位移区分）
    public enum PlayerRelocateTransitStyle
    {
        // 默认：无过渡表现，等待固定时长后 Teleport
        WaitOnly = 0,
        // 淡出 → 光点飞行 → 淡入
        GhostOrb = 1,
        // 入口 → 藤顶 → 停顿 → 跳跃落点
        VineClimb = 2,
    }

    public struct PlayerRelocateSpec
    {
        public PlayerRelocateTransitStyle TransitStyle;
        public Vector2? EntryLogicPos;
        public Vector2? MidLogicPos;
        public Vector2 FinalLogicPos;
    }

    public struct GhostRelocateTimings
    {
        public float PlayerFadeOut;
        public float OrbFadeIn;
        public float OrbMove;
        public float OrbFadeOut;
        public float PlayerFadeIn;
    }

    public static class PlayerRelocateTimings
    {
        public const float WaitOnlyDefault = 0.50f;

        public const float GhostFadeOut = 0.10f;
        public const float GhostOrbFadeIn = 0.08f;
        public const float GhostOrbMove = 0.30f;
        public const float GhostOrbFadeOut = 0.10f;
        public const float GhostPlayerFadeIn = 0.12f;

        public const float VineEntry = 0.35f;
        public const float VineClimb = 0.80f;
        public const float VinePause = 0.00f;
        public const float VineJump = 0.35f;

        public static GhostRelocateTimings Ghost => new GhostRelocateTimings
        {
            PlayerFadeOut = GhostFadeOut,
            OrbFadeIn = GhostOrbFadeIn,
            OrbMove = GhostOrbMove,
            OrbFadeOut = GhostOrbFadeOut,
            PlayerFadeIn = GhostPlayerFadeIn,
        };

        public static float GetGhostTotal()
        {
            return GhostFadeOut + GhostOrbFadeIn + GhostOrbMove + GhostOrbFadeOut + GhostPlayerFadeIn;
        }

        public static float GetTotalDuration(PlayerRelocateSpec spec)
        {
            switch (spec.TransitStyle)
            {
                case PlayerRelocateTransitStyle.WaitOnly:
                    return WaitOnlyDefault;
                case PlayerRelocateTransitStyle.GhostOrb:
                    return GetGhostTotal();
                case PlayerRelocateTransitStyle.VineClimb:
                    float total = 0f;
                    if (spec.EntryLogicPos.HasValue)
                    {
                        total += VineEntry;
                    }

                    total += VineClimb + VinePause + VineJump;
                    return total;
                default:
                    return WaitOnlyDefault;
            }
        }
    }
}
