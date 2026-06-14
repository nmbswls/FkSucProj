using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // orb prefab 表现：逻辑层 FollowOwner，轨道球数量与间距由 ammo 资源驱动。
    [RequireComponent(typeof(SkillProxyPresenter))]
    public class SkillProxyOrbView : MonoBehaviour
    {
        [SerializeField] private Transform orbitRoot;
        [SerializeField] private Transform orbitVisual;
        [SerializeField] private Transform slotOrbitRoot;
        [SerializeField] private SkillProxyOrbSlotView slotTemplate;
        [SerializeField] private float orbitRadius = 1.2f;
        [SerializeField] private float orbitAngularSpeed = 120f;
        [SerializeField] private float orbitInitialAngle;

        [Header("Relayout Smooth")]
        [SerializeField] private float relayoutDuration = 0.25f;
        [SerializeField] private float fadeDuration = 0.18f;

        SkillProxyPresenter _presenter;
        readonly List<SkillProxyOrbSlotView> _runtimeSlots = new();
        int _displayOrbCount;
        bool _slotUpright = true;

        // 过渡状态
        bool _relayoutActive;
        float _relayoutTimer;
        float _relayoutStepFromDeg;
        float _relayoutStepToDeg;
        // 正在执行淡出动画（消耗场景）的槽，过渡期间跳过其位置更新
        SkillProxyOrbSlotView _fadingOutSlot;
        // 是否是首次绑定（首次跳过动画直接布局）
        bool _firstBind = true;

        void Awake()
        {
            _presenter = GetComponent<SkillProxyPresenter>();

            if (orbitRoot == null)
            {
                orbitRoot = transform.Find("orbitRoot");
            }

            if (orbitVisual == null && orbitRoot != null && orbitRoot.childCount > 0)
            {
                orbitVisual = orbitRoot.GetChild(0);
            }

            if (slotOrbitRoot == null)
            {
                slotOrbitRoot = transform.Find("ammo_slots");
            }

            if (slotOrbitRoot == null)
            {
                slotOrbitRoot = transform;
            }

            InitSlotTemplate();
            ApplyOrbitLayout();
        }

        void InitSlotTemplate()
        {
            if (slotTemplate == null && slotOrbitRoot != null)
            {
                slotTemplate = slotOrbitRoot.Find("slot_template")?.GetComponent<SkillProxyOrbSlotView>();
            }

            if (slotTemplate == null && slotOrbitRoot != null)
            {
                slotTemplate = slotOrbitRoot.GetComponentInChildren<SkillProxyOrbSlotView>(true);
            }

            if (slotTemplate == null)
            {
                return;
            }

            // 清理 prefab 遗留的静态预设槽，只保留 template
            var legacySlots = slotOrbitRoot.GetComponentsInChildren<SkillProxyOrbSlotView>(true);
            for (int i = 0; i < legacySlots.Length; i++)
            {
                var slot = legacySlots[i];
                if (slot == null || slot == slotTemplate)
                {
                    continue;
                }

                Destroy(slot.gameObject);
            }

            slotTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.EvBound += OnBound;
            _presenter.EvUnbound += OnUnbound;
            _presenter.EvResourceChanged += OnResourceChanged;
            _presenter.EvPeriodicCast += OnPeriodicCast;

            TryRefreshIfAlreadyBound();
        }

        void OnDisable()
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.EvBound -= OnBound;
            _presenter.EvUnbound -= OnUnbound;
            _presenter.EvResourceChanged -= OnResourceChanged;
            _presenter.EvPeriodicCast -= OnPeriodicCast;

            KillAllFadeTweens();
            _relayoutActive = false;
        }

        void Update()
        {
            ApplyOrbitLayout();
        }

        void ApplyOrbitLayout()
        {
            if (orbitRoot != null)
            {
                orbitRoot.localRotation = Quaternion.identity;
            }

            if (orbitVisual != null)
            {
                orbitVisual.localPosition = Vector3.zero;
                orbitVisual.localRotation = Quaternion.identity;
            }

            if (_displayOrbCount <= 0)
            {
                return;
            }

            float orbitAngleDeg = orbitInitialAngle;
            float layoutRadius = orbitRadius;
            if (_presenter != null && _presenter.GetLogicEntity() is SkillProxyLogicEntity logic && logic.Cfg != null)
            {
                orbitAngleDeg = logic.GetOrbitAngleDeg();
                layoutRadius = logic.Cfg.OrbitRadius;
            }

            float stepDeg;
            if (_relayoutActive)
            {
                _relayoutTimer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_relayoutTimer / relayoutDuration));
                stepDeg = Mathf.Lerp(_relayoutStepFromDeg, _relayoutStepToDeg, t);
                if (_relayoutTimer >= relayoutDuration)
                {
                    _relayoutActive = false;
                    stepDeg = _relayoutStepToDeg;
                }
            }
            else
            {
                stepDeg = _displayOrbCount > 0 ? 360f / _displayOrbCount : 0f;
            }

            for (int i = 0; i < _displayOrbCount; i++)
            {
                var slot = _runtimeSlots[i];
                if (slot == null || slot == _fadingOutSlot)
                {
                    continue;
                }

                Vector2 localOffset = SkillProxyOrbLayout.ComputeSlotLocalOffsetWithStep(
                    i,
                    stepDeg,
                    orbitAngleDeg,
                    layoutRadius);
                slot.transform.localPosition = new Vector3(localOffset.x, localOffset.y, 0f);

                if (_slotUpright)
                {
                    float angleDeg = orbitAngleDeg + stepDeg * i;
                    slot.transform.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
                }
            }
        }

        void TryRefreshIfAlreadyBound()
        {
            if (_presenter.GetLogicEntity() is SkillProxyLogicEntity logic)
            {
                OnBound(logic);
            }
        }

        void OnBound(SkillProxyLogicEntity logic)
        {
            _firstBind = true;

            if (!TryReadAmmoState(logic, out int current, out int max))
            {
                RefreshOrbs(0, 0);
                return;
            }

            RefreshOrbs(current, max);
        }

        void OnUnbound()
        {
            KillAllFadeTweens();
            _relayoutActive = false;
            _fadingOutSlot = null;
            ClearRuntimeSlots();
            _displayOrbCount = 0;
            _firstBind = true;
            ApplyOrbitLayout();
        }

        void OnResourceChanged(string resourceId, int current, int max)
        {
            if (resourceId != AttrIdConsts.Ammo)
            {
                return;
            }

            RefreshOrbs(current, max);
        }

        void OnPeriodicCast() { }

        bool TryReadAmmoState(SkillProxyLogicEntity logic, out int current, out int max)
        {
            current = 0;
            max = 0;

            if (logic == null || logic.GetResourceMax(AttrIdConsts.Ammo) <= 0)
            {
                return false;
            }

            current = (int)logic.GetAttr(AttrIdConsts.Ammo);
            max = (int)logic.GetResourceMax(AttrIdConsts.Ammo);

            current = Mathf.Clamp(current, 0, max);
            max = Mathf.Max(0, max);
            return true;
        }

        void RefreshOrbs(int current, int max)
        {
            int newCount = Mathf.Clamp(current, 0, max);
            int oldCount = _displayOrbCount;

            EnsureRuntimeSlots(max);

            // 首次绑定：直接瞬时布局，无动画
            if (_firstBind)
            {
                _firstBind = false;
                _displayOrbCount = newCount;
                KillAllFadeTweens();
                _relayoutActive = false;
                _fadingOutSlot = null;

                for (int i = 0; i < _runtimeSlots.Count; i++)
                {
                    var slot = _runtimeSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    bool visible = i < _displayOrbCount;
                    slot.gameObject.SetActive(visible);
                    if (visible)
                    {
                        slot.SetActive(true);
                        slot.SetAlpha(1f);
                    }
                }

                ApplyOrbitLayout();
                return;
            }

            if (newCount == oldCount)
            {
                return;
            }

            // 连续变化：中断上一次过渡，以当前插值中的 step 作为新起点
            float currentStepDeg = oldCount > 0 ? 360f / oldCount : 0f;
            if (_relayoutActive)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_relayoutTimer / relayoutDuration));
                currentStepDeg = Mathf.Lerp(_relayoutStepFromDeg, _relayoutStepToDeg, t);
            }

            KillAllFadeTweens();
            _relayoutActive = false;
            _fadingOutSlot = null;

            _displayOrbCount = newCount;
            _relayoutStepFromDeg = currentStepDeg;
            _relayoutStepToDeg = newCount > 0 ? 360f / newCount : 0f;
            _relayoutTimer = 0f;
            _relayoutActive = newCount > 0;

            if (newCount < oldCount)
            {
                // 消耗：末槽淡出后隐藏
                int fadeOutIndex = oldCount - 1;
                if (fadeOutIndex >= 0 && fadeOutIndex < _runtimeSlots.Count)
                {
                    var outSlot = _runtimeSlots[fadeOutIndex];
                    if (outSlot != null)
                    {
                        _fadingOutSlot = outSlot;
                        outSlot.gameObject.SetActive(true);
                        outSlot.SetActive(true);
                        outSlot.SetAlpha(1f);
                        outSlot.PlayFade(0f, fadeDuration, () =>
                        {
                            outSlot.gameObject.SetActive(false);
                            outSlot.SetAlpha(1f);
                            if (_fadingOutSlot == outSlot)
                            {
                                _fadingOutSlot = null;
                            }
                        });
                    }
                }

                // 其余槽：确保正确显隐
                for (int i = 0; i < _runtimeSlots.Count; i++)
                {
                    var slot = _runtimeSlots[i];
                    if (slot == null || slot == _fadingOutSlot)
                    {
                        continue;
                    }

                    bool visible = i < newCount;
                    slot.gameObject.SetActive(visible);
                    if (visible)
                    {
                        slot.SetActive(true);
                        slot.SetAlpha(1f);
                    }
                }
            }
            else
            {
                // 恢复：新末槽淡入
                int fadeInIndex = newCount - 1;
                for (int i = 0; i < _runtimeSlots.Count; i++)
                {
                    var slot = _runtimeSlots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    bool visible = i < newCount;
                    slot.gameObject.SetActive(visible);
                    if (visible)
                    {
                        slot.SetActive(true);
                        if (i == fadeInIndex)
                        {
                            slot.SetAlpha(0f);
                            slot.PlayFade(1f, fadeDuration);
                        }
                        else
                        {
                            slot.SetAlpha(1f);
                        }
                    }
                }
            }

            // 超出 max 的槽一律隐藏
            for (int i = max; i < _runtimeSlots.Count; i++)
            {
                if (_runtimeSlots[i] != null)
                {
                    _runtimeSlots[i].gameObject.SetActive(false);
                }
            }
        }

        void KillAllFadeTweens()
        {
            foreach (var slot in _runtimeSlots)
            {
                slot?.KillFadeTweens();
            }
        }

        void EnsureRuntimeSlots(int max)
        {
            if (slotTemplate == null || max <= 0)
            {
                return;
            }

            while (_runtimeSlots.Count < max)
            {
                var clone = Instantiate(slotTemplate, slotOrbitRoot);
                clone.name = $"slot_{_runtimeSlots.Count}";
                clone.gameObject.SetActive(false);
                _runtimeSlots.Add(clone);
            }
        }

        void ClearRuntimeSlots()
        {
            for (int i = 0; i < _runtimeSlots.Count; i++)
            {
                if (_runtimeSlots[i] != null)
                {
                    Destroy(_runtimeSlots[i].gameObject);
                }
            }

            _runtimeSlots.Clear();
        }
    }
}
