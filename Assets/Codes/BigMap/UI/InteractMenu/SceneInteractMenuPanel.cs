using My.Map;
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
using static SceneInteractSystem;
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

        public RectTransform ObjFloatingNameBox;
        public TextMeshProUGUI ObjFloatingNameText;
        public RectTransform ObjSwitchHint;
        public UISceneInteractMenu4Choose ChooseInteractMenu;

        public bool WithHigherInteract;

        /// <summary>
        /// 当前活跃可交互列表
        /// </summary>
        public List<IntResultItem> ActiveInteractableList = new();
        public ISceneInteractable? currFocusInteractable = null;

        public void Awake()
        {

            ChooseInteractMenu.EvOnTabConfirmed += (idx) =>
            {

            };
            //ChooseObjMenu.EvOnCanceled += () =>
            //{
            //};

            ChooseInteractMenu.gameObject.SetActive(false);
            //ChooseObjMenu.gameObject.SetActive(false);
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
        }

        private float _refreshSelectionTimer = 0;

        /// <summary>
        /// 尝试更新已存在的交互
        /// 主要用于更新cd等
        /// </summary>
        private void TryUpdateInteractSelections()
        {
            if(LogicTime.time - _refreshSelectionTimer < 0.2f)
            {
                return;
            }

            _refreshSelectionTimer = LogicTime.time;

            if(currFocusInteractable == null)
            {
                return;
            }
            
            //if (ChooseInteractMenu.gameObject.activeSelf)
            //{


            //    var dif = MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos - currFocusInteractable.Pos;

            //    var innerList = new List<(long, string, bool)>();
            //    var selections = currFocusInteractable.GetInteractSelections(dif.magnitude);
            //    foreach (var one in selections)
            //    {
            //        innerList.Add(new(one.SelectId, one.SelectContent, one.Selectable));
            //    }

            //    bool same = true;
            //    if (innerList.Count != ChooseInteractMenu.data.Count)
            //    {
            //        same = false;
            //    }
            //    else
            //    {
            //        for (int i = 0; i < innerList.Count; i++)
            //        {
            //            if (innerList[i].Item1 != ChooseInteractMenu.data[i].Item1
            //                || innerList[i].Item2 != ChooseInteractMenu.data[i].Item2
            //                 || innerList[i].Item3 != ChooseInteractMenu.data[i].Item3)
            //            {
            //                same = false;
            //                break;
            //            }
            //        }
            //    }

            //    if (!same)
            //    {
            //        ChooseInteractMenu.SetData(innerList);
            //    }
            //}
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
        /// 保底刷新交互列表
        /// </summary>
        public void RefreshFocusInteractable()
        {
            if (currFocusInteractable == null && ActiveInteractableList.Count > 0)
            {
                currFocusInteractable = ActiveInteractableList[0].interactable;
                UpdateFocusInteractableView();
            }
            // 如果列表空了，彻底关闭UI
            else if (currFocusInteractable != null && ActiveInteractableList.Count == 0)
            {
                currFocusInteractable = null;
                UpdateFocusInteractableView();
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

                var innerList = new List<(long, string, bool)>();
                foreach (var one in selections)
                {
                    innerList.Add(new(one.SelectId, one.SelectContent, one.Selectable));
                }
                ChooseInteractMenu.SetData(innerList);

                ObjFloatingNameText.text = currFocusInteractable.ShowName;
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
                //var firstPoint = interactables[0];
                //this.ActiveInteractableList.Add(firstPoint);


                //for (int i= 1; i < interactables.Count; i++)
                //{
                //    if ((interactables[i].pos - firstPoint.pos).sqrMagnitude < 0.3f * 0.3f)
                //    {
                //        this.ActiveInteractableList.AddRange(interactables);
                //    }
                //}

                foreach (var oneInt in interactables) 
                {
                    this.ActiveInteractableList.Add(oneInt);
                }
            }

            currFocusInteractable = null;
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
        /// 
        /// </summary>
        /// <param name="block"></param>
        public void UpdateNormalInteractBlock(bool block)
        {
            this.WithHigherInteract = block;

            if(block)
            {
                ChooseInteractMenu.SetBlockInteract(true);
            }
            else
            {
                ChooseInteractMenu.SetBlockInteract(false);
            }
        }

        public bool OnConfirm()
        {
            if(WithHigherInteract)
            {
                return false;
            }

            if(currFocusInteractable == null)
            {
                return false;
            }

            int idx = ChooseInteractMenu.CurrentIndex;
            int content = (int)ChooseInteractMenu.data[idx].Item1;
            currFocusInteractable.TriggerInteract(content);

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
