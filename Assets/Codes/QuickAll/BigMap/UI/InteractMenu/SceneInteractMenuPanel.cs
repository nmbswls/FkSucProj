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

namespace My.UI
{
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

        public UISceneInteractMenu4Choose ChooseObjMenu;
        public UISceneInteractMenu4Choose ChooseInteractMenu;

        public enum EShowStatus
        {
            Hide,
            ShowObj,
            ShowInteract,
        }
        public EShowStatus ShowStatus;

        public List<ISceneInteractable> CurrInteractPoint = new();
        public ISceneInteractable? currBindPoint = null;


        public void Awake()
        {
            ChooseObjMenu.EvOnTabConfirmed += (idx) =>
            {
                if (idx < 0 || idx >= CurrInteractPoint.Count)
                {
                    Debug.LogError("Invalid chosse");
                    return;
                }

                if (currBindPoint == CurrInteractPoint[idx])
                {
                    return;
                }

                currBindPoint = CurrInteractPoint[idx];
                ShowDirectInteractMenuOnObj(currBindPoint);
            };



            ChooseInteractMenu.EvOnTabConfirmed += (idx) =>
            {

            };
            ChooseObjMenu.EvOnCanceled += () =>
            {
            };

            ChooseInteractMenu.gameObject.SetActive(false);
            ChooseObjMenu.gameObject.SetActive(false);
        }

        public void Update()
        {
            //HandleInput();

            UpdateChooseObjMenu();

            if (ChooseInteractMenu.gameObject.activeSelf && currBindPoint != null)
            {

                var hintPos = currBindPoint.GetHintAnchorPosition();
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

                // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                ChooseInteractMenu.transform.localPosition = localPos;
            }
        }

        private void ShowSceneObjChooseMenu()
        {
            this.ChooseInteractMenu.gameObject.SetActive(false);

            ChooseObjMenu.gameObject.SetActive(true);
            ChooseObjMenu.SetData(CurrInteractPoint.Select(item => { return item.Id.ToString(); }).ToList());

        }

        /// <summary>
        /// 刷新详细交互小界面
        /// </summary>
        /// <param name="interactObj"></param>
        private void ShowDirectInteractMenuOnObj(ISceneInteractable interactObj)
        {
            this.ChooseObjMenu.gameObject.SetActive(false);
            ChooseInteractMenu.gameObject.SetActive(true);

            this.ShowStatus = EShowStatus.ShowInteract;
            this.currBindPoint = interactObj;

            var selections = interactObj.GetInteractSelections();

            var hintPos = interactObj.GetHintAnchorPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

            // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                UIManager.Instance.RootCanvas.transform as RectTransform,
                screenPos,
                UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                out Vector2 localPos
            );
            ChooseInteractMenu.transform.localPosition = localPos;

            ChooseInteractMenu.SetData(new List<string>(selections));
        }


        private void UpdateChooseObjMenu()
        {
            if (MainGameManager.Instance.playerScenePresenter != null)
            {
                var hintPos = MainGameManager.Instance.playerScenePresenter.transform.position;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

                // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    UIManager.Instance.RootCanvas.transform as RectTransform,
                    screenPos,
                    UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                    out Vector2 localPos
                );
                ChooseObjMenu.transform.localPosition = localPos;
            }
            else
            {
                ChooseObjMenu.transform.position = new Vector2(-1000, -1000);
            }
        }

        /// <summary>
        /// 刷新交互物
        /// </summary>
        /// <param name="interactPoints"></param>
        public void RefreshInteractObjs(List<ISceneInteractable> interactPoints)
        {
            this.CurrInteractPoint.Clear();
            this.CurrInteractPoint.AddRange(interactPoints);

            // 无可交互物 全部隐藏
            if (CurrInteractPoint.Count == 0)
            {
                ShowStatus = EShowStatus.Hide;
                ChooseObjMenu.gameObject.SetActive(false);
                ChooseInteractMenu.gameObject.SetActive(false);
            }
            else if (CurrInteractPoint.Count == 1)
            {
                ShowStatus = EShowStatus.ShowInteract;
                ShowDirectInteractMenuOnObj(CurrInteractPoint.First());
            }
            else
            {
                ShowStatus = EShowStatus.ShowObj;
                ShowSceneObjChooseMenu();
            }
        }

        public bool OnConfirm()
        {
            // 在物体界面选择 进入详细交互
            if (ShowStatus == EShowStatus.ShowObj)
            {
                int idx = ChooseObjMenu.CurrentIndex;
                var chosenInteract = CurrInteractPoint[idx];

                this.currBindPoint = chosenInteract;
                ShowDirectInteractMenuOnObj(chosenInteract);
                return true;
            }
            else if (ShowStatus == EShowStatus.ShowInteract)
            {
                int idx = ChooseInteractMenu.CurrentIndex;
                if (currBindPoint == null)
                {
                    Debug.LogError("nooooo bind interact");
                    return false;
                }
                var content = ChooseInteractMenu.data[idx];
                currBindPoint.TriggerInteract(content);
            }
            return false;
        }

        public bool OnCancel()
        {
            if (ShowStatus == EShowStatus.ShowInteract && CurrInteractPoint.Count > 1)
            {
                this.currBindPoint = null;
                ChooseObjMenu.gameObject.SetActive(false);
                ChooseInteractMenu.gameObject.SetActive(false);
                return false;
            }
            else
            {
                return false;
            }
        }

        public bool OnNavigate(Vector2 dir)
        {
            //throw new System.NotImplementedException();
            return false;
        }

        public bool OnHotkey(int index)
        {
            //throw new System.NotImplementedException();
            return false;
        }

        public bool OnScroll(float deltaY)
        {
            if (Mathf.Abs(deltaY) > 0.01f)
            {
                if (deltaY > 0f)
                {
                    if (ShowStatus == EShowStatus.ShowObj)
                    {
                        ChooseObjMenu.MoveCursor(-1);  // 上滚：索引减
                    }
                    else if (ShowStatus == EShowStatus.ShowInteract)
                    {
                        ChooseInteractMenu.MoveCursor(-1);  // 上滚：索引减
                    }
                }
                else
                {
                    if (ShowStatus == EShowStatus.ShowObj)
                    {
                        ChooseObjMenu.MoveCursor(1);  // 上滚：索引减
                    }
                    else if (ShowStatus == EShowStatus.ShowInteract)
                    {
                        ChooseInteractMenu.MoveCursor(1);  // 上滚：索引减
                    }

                }
            }
            return true;
        }

        public void Refresh()
        {
            //
        }

        public bool OnSpace()
        {
            return false;
        }
    }

}
