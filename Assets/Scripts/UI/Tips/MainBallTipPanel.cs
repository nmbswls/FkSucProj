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
            else if (tipParams.Param1 == 5)
            {
                TitleText.text = "精浴";
                ValueText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerJingYu) * 0.001f)).ToString();

                DescText.text = "主键消耗并回血";
            }
            else if (tipParams.Param1 == 6)
            {
                var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                long hp = p.GetAttr(AttrIdConsts.HP);
                long hpMax = p.GetAttr(AttrIdConsts.HP_MAX);
                TitleText.text = "血量";
                ValueText.text = $"{(int)(hp * 0.001f)} / {(int)(hpMax * 0.001f)}";
                DescText.text = "当前生命值";
            }
            else if (tipParams.Param1 == 7)
            {
                var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                long pleasure = p.GetAttr(AttrIdConsts.PlayerPleasure);
                TitleText.text = "高潮";
                // PlayerPleasure 以 0-100000 表示 0%-100%
                ValueText.text = $"{pleasure * 0.001f:0.#}%";
                DescText.text = "兴奋度积累值";
            }
            else if (tipParams.Param1 == 8)
            {
                var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                long estrus = p.GetAttr(AttrIdConsts.PlayerEstrusProgrss);
                // PlayerEstrusProgrss 以 0-100000 表示进度
                TitleText.text = "欲望";
                ValueText.text = $"{estrus * 0.001f:0.#}%";
                DescText.text = "发情程度";
            }
        }
    }

}
