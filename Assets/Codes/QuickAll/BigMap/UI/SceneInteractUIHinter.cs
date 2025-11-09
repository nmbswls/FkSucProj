using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class SceneInteractUIHinter : MonoBehaviour
{
    public ISceneInteractable BindInteractPoint;

    public bool IsExpanded = false;

    public int SelectIdx = 0;

    //public GameObject SelectItemPrefab;
    public Transform ShowRoot;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if(BindInteractPoint == null)
        {
            return;
        }

        if(BindInteractPoint.CanInteractEnable())
        {
            ShowRoot.gameObject.SetActive(true);
        }
        else
        {
            ShowRoot.gameObject.SetActive(false);
        }

        var hintPos = BindInteractPoint.GetHintAnchorPosition();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

        // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            MainUIManager.Instance.RootCanvas.transform as RectTransform,
            screenPos,
            MainUIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
            out Vector2 localPos
        );
        transform.localPosition = localPos;
    }

    public void InitBind(ISceneInteractable sceneInteract)
    {
        this.BindInteractPoint = sceneInteract;
        //this.BindInteractPoint.EventOnInteractStateChanged += OnExpandStateChanged;
    }

    public void Clear()
    {
        //BindInteractPoint.EventOnInteractStateChanged -= OnExpandStateChanged;
        BindInteractPoint = null;

        OnExpandStateChanged(false);
    }

    /// <summary>
    /// 展示变化
    /// </summary>
    public void OnExpandStateChanged(bool expanded)
    {
        //this.IsExpanded = expanded;
        //Debug.Log("OnExpandStateChanged change:" + expanded);
        //if (this.IsExpanded)
        //{
        //    DetailList.gameObject.SetActive(true);
        //    var selectis = BindInteractPoint.GetInteractSelections();
        //    for(int i=0;i< selectis.Count;i++)
        //    {
        //        ItemWrappers[i].Root.gameObject.SetActive(true);
        //        ItemWrappers[i].Content.text = selectis[i];
        //        ItemWrappers[i].Selection = selectis[i];
        //    }
        //    for (int i = selectis.Count; i < 3; i++)
        //    {
        //        ItemWrappers[i].Root.gameObject.SetActive(false);
        //        ItemWrappers[i].Selection = string.Empty;
        //    }
        //}
        //else
        //{
        //    DetailList.gameObject.SetActive(false);
        //}
    }

    //public void OnSelectClicked(SelectItemWrapper wrapper)
    //{
    //    //BindInteractPoint.TriggerInteract(wrapper.Selection);
    //}
}
