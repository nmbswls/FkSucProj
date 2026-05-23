using cfg.demo;using My.Config;
using My.Player;
using UnityEngine;

namespace My.Map.Scene
{
    // 人类武器表现：挂载在 PlayerScenePresenter，BindPoint1 下动态挂载通用 View
    public sealed class PlayerHumanWeaponView : MonoBehaviour
    {
        Transform _bindPoint;
        MapUnitWeaponCtrl _weaponCtrl;
        MapUnitWeaponOne _equippedView;
        string _equippedItemId;

        void Awake()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            var presenter = GetComponent<SceneUnitPresenter>();
            if (presenter == null)
            {
                return;
            }

            _weaponCtrl = presenter.WeaponCtrl;
            _bindPoint = presenter.transform.Find("WeaponRoot/BindPoint1");
            if (_bindPoint == null)
            {
                Debug.LogWarning("PlayerHumanWeaponView: BindPoint1 not found under WeaponRoot.");
            }
        }

        public void Equip(string itemId)
        {
            if (_bindPoint == null || _weaponCtrl == null)
            {
                ResolveRefs();
            }

            if (_bindPoint == null || _weaponCtrl == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                Unequip();
                return;
            }

            var def = HumanWeaponCatalog.GetOrDefault(itemId);
            if (def == null)
            {
                Unequip();
                return;
            }

            if (_equippedView == null || _equippedItemId != itemId)
            {
                EnsureViewInstance(def);
                ApplySprite(def);
                _equippedItemId = itemId;
            }
            else
            {
                ApplySprite(def);
            }

            if (_equippedView != null)
            {
                _equippedView.KeepVisibleWhenIdle = true;
                _equippedView.gameObject.SetActive(true);
            }
        }

        public void Unequip()
        {
            _equippedItemId = null;
            if (_equippedView != null)
            {
                _equippedView.KeepVisibleWhenIdle = false;
                _equippedView.gameObject.SetActive(false);
            }
        }

        void EnsureViewInstance(HumanWeapon def)
        {
            if (_equippedView != null)
            {
                return;
            }

            var prefab = LoadViewPrefab(def);
            if (prefab == null)
            {
                Debug.LogError(
                    $"PlayerHumanWeaponView: view prefab not found for '{def.ItemId}'. "
                    + $"Set humanweapon.view_prefab or use {HumanWeaponCatalog.DefaultViewPrefab}.");
                return;
            }

            var instance = Object.Instantiate(prefab, _bindPoint);
            instance.name = HumanWeaponCatalog.ViewKey;
            _equippedView = instance.GetComponent<MapUnitWeaponOne>();
            if (_equippedView == null)
            {
                Debug.LogError(
                    $"PlayerHumanWeaponView: prefab '{prefab.name}' has no MapUnitWeaponOne; fix the prefab instead of runtime fallback.");
                Object.Destroy(instance);
                return;
            }

            _weaponCtrl.RegisterDynamicWeapon(_equippedView);
        }

        static GameObject LoadViewPrefab(HumanWeapon def)
        {
            if (!string.IsNullOrEmpty(def.ViewPrefab))
            {
                var custom = Resources.Load<GameObject>(def.ViewPrefab);
                if (custom != null)
                {
                    return custom;
                }

                Debug.LogWarning(
                    $"PlayerHumanWeaponView: view_prefab '{def.ViewPrefab}' not found, trying default.");
            }

            return Resources.Load<GameObject>(HumanWeaponCatalog.DefaultViewPrefab);
        }

        void ApplySprite(HumanWeapon def)
        {
            if (_equippedView == null || _equippedView.weaponParts == null)
            {
                return;
            }

            var sprite = LoadViewSprite(def.ViewSprite);
            if (sprite == null)
            {
                return;
            }

            foreach (var part in _equippedView.weaponParts)
            {
                if (part?.spriteVisual != null)
                {
                    part.spriteVisual.sprite = sprite;
                }
            }
        }

        static Sprite LoadViewSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            var sprite = SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
            if (sprite != null)
            {
                return sprite;
            }

            return SimpleResManager.Load<Sprite>("Sprites/" + spriteName);
        }

    }
}
