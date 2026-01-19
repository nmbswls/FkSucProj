using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace My
{
    public class MapSpeechBubble : MonoBehaviour
    {
        public TextMeshProUGUI textComponent;
        public RectTransform backgroundRect;
        private Vector3 offset = new Vector3(0, 0f, 0);

        private IScenePresentation targetPresenter;

        public void Init(IScenePresentation target)
        {
            targetPresenter = target;
            gameObject.SetActive(true);
        }

        public void SetText(string text)
        {
            textComponent.text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            targetPresenter = null;
        }

        //void LateUpdate()
        //{
        //    // 增加检测：如果目标为空（角色被销毁），自动关闭
        //    if (targetPresenter == null)
        //    {
        //        if (gameObject.activeSelf) Hide();
        //        return;
        //    }
        //    var p = targetPresenter.PivotHeader.position + offset;
        //    p.z = 0;
        //    transform.position = p;

        //    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        //}
    }
}


