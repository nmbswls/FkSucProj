using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public enum EVisualVolumeMode
    {
        AutoAabb,
        ManualConvexHull,
    }

    public struct FacingLocalVolume
    {
        public Vector2 Center;
        public Vector2 HalfExtents;
        public Vector2[] Hull;
        public EVisualVolumeMode Mode;
    }

    // 挂在 SceneUnitPresenter 上，提供面向 local 的视觉范围查询（默认 AABB，可选手动凸包）
    [DisallowMultipleComponent]
    public class UnitPresentationVisualVolume : MonoBehaviour
    {
        [SerializeField] EVisualVolumeMode mode = EVisualVolumeMode.AutoAabb;
        [SerializeField][Range(0.5f, 1f)] float boundsInset = 0.88f;
        [SerializeField] Vector2 halfExtentScale = Vector2.one;
        [SerializeField] Vector2 centerOffsetLocal;
        [SerializeField] List<Vector2> hullPointsLocal = new();

        SceneUnitPresenter _presenter;

        public EVisualVolumeMode Mode => mode;
        public float BoundsInset => boundsInset;
        public Vector2 HalfExtentScale => halfExtentScale;
        public Vector2 CenterOffsetLocal => centerOffsetLocal;
        public IReadOnlyList<Vector2> HullPointsLocal => hullPointsLocal;

        void Awake()
        {
            _presenter = GetComponent<SceneUnitPresenter>();
        }

        public bool TryGetVolume(float facingAngleDeg, out FacingLocalVolume vol)
        {
            if (mode == EVisualVolumeMode.ManualConvexHull
                && hullPointsLocal != null
                && hullPointsLocal.Count >= VisualVolumeConvexMath.MinHullPoints
                && VisualVolumeConvexMath.IsConvexCCW(hullPointsLocal))
            {
                var hull = hullPointsLocal.ToArray();
                VisualVolumeConvexMath.ComputeAabb(hull, out var center, out var half);
                vol = new FacingLocalVolume
                {
                    Mode = EVisualVolumeMode.ManualConvexHull,
                    Center = center,
                    HalfExtents = half,
                    Hull = hull,
                };
                return true;
            }

            return TryComputeAutoAabb(facingAngleDeg, out vol);
        }

        public bool TryComputeAutoAabb(float facingAngleDeg, out FacingLocalVolume vol)
        {
            var presenter = _presenter != null ? _presenter : GetComponent<SceneUnitPresenter>();
            return TryComputeSpriteAutoAabb(
                presenter,
                transform.position,
                facingAngleDeg,
                boundsInset,
                halfExtentScale,
                centerOffsetLocal,
                out vol);
        }

        public static bool TryComputeSpriteAutoAabb(
            SceneUnitPresenter presenter,
            Vector3 origin,
            float facingAngleDeg,
            float inset,
            Vector2 halfScale,
            Vector2 centerOffset,
            out FacingLocalVolume vol)
        {
            vol = default;

            if (presenter == null)
            {
                return false;
            }

            var root = presenter.AgentView != null ? presenter.AgentView : presenter.transform;
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);

            bool hasBounds = false;
            Bounds worldBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null || !sr.enabled || sr.sprite == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = sr.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(sr.bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            var invRot = Quaternion.Euler(0f, 0f, -facingAngleDeg);
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    Vector3 corner = c + new Vector3(e.x * sx, e.y * sy, 0f);
                    Vector3 rel = corner - origin;
                    Vector3 local = invRot * rel;
                    minX = Mathf.Min(minX, local.x);
                    maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }
            }

            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f) + centerOffset;
            var half = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
            half.x *= halfScale.x * inset;
            half.y *= halfScale.y * inset;

            vol = new FacingLocalVolume
            {
                Mode = EVisualVolumeMode.AutoAabb,
                Center = center,
                HalfExtents = half,
                Hull = null,
            };
            return true;
        }

        public static bool ContainsFacingLocal(Vector2 point, in FacingLocalVolume vol)
        {
            if (vol.Mode == EVisualVolumeMode.ManualConvexHull && vol.Hull != null && vol.Hull.Length >= VisualVolumeConvexMath.MinHullPoints)
            {
                return VisualVolumeConvexMath.ContainsConvexCCW(point, vol.Hull);
            }

            var d = point - vol.Center;
            return Mathf.Abs(d.x) <= vol.HalfExtents.x + 1e-5f
                && Mathf.Abs(d.y) <= vol.HalfExtents.y + 1e-5f;
        }

        public static Vector2 ClampFacingLocal(Vector2 point, in FacingLocalVolume vol)
        {
            if (vol.Mode == EVisualVolumeMode.ManualConvexHull && vol.Hull != null && vol.Hull.Length >= VisualVolumeConvexMath.MinHullPoints)
            {
                return VisualVolumeConvexMath.ClampToConvexCCW(point, vol.Hull);
            }

            var d = point - vol.Center;
            d.x = Mathf.Clamp(d.x, -vol.HalfExtents.x, vol.HalfExtents.x);
            d.y = Mathf.Clamp(d.y, -vol.HalfExtents.y, vol.HalfExtents.y);
            return vol.Center + d;
        }

        public bool SetHullPoints(IEnumerable<Vector2> points)
        {
            hullPointsLocal ??= new List<Vector2>();
            hullPointsLocal.Clear();

            if (points == null)
            {
                return false;
            }

            foreach (var p in points)
            {
                if (hullPointsLocal.Count >= VisualVolumeConvexMath.MaxHullPoints)
                {
                    break;
                }

                hullPointsLocal.Add(p);
            }

            return hullPointsLocal.Count >= VisualVolumeConvexMath.MinHullPoints
                && VisualVolumeConvexMath.IsConvexCCW(hullPointsLocal);
        }

        public bool ValidateHull(out string message)
        {
            if (hullPointsLocal == null || hullPointsLocal.Count < VisualVolumeConvexMath.MinHullPoints)
            {
                message = $"Hull needs at least {VisualVolumeConvexMath.MinHullPoints} points.";
                return false;
            }

            if (hullPointsLocal.Count > VisualVolumeConvexMath.MaxHullPoints)
            {
                message = $"Hull exceeds max {VisualVolumeConvexMath.MaxHullPoints} points.";
                return false;
            }

            if (!VisualVolumeConvexMath.IsConvexCCW(hullPointsLocal))
            {
                message = "Hull is not a valid CCW convex polygon.";
                return false;
            }

            message = "Hull is valid.";
            return true;
        }

        public void FixHullToConvex()
        {
            hullPointsLocal = VisualVolumeConvexMath.BuildConvexHullGraham(hullPointsLocal);
        }

        public bool GenerateHullFromAutoAabb(float facingAngleDeg)
        {
            if (!TryComputeAutoAabb(facingAngleDeg, out var vol))
            {
                return false;
            }

            hullPointsLocal = VisualVolumeConvexMath.BuildRectHull(vol.Center, vol.HalfExtents);
            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (hullPointsLocal == null)
            {
                hullPointsLocal = new List<Vector2>();
            }

            while (hullPointsLocal.Count > VisualVolumeConvexMath.MaxHullPoints)
            {
                hullPointsLocal.RemoveAt(hullPointsLocal.Count - 1);
            }
        }
#endif
    }
}
