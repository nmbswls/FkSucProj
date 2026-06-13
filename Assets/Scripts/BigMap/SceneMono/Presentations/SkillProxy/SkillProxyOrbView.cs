using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // orb 专属表现：本体跟随玩家（逻辑层 FollowOwner），子节点 orbitRoot 做视觉公转。
    [RequireComponent(typeof(SkillProxyPresenter))]
    public class SkillProxyOrbView : MonoBehaviour
    {
        [SerializeField] private Transform orbitRoot;
        [SerializeField] private float visualOrbitDegPerSec = 120f;
        [SerializeField] private SkillProxyOrbSlotView[] orbSlots;

        SkillProxyPresenter _presenter;

        void Awake()
        {
            _presenter = GetComponent<SkillProxyPresenter>();

            if (orbitRoot == null)
            {
                orbitRoot = transform.Find("orbitRoot");
            }

            if (orbSlots == null || orbSlots.Length == 0)
            {
                orbSlots = GetComponentsInChildren<SkillProxyOrbSlotView>(true);
            }
        }

        void OnEnable()
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.EvBound += OnBound;
            _presenter.EvUnbound += OnUnbound;
            _presenter.EvAmmoChanged += OnAmmoChanged;
            _presenter.EvPeriodicCast += OnPeriodicCast;

            // 对象池复用时 Bind 可能早于 OnEnable，补一次同步避免 slot 全灭。
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
            _presenter.EvAmmoChanged -= OnAmmoChanged;
            _presenter.EvPeriodicCast -= OnPeriodicCast;
        }

        void Update()
        {
            if (orbitRoot == null)
            {
                return;
            }

            orbitRoot.Rotate(0f, 0f, visualOrbitDegPerSec * Time.deltaTime);
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
            int current = (int)logic.GetAttr(logic.Cfg.AmmoResourceId);
            RefreshSlots(current);
        }

        void OnUnbound()
        {
            RefreshSlots(0);
        }

        void OnAmmoChanged(int current, int max)
        {
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
