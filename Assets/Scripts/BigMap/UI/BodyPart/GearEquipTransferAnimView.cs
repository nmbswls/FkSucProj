using System;
using cfg.demo;
using DG.Tweening;
using My.Config;
using My.Player;
using My.UI;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // 部位装备面板：拆装时的飞入/飞出与已装备栏补位动画
    public sealed class GearEquipTransferAnimView : MonoBehaviour
    {
        [SerializeField] RectTransform fxLayer;
        [SerializeField] GearEquipEquippedBarView equippedBar;
        [SerializeField] GearEquipBagGridView bagGrid;
        [SerializeField] float flyDuration = 0.32f;
        [SerializeField] float slideDuration = 0.24f;
        [SerializeField] float flyArcHeight = 48f;
        [SerializeField] Ease flyEase = Ease.InOutQuad;
        [SerializeField] Ease slideEase = Ease.OutQuad;

        bool _busy;
        Sequence _sequence;

        public bool IsBusy => _busy;

        public void Configure(GearEquipEquippedBarView bar, GearEquipBagGridView grid, RectTransform layer = null)
        {
            equippedBar = bar;
            bagGrid = grid;
            if (layer != null)
            {
                fxLayer = layer;
            }
        }

        void Awake()
        {
            EnsureFxLayer();
        }

        void OnDisable()
        {
            KillSequence();
            _busy = false;
        }

        void EnsureFxLayer()
        {
            if (fxLayer != null)
            {
                return;
            }

            var go = new GameObject("GearEquipFxLayer", typeof(RectTransform));
            fxLayer = go.GetComponent<RectTransform>();
            fxLayer.SetParent(transform, false);
            fxLayer.anchorMin = Vector2.zero;
            fxLayer.anchorMax = Vector2.one;
            fxLayer.offsetMin = Vector2.zero;
            fxLayer.offsetMax = Vector2.zero;
            fxLayer.SetAsLastSibling();
        }

        public bool TryPlayUnequip(
            PlayerEquipmentManager equipment,
            EBodyPart part,
            int equippedIndex,
            Action onFinished)
        {
            if (_busy || equipment == null || equippedBar == null || bagGrid == null)
            {
                return false;
            }

            if (!equippedBar.TryGetFilledCell(equippedIndex, out var sourceCell, out var sourceIcon, out string itemId))
            {
                return false;
            }

            long itemInstanceId = 0;
            var equippedList = equipment.GetEquippedOnPart(part);
            if (equippedIndex >= 0 && equippedIndex < equippedList.Count && equippedList[equippedIndex] != null)
            {
                itemInstanceId = equippedList[equippedIndex].ItemInstanceId;
            }

            if (!equipment.TryUnequip(part, equippedIndex, out _))
            {
                return false;
            }

            bagGrid.Refresh(equipment, part, null);
            if (!bagGrid.TryFindCellIcon(itemInstanceId, itemId, out var bagIcon))
            {
                bagGrid.TryGetCandidatesAreaCenter(out bagIcon);
            }

            if (bagIcon != null)
            {
                bagGrid.SetIconVisible(bagIcon, false);
            }

            BeginSequence();
            var sprite = ItemCatalog.GetIcon(itemId);
            var flyIcon = SpawnFlyIcon(sourceIcon, sprite);
            sourceCell.SetVisualHidden(true);

            float step = equippedBar.GetCellStep();
            var slideTargets = equippedBar.CollectSlideTargetsAfter(equippedIndex);
            equippedBar.BeginManualLayout();

            _sequence.Join(CreateFlyTween(flyIcon, sourceIcon.position, bagIcon != null ? bagIcon.position : sourceIcon.position));
            for (int i = 0; i < slideTargets.Count; i++)
            {
                var rt = slideTargets[i];
                float targetX = rt.anchoredPosition.x - step;
                _sequence.Join(rt.DOAnchorPosX(targetX, slideDuration).SetEase(slideEase));
            }

            _sequence.OnComplete(() =>
            {
                DestroyFlyIcon(flyIcon);
                equippedBar.EndManualLayoutAndRemoveAt(equippedIndex);
                if (bagIcon != null)
                {
                    bagGrid.SetIconVisible(bagIcon, true);
                }

                Finish(onFinished);
            });

            return true;
        }

        public bool TryPlayEquip(
            PlayerEquipmentManager equipment,
            EBodyPart part,
            int bagFlatIndex,
            AnyContainerItemCell sourceCell,
            Action onFinished)
        {
            if (_busy || equipment == null || equippedBar == null || sourceCell == null)
            {
                return false;
            }

            if (!sourceCell.TryGetIconRect(out var sourceIcon))
            {
                return false;
            }

            string itemId = sourceCell.GetBoundStack()?.ItemID;
            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            if (!equipment.TryEquipFromMainBag(part, bagFlatIndex, out _))
            {
                return false;
            }

            if (!equippedBar.TryGetAppendSlotWorldPos(out var targetWorld))
            {
                targetWorld = sourceIcon.position;
            }

            BeginSequence();
            var sprite = ItemCatalog.GetIcon(itemId);
            var flyIcon = SpawnFlyIcon(sourceIcon, sprite);
            sourceCell.SetVisualHidden(true);

            _sequence.Append(CreateFlyTween(flyIcon, sourceIcon.position, targetWorld));

            _sequence.OnComplete(() =>
            {
                DestroyFlyIcon(flyIcon);
                Finish(onFinished);
            });

            return true;
        }

        void BeginSequence()
        {
            KillSequence();
            _busy = true;
            _sequence = DOTween.Sequence().SetUpdate(true);
        }

        void Finish(Action onFinished)
        {
            KillSequence();
            _busy = false;
            onFinished?.Invoke();
        }

        void KillSequence()
        {
            if (_sequence == null)
            {
                return;
            }

            _sequence.Kill();
            _sequence = null;
        }

        Tween CreateFlyTween(RectTransform flyIcon, Vector3 startWorld, Vector3 endWorld)
        {
            flyIcon.position = startWorld;
            var mid = (startWorld + endWorld) * 0.5f;
            mid.y += flyArcHeight;

            var path = new[]
            {
                (Vector3)startWorld,
                mid,
                endWorld,
            };

            return flyIcon
                .DOPath(path, flyDuration, PathType.CatmullRom)
                .SetEase(flyEase);
        }

        RectTransform SpawnFlyIcon(RectTransform sourceIcon, Sprite sprite)
        {
            EnsureFxLayer();

            var go = new GameObject("GearEquipFlyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(fxLayer, false);
            rt.sizeDelta = sourceIcon != null ? sourceIcon.rect.size : new Vector2(48f, 48f);
            rt.position = sourceIcon != null ? sourceIcon.position : Vector3.zero;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = sprite != null;

            return rt;
        }

        static void DestroyFlyIcon(RectTransform flyIcon)
        {
            if (flyIcon != null)
            {
                Destroy(flyIcon.gameObject);
            }
        }
    }
}
