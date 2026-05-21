using Animancer;
using cfg.demo;
using My.Config;
using My.Map.Scene;
using My.Player;
using UnityEngine;

namespace My.Player
{
    // 人类武器视觉：BindPoint1 下动态挂载通用 View，切换时换 Sprite
    public sealed class HumanWeaponVisualRuntime
    {
        Transform _bindPoint;
        MapUnitWeaponCtrl _weaponCtrl;
        MapUnitWeaponOne _equippedView;
        string _equippedItemId;

        public void Bind(SceneUnitPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            _weaponCtrl = presenter.WeaponCtrl;
            _bindPoint = presenter.transform.Find("WeaponRoot/BindPoint1");
            if (_bindPoint == null)
            {
                Debug.LogWarning("HumanWeaponVisualRuntime: BindPoint1 not found under WeaponRoot.");
            }
        }

        public void Equip(string itemId)
        {
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
                _equippedView.gameObject.SetActive(true);
            }
        }

        public void Unequip()
        {
            _equippedItemId = null;
            if (_equippedView != null)
            {
                _equippedView.gameObject.SetActive(false);
            }
        }

        void EnsureViewInstance(HumanWeapon def)
        {
            if (_equippedView != null)
            {
                return;
            }

            var prefabPath = string.IsNullOrEmpty(def.ViewPrefab)
                ? HumanWeaponCatalog.DefaultViewPrefab
                : def.ViewPrefab;
            var prefab = Resources.Load<GameObject>(prefabPath);
            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, _bindPoint);
            }
            else
            {
                instance = CreateFallbackView();
                instance.transform.SetParent(_bindPoint, false);
            }

            instance.name = HumanWeaponCatalog.ViewKey;
            _equippedView = instance.GetComponent<MapUnitWeaponOne>();
            if (_equippedView == null)
            {
                _equippedView = instance.AddComponent<MapUnitWeaponOne>();
            }

            _weaponCtrl.RegisterDynamicWeapon(_equippedView);
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

            var sprite = SimpleResManager.Load<Sprite>("Sprites/" + spriteName);
            if (sprite != null)
            {
                return sprite;
            }

            return SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
        }

        static GameObject CreateFallbackView()
        {
            var root = new GameObject(HumanWeaponCatalog.ViewKey);
            var rotator = new GameObject("Rotator").transform;
            rotator.SetParent(root.transform, false);
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(rotator, false);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.2f);
            col.offset = new Vector2(0.35f, 0f);

            var animator = root.AddComponent<Animator>();
            var animancer = root.AddComponent<AnimancerComponent>();
            animancer.Animator = animator;

            var weaponOne = root.AddComponent<MapUnitWeaponOne>();
            weaponOne.weaponAnimancer = animancer;
            weaponOne.weaponAnim = animator;
            weaponOne.weaponParts = new[]
            {
                new WeaponPart
                {
                    rotator = rotator,
                    spriteVisual = sr,
                },
            };
            return root;
        }
    }
}
