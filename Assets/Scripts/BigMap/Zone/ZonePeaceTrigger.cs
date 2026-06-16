using My;
using My.Map;
using My.Map.Fight;
using My.Map.Scene;
using UnityEngine;

namespace My
{
    // Zone 层 Trigger：进入/离开 PeaceZone 时通知逻辑层挂卸 zone_peace buff
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class ZonePeaceTrigger : MonoBehaviour
    {
        [SerializeField] ZoneInfoProvider _provider;

        long _zoneSourceId;

        void Awake()
        {
            if (_provider == null)
            {
                _provider = GetComponent<ZoneInfoProvider>();
            }

            EnsureTriggerSetup();
            _zoneSourceId = gameObject.GetInstanceID();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPeaceZoneActive())
            {
                return;
            }

            var player = ResolvePlayer(other);
            if (player == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            PeaceCombatBuffRefresh.NotifyZonePeaceEnter(glm, player.Id, _zoneSourceId);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPeaceZoneActive())
            {
                return;
            }

            var player = ResolvePlayer(other);
            if (player == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            PeaceCombatBuffRefresh.NotifyZonePeaceExit(glm, player.Id, _zoneSourceId);
        }

        bool IsPeaceZoneActive()
        {
            return _provider != null
                && !_provider.IsForbidden
                && (_provider.ZoneType & ZoneInfoProvider.EZoneFlag.PeaceZone) != 0;
        }

        static PlayerLogicEntity ResolvePlayer(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            var playerPresenter = other.GetComponentInParent<PlayerScenePresenter>();
            return playerPresenter?.PlayerEntity;
        }

        void EnsureTriggerSetup()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_provider == null)
            {
                _provider = GetComponent<ZoneInfoProvider>();
            }

            if (_provider == null || (_provider.ZoneType & ZoneInfoProvider.EZoneFlag.PeaceZone) == 0)
            {
                return;
            }

            EnsureTriggerSetup();

            int zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer >= 0 && gameObject.layer != zoneLayer)
            {
                Debug.LogWarning(
                    $"[ZonePeaceTrigger] Peace zone '{name}' should use Layer 'Zone'.",
                    this);
            }
        }
#endif
    }
}
