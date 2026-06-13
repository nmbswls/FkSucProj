using System.Collections.Generic;
using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    // 拖拽会话：OnDrop 优先提交；OnEndDrag 仅在未提交时射线回退，避免重复装配。
    public static class SkillDragSession
    {
        static GameObject _ghostRoot;
        static RectTransform _ghostRect;
        static RectTransform _ghostParentRect;
        static Image _ghostIcon;
        static TextMeshProUGUI _ghostLabel;

        static bool _dropCommitted;

        public static string DraggingSkillId { get; private set; }

        public static ISkillDropBehavior ActiveDropBehavior { get; private set; }

        public static bool IsDragging => !string.IsNullOrEmpty(DraggingSkillId);

        public static void Configure(GameObject ghostRoot, Image ghostIcon, TextMeshProUGUI ghostLabel, Canvas canvas)
        {
            _ghostRoot = ghostRoot;
            _ghostIcon = ghostIcon;
            _ghostLabel = ghostLabel;
            CacheGhostRects();
            if (_ghostRoot != null)
            {
                _ghostRoot.SetActive(false);
            }
        }

        public static void SetCanvas(Canvas canvas)
        {
            // 保留接口兼容；ghost 坐标改由 ghost 父节点换算，不再依赖外部 canvas。
        }

        public static void Begin(string skillId, ISkillDropBehavior dropBehavior)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            if (IsDragging)
            {
                ClearGhostOnly();
            }

            _dropCommitted = false;
            DraggingSkillId = skillId;
            ActiveDropBehavior = dropBehavior;
            CacheGhostRects();

            if (_ghostRoot == null)
            {
                return;
            }

            _ghostRoot.transform.SetAsLastSibling();
            _ghostRoot.SetActive(true);

            if (_ghostLabel != null)
            {
                _ghostLabel.text = skillId;
            }

            if (_ghostIcon != null)
            {
                var cfg = SkillLibrary.GetSkillConfig(skillId);
                if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
                {
                    var sp = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
                    _ghostIcon.sprite = sp;
                    _ghostIcon.enabled = sp != null;
                }
                else
                {
                    _ghostIcon.sprite = null;
                    _ghostIcon.enabled = false;
                }
            }
        }

        public static void FollowScreenPoint(Vector2 screenPos)
        {
            if (!IsDragging)
            {
                return;
            }

            CacheGhostRects();
            if (_ghostRect == null || _ghostParentRect == null)
            {
                return;
            }

            if (!_ghostRoot.activeSelf)
            {
                _ghostRoot.SetActive(true);
                _ghostRoot.transform.SetAsLastSibling();
            }

            var canvas = _ghostParentRect.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = canvas.worldCamera;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _ghostParentRect,
                    screenPos,
                    cam,
                    out var local))
            {
                _ghostRect.localPosition = local;
            }
        }

        // 拖拽源 OnEndDrag：未由 OnDrop 处理时，先尝试落点，再尝试空投。
        public static void EndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            if (!_dropCommitted && eventData != null)
            {
                if (TryFinalizeDropAtScreen(eventData.position))
                {
                    TryCommitDropEmpty();
                }
            }
            else if (!_dropCommitted)
            {
                TryCommitDropEmpty();
            }

            Clear();
        }

        public static void End() => Clear();

        // 由 SkillSlotDropZone.OnDrop 调用；同一次拖拽只会进入一次。
        public static bool TryCommitDropToZone(SkillSlotDropZone zone)
        {
            if (!IsDragging || _dropCommitted || zone == null || zone.view == null)
            {
                return false;
            }

            var panel = SkillLoadoutPanel.Current;
            if (panel == null)
            {
                return false;
            }

            if (zone.mode == SkillSlotDropMode.Fixed)
            {
                return false;
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr?.SkillSystem == null)
            {
                return false;
            }

            var skillId = DraggingSkillId;
            var sys = mgr.SkillSystem;
            var behavior = ActiveDropBehavior;
            if (behavior == null)
            {
                Debug.Log("Skill drop rejected: no_drop_behavior");
                return false;
            }

            if (!behavior.TryDropOnSlot(panel, sys, zone.view.slotKind, zone.view.SlotIndex, skillId, out var fail))
            {
                if (!string.IsNullOrEmpty(fail))
                {
                    Debug.Log("Skill drop rejected: " + fail);
                }

                return false;
            }

            _dropCommitted = true;
            panel.ApplyLoadoutToEntity();
            panel.RefreshAll();
            // OnDrop 成功后会 RefreshAll 销毁拖拽源，OnEndDrag 可能不会再触发，此处必须清理 ghost。
            Clear();
            return true;
        }

        // 返回 true 表示指针未落在任何 DropZone 上，可尝试空投；落在 Zone 上但失败则返回 false。
        static bool TryFinalizeDropAtScreen(Vector2 screenPos)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return true;
            }

            var pointerData = new PointerEventData(eventSystem)
            {
                position = screenPos,
            };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var zone = results[i].gameObject.GetComponentInParent<SkillSlotDropZone>();
                if (zone == null)
                {
                    continue;
                }

                TryCommitDropToZone(zone);
                return false;
            }

            return true;
        }

        static void TryCommitDropEmpty()
        {
            var panel = SkillLoadoutPanel.Current;
            var sys = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            if (panel == null || sys == null || ActiveDropBehavior == null)
            {
                return;
            }

            ActiveDropBehavior.OnDropToEmpty(panel, sys);
        }

        static void CacheGhostRects()
        {
            if (_ghostRoot == null)
            {
                _ghostRect = null;
                _ghostParentRect = null;
                return;
            }

            if (_ghostRect == null)
            {
                _ghostRect = _ghostRoot.transform as RectTransform;
            }

            if (_ghostRect != null)
            {
                _ghostParentRect = _ghostRect.parent as RectTransform;
            }
        }

        static void ClearGhostOnly()
        {
            if (_ghostRoot != null)
            {
                _ghostRoot.SetActive(false);
            }
        }

        static void Clear()
        {
            DraggingSkillId = null;
            ActiveDropBehavior = null;
            _dropCommitted = false;
            ClearGhostOnly();
        }
    }
}
