using My;
using My.Map.Fight;
using UnityEngine;
using UnityEngine.Rendering;

// 钩爪绳 + 钩头：美术尺寸用「世界单位」描述，脚本内部再换算 LineRenderer 参数
[RequireComponent(typeof(MapProjectile))]
public class GrappleHookLineCtrl : MonoBehaviour
{
    public const string GrappleBulletId = GrappleHookSpecs.BulletId;

    [Header("引用")]
    [SerializeField] LineRenderer ropeLine;
    [SerializeField] Transform hookTipTransform;
    [SerializeField] SpriteRenderer hookTipSprite;

    [Header("链绳外观（世界单位）")]
    [Tooltip("勾选后，每节长度 / 绳粗从 RopeLine 材质贴图 + Sprite PPU 自动读取")]
    [SerializeField] bool syncArtFromMaterial = true;
    [Tooltip("沿绳方向，一节链环占多长。例：80px @100PPU → 0.8")]
    [SerializeField] float linkLengthWorld = 0.8f;
    [Tooltip("绳子在场景里有多粗。例：182px @100PPU → 1.82")]
    [SerializeField] float ropeThicknessWorld = 1.82f;
    [Tooltip("与链绳贴图 Sprite 的 Pixels Per Unit 一致，仅在「从材质同步」时用")]
    [SerializeField] float spritePixelsPerUnit = 100f;

    [Header("链绳运动")]
    [SerializeField] float maxRopeLength = GrappleHookSpecs.MaxLength;
    [Tooltip("绳子和钩头伸出去的速度，应与 GrappleHookSpecs.FlySpeed 一致")]
    [SerializeField] float extendSpeed = GrappleHookSpecs.FlySpeed;
    [Tooltip("收回动画持续时间（固定时长，与玩家移动无关，保证必然收回）")]
    [SerializeField] float retractDuration = 0.25f;

    [Header("钩头")]
    [SerializeField] float hookTipRotationOffset;

    // 钩头命中实体后触发，参数为命中世界坐标
    public event System.Action<Vector2> HookHit;

    // 外部（PlayerGrappleController）可查询钩爪是否已完全收回并可销毁
    public bool IsDone => _phase == ERopePhase.Done;

#if UNITY_EDITOR
    [Header("编辑器预览")]
    [SerializeField] bool previewInEditor;
    [SerializeField][Range(0f, GrappleHookSpecs.MaxLength)] float editorPreviewLength = 3f;
    [SerializeField][Range(-180f, 180f)] float editorPreviewAngle;

    float _editorRetractTimer;
#endif

    MapProjectile _proj;
    long _casterId;
    Vector2 _fireDir = Vector2.right;

    float _displayLength;
    bool _retractDone;
    bool _hasEntityHit;
    // 钩头命中点世界坐标（Stuck 阶段绳端点，也是 Retract 的起点）
    Vector2 _stuckTipWorld;

    // 收回阶段：记录发起收回时钩头的世界坐标，保证 Lerp 从固定起点飞向玩家
    Vector2 _retractStartTipWorld;
    float _retractTimer;

    enum ERopePhase
    {
        Extend,
        Stuck,   // 钩头已入目标，等待外部（PlayerGrappleController）驱动玩家移动完成后再收回
        Retract,
        Done,
    }

    ERopePhase _phase = ERopePhase.Extend;

    bool _isEditorPreview;
    float _editorAnimLength;
    bool _editorAnimRetracting;

    public float LinkLengthWorld => linkLengthWorld;
    public float RopeThicknessWorld => ropeThicknessWorld;
    public bool IsEditorPreviewActive => _isEditorPreview;

    void Awake()
    {
        _proj = GetComponent<MapProjectile>();
        ValidatePrefabRefs();
        ApplyRopeLineSettings();
    }

    void OnEnable()
    {
        if (_proj != null)
        {
            _proj.EventEntityHit += OnEntityHit;
        }
    }

    void OnDisable()
    {
        if (_proj != null)
        {
            _proj.EventEntityHit -= OnEntityHit;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ValidatePrefabRefs();
        ApplyRopeLineSettings();

        if (Application.isPlaying)
        {
            return;
        }

        if (previewInEditor)
        {
            ApplyEditorPreviewInternal(editorPreviewLength);
        }
    }
#endif

    void ValidatePrefabRefs()
    {
        if (ropeLine == null)
        {
            Debug.LogError("GrappleHookLineCtrl: assign LineRenderer (RopeLine) on grapple_hook prefab.", this);
            return;
        }

        if (hookTipTransform == null)
        {
            Debug.LogError("GrappleHookLineCtrl: assign hookTipTransform (view/body) on grapple_hook prefab.", this);
        }

        if (hookTipSprite == null && hookTipTransform != null)
        {
            hookTipSprite = hookTipTransform.GetComponent<SpriteRenderer>();
        }

        if (hookTipSprite == null)
        {
            Debug.LogError("GrappleHookLineCtrl: assign hookTipSprite on grapple_hook prefab.", this);
        }
    }

    public void ApplyRopeLineSettings()
    {
        if (ropeLine == null)
        {
            return;
        }

        if (syncArtFromMaterial)
        {
            SyncArtSizeFromMaterial();
        }

        linkLengthWorld = Mathf.Max(linkLengthWorld, 0.01f);
        ropeThicknessWorld = Mathf.Max(ropeThicknessWorld, 0.01f);

        ropeLine.widthMultiplier = ropeThicknessWorld;
        ropeLine.textureMode = LineTextureMode.Tile;
        ropeLine.alignment = LineAlignment.TransformZ;
        ropeLine.useWorldSpace = true;

        var group = GetComponent<SortingGroup>();
        if (group != null)
        {
            ropeLine.sortingLayerID = group.sortingLayerID;
            int tipOrder = hookTipSprite != null ? hookTipSprite.sortingOrder : group.sortingOrder + 1;
            ropeLine.sortingOrder = tipOrder - 1;
        }
    }

    void SyncArtSizeFromMaterial()
    {
        float ppu = Mathf.Max(spritePixelsPerUnit, 1f);

        if (ropeLine?.sharedMaterial?.mainTexture is Texture2D tex)
        {
            linkLengthWorld = tex.width / ppu;
            ropeThicknessWorld = tex.height / ppu;
            return;
        }

        linkLengthWorld = GrappleHookArtSpec.DefaultChainLinkTileWidthPx / ppu;
        ropeThicknessWorld = GrappleHookArtSpec.DefaultChainLinkTileHeightPx / ppu;
    }

    public void InitFromLaunch()
    {
        ValidatePrefabRefs();
        ApplyRopeLineSettings();

        if (_proj?.bindingProjInfo == null || ropeLine == null)
        {
            return;
        }

        _isEditorPreview = false;

        _casterId = _proj.bindingProjInfo.ownerEntity?.Id ?? 0;
        var dir = _proj.bindingProjInfo.initialDir;
        _fireDir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.right;

        _displayLength = 0f;
        _retractDone = false;
        _hasEntityHit = false;
        _stuckTipWorld = default;
        _phase = ERopePhase.Extend;

        _proj.SetMotionFrozen(false);        if (hookTipTransform != null)
        {
            hookTipTransform.localPosition = Vector3.zero;
        }

        ropeLine.enabled = true;
        GrappleHookLineUtil.ApplyLineAlpha(ropeLine, 1f);

        if (hookTipSprite != null)
        {
            hookTipSprite.enabled = true;
        }

        var spawnWorld = (Vector2)transform.position;
        UpdateRopeVisual(spawnWorld, spawnWorld, 0f);
        UpdateHookTip(spawnWorld, _fireDir);
    }

    void OnEntityHit(Vector2 hitWorldPos)
    {
        _hasEntityHit = true;
        _stuckTipWorld = hitWorldPos;

        if (GrappleHookLineUtil.TryResolveAnchorWorld(_casterId, out var anchor))
        {
            var diff = _stuckTipWorld - anchor;
            if (diff.sqrMagnitude > maxRopeLength * maxRopeLength)
            {
                _stuckTipWorld = anchor + diff.normalized * maxRopeLength;
            }
        }

        HookHit?.Invoke(_stuckTipWorld);
    }

    public bool TryDeferDespawn()
    {
        if (_phase == ERopePhase.Done)
        {
            return false;
        }

        if (_phase == ERopePhase.Extend)
        {
            if (_hasEntityHit)
            {
                // 转 Stuck，等待 PlayerGrappleController 完成拉扯后调 BeginRetract
                _phase = ERopePhase.Stuck;
            }
            else
            {
                BeginRetract();
            }

            return true;
        }

        if (_phase == ERopePhase.Stuck)
        {
            return true;
        }

        if (_phase == ERopePhase.Retract)
        {
            return !_retractDone;
        }

        return false;
    }

    // 由 PlayerGrappleController 在玩家拉扯完成后调用
    public void BeginRetract()
    {
        if (_phase != ERopePhase.Extend && _phase != ERopePhase.Stuck)
        {
            return;
        }

        bool wasStuck = _phase == ERopePhase.Stuck;
        _phase = ERopePhase.Retract;
        _retractTimer = 0f;

        // Stuck 阶段：从命中点收回；Extend Miss 阶段：从钩头当前位置收回
        _retractStartTipWorld = wasStuck && _hasEntityHit
            ? _stuckTipWorld
            : (hookTipTransform != null ? (Vector2)hookTipTransform.position : (Vector2)transform.position);

        if (hookTipSprite != null)
        {
            hookTipSprite.enabled = false;
        }
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && _isEditorPreview)
        {
            return;
        }
#endif

        if (ropeLine == null || !ropeLine.enabled || _proj?.bindingProjInfo == null)
        {
            return;
        }

        if (!TryResolveAnchor(out var anchorWorld))
        {
            return;
        }

        switch (_phase)
        {
            case ERopePhase.Extend:
                UpdateExtendVisual(anchorWorld);
                break;
            case ERopePhase.Stuck:
                UpdateStuckVisual(anchorWorld);
                break;
            case ERopePhase.Retract:
                UpdateRetractVisual(anchorWorld);
                break;
        }
    }

    bool TryResolveAnchor(out Vector2 anchorWorld)
    {
        if (GrappleHookLineUtil.TryResolveAnchorWorld(_casterId, out anchorWorld))
        {
            return true;
        }

        var bulletWorld = (Vector2)transform.position;

        if (_phase == ERopePhase.Extend && _displayLength <= 0.01f)
        {
            anchorWorld = bulletWorld;
            return true;
        }

        anchorWorld = bulletWorld - _fireDir * Mathf.Max(_displayLength, 0.01f);
        return _displayLength > 0.01f;
    }

    void UpdateExtendVisual(Vector2 anchorWorld)
    {
        var bulletWorld = (Vector2)transform.position;
        var toBullet = bulletWorld - anchorWorld;
        float bulletDist = toBullet.magnitude;
        if (toBullet.sqrMagnitude > 1e-6f)
        {
            _fireDir = toBullet / bulletDist;
        }

        float clampedDist = Mathf.Min(bulletDist, maxRopeLength);
        _displayLength = Mathf.MoveTowards(_displayLength, clampedDist, extendSpeed * Time.deltaTime);

        var ropeEnd = anchorWorld + _fireDir * _displayLength;
        UpdateRopeVisual(anchorWorld, ropeEnd, _displayLength);
        UpdateHookTip(bulletWorld, _fireDir);
    }

    void UpdateStuckVisual(Vector2 anchorWorld)
    {
        float len = Vector2.Distance(anchorWorld, _stuckTipWorld);
        UpdateRopeVisual(anchorWorld, _stuckTipWorld, len);
        var toStuck = _stuckTipWorld - anchorWorld;
        if (toStuck.sqrMagnitude > 1e-6f)
        {
            UpdateHookTip(_stuckTipWorld, toStuck.normalized);
        }
    }

    void UpdateRetractVisual(Vector2 anchorWorld)
    {
        _retractTimer += Time.deltaTime;

        float t = retractDuration > 1e-4f
            ? Mathf.Clamp01(_retractTimer / retractDuration)
            : 1f;

        // SmoothStep：开始快、末段减速贴合锚点，视觉更自然
        float smooth = Mathf.SmoothStep(0f, 1f, t);

        // 钩头从固定起点飞向移动中的玩家锚点，不受玩家逃跑影响
        var currentTip = Vector2.Lerp(_retractStartTipWorld, anchorWorld, smooth);
        float len = Vector2.Distance(anchorWorld, currentTip);

        UpdateRopeVisual(anchorWorld, currentTip, len);
        GrappleHookLineUtil.ApplyLineAlpha(ropeLine, 1f - smooth);

        var toAnchor = anchorWorld - currentTip;
        if (toAnchor.sqrMagnitude > 1e-6f)
        {
            UpdateHookTip(currentTip, toAnchor.normalized);
        }

        if (t >= 1f)
        {
            _retractDone = true;
            _phase = ERopePhase.Done;
            ropeLine.enabled = false;
        }
    }

    void UpdateRopeVisual(Vector2 anchorWorld, Vector2 tipWorld, float lengthWorld)
    {
        if (lengthWorld <= 0.01f)
        {
            ropeLine.SetPosition(0, tipWorld);
            ropeLine.SetPosition(1, tipWorld);
            ropeLine.textureScale = Vector2.zero;
            return;
        }

        // pos0 = 钩爪头（UV=0），纹理固定在钩头处；pos1 = 玩家锚点，绳子从玩家侧生长
        ropeLine.SetPosition(0, tipWorld);
        ropeLine.SetPosition(1, anchorWorld);
        ropeLine.textureScale = new Vector2(
            GrappleHookLineUtil.CalcChainTextureScale(linkLengthWorld),
            1f);
    }

    void UpdateHookTip(Vector2 tipWorld, Vector2 dir)
    {
        if (hookTipTransform == null)
        {
            return;
        }

        hookTipTransform.position = tipWorld;
        if (dir.sqrMagnitude > 1e-6f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + hookTipRotationOffset;
            hookTipTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void ApplyEditorPreview(float length)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyEditorPreviewInternal(length);
        }
#endif
    }

    public void ClearEditorPreview()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ClearEditorPreviewInternal();
        }
#endif
    }

    public bool StepEditorPreview(float deltaTime)
    {
#if UNITY_EDITOR
        if (Application.isPlaying || deltaTime <= 0f)
        {
            return false;
        }

        return StepEditorPreviewInternal(deltaTime);
#else
        return false;
#endif
    }

    public void ResetEditorPreviewCycle()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ResetEditorPreviewCycleInternal();
        }
#endif
    }

#if UNITY_EDITOR
    void ApplyEditorPreviewInternal(float length)
    {
        if (ropeLine == null)
        {
            return;
        }

        ApplyRopeLineSettings();
        _isEditorPreview = true;
        _editorAnimRetracting = false;

        float clamped = Mathf.Clamp(length, 0f, maxRopeLength);
        editorPreviewLength = clamped;

        ropeLine.enabled = true;
        GrappleHookLineUtil.ApplyLineAlpha(ropeLine, 1f);

        if (hookTipSprite != null)
        {
            hookTipSprite.enabled = clamped > 0.01f;
        }

        DrawEditorPreview(clamped);
    }

    void ClearEditorPreviewInternal()
    {
        _isEditorPreview = false;
        _editorAnimLength = 0f;
        _editorAnimRetracting = false;
        editorPreviewLength = 0f;

        if (ropeLine != null)
        {
            ropeLine.enabled = false;
        }

        if (hookTipTransform != null)
        {
            hookTipTransform.localPosition = Vector3.zero;
            hookTipTransform.localRotation = Quaternion.identity;
        }
    }

    bool StepEditorPreviewInternal(float deltaTime)
    {
        if (ropeLine == null)
        {
            return false;
        }

        _isEditorPreview = true;
        ropeLine.enabled = true;
        GrappleHookLineUtil.ApplyLineAlpha(ropeLine, 1f);

        if (_editorAnimRetracting)
        {
            _editorRetractTimer += deltaTime;
            float t = retractDuration > 1e-4f
                ? Mathf.Clamp01(_editorRetractTimer / retractDuration)
                : 1f;
            _editorAnimLength = Mathf.Lerp(maxRopeLength, 0f, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f)
            {
                ClearEditorPreviewInternal();
                return false;
            }
        }
        else
        {
            _editorAnimLength = Mathf.MoveTowards(_editorAnimLength, maxRopeLength, extendSpeed * deltaTime);
            if (_editorAnimLength >= maxRopeLength - 0.01f)
            {
                _editorAnimRetracting = true;
                _editorRetractTimer = 0f;
            }
        }

        editorPreviewLength = _editorAnimLength;
        DrawEditorPreview(_editorAnimLength);

        if (hookTipSprite != null)
        {
            hookTipSprite.enabled = _editorAnimLength > 0.01f;
        }

        return true;
    }

    void ResetEditorPreviewCycleInternal()
    {
        _editorAnimLength = 0f;
        _editorAnimRetracting = false;
        _editorRetractTimer = 0f;
        ApplyEditorPreviewInternal(0f);
    }

    void DrawEditorPreview(float length)
    {
        var anchor = (Vector2)transform.position;
        var rad = editorPreviewAngle * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        var tip = anchor + dir * length;
        UpdateRopeVisual(anchor, tip, length);
        UpdateHookTip(tip, dir);
    }
#endif
}
