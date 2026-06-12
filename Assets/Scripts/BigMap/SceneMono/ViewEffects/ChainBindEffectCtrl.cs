using System.Collections.Generic;
using My.Map.Scene;
using UnityEngine;

// ChainBindEffectCtrl 通过 UnitPresentationVisualVolume 查询单位视觉范围

// 锁链捆缚：锚点限制在绑定单位 sprite 可视范围内 + Bezier 链段
[RequireComponent(typeof(MapSceneEffectCtrl))]
public class ChainBindEffectCtrl : MonoBehaviour
{
    struct ChainSegment
    {
        public int AnchorA;
        public int AnchorB;
        public float Bulge;
    }

    struct ChainLayoutRegion
    {
        public FacingLocalVolume Volume;
        public float MaxBulge;

        public Vector2 Center => Volume.Center;
        public Vector2 HalfExtents => Volume.HalfExtents;
    }

    // 规范化锚点（local，+Y 为角色 forward）；运行时映射到 sprite 半宽/半高
    static readonly Vector2[] DefaultBodyAnchors =
    {
        new(0f, 0.42f),
        new(0.32f, 0.18f),
        new(0.28f, -0.22f),
        new(0f, -0.38f),
        new(-0.28f, -0.22f),
        new(-0.32f, 0.18f),
    };

    static readonly (int a, int b)[] FixedCoveragePairs =
    {
        (0, 3),
        (1, 4),
        (2, 5),
        (0, 2),
        (0, 4),
        (1, 5),
    };

    static readonly float[] FixedBulgeScales = { 0.24f, 0.30f, 0.28f, 0.22f, 0.22f, 0.18f };

    [Header("Chain Bind")]
    [SerializeField] int chainCount = 4;
    [SerializeField] int pointCount = 14;
    [SerializeField] float bindRadius = 0.5f;
    [SerializeField] float chainBulge = 0.24f;
    [SerializeField][Range(0.3f, 1.2f)] float chainBulgeMul = 1f;
    [SerializeField] float lineWidth = 0.08f;
    [SerializeField] int sortingOrder = 7;
    [SerializeField] string sortingLayerName = "Normal";
    [SerializeField] Material chainMaterial;
    [SerializeField] float textureTilesPerUnit = 4f;
    [SerializeField] bool rotateWithUnitFacing = true;

    [Header("Animation")]
    [SerializeField] bool enableStruggleJitter = true;
    [SerializeField] float jitterAmplitude = 0.03f;
    [SerializeField] float jitterSpeed = 8f;

    [Header("Spawn")]
    [SerializeField] bool spawnOnlyMode;
    [SerializeField] float spawnDuration = 0.45f;
    [SerializeField] float spawnStartRadiusScale = 1.25f;
    [SerializeField] float persistentTightenDuration = 0.35f;

    [Header("Editor Preview")]
    [SerializeField] bool previewInEditor;
    [SerializeField] int previewSeed = 12345;
    [SerializeField][Range(0f, 1f)] float previewRadiusScale = 1f;
    [SerializeField] Vector2 previewHalfExtents = new(0.22f, 0.32f);

    readonly List<LineRenderer> _lines = new();
    readonly List<Vector2[]> _basePoints = new();
    readonly List<ChainSegment> _segments = new();
    Vector2[] _scaledAnchors;

    MapSceneEffectCtrl _effectCtrl;
    ChainLayoutRegion _layoutRegion;
    float _spawnTimer;
    float _fadeTimer;
    float _activeSpawnDuration;
    float _radiusScale = 1f;
    float _alpha = 1f;
    bool _spawnFinished;
    bool _fadeStarted;
    bool _isEditorPreview;
    int _layoutSeed = 12345;

    void Awake()
    {
        _effectCtrl = GetComponent<MapSceneEffectCtrl>();
    }

    void OnEnable()
    {
        if (_effectCtrl == null)
        {
            return;
        }

        _effectCtrl.OnShown += HandleShown;
        _effectCtrl.OnProgressChanged += HandleProgressChanged;
    }

    void OnDisable()
    {
        if (_effectCtrl != null)
        {
            _effectCtrl.OnShown -= HandleShown;
            _effectCtrl.OnProgressChanged -= HandleProgressChanged;
        }

        ClearChains();
    }

    void HandleShown()
    {
        _isEditorPreview = false;
        _layoutSeed = _effectCtrl != null ? _effectCtrl.BindingRandomSeed : 12345;
        SyncFacingRotation();
        RebuildChains();

        _spawnTimer = 0f;
        _fadeTimer = 0f;
        _radiusScale = spawnStartRadiusScale;
        _alpha = 1f;
        _spawnFinished = false;
        _fadeStarted = false;
        _activeSpawnDuration = spawnOnlyMode ? spawnDuration : persistentTightenDuration;
        ApplyAllPoints();
    }

    void HandleProgressChanged(float progress01)
    {
        _radiusScale = Mathf.Lerp(spawnStartRadiusScale, 1f, progress01);
        ApplyAllPoints();
    }

    void Update()
    {
        if (!_isEditorPreview && rotateWithUnitFacing)
        {
            SyncFacingRotation();
        }

        if (_isEditorPreview || _lines.Count == 0)
        {
            return;
        }

        if (!_spawnFinished)
        {
            _spawnTimer += Time.deltaTime;
            float t = _activeSpawnDuration <= 0f
                ? 1f
                : Mathf.Clamp01(_spawnTimer / _activeSpawnDuration);
            _radiusScale = Mathf.Lerp(spawnStartRadiusScale, 1f, t);
            ApplyAllPoints();

            if (t >= 1f)
            {
                _spawnFinished = true;
                if (spawnOnlyMode)
                {
                    enableStruggleJitter = false;
                    _fadeStarted = true;
                }
            }

            return;
        }

        if (_fadeStarted && spawnOnlyMode)
        {
            _fadeTimer += Time.deltaTime;
            _alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(_fadeTimer / 0.25f));
            ApplyAllPoints();
            return;
        }

        if (enableStruggleJitter)
        {
            ApplyJitter();
        }
    }

    void SyncFacingRotation()
    {
        var pres = TryGetBoundPresenter();
        if (pres?.UnitEntity == null)
        {
            return;
        }

        var look = pres.UnitEntity.CurrentLook;
        if (look.sqrMagnitude < 1e-4f)
        {
            return;
        }

        float angle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    SceneUnitPresenter TryGetBoundPresenter()
    {
        var ctx = _effectCtrl?.GetBoundEffectCtx();
        if (ctx?.BindingUnit == null || SceneAOIManager.Instance == null)
        {
            return null;
        }

        return SceneAOIManager.Instance.GetActivePresentation(ctx.BindingUnit.Value) as SceneUnitPresenter;
    }

    void ResolveLayoutRegion(SceneUnitPresenter presenter)
    {
        if (_isEditorPreview)
        {
            _layoutRegion = BuildFallbackRegion(previewHalfExtents, Vector2.zero, chainBulgeMul);
            return;
        }

        float facingAngleDeg = ResolveFacingAngleDeg(presenter);

        if (presenter != null)
        {
            var volumeComp = presenter.GetComponent<UnitPresentationVisualVolume>();
            if (volumeComp != null && volumeComp.TryGetVolume(facingAngleDeg, out var vol))
            {
                _layoutRegion = BuildRegionFromVolume(vol, chainBulgeMul);
                return;
            }

            if (UnitPresentationVisualVolume.TryComputeSpriteAutoAabb(
                    presenter,
                    presenter.transform.position,
                    facingAngleDeg,
                    0.88f,
                    Vector2.one,
                    Vector2.zero,
                    out vol))
            {
                _layoutRegion = BuildRegionFromVolume(vol, chainBulgeMul);
                return;
            }
        }

        var half = Vector2.one * bindRadius;
        _layoutRegion = BuildFallbackRegion(half, Vector2.zero, chainBulgeMul);
    }

    static float ResolveFacingAngleDeg(SceneUnitPresenter presenter)
    {
        if (presenter?.UnitEntity == null)
        {
            return 0f;
        }

        var look = presenter.UnitEntity.CurrentLook;
        if (look.sqrMagnitude < 1e-4f)
        {
            return 0f;
        }

        return Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
    }

    static ChainLayoutRegion BuildRegionFromVolume(in FacingLocalVolume vol, float bulgeMul)
    {
        float maxBulge = Mathf.Min(vol.HalfExtents.x, vol.HalfExtents.y) * 0.35f * bulgeMul;
        return new ChainLayoutRegion
        {
            Volume = vol,
            MaxBulge = maxBulge,
        };
    }

    static ChainLayoutRegion BuildFallbackRegion(Vector2 halfExtents, Vector2 center, float bulgeMul)
    {
        var vol = new FacingLocalVolume
        {
            Mode = EVisualVolumeMode.AutoAabb,
            Center = center,
            HalfExtents = halfExtents,
            Hull = null,
        };
        return BuildRegionFromVolume(vol, bulgeMul);
    }

#if UNITY_EDITOR
    Transform _editorPreviewAnchor;

    bool NeedsExternalEditorPreviewParent()
    {
        if (Application.isPlaying)
        {
            return false;
        }

        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return true;
        }

        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null && stage.IsPartOfPrefabContents(gameObject);
    }

    void EnsureEditorPreviewAnchor()
    {
        if (_editorPreviewAnchor != null)
        {
            return;
        }

        var go = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(
            $"__ChainBindPreview_{GetInstanceID()}",
            HideFlags.HideAndDontSave);
        _editorPreviewAnchor = go.transform;
    }

    void SyncEditorPreviewAnchorTransform()
    {
        if (_editorPreviewAnchor == null)
        {
            return;
        }

        _editorPreviewAnchor.SetPositionAndRotation(transform.position, transform.rotation);
        _editorPreviewAnchor.localScale = transform.lossyScale;
    }

    void DestroyEditorPreviewAnchor()
    {
        if (_editorPreviewAnchor == null)
        {
            return;
        }

        Object.DestroyImmediate(_editorPreviewAnchor.gameObject);
        _editorPreviewAnchor = null;
    }

    Transform ResolveLineParent()
    {
        if (NeedsExternalEditorPreviewParent())
        {
            EnsureEditorPreviewAnchor();
            SyncEditorPreviewAnchorTransform();
            return _editorPreviewAnchor;
        }

        return transform;
    }

    void OnDestroy()
    {
        if (!Application.isPlaying)
        {
            DestroyEditorPreviewAnchor();
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            if (!previewInEditor)
            {
                ClearEditorPreview();
                return;
            }

            RebuildEditorPreview();
        };
    }

    public void RebuildEditorPreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        _isEditorPreview = true;
        _layoutSeed = previewSeed;
        _radiusScale = Mathf.Lerp(spawnStartRadiusScale, 1f, previewRadiusScale);
        _alpha = 1f;
        _spawnFinished = true;
        _fadeStarted = false;
        RebuildChains();
        ApplyAllPoints();
    }

    public void ClearEditorPreview()
    {
        if (Application.isPlaying)
        {
            return;
        }

        _isEditorPreview = false;
        ClearChains();
    }

    void OnDrawGizmosSelected()
    {
        if (!previewInEditor)
        {
            return;
        }

        if (_editorPreviewAnchor != null)
        {
            SyncEditorPreviewAnchorTransform();
        }

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
        DrawRegionGizmo(_layoutRegion, _radiusScale);

        if (_scaledAnchors == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
        for (int i = 0; i < _scaledAnchors.Length; i++)
        {
            var scaled = ScalePoint(_scaledAnchors[i], _radiusScale);
            var world = transform.TransformPoint(new Vector3(scaled.x, scaled.y, 0f));
            Gizmos.DrawSphere(world, 0.02f);
        }

        DrawPreviewChainGizmos();
    }

    void DrawPreviewChainGizmos()
    {
        if (_basePoints.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(0.72f, 0.68f, 0.62f, 0.95f);
        for (int i = 0; i < _basePoints.Count; i++)
        {
            var pts = _basePoints[i];
            if (pts == null || pts.Length < 2)
            {
                continue;
            }

            for (int p = 1; p < pts.Length; p++)
            {
                var a = ScalePoint(pts[p - 1], _radiusScale);
                var b = ScalePoint(pts[p], _radiusScale);
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector3(a.x, a.y, 0f)),
                    transform.TransformPoint(new Vector3(b.x, b.y, 0f)));
            }
        }
    }

    void DrawRegionGizmo(ChainLayoutRegion region, float scale)
    {
        Vector3 ToWorld(Vector2 local)
        {
            var s = ScalePoint(local, scale);
            return transform.TransformPoint(new Vector3(s.x, s.y, 0f));
        }

        if (region.Volume.Mode == EVisualVolumeMode.ManualConvexHull
            && region.Volume.Hull != null
            && region.Volume.Hull.Length >= VisualVolumeConvexMath.MinHullPoints)
        {
            for (int i = 0; i < region.Volume.Hull.Length; i++)
            {
                int next = (i + 1) % region.Volume.Hull.Length;
                Gizmos.DrawLine(ToWorld(region.Volume.Hull[i]), ToWorld(region.Volume.Hull[next]));
            }

            return;
        }

        Vector3 bl = ToWorld(region.Center + new Vector2(-region.HalfExtents.x, -region.HalfExtents.y));
        Vector3 br = ToWorld(region.Center + new Vector2(region.HalfExtents.x, -region.HalfExtents.y));
        Vector3 tr = ToWorld(region.Center + new Vector2(region.HalfExtents.x, region.HalfExtents.y));
        Vector3 tl = ToWorld(region.Center + new Vector2(-region.HalfExtents.x, region.HalfExtents.y));
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    public void RandomizePreviewSeed()
    {
        previewSeed = Random.Range(1, 99999);
        if (previewInEditor)
        {
            RebuildEditorPreview();
        }
    }
#endif

    void RebuildChains()
    {
        ClearChains();
        ResolveLayoutRegion(TryGetBoundPresenter());
        BuildScaledAnchors(_layoutSeed);

        var rng = new System.Random(_layoutSeed);
        var pairIndices = BuildShuffledPairIndices(rng);
        int count = Mathf.Clamp(chainCount, 1, pairIndices.Count);
        int segments = Mathf.Clamp(pointCount, 6, 32);

        for (int i = 0; i < count; i++)
        {
            var pair = FixedCoveragePairs[pairIndices[i]];
            float bulgeScale = i < FixedBulgeScales.Length ? FixedBulgeScales[i] : chainBulge;
            float bulgeJitter = 0.65f + (float)rng.NextDouble() * 0.7f;
            float bulge = _layoutRegion.MaxBulge * (bulgeScale / 0.24f) * bulgeJitter;
            bulge = Mathf.Min(bulge, _layoutRegion.MaxBulge);

            _segments.Add(new ChainSegment
            {
                AnchorA = pair.a,
                AnchorB = pair.b,
                Bulge = bulge,
            });

            var points = BuildAnchorChainPoints(
                segments,
                _scaledAnchors[pair.a],
                _scaledAnchors[pair.b],
                bulge,
                rng);
            float arcLength = ComputePolylineLength(points);
            _basePoints.Add(points);
            _lines.Add(CreateLineRenderer(i, arcLength));
        }
    }

    static List<int> BuildShuffledPairIndices(System.Random rng)
    {
        var indices = new List<int>(FixedCoveragePairs.Length);
        for (int i = 0; i < FixedCoveragePairs.Length; i++)
        {
            indices.Add(i);
        }

        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices;
    }

    void BuildScaledAnchors(int seed)
    {
        var rng = new System.Random(seed ^ 0x5f3759df);
        _scaledAnchors = new Vector2[DefaultBodyAnchors.Length];
        var jitter = _layoutRegion.HalfExtents * 0.08f;

        for (int i = 0; i < DefaultBodyAnchors.Length; i++)
        {
            var mapped = MapNormalizedAnchor(DefaultBodyAnchors[i]);
            float jx = ((float)rng.NextDouble() - 0.5f) * 2f * jitter.x;
            float jy = ((float)rng.NextDouble() - 0.5f) * 2f * jitter.y;
            _scaledAnchors[i] = ClampToRegion(mapped + new Vector2(jx, jy));
        }
    }

    Vector2 MapNormalizedAnchor(Vector2 normalized)
    {
        return _layoutRegion.Center + Vector2.Scale(normalized, _layoutRegion.HalfExtents);
    }

    Vector2 ClampToRegion(Vector2 point)
    {
        return UnitPresentationVisualVolume.ClampFacingLocal(point, _layoutRegion.Volume);
    }

    Vector2[] BuildAnchorChainPoints(int segments, Vector2 anchorA, Vector2 anchorB, float bulge, System.Random rng)
    {
        var control = ClampToRegion(ComputeBezierControl(anchorA, anchorB, bulge, rng));
        var points = SampleBezierByArcLength(anchorA, control, anchorB, segments);
        for (int i = 1; i < points.Length - 1; i++)
        {
            points[i] = ClampToRegion(points[i]);
        }

        return points;
    }

    Vector2 ComputeBezierControl(Vector2 anchorA, Vector2 anchorB, float bulge, System.Random rng)
    {
        var mid = (anchorA + anchorB) * 0.5f;
        var tangent = anchorB - anchorA;
        if (tangent.sqrMagnitude < 1e-6f)
        {
            tangent = Vector2.up;
        }
        else
        {
            tangent.Normalize();
        }

        var normal = new Vector2(-tangent.y, tangent.x);
        var outward = (mid - _layoutRegion.Center).sqrMagnitude > 1e-4f
            ? (mid - _layoutRegion.Center).normalized
            : Vector2.up;
        float outwardWeight = 0.25f + (float)rng.NextDouble() * 0.55f;
        float sideWeight = ((float)rng.NextDouble() - 0.5f) * 1.1f;
        var offsetDir = outward * outwardWeight + normal * sideWeight;
        if (offsetDir.sqrMagnitude < 1e-6f)
        {
            offsetDir = outward;
        }

        return mid + offsetDir.normalized * bulge;
    }

    static Vector2[] SampleBezierByArcLength(Vector2 anchorA, Vector2 control, Vector2 anchorB, int segments)
    {
        const int denseSamples = 48;
        var dense = new Vector2[denseSamples + 1];
        var cumulative = new float[denseSamples + 1];
        dense[0] = anchorA;
        cumulative[0] = 0f;

        for (int i = 1; i <= denseSamples; i++)
        {
            float t = i / (float)denseSamples;
            dense[i] = QuadraticBezier2D(anchorA, control, anchorB, t);
            cumulative[i] = cumulative[i - 1] + Vector2.Distance(dense[i - 1], dense[i]);
        }

        float totalLength = cumulative[denseSamples];
        var points = new Vector2[segments + 1];
        points[0] = anchorA;
        points[segments] = anchorB;

        for (int p = 1; p < segments; p++)
        {
            float target = totalLength * p / segments;
            points[p] = SamplePolylineAtDistance(dense, cumulative, target);
        }

        return points;
    }

    static Vector2 SamplePolylineAtDistance(Vector2[] points, float[] cumulative, float distance)
    {
        for (int i = 1; i < cumulative.Length; i++)
        {
            if (cumulative[i] < distance - 1e-5f)
            {
                continue;
            }

            float segLen = cumulative[i] - cumulative[i - 1];
            float t = segLen <= 1e-6f ? 0f : (distance - cumulative[i - 1]) / segLen;
            return Vector2.Lerp(points[i - 1], points[i], t);
        }

        return points[points.Length - 1];
    }

    static float ComputePolylineLength(Vector2[] points)
    {
        float length = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            length += Vector2.Distance(points[i - 1], points[i]);
        }

        return Mathf.Max(length, 0.01f);
    }

    static Vector2 QuadraticBezier2D(Vector2 a, Vector2 control, Vector2 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * control + t * t * b;
    }

    LineRenderer CreateLineRenderer(int index, float arcLength)
    {
        var go = new GameObject($"chain_{index}");
        Transform parent = transform;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            parent = ResolveLineParent();
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
#endif
        go.transform.SetParent(parent, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = LineTextureMode.Tile;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.sortingLayerName = sortingLayerName;
        lr.sortingOrder = sortingOrder;
        lr.material = chainMaterial != null ? chainMaterial : CreateFallbackMaterial();
        lr.textureScale = new Vector2(arcLength * textureTilesPerUnit, 1f);
        ApplyLineAlpha(lr, 1f);
        return lr;
    }

    static void ApplyLineAlpha(LineRenderer lr, float alpha)
    {
        var c = new Color(1f, 1f, 1f, alpha);
        lr.startColor = c;
        lr.endColor = c;
    }

    static Material CreateFallbackMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        var mat = new Material(shader);
        mat.color = new Color(0.65f, 0.67f, 0.72f, 1f);
        return mat;
    }

    void ApplyAllPoints()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            var lr = _lines[i];
            var basePts = _basePoints[i];
            lr.positionCount = basePts.Length;
            ApplyLineAlpha(lr, _alpha);

            float arcLength = 0f;
            Vector3 prev = default;
            for (int p = 0; p < basePts.Length; p++)
            {
                var scaled = ScalePoint(basePts[p], _radiusScale);
                var pos = new Vector3(scaled.x, scaled.y, 0f);
                lr.SetPosition(p, pos);
                if (p > 0)
                {
                    arcLength += Vector3.Distance(prev, pos);
                }

                prev = pos;
            }

            lr.textureScale = new Vector2(Mathf.Max(arcLength, 0.01f) * textureTilesPerUnit, 1f);
        }
    }

    void ApplyJitter()
    {
        float time = Time.time * jitterSpeed;
        float jitterScale = Mathf.Min(_layoutRegion.HalfExtents.x, _layoutRegion.HalfExtents.y) * 0.08f;
        float amp = Mathf.Min(jitterAmplitude, jitterScale);

        for (int i = 0; i < _lines.Count; i++)
        {
            var lr = _lines[i];
            var basePts = _basePoints[i];
            lr.positionCount = basePts.Length;
            ApplyLineAlpha(lr, _alpha);

            float arcLength = 0f;
            Vector3 prev = default;
            for (int p = 0; p < basePts.Length; p++)
            {
                float phase = i * 1.7f + p * 0.55f;
                float jx = Mathf.Sin(time + phase) * amp;
                float jy = Mathf.Cos(time * 0.85f + phase) * amp;
                var scaled = ScalePoint(basePts[p], _radiusScale);
                var jittered = ClampToRegion(new Vector2(scaled.x + jx, scaled.y + jy));
                var pos = new Vector3(jittered.x, jittered.y, 0f);
                lr.SetPosition(p, pos);
                if (p > 0)
                {
                    arcLength += Vector3.Distance(prev, pos);
                }

                prev = pos;
            }

            lr.textureScale = new Vector2(Mathf.Max(arcLength, 0.01f) * textureTilesPerUnit, 1f);
        }
    }

    Vector2 ScalePoint(Vector2 point, float scale)
    {
        return _layoutRegion.Center + (point - _layoutRegion.Center) * scale;
    }

    static void DestroyLineObject(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(go);
            return;
        }
#endif
        Object.Destroy(go);
    }

    void ClearChains()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
            {
                DestroyLineObject(_lines[i].gameObject);
            }
        }

        _lines.Clear();
        _basePoints.Clear();
        _segments.Clear();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyEditorPreviewAnchor();
        }
#endif
    }
}
