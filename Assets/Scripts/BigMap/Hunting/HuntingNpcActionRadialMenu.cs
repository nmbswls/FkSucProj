using System.Collections.Generic;
using My.Map.Scene;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.Hunting
{
    public class HuntingNpcActionRadialMenu : MonoBehaviour
    {
        public enum EActionSlot
        {
            Execute,
            Control,
        }

        [System.Serializable]
        public class ActionSlotData
        {
            public EActionSlot Slot;
            public string Label;
            public bool Interactable = true;
        }

        public RectTransform MenuRoot;
        public RectTransform SectorContainer;
        public RadialSectorItem SectorPrefab;

        public float Radius = 120f;
        public float InnerRadius = 36f;
        public Color ColorNormal = new Color(0.15f, 0.12f, 0.18f, 0.92f);
        public Color ColorHighlight = new Color(0.85f, 0.55f, 0.15f, 0.98f);

        readonly List<RadialSectorItem> _sectors = new();
        readonly List<ActionSlotData> _slots = new();
        SceneNpcPresenter _target;
        Camera _uiCam;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (MenuRoot == null)
            {
                MenuRoot = transform as RectTransform;
            }

            if (SectorContainer == null && MenuRoot != null)
            {
                SectorContainer = MenuRoot;
            }

            EnsureSectorPrefab();
            if (SectorPrefab != null)
            {
                SectorPrefab.gameObject.SetActive(false);
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCam = canvas.worldCamera;
            }

            SetOpen(false);
        }

        void EnsureSectorPrefab()
        {
            if (SectorPrefab != null)
            {
                return;
            }

            var menu = Resources.Load<MapPlayerRadialMenu>("UI/Prefabs/MapPlayerRadialMenu");
            if (menu != null)
            {
                SectorPrefab = menu.sectorPrefab;
            }
        }

        public void Show(SceneNpcPresenter target, bool canExecute, bool canControl)
        {
            _target = target;
            _slots.Clear();
            _slots.Add(new ActionSlotData
            {
                Slot = EActionSlot.Execute,
                Label = "处决",
                Interactable = canExecute,
            });
            _slots.Add(new ActionSlotData
            {
                Slot = EActionSlot.Control,
                Label = "操控",
                Interactable = canControl,
            });

            RebuildSectors();
            RefreshLayout();
            SetOpen(true);
        }

        public void Close()
        {
            _target = null;
            SetOpen(false);
            ClearSectors();
        }

        public bool TryHandleClick(Vector2 screenPos)
        {
            if (!IsOpen || SectorContainer == null)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    SectorContainer, screenPos, _uiCam, out Vector2 local))
            {
                Close();
                return true;
            }

            float dist = local.magnitude;
            if (dist < InnerRadius || dist > Radius * 1.15f)
            {
                Close();
                return true;
            }

            int idx = AngleToIndex(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
            if (idx < 0 || idx >= _slots.Count)
            {
                Close();
                return true;
            }

            var slot = _slots[idx];
            if (!slot.Interactable)
            {
                Close();
                return true;
            }

            var manager = HuntingModeManager.Instance;
            if (manager == null || _target == null)
            {
                Close();
                return true;
            }

            switch (slot.Slot)
            {
                case EActionSlot.Execute:
                    manager.TryExecuteTarget(_target);
                    break;
                case EActionSlot.Control:
                    manager.TryControlTarget(_target);
                    break;
            }

            Close();
            return true;
        }

        void RebuildSectors()
        {
            ClearSectors();
            if (SectorPrefab == null || SectorContainer == null)
            {
                return;
            }

            int count = _slots.Count;
            float step = 360f / count;
            float fillAmount = step / 360f;

            for (int i = 0; i < count; i++)
            {
                var slot = _slots[i];
                var inst = Instantiate(SectorPrefab, SectorContainer);
                inst.gameObject.SetActive(true);
                inst.index = i;

                var radialItem = new MapPlayerRadialMenu.RadialItem
                {
                    RadialFunc = MapPlayerRadialMenu.ERadialFunc.UseSkill,
                    SkillId = slot.Label,
                    Interactable = slot.Interactable,
                };
                inst.SetData(radialItem, ColorNormal, fillAmount);

                float startAngle = 0f - i * step;
                inst.SectRoot.localRotation = Quaternion.Euler(0f, 0f, startAngle + step / 2f - 1f);

                if (inst.label != null)
                {
                    inst.label.text = slot.Label;
                }

                if (inst.InfoRoot != null)
                {
                    float midAngleRad = Mathf.Deg2Rad * (startAngle + 90f);
                    Vector2 dir = new Vector2(Mathf.Cos(midAngleRad), Mathf.Sin(midAngleRad));
                    inst.InfoRoot.anchoredPosition = dir * ((Radius + InnerRadius) * 0.5f);
                }

                _sectors.Add(inst);
            }
        }

        void RefreshLayout()
        {
            if (_target == null || MenuRoot == null || UIManager.Instance == null)
            {
                return;
            }

            var hintPos = _target.GetHintAnchorPosition();
            var gameplayCam = Camera.main;
            if (gameplayCam == null)
            {
                return;
            }

            Vector3 screenPos = gameplayCam.WorldToScreenPoint(hintPos);
            var rootRt = UIManager.Instance.RootCanvas.transform as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootRt, screenPos, UIManager.Instance.UICamera, out Vector2 localInRoot))
            {
                return;
            }

            var positionParent = MenuRoot.parent as RectTransform;
            if (positionParent == null)
            {
                return;
            }

            Vector3 worldOnCanvas = rootRt.TransformPoint(new Vector3(localInRoot.x, localInRoot.y, 0f));
            MenuRoot.localPosition = positionParent.InverseTransformPoint(worldOnCanvas);
        }

        public void RefreshLayoutIfOpen()
        {
            if (IsOpen)
            {
                RefreshLayout();
            }
        }

        int AngleToIndex(float angleDeg)
        {
            int count = _slots.Count;
            if (count == 0)
            {
                return -1;
            }

            float step = 360f / count;
            return Mathf.RoundToInt(Mathf.Repeat((-angleDeg + 90f) / step, count)) % count;
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            if (MenuRoot != null)
            {
                MenuRoot.gameObject.SetActive(open);
            }
        }

        void ClearSectors()
        {
            foreach (var sector in _sectors)
            {
                if (sector != null)
                {
                    Destroy(sector.gameObject);
                }
            }

            _sectors.Clear();
        }
    }
}
