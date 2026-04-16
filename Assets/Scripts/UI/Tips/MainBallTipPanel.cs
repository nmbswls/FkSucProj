using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using TMPro;
using UnityEngine;

namespace My.UI
{

    public class MainBallTipPanel : MonoBehaviour, IHoverTipPanel
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ValueText;
        public TextMeshProUGUI DescText;

        public RectTransform Root;

        public void Awake()
        {
            Root = transform as RectTransform;
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            Vector3 anchorPos = provider.TooltipPosition;
            anchorPos.z = 0;
            Root.position = anchorPos;

            Root.localPosition = new Vector3 (Root.localPosition.x, Root.localPosition.y, 0);

            var uiHover = (BaseUIHoverProvider)provider;

            if(uiHover.name == "PlayerClothes")
            {
                TitleText.text = "衣装";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) * 0.001f)).ToString();

                DescText.text = "归零就会暴露";
            }
            else if (uiHover.name == "PlayerExpose")
            {
                TitleText.text = "真身";
                ValueText.text = "999";

                DescText.text = "你以恶魔姿态现身，积攒能量";
            }
            else
            {
                TitleText.text = "未知";
            }
        }
    }

}
