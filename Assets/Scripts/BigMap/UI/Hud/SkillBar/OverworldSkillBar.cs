using My.Input;
using My.Map;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace My.UI
{
    public class OverworldSkillBar : MonoBehaviour
    {
        protected OverworldHUDPanel HudPanel { get; private set; }

        public GridLayoutGroup grid;

        public GameObject SkillSlotTemplate;

        public Transform pageSpotContainer;
        //public Button prevButton;
        //public Button nextButton;

        [Header("Grid Config")]
        public int rows = 1;
        public int columns = 5;

        //private List<BattleSkillBarPageSpot> pageSpots = new List<BattleSkillBarPageSpot>();
        private List<OverworldSkillBarSlot> slots = new List<OverworldSkillBarSlot>();
        private int pageIndex = 0;
        private int capacity;

        public int TotalPages;

        public bool IsFaQingSpecialSkill;

        private string[] showSkills = null;


        void Awake()
        {
            capacity = rows * columns;

            SkillSlotTemplate.gameObject.SetActive(false);

            for (int i=0;i<capacity;i++)
            {
                var slotGo = GameObject.Instantiate(SkillSlotTemplate, grid.transform);
                var slotComp = slotGo.GetComponent<OverworldSkillBarSlot>();
                slots.Add(slotComp);

                slotGo.SetActive(true);
                slotComp.Setup(this, i);

                slotComp.shineRect.gameObject.SetActive(false);
            }

            //for (int i = 0; i < pageSpotContainer.transform.childCount; i++)
            //{
            //    var child = pageSpotContainer.transform.GetChild(i);
            //    var pageSpot = child.GetComponent<BattleSkillBarPageSpot>();
            //    pageSpot.pageIndex = i;
            //    pageSpot.button.onClick.AddListener(() =>
            //    {
            //        OnSkillPageChanged(pageSpot);
            //    });
            //    pageSpots.Add(pageSpot);
            //}

            //Refresh();
        }

        /// <summary>
        /// 初始化技能组
        /// </summary>
        public void InitSkills(OverworldHUDPanel hudPanel)
        {
            this.HudPanel = hudPanel;

            pageIndex = 0;
            Refresh();
        }

        public void OnSkillReplaced(int idx, int skillId)
        {
            Refresh();
        }

        //private void OnSkillPageChanged(BattleSkillBarPageSpot pageSpot)
        //{
        //    pageIndex = pageSpot.pageIndex;
        //    foreach (var spot in pageSpots)
        //    {
        //        if (spot == pageSpot)
        //        {
        //            spot.SetStatus(true);
        //        }
        //        else
        //        {
        //            spot.SetStatus(false);
        //        }
        //    }
        //    Refresh();
        //}


        /// <summary>
        /// 是否翻转
        /// </summary>
        /// <param name="flip"></param>
        public void Refresh(bool hint = false)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            var mdm = glm?.playerDataManager;
            if (player == null || mdm == null)
            {
                return;
            }

            var showSkills = mdm.GetSkillSlotsByState();
            if (showSkills == null)
            {
                return;
            }

            bool humanQuickBar = glm.IsHumanQuickBarAvailable();

            for (int i = 0; i < showSkills.Length; i++)
            {
                if (i >= slots.Count)
                {
                    break;
                }

                slots[i].ApplyKeyHint(i, humanQuickBar);

                string skillName = showSkills[i];
                if (humanQuickBar && i == 0 && !mdm.IsUsingFaQingSkillBar())
                {
                    skillName = mdm.HumanQuickBar.ResolveLeftClickSkillId();
                }

                if (string.IsNullOrEmpty(skillName))
                {
                    slots[i].Clear();
                    continue;
                }

                if (player.ablilityManager.SkillRuntimes.TryGetValue(skillName, out var skillRuntime)
                    && skillRuntime != null)
                {
                    slots[i].BindingSkill(skillRuntime, hint);
                }
                else
                {
                    slots[i].BindingSkillId(skillName, hint);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slotIdx"></param>
        public void OnSkillSlotClicked(int slotIdx)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var mdm = glm?.playerDataManager;
            var showSkills = mdm?.GetSkillSlotsByState();

            if (showSkills == null || slotIdx < 0 || slotIdx >= showSkills.Length)
            {
                return;
            }

            string skillId;
            if (glm != null && glm.IsHumanQuickBarAvailable() && slotIdx == 0 && !mdm.IsUsingFaQingSkillBar())
            {
                skillId = mdm.HumanQuickBar.ResolveLeftClickSkillId();
            }
            else
            {
                skillId = showSkills[slotIdx];
            }

            if (string.IsNullOrEmpty(skillId))
            {
                return;
            }

            HudPanel.OnClickUseSkill(skillId);
        }
    }
}

