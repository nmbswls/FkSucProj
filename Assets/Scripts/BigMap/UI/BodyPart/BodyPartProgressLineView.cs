using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;

namespace My.UI.BodyPart
{
    // 部位养成里程碑进度线：按 OneSlot 模板拼装，每格自带连线段与节点
    public sealed class BodyPartProgressLineView : MonoBehaviour
    {
        [SerializeField] Transform slotRoot;
        [SerializeField] BodyPartProgressSlotView slotTemplate;
        [SerializeField] TextMeshProUGUI detailTitleText;
        [SerializeField] TextMeshProUGUI detailDescText;
        [SerializeField] Transform bonusContent;
        [SerializeField] PartPropInfoRowView bonusRowTemplate;

        readonly List<BodyPartProgressSlotView> _slots = new();
        readonly List<PartPropInfoRowView> _bonusRows = new();
        readonly List<BodyPartProgressInfo> _milestones = new();

        EBodyPart _boundPart = EBodyPart.None;
        int _currentLevel;
        int _selectedMilestoneId;

        public void Refresh(EBodyPart partId, int currentLevel)
        {
            if (slotRoot == null || slotTemplate == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            if (_boundPart != partId)
            {
                _boundPart = partId;
                _selectedMilestoneId = 0;
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
                slot.Bind(
                    cfg,
                    segmentStart,
                    _currentLevel,
                    hideLine: false,
                    selected: cfg.Id == _selectedMilestoneId,
                    onSelected: OnSlotSelected);
            }

            if (_selectedMilestoneId <= 0)
            {
                _selectedMilestoneId = ResolveDefaultSelection();
            }

            RefreshSelectionVisual();
            RefreshDetail();
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

        void OnSlotSelected(int milestoneId)
        {
            _selectedMilestoneId = milestoneId;
            RefreshSelectionVisual();
            RefreshDetail();
        }

        void RefreshSelectionVisual()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || !slot.gameObject.activeSelf)
                {
                    continue;
                }

                slot.SetSelected(slot.MilestoneId == _selectedMilestoneId);
            }
        }

        void RefreshDetail()
        {
            BodyPartProgressInfo cfg = null;
            for (int i = 0; i < _milestones.Count; i++)
            {
                if (_milestones[i].Id == _selectedMilestoneId)
                {
                    cfg = _milestones[i];
                    break;
                }
            }

            if (cfg == null)
            {
                ClearBonusRows();
                if (detailTitleText != null)
                {
                    detailTitleText.text = string.Empty;
                }

                if (detailDescText != null)
                {
                    detailDescText.text = string.Empty;
                }

                return;
            }

            bool unlocked = _currentLevel >= cfg.Level;
            if (detailTitleText != null)
            {
                detailTitleText.text = $"Lv{cfg.Level} 里程碑";
            }

            if (detailDescText != null)
            {
                var desc = new StringBuilder();
                if (!string.IsNullOrEmpty(cfg.Desc))
                {
                    desc.Append(cfg.Desc);
                }

                if (!unlocked)
                {
                    if (desc.Length > 0)
                    {
                        desc.AppendLine();
                    }

                    desc.Append($"（未解锁，需要 Lv{cfg.Level}）");
                }

                detailDescText.text = desc.ToString();
            }

            ClearBonusRows();
            BuildBonusRows(cfg, unlocked);
        }

        void BuildBonusRows(BodyPartProgressInfo cfg, bool unlocked)
        {
            if (bonusContent == null || bonusRowTemplate == null)
            {
                return;
            }

            bool any = false;
            if (cfg.GlobalBonuses != null)
            {
                for (int i = 0; i < cfg.GlobalBonuses.Count; i++)
                {
                    var bonus = cfg.GlobalBonuses[i];
                    if (bonus == null)
                    {
                        continue;
                    }

                    any = true;
                    string prefix = unlocked ? string.Empty : "[锁定] ";
                    SpawnBonusRow($"{prefix}全局属性 {bonus.AttrId}: +{bonus.Val}");
                }
            }

            if (!string.IsNullOrEmpty(cfg.PassiveSkillId))
            {
                any = true;
                string prefix = unlocked ? string.Empty : "[锁定] ";
                SpawnBonusRow($"{prefix}被动: {cfg.PassiveSkillId}");
            }

            if (!any)
            {
                SpawnBonusRow(unlocked ? "(无额外加成)" : "(解锁后生效)");
            }
        }

        void SpawnBonusRow(string text)
        {
            if (bonusRowTemplate == null || bonusContent == null)
            {
                return;
            }

            var row = Instantiate(bonusRowTemplate, bonusContent);
            row.gameObject.SetActive(true);
            row.Bind(text);
            _bonusRows.Add(row);
        }

        void ClearBonusRows()
        {
            for (int i = 0; i < _bonusRows.Count; i++)
            {
                if (_bonusRows[i] != null)
                {
                    Destroy(_bonusRows[i].gameObject);
                }
            }

            _bonusRows.Clear();

            if (bonusContent == null || bonusRowTemplate == null)
            {
                return;
            }

            for (int i = bonusContent.childCount - 1; i >= 0; i--)
            {
                var child = bonusContent.GetChild(i);
                if (child == bonusRowTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        int ResolveDefaultSelection()
        {
            if (_milestones.Count == 0)
            {
                return 0;
            }

            int lastUnlockedId = 0;
            int firstLockedId = 0;
            for (int i = 0; i < _milestones.Count; i++)
            {
                var cfg = _milestones[i];
                if (_currentLevel >= cfg.Level)
                {
                    lastUnlockedId = cfg.Id;
                }
                else if (firstLockedId <= 0)
                {
                    firstLockedId = cfg.Id;
                }
            }

            if (lastUnlockedId > 0)
            {
                return lastUnlockedId;
            }

            return firstLockedId > 0 ? firstLockedId : _milestones[0].Id;
        }

        void OnDestroy()
        {
            ClearSpawnedSlots();
            ClearBonusRows();
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
