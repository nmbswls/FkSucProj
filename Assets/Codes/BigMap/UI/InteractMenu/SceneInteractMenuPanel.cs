using DG.Tweening;
using My.Map;
using My.Map.Scene;
using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static My.Input.QuickPlayerInputBinder;
using static My.UI.UISceneInteractMenu4Choose;
using static SceneInteractSystem;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

namespace My.UI
{

    /// <summary>
    /// 交互面板
    /// </summary>
    public class SceneInteractMenuPanel : PanelBase, IInputConsumer, IRefreshable
    {
        public static SceneInteractMenuPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("InteractMenu");
                if (panel != null && panel is SceneInteractMenuPanel interactMenu)
                {
                    return interactMenu;
                }
                return null;
            }
        }


        /// <summary>
        /// UI组件
        /// </summary>
        /// 
        public RectTransform ObjDetailHint;

        [Header("根canvas group")]
        public CanvasGroup ObjDetailHintCG;

        public RectTransform CenterHint;
        public GameObject ObjHintIconEnable;
        public GameObject ObjHintIconForbid;

        public RectTransform ObjFloatingNameBox;
        public TextMeshProUGUI ObjFloatingNameText;
        public RectTransform ObjSwitchHint;

        public UISceneInteractMenu4Choose ChooseInteractMenu;

        public bool WithHigherInteract;


        public RectTransform HModeExecuteHint;

        /// <summary>
        /// 当前活跃可交互列表
        /// </summary>
        public List<IntResultItem> ActiveInteractableList = new();
        public ISceneInteractable? currFocusInteractable { get; set; } = null;

        public SceneNpcPresenter? currExecuteTarget = null;


        public void Awake()
        {
            ObjDetailHint.gameObject.SetActive(false);

            //// 对 UI 元素进行呼吸
            //ObjHintCircle.DOScale(new Vector3(1.1f, 1.1f, 1f), 0.8f)
            //        .SetLoops(-1, LoopType.Yoyo)
            //        .SetEase(Ease.InOutSine)
            //        .SetUpdate(true); // 即使游戏暂停(Time.timeScale=0)也能继续呼吸

            ObjHintIconEnable.SetActive(true);
            ObjHintIconForbid.SetActive(false);

            ObjSwitchHint.gameObject.SetActive(false);

            HModeExecuteHint.gameObject.SetActive(false);
        }

        private float _interactViewUpdateTimer = 0;
        private Vector2? ActiveClosePanelPos = null; // 主动关闭交互面板的位置


        public void Update()
        {
            TryUpdateInteractSelections();

            RefreshFocusInteractable();

            if(currFocusInteractable != null)
            {
                // 更新详情条位置
                var hintPos = currFocusInteractable.GetHintAnchorPosition();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                ObjDetailHint.transform.localPosition = localPos;
            }

            if(currExecuteTarget != null)
            {
                // 更新详情条位置
                var hintPos = currExecuteTarget.GetHintAnchorPosition();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                HModeExecuteHint.transform.localPosition = localPos;
            }

            TickRefreshNormalInteractBlock();
        }

        private float _refreshSelectionTimer = 0;
        public void ResetRefreshSelection()
        {
            _refreshSelectionTimer = 0;
        }
        /// <summary>
        /// 尝试更新已存在的交互
        /// 主要用于更新cd等
        /// </summary>
        private void TryUpdateInteractSelections()
        {
            if(LogicTime.time - _refreshSelectionTimer < 1f)
            {
                return;
            }

            _refreshSelectionTimer = LogicTime.time;

            if(currFocusInteractable == null)
            {
                return;
            }

            var innerList = new List<ChooseItem>();
            var selections = currFocusInteractable.GetInteractSelections();
            foreach (var one in selections)
            {
                innerList.Add(new ChooseItem()
                    {
                        SelectId = one.SelectId,
                        Content = one.SelectContent,
                        Selectable = one.Selectable
                    }
                );
            }

            bool same = true;
            if (innerList.Count != ChooseInteractMenu.data.Count)
            {
                same = false;
            }
            else
            {
                for (int i = 0; i < innerList.Count; i++)
                {
                    if (innerList[i].SelectId != ChooseInteractMenu.data[i].SelectId
                        || innerList[i].Content != ChooseInteractMenu.data[i].Content
                         || innerList[i].Selectable != ChooseInteractMenu.data[i].Selectable)
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (!same)
            {
                ChooseInteractMenu.SetData(innerList);
            }
        }

        /// <summary>
        /// 切换下一个focus目标
        /// </summary>
        public void CycleNextFocusTarget()
        {
            if (ActiveInteractableList.Count <= 1) return; // 只有一个或没有，不用切

            // 1. 这是一个好的时机对列表进行一次排序
            // 让切换顺序符合直觉（比如按从左到右，或由近到远）
            // 注意：不要每帧排序，只在切换时排序
            SortCandidatesByDistance();

            int currentIndex = -1;
            if (currFocusInteractable != null)
            {
                currentIndex = ActiveInteractableList.FindIndex(x => x.interactable == currFocusInteractable);
            }

            int nextIndex = 0;
            if (currentIndex == -1)
            {
                nextIndex = 0;
            }
            else
            {
                nextIndex = (currentIndex + 1) % ActiveInteractableList.Count;
            }


            if (currFocusInteractable != null)
            {
                if (currFocusInteractable is SceneNpcPresenter npcPresenter)
                {
                    npcPresenter.InteractDetailMode = false;
                }
            }

            currFocusInteractable = ActiveInteractableList[nextIndex].interactable;
            UpdateFocusInteractableView();
        }

        /// <summary>
        /// 排序
        /// </summary>
        private void SortCandidatesByDistance()
        {
            var playerPos = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

            //ActiveInteractableList.Sort();
        }

        /// <summary>
        /// 检查锁交互面板
        /// </summary>
        private void TickRefreshNormalInteractBlock()
        {
            var playerEntity = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            if(playerEntity == null) return;


            bool blocked = false;

            bool selfInteract = false;
            if(playerEntity.IsInStealth())
            {
                selfInteract = true;
            }

            if (playerEntity.AtttachingObjList.Count > 0)
            {
                selfInteract = true;
            }

            if(selfInteract)
            {
                blocked = true;
            }

            if (currExecuteTarget != null)
            {
                blocked = true;
            }

            UpdateNormalInteractBlock(blocked);
        }

        /// <summary>
        /// 保底刷新交互列表
        /// </summary>
        public void RefreshFocusInteractable()
        {
            if(ActiveInteractableList.Count > 0)
            {
                if(currFocusInteractable == ActiveInteractableList[0].interactable)
                {
                    return;
                }

                if (currFocusInteractable != null)
                {
                    OnInteractUnFocus(currFocusInteractable);
                }

                currFocusInteractable = ActiveInteractableList[0].interactable;
            }
            else
            {
                if(currFocusInteractable == null)
                {
                    return;
                }
                OnInteractUnFocus(currFocusInteractable);
                currFocusInteractable = null;
            }
        }

        /// <summary>
        /// 更新视图
        /// </summary>
        private void UpdateFocusInteractableView()
        {
            if(currFocusInteractable == null)
            {
                ObjDetailHint.gameObject.SetActive(false);
            }
            else
            {
                ObjDetailHint.gameObject.SetActive(true);

                // 更新详情条位置
                var hintPos = currFocusInteractable.GetHintAnchorPosition();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                ObjDetailHint.transform.localPosition = localPos;

                var selections = currFocusInteractable.GetInteractSelections();

                var innerList = new List<ChooseItem>();
                foreach (var one in selections)
                {
                    innerList.Add(new ChooseItem()
                        {
                            SelectId = one.SelectId,
                            Content = one.SelectContent,
                            Selectable = one.Selectable
                        }
                    );
                }
                ChooseInteractMenu.SetData(innerList);

                ObjFloatingNameText.text = currFocusInteractable.ShowName;

                // 有可切换项时 切换
                if(ActiveInteractableList.Count > 1)
                {
                    ObjSwitchHint.gameObject.SetActive(true);
                }
                else
                {
                    ObjSwitchHint.gameObject.SetActive(false);
                }

                var nameOffset = currFocusInteractable.GetHintOffsetInfos();
                if(nameOffset < 0)
                {
                    ObjFloatingNameBox.transform.localPosition = new Vector3(0, 40.0f, 0);
                }
                else
                {
                    ObjFloatingNameBox.transform.localPosition = new Vector3(0, nameOffset, 0);
                }
            }
        }


        /// <summary>
        /// 刷新交互物
        /// </summary>
        /// <param name="interactables"></param>
        public void RefreshActiveInteractableObjs(List<IntResultItem> interactables)
        {
            // 仅维护当前
            this.ActiveInteractableList.Clear();
            if(interactables.Count > 0)
            {
                var firstPoint = interactables[0];
                this.ActiveInteractableList.Add(firstPoint);


                for (int i = 1; i < interactables.Count; i++)
                {
                    if ((interactables[i].pos - firstPoint.pos).sqrMagnitude < 0.3f * 0.3f)
                    {
                        this.ActiveInteractableList.AddRange(interactables);
                    }
                }

                //foreach (var oneInt in interactables) 
                //{
                //    this.ActiveInteractableList.Add(oneInt);
                //}
            }

            //// 当可交互列表
            //if (currFocusInteractable != null)
            //{
            //    var currentIndex = ActiveInteractableList.FindIndex(x => x.interactable == currFocusInteractable);
            //    if(currentIndex == -1)
            //    {
            //        currFocusInteractable = null;
            //    }
            //}

            // 刷新focus对象
            RefreshFocusInteractable();
        }


        /// <summary>
        /// 刷新处决列表
        /// </summary>
        public void RefreshExecuteTarget(SceneNpcPresenter npcPresenter)
        {
            if(npcPresenter == currExecuteTarget)
            {
                return;
            }

            currExecuteTarget = npcPresenter;

            if(currExecuteTarget == null)
            {
                HModeExecuteHint.gameObject.SetActive(false);
            }
            else
            {
                HModeExecuteHint.gameObject.SetActive(true);

                // 更新详情条位置
                var hintPos = currExecuteTarget.GetHintAnchorPosition();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                HModeExecuteHint.transform.localPosition = localPos;
            }
            

            if (currExecuteTarget != null)
            {
                UpdateNormalInteractBlock(true);
            }
            else
            {
                UpdateNormalInteractBlock(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="block"></param>
        public void UpdateNormalInteractBlock(bool block)
        {
            this.WithHigherInteract = block;

            if(block)
            {
                ObjDetailHintCG.alpha = 0.6f;

                ObjHintIconEnable.SetActive(false);
                ObjHintIconForbid.SetActive(true);
                //ChooseInteractMenu.SetBlockInteract(true);
            }
            else
            {
                ObjDetailHintCG.alpha = 1f;

                ObjHintIconEnable.SetActive(true);
                ObjHintIconForbid.SetActive(false);
                //ChooseInteractMenu.SetBlockInteract(false);
            }
        }

        private void OnInteractUnFocus(ISceneInteractable interactable)
        {
            if (currFocusInteractable is SceneNpcPresenter npcPresenter)
            {
                npcPresenter.InteractDetailMode = false;
            }
        }


        /// <summary>
        /// 确认
        /// </summary>
        /// <returns></returns>
        public bool OnConfirm()
        {

            if(currExecuteTarget != null)
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.UseSkill("h_mode_execute", target:currExecuteTarget.NpcEntity);
                return true;
            }


            if (currFocusInteractable == null)
            {
                return false;
            }

            int idx = ChooseInteractMenu.CurrentIndex;
            ChooseInteractMenu.ItemOnClick(idx);

            int selectId = (int)ChooseInteractMenu.data[idx].SelectId;
            currFocusInteractable.TriggerInteract(selectId);

            return true;
        }

        public bool OnCancel()
        {
            return false;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return false;
        }


        /// <summary>
        /// 监听滚轮事件
        /// </summary>
        /// <param name="deltaY"></param>
        /// <returns></returns>
        public bool OnScroll(float deltaY)
        {
            if (WithHigherInteract)
            {
                return false;
            }

            if (currFocusInteractable == null)
            {
                return false;
            }


            if (Mathf.Abs(deltaY) > 0.01f)
            {
                if (deltaY > 0f)
                {
                    ChooseInteractMenu.MoveCursor(-1);  // 上滚：索引减
                }
                else
                {
                    ChooseInteractMenu.MoveCursor(1);  // 上滚：索引减
                }
            }
            return true;
        }

        public void Refresh()
        {
            //
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            return false;
        }

        public bool OnHoldUpdate(string holdKey)
        {
            return false;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            return false;
        }

        public bool OnHotkey(string keyName)
        {
            //throw new System.NotImplementedException();
            if(keyName == EInputKey.Tab.ToString())
            {
                if(ActiveInteractableList.Count > 1)
                {
                    CycleNextFocusTarget();

                    return true;
                }
            }
            return false;
        }
    }

}
