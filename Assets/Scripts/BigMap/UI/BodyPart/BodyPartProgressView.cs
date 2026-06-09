using System.Collections.Generic;
using cfg.demo;
using My.Config;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{

    public class BodyPartProgressSeg : MonoBehaviour
    {
        public Image Line;

        static readonly Color LitLineColor = new Color(0.88f, 0.74f, 0.32f, 1f);
        static readonly Color LockedLineColor = new Color(0.23529412f, 0.23529412f, 0.23529412f, 1f);

        public void Bind()
        {
            Line = GetComponentInChildren<Image>();
        }

        public void ShowActive(bool active)
        {
            if(active)
            {
                Line.color = LitLineColor;
            }
            else
            {
                Line.color = LockedLineColor;
            }
        }
    }

    // 部位养成里程碑进度线：SlotRow 下按 OneSlot 横向拼装
    public sealed class BodyPartProgressView : MonoBehaviour
    {
        [SerializeField] Transform slotRoot;
        [SerializeField] BodyPartProgressSlotView slotTemplate;

        readonly List<BodyPartProgressSlotView> _slots = new();
        readonly List<BodyPartProgressInfo> _milestones = new();

        public Transform LineRoot;
        [SerializeField]  GameObject OneSegTemplate;
        readonly List<BodyPartProgressSeg> _segLines = new();

        EBodyPart _boundPart = EBodyPart.None;
        int _currentLevel;

        public void Refresh(EBodyPart partId, int currentLevel)
        {
            if (slotRoot == null || slotTemplate == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            if (_boundPart != partId)
            {
                _boundPart = partId;
            }

            _currentLevel = Mathf.Max(0, currentLevel);
            BuildMilestones(partId);
            EnsureSlotCount(_milestones.Count);
            EnsureSegCount(_milestones.Count - 1);

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i >= _milestones.Count)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                var cfg = _milestones[i];
                int segmentStart = i > 0 ? _milestones[i - 1].Level : 0;
                slot.gameObject.SetActive(true);
                slot.Bind(cfg, segmentStart, _currentLevel, hideLine: false);
            }

            for (int i = 0; i < _segLines.Count; i++)
            {
                var seg = _segLines[i];
                if (seg == null)
                {
                    continue;
                }

                if (i >= _milestones.Count - 1)
                {
                    seg.ShowActive(false);
                }
                else
                {
                    seg.ShowActive(true);
                }
            }
        }

        void BuildMilestones(EBodyPart partId)
        {
            _milestones.Clear();
            if (partId == EBodyPart.None)
            {
                return;
            }

            var list = CfgMgr.Cfgs.TbBodyPartProgressInfo.DataList;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row != null && row.PartId == partId)
                {
                    _milestones.Add(row);
                }
            }

            _milestones.Sort((a, b) => a.Level.CompareTo(b.Level));
        }

        void EnsureSlotCount(int count)
        {
            while (_slots.Count < count)
            {
                var view = Instantiate(slotTemplate, slotRoot);
                view.gameObject.SetActive(true);
                _slots.Add(view);
            }

            if (slotTemplate != null)
            {
                slotTemplate.gameObject.SetActive(false);
            }
        }

        void EnsureSegCount(int count)
        {
            while (_segLines.Count < count)
            {
                var view = Instantiate(OneSegTemplate, LineRoot);
                var comp = view.GetOrAddComponent<BodyPartProgressSeg>();
                view.gameObject.SetActive(true);
                _segLines.Add(comp);
            }

            if (OneSegTemplate != null)
            {
                OneSegTemplate.gameObject.SetActive(false);
            }
        }



        void OnDestroy()
        {
            ClearSpawnedSlots();
        }

        void ClearSpawnedSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && _slots[i] != slotTemplate)
                {
                    Destroy(_slots[i].gameObject);
                }
            }

            _slots.Clear();

            if (slotRoot == null || slotTemplate == null)
            {
                return;
            }

            for (int i = slotRoot.childCount - 1; i >= 0; i--)
            {
                var child = slotRoot.GetChild(i);
                if (child == slotTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }
    }
}
