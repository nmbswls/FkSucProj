using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UISceneInteractMenu : MonoBehaviour
{
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

    [Header("Input")]
    public KeyCode confirmKey = KeyCode.F;
    public string mouseScrollAxis = "Mouse ScrollWheel"; // Input Manager 轴名

    public void Awake()
    {
        ChooseObjMenu.EvOnTabConfirmed += (idx) =>
        {
            if(idx < 0 || idx >= CurrInteractPoint.Count)
            {
                Debug.LogError("Invalid chosse");
                return;
            }

            if(currBindPoint == CurrInteractPoint[idx])
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
        HandleInput();

        UpdateChooseObjMenu();

        if(ChooseInteractMenu.gameObject.activeSelf && currBindPoint != null)
        {

            var hintPos = currBindPoint.GetHintAnchorPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

            // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                MainUIManager.Instance.RootCanvas.transform as RectTransform,
                screenPos,
                MainUIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                out Vector2 localPos
            );
            ChooseInteractMenu.transform.localPosition = localPos;
        }
    }

    /// <summary>
    /// 调整为 监听ui事件的uimanager
    /// </summary>
    private void HandleInput()
    {
        float scroll = Input.GetAxis(mouseScrollAxis);
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (scroll > 0f)
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

        if (Input.GetKeyDown(confirmKey))
        {
            // 在物体界面选择 进入详细交互
            if (ShowStatus == EShowStatus.ShowObj)
            {
                int idx = ChooseObjMenu.CurrentIndex;
                var chosenInteract = CurrInteractPoint[idx];

                this.currBindPoint = chosenInteract;
                ShowDirectInteractMenuOnObj(chosenInteract);
            }
            else if (ShowStatus == EShowStatus.ShowInteract)
            {
                int idx = ChooseInteractMenu.CurrentIndex;
                if(currBindPoint == null)
                {
                    Debug.LogError("nooooo bind interact");
                    return;
                }
                var content = ChooseInteractMenu.data[idx];
                currBindPoint.TriggerInteract(content);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (ShowStatus == EShowStatus.ShowInteract && CurrInteractPoint.Count > 1)
            {
                this.currBindPoint = null;
                ChooseObjMenu.gameObject.SetActive(false);
                ChooseInteractMenu.gameObject.SetActive(false);
            }
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

        this.ShowStatus = EShowStatus.ShowInteract;
        this.currBindPoint = interactObj;

        var selections = interactObj.GetInteractSelections();

        var hintPos = interactObj.GetHintAnchorPosition();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

        // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            MainUIManager.Instance.RootCanvas.transform as RectTransform,
            screenPos,
            MainUIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
            out Vector2 localPos
        );
        ChooseInteractMenu.transform.localPosition = localPos;

        ChooseInteractMenu.SetData(new List<string>(selections));
        ChooseInteractMenu.gameObject.SetActive(true);
    }


    private void UpdateChooseObjMenu()
    {
        if (MainGameManager.Instance.playerScenePresenter != null)
        {
            var hintPos = MainGameManager.Instance.playerScenePresenter.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

            // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                MainUIManager.Instance.RootCanvas.transform as RectTransform,
                screenPos,
                MainUIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
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
        else if(CurrInteractPoint.Count == 1)
        {
            //// 如果当前处于隐藏态 需要初始化
            //if (ShowStatus == EShowStatus.Hide)
            //{
            //    ShowStatus = EShowStatus.ShowInteract;
            //    ChooseObjMenu.gameObject.SetActive(false);

            //    var interactPoint = CurrInteractPoint.First();
            //    var selections = interactPoint.GetInteractSelections();

            //    var hintPos = interactPoint.GetHintAnchorPosition();
            //    Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

            //    // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
            //    RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //        MainUIManager.Instance.RootCanvas.transform as RectTransform,
            //        screenPos,
            //        MainUIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
            //        out Vector2 localPos
            //    );
            //    transform.localPosition = localPos;

            //    ChooseInteractMenu.SetData(new List<string>(selections));
            //    ChooseInteractMenu.gameObject.SetActive(true);
            //}
            //else
            //{
            //    ShowStatus = EShowStatus.ShowInteract;
            //    ChooseObjMenu.gameObject.SetActive(false);
            //    ChooseInteractMenu.gameObject.SetActive(true);
            //}
            ShowStatus = EShowStatus.ShowInteract;
            ShowDirectInteractMenuOnObj(CurrInteractPoint.First());
        }
        else
        {
            ShowStatus = EShowStatus.ShowObj;

            ShowSceneObjChooseMenu();
            
        }
    }
}
