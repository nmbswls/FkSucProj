using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // orb prefab 表现：逻辑层 FollowOwner，子节点 orbitRoot 按半径/角速度/初相做视觉公转。
    [RequireComponent(typeof(SkillProxyPresenter))]
    public class SkillProxyOrbView : MonoBehaviour
    {
        [SerializeField] private string slotResourceId = "ammo";
        [SerializeField] private Transform orbitRoot;
        [SerializeField] private Transform orbitVisual;
        [SerializeField] private float orbitRadius = 1.2f;
        [SerializeField] private float orbitAngularSpeed = 120f;
        [SerializeField] private float orbitInitialAngle;
        [SerializeField] private SkillProxyOrbSlotView[] orbSlots;

        SkillProxyPresenter _presenter;
        float _orbitAngleDeg;

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

            if (orbSlots == null || orbSlots.Length == 0)
            {
                orbSlots = GetComponentsInChildren<SkillProxyOrbSlotView>(true);
            }

            _orbitAngleDeg = orbitInitialAngle;
            ApplyOrbitLayout();
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
        }

        void Update()
        {
            if (orbitRoot == null)
            {
                return;
            }

            _orbitAngleDeg += orbitAngularSpeed * Time.deltaTime;
            orbitRoot.localRotation = Quaternion.Euler(0f, 0f, _orbitAngleDeg);
        }

        void ApplyOrbitLayout()
        {
            if (orbitVisual != null)
            {
                orbitVisual.localPosition = new Vector3(orbitRadius, 0f, 0f);
            }

            if (orbitRoot != null)
            {
                orbitRoot.localRotation = Quaternion.Euler(0f, 0f, _orbitAngleDeg);
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
            if (logic.Cfg.InitialResources != null &&
                logic.Cfg.InitialResources.TryGetValue(slotResourceId, out _))
            {
                int current = (int)logic.GetAttr(slotResourceId);
                RefreshSlots(current);
            }
        }

        void OnUnbound()
        {
            RefreshSlots(0);
        }

        void OnResourceChanged(string resourceId, int current, int max)
        {
            if (resourceId != slotResourceId)
            {
                return;
            }

            RefreshSlots(current);
        }

        void OnPeriodicCast() { }

        void RefreshSlots(int current)
        {
            if (orbSlots == null)
            {
                return;
            }

            for (int i = 0; i < orbSlots.Length; i++)
            {
                orbSlots[i]?.SetActive(i < current);
            }
        }
    }
}
