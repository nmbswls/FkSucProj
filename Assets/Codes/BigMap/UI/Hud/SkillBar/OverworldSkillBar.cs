using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;

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
            int start = pageIndex * capacity;

            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

            var showSkills = MainGameManager.Instance.gameLogicManager.playerDataManager.GetSkillSlotsByState();

            if (showSkills == null)
            {
                return;
            }

            for (int i = 0; i < showSkills.Length; i++)
            {
                if (i >= slots.Count)
                {
                    break;
                }


                if (showSkills[i] == null)
                {
                    slots[i].Clear();
                }
                else
                {
                    var skillName = showSkills[i];
                    player.ablilityManager.SkillRuntimes.TryGetValue(skillName, out var skillRuntime);
                    if (skillRuntime == null)
                    {
                        slots[i].Clear();
                    }
                    else
                    {
                        slots[i].BindingSkill(skillRuntime, hint);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slotIdx"></param>
        public void OnSkillSlotClicked(int slotIdx)
        {
            if (showSkills == null)
            {
                Debug.LogError("OnSkillSlotClicked no skill slots");
                return;
            }

            if (slotIdx < 0 || slotIdx >= showSkills.Length)
            {
                Debug.LogError("OnSkillSlotClicked slot index invalid");
                return;
            }
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            player.ablilityManager.SkillRuntimes.TryGetValue(showSkills[slotIdx], out var skillRuntime);
            if (skillRuntime == null)
            {
                Debug.LogError("OnSkillSlotClicked no skill instance found.");
                return;
            }

            bool isReady = player.ablilityManager.IsSkillReady(showSkills[slotIdx]);
            if(!isReady)
            {
                return;
            }
            HudPanel.OnClickUseSkill(skillRuntime.SkillName);
        }


        
    }
}

