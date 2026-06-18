using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My
{
    public class DialogTriggerZone : MonoBehaviour
    {
        public string DialogId;
        public List<CommonCheckCond> EnableCondition = new();

        private Collider2D[] _colliders;

        private void Awake()
        {
            RefreshColliders();
        }

        public bool ContainsPoint(Vector2 worldPos)
        {
            if (_colliders == null || _colliders.Length == 0)
            {
                RefreshColliders();
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                var col = _colliders[i];
                if (col != null && col.enabled && col.OverlapPoint(worldPos))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryTriggerDialog()
        {
            if (string.IsNullOrEmpty(DialogId))
            {
                return false;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return false;
            }

            if (EnableCondition != null)
            {
                for (int i = 0; i < EnableCondition.Count; i++)
                {
                    if (!glm.CheckCommonCond(EnableCondition[i]))
                    {
                        return false;
                    }
                }
            }

            return glm.playerDataManager?.DialogTriggerSystem?.TryPlayDialogByTriggerZone(DialogId) ?? false;
        }

        private void RefreshColliders()
        {
            _colliders = GetComponentsInChildren<Collider2D>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshColliders();

            var colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].isTrigger = true;
                }
            }

            int zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer >= 0)
            {
                var transforms = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].gameObject.layer = zoneLayer;
                }
            }
        }
#endif
    }
}
