using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.UI.BodyPart
{
    // 部位养成里程碑进度线：SlotRow 下按 OneSlot 横向拼装
    public sealed class BodyPartProgressLineView : MonoBehaviour
    {
        [SerializeField] Transform slotRoot;
        [SerializeField] BodyPartProgressSlotView slotTemplate;

        readonly List<BodyPartProgressSlotView> _slots = new();
        readonly List<BodyPartProgressInfo> _milestones = new();

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
