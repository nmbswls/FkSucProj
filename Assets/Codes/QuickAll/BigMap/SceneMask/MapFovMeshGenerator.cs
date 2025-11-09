using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;


namespace Map.Scene.Fov
{
    //[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MapFovMeshGenerator : MonoBehaviour
    {
        public static class Geo2D
        {
            // 射线(起点 p, 方向 dir) 与 线段 (a,b) 相交，返回是否相交与交点
            public static bool RaySegmentIntersection(Vector2 p, Vector2 dir, Vector2 a, Vector2 b, out Vector2 hit, out float tRay)
            {
                hit = default;
                tRay = float.PositiveInfinity;
                Vector2 v1 = p - a;
                Vector2 v2 = b - a;
                float cross = Cross(dir, v2);
                const float eps = 1e-7f;
                if (Mathf.Abs(cross) < eps) return false; // 平行或共线

                float t = Cross(v2, v1) / cross; // 射线上参数
                float u = Cross(dir, v1) / cross; // 线段上的参数 [0,1]
                if (t >= 0f && u >= 0f && u <= 1f)
                {
                    hit = p + dir * t;
                    tRay = t;
                    return true;
                }
                return false;
            }

            public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

            public static float AngleOf(Vector2 v)
            {
                return Mathf.Atan2(v.y, v.x);
            }

            public static Vector2 DirFromAngle(float ang)
            {
                return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            }
        }

        

        [Header("Sampling")]
        public float epsilonAngle = 0.0005f; // ε角度微偏移
        public int uniformSamples = 64;      // FOV 内的均匀采样数量（用于补空域）
        public bool includeVertexDirections = true;

        [Header("Index")]
        public float gridCellSize = 2f;

        private Mesh mesh;
        private HashSet<int> candidateSet = new HashSet<int>();
        private List<Vector2> points = new List<Vector2>();
        private List<float> angles = new List<float>();


        public ObstacleSegmentProvider segmentProvider;
        public bool NeedMask = true;

        public float orientationDegrees = 0f; // 主角朝向(世界角度)
        private Vector2? lastPosUpdateVal = null;
        private float? lastAngleUpdateVal = null;
        public float angleUpdateInterval = 5f;
        public float posUpdateInterval = 0.01f;

        public GameObject CircleFovShape;
        public MeshFilter FovShapeMesh;
        public Image SceneMask;

        public float viewRadius = 8f;
        public float fovDegrees = 90f;

        float GetCurrentQuantiedDeg()
        {
            var quatiedDeg = (int)(orientationDegrees / angleUpdateInterval);
            return quatiedDeg * angleUpdateInterval;
        }

        Vector2 GetCurrentQuantiedPos()
        {
            var point = MainGameManager.Instance.playerScenePresenter.transform;
            var quanted = new Vector2((int)(point.position.x / posUpdateInterval), (int)(point.position.y / posUpdateInterval));
            //return quanted * posUpdateInterval;
            return point.position;
        }


        void Awake()
        {
            mesh = new Mesh { name = "MapFovMesh" };
            FovShapeMesh.sharedMesh = mesh;
        }

        private void Update()
        {
            //transform.position = MainGameManager.Instance.playerScenePresenter.ViewPoint.position;
        }
        public void OnAreaEnter()
        {
            segmentProvider = WorldAreaManager.Instance.SegmentProvider;

            fovDegrees = MainGameManager.Instance.playerScenePresenter.PlayerEntity.fovDegrees;
            viewRadius = MainGameManager.Instance.playerScenePresenter.PlayerEntity.viewRadius;
        }

        void LateUpdate()
        {
            var playerFaceDir = MainGameManager.Instance.playerScenePresenter.PlayerEntity.FaceDir;
            float angle = Mathf.Atan2(playerFaceDir.y, playerFaceDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
            orientationDegrees = angle;
            bool needUpdate = false;
            var currQuantiedAngle = GetCurrentQuantiedDeg();
            if (lastAngleUpdateVal == null || lastAngleUpdateVal.Value != currQuantiedAngle)
            {
                needUpdate = true;
            }

            var currQuantiedPos = GetCurrentQuantiedPos();
            if (lastPosUpdateVal == null || currQuantiedPos != lastPosUpdateVal.Value)
            {
                needUpdate = true;
            }

            if (needUpdate)
            {
                ComputeAndRender();
            }

            if(!NeedMask || segmentProvider == null)
            {
                SceneMask.gameObject.SetActive(false);
                //CircleFovShape.SetActive(false);
            }
            else
            {
                SceneMask.gameObject.SetActive(true);
                //CircleFovShape.SetActive(true);
            }
        }

        void ComputeAndRender()
        {
            if (!NeedMask) return;
            if (segmentProvider == null) return;

            Vector2 P = MainGameManager.Instance.playerScenePresenter.transform.position;
            
            float fov = Mathf.Max(1f, fovDegrees) * Mathf.Deg2Rad;

            //orientationDegrees = Mathf.Repeat(orientationDegrees, 360f);
            orientationDegrees = (int)(orientationDegrees / 2) * 2;

            float theta = orientationDegrees * Mathf.Deg2Rad;
            float left = theta - fov * 0.5f;
            float right = theta + fov * 0.5f;

            // 收集方向
            var dirs = new List<float>(uniformSamples + 64);
            dirs.Add(left);
            dirs.Add(right);

            // 均匀采样
            for (int i = 1; i < uniformSamples - 1; i++)
            {
                float a = Mathf.Lerp(left, right, i / (float)(uniformSamples - 1));
                dirs.Add(a);
            }

            // 相关顶点方向
            if (includeVertexDirections)
            {
                candidateSet.Clear();
                segmentProvider.QueryCircle(P, viewRadius, candidateSet); // 仅圈内候选
                foreach (int idx in candidateSet)
                {
                    var seg = segmentProvider.GetSegment(idx);
                    AddVertexIfInFov(P, left, right, seg.a, dirs);
                    AddVertexIfInFov(P, left, right, seg.b, dirs);
                }

                //foreach (var seg in segmentProvider.segments)
                //{
                //    AddVertexIfInFov(P, left, right, seg.a, dirs);
                //    AddVertexIfInFov(P, left, right, seg.b, dirs);
                //}
            }

            // 去重并排序
            //dirs.Sort();
            SortUnwrapped(dirs);

            CompactAngles(dirs, epsilonAngle * 0.5f);

            // 对每个方向做 ε 微偏移与射线测试
            points.Clear();
            angles.Clear();

            foreach (var a in dirs)
            {
                SampleDirection(P, a, ref points, ref angles);
                SampleDirection(P, a - epsilonAngle, ref points, ref angles);
                SampleDirection(P, a + epsilonAngle, ref points, ref angles);
            }

            // 角度排序（相对于 P）
            var idxs = new int[points.Count];
            for (int i = 0; i < idxs.Length; i++) idxs[i] = i;
            Array.Sort(idxs, (i, j) => angles[i].CompareTo(angles[j]));

            // 构建三角扇 Mesh
            BuildTriangleFanMesh(P, points, idxs);
        }

        void SortUnwrapped(List<float> angles)
        {
            if (angles.Count == 0) return;
            float baseA = angles[0];
            for (int i = 0; i < angles.Count; i++)
            {
                float a = angles[i];
                // 展开到相对 baseA 的连续域
                float d = a - baseA;
                while (d > Mathf.PI) d -= 2f * Mathf.PI;
                while (d < -Mathf.PI) d += 2f * Mathf.PI;
                angles[i] = baseA + d;
            }
            angles.Sort();
        }


        void AddVertexIfInFov(Vector2 P, float left, float right, Vector2 v, List<float> dirs)
        {
            Vector2 d = v - P;
            if (d.sqrMagnitude < 1e-8f) return;
            float a = Geo2D.AngleOf(d);
            // 处理角度在 [left, right]，考虑跨越 -PI/PI 的情况
            if (AngleInInterval(a, left, right))
            {
                dirs.Add(a);
            }
        }

        static float NormalizeAngleRad(float a)
        {
            // 规范到 (-π, π]
            if (a > Mathf.PI) a -= 2f * Mathf.PI;
            if (a <= -Mathf.PI) a += 2f * Mathf.PI;
            return a;
        }

        bool AngleInInterval(float a, float left, float right)
        {
            a = NormalizeAngleRad(a);
            left = NormalizeAngleRad(left);
            right = NormalizeAngleRad(right);

            if (left <= right)
            {
                return a >= left && a <= right;
            }
            else
            {
                // 区间跨越 -π/π，落在 [left, π] 或 [-π, right]
                return a >= left || a <= right;
            }
        }

        void CompactAngles(List<float> angles, float minDelta)
        {
            if (angles.Count == 0) return;
            var res = new List<float>(angles.Count);
            float last = float.NegativeInfinity;
            foreach (var a in angles)
            {
                if (res.Count == 0 || Mathf.Abs(a - last) > minDelta)
                {
                    res.Add(a);
                    last = a;
                }
            }
            angles.Clear();
            angles.AddRange(res);
        }

        void SampleDirection(Vector2 P, float angle, ref List<Vector2> pts, ref List<float> angs)
        {
            Vector2 dir = Geo2D.DirFromAngle(angle).normalized;
            Vector2 bestHit = P + dir * viewRadius;
            float bestT = viewRadius;

            // 候选边索引
            segmentProvider.QueryRay(P, dir, viewRadius, candidateSet);

            foreach (int idx in candidateSet)
            {
                var seg = segmentProvider.GetSegment(idx);
                if (!RayBoundsPossible(P, dir, seg.bounds, viewRadius)) continue;

                if (Geo2D.RaySegmentIntersection(P, dir, seg.a, seg.b, out var hit, out var t))
                {
                    if (t < bestT && t <= viewRadius + 1e-6f)
                    {
                        bestT = t;
                        bestHit = hit;
                    }
                }
            }

            pts.Add(bestHit);
            angs.Add(angle);
        }

        bool RayBoundsPossible(Vector2 p, Vector2 dir, Bounds b, float maxDist)
        {
            // 粗略剔除：起点到包围盒的最近点距离是否在 maxDist 以内
            Vector2 c = b.ClosestPoint(p);
            return (c - p).sqrMagnitude <= (maxDist * maxDist + 1e-4f);
        }

        //void BuildTriangleFanMesh(Vector2 center, List<Vector2> pts, int[] order)
        //{
        //    int n = order.Length;
        //    if (n < 2)
        //    {
        //        mesh.Clear();
        //        return;
        //    }

        //    // 顶点：中心 + 有序边界
        //    var vertices = new Vector3[n + 1];
        //    vertices[0] = new Vector3(center.x, center.y, 0f);
        //    for (int i = 0; i < n; i++)
        //    {
        //        var p = pts[order[i]];
        //        vertices[i + 1] = new Vector3(p.x, p.y, 0f);
        //    }

        //    // 三角索引
        //    var triangles = new int[(n - 1) * 3];
        //    for (int i = 0; i < n - 1; i++)
        //    {
        //        triangles[i * 3 + 0] = 0;
        //        triangles[i * 3 + 1] = i + 1;
        //        triangles[i * 3 + 2] = i + 2;
        //    }

        //    // UV 可选（用于渐变/着色）
        //    var uvs = new Vector2[n + 1];
        //    for (int i = 0; i < n + 1; i++) uvs[i] = Vector2.zero;

        //    mesh.Clear();
        //    mesh.SetVertices(vertices);
        //    mesh.SetTriangles(triangles, 0);
        //    mesh.SetUVs(0, uvs);
        //    mesh.RecalculateBounds();
        //    mesh.RecalculateNormals(); // 2D 可忽略
        //}

        //public float blurWidth = 0.5f;   // 边缘向内收的宽度（世界单位）
        //public float innerAlpha = 0.3f;  // 边缘带内侧最低透明度（0~1）
        //public Material softEdgeMat;     // 使用下方 Shader

        void BuildTriangleFanMesh(Vector2 centerWorld2D, List<Vector2> ptsWorld2D, int[] order)
        {
            int n = order.Length;
            if (n < 2) { mesh.Clear(); return; }

            // 将 2D 世界点提升为 3D 世界点（假设在 XY 平面，z 用物体的 z）
            float z = transform.position.z;

            var vertices = new Vector3[n + 1];
            Vector3 centerWorld3D = new Vector3(centerWorld2D.x, centerWorld2D.y, z);
            vertices[0] = transform.InverseTransformPoint(centerWorld3D); // 本地

            for (int i = 0; i < n; i++)
            {
                var p2 = ptsWorld2D[order[i]];
                Vector3 pw = new Vector3(p2.x, p2.y, z);
                vertices[i + 1] = transform.InverseTransformPoint(pw); // 本地
            }

            var triangles = new int[(n - 1) * 3];
            for (int i = 0; i < n - 1; i++)
            {
                triangles[i * 3 + 0] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.Clear();
            mesh.SetVertices(vertices);   // 本地坐标
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
