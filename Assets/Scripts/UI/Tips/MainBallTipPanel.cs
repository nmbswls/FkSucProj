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

            if(tipParams.Param1 == 1)
            {
                TitleText.text = "饱腹";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerHunger) * 0.001f)).ToString();

                DescText.text = "归零就会暴露";
            }
            else if (tipParams.Param1 == 2)
            {
                TitleText.text = "理智";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerSanity) * 0.001f)).ToString();

                DescText.text = "理智降低，莉莉丝将无法控制自己的欲望本能。";
            }
            else if (tipParams.Param1 == 3)
            {
                TitleText.text = "衣装";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes) * 0.001f)).ToString();

                DescText.text = "伪装进度";
            }
            else if (tipParams.Param1 == 4)
            {
                TitleText.text = "真身";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerOriginPower) * 0.001f)).ToString();

                DescText.text = "你以恶魔姿态现身，积攒能量";
            }
        }
    }

}
