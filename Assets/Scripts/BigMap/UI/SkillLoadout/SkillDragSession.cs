using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public static class SkillDragSession
    {
        static GameObject _ghostRoot;
        static Image _ghostIcon;
        static TextMeshProUGUI _ghostLabel;
        static Canvas _rootCanvas;

        public static string DraggingSkillId { get; private set; }

        public static ISkillDropBehavior ActiveDropBehavior { get; private set; }

        public static bool IsDragging => !string.IsNullOrEmpty(DraggingSkillId);

        public static void Configure(GameObject ghostRoot, Image ghostIcon, TextMeshProUGUI ghostLabel, Canvas canvas)
        {
            _ghostRoot = ghostRoot;
            _ghostIcon = ghostIcon;
            _ghostLabel = ghostLabel;
            _rootCanvas = canvas;
            if (_ghostRoot != null)
                _ghostRoot.SetActive(false);
        }

        public static void SetCanvas(Canvas canvas) => _rootCanvas = canvas;

        public static void Begin(string skillId, ISkillDropBehavior dropBehavior)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            DraggingSkillId = skillId;
            ActiveDropBehavior = dropBehavior;

            if (_ghostRoot != null)
            {
                _ghostRoot.SetActive(true);
                if (_ghostLabel != null)
                    _ghostLabel.text = skillId;

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
        }

        public static void FollowScreenPoint(Vector2 screenPos)
        {
            if (_ghostRoot == null || _rootCanvas == null) return;

            var canvasRect = _rootCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                _rootCanvas.worldCamera,
                out var local);

            _ghostRoot.transform.localPosition = local;
        }

        public static void End()
        {
            DraggingSkillId = null;
            ActiveDropBehavior = null;
            if (_ghostRoot != null)
                _ghostRoot.SetActive(false);
        }
    }
}
