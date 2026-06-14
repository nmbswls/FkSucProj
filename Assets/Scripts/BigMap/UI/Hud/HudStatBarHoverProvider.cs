using UnityEngine;

namespace My.UI
{
    // 挂载到 HUD 属性条根节点，Inspector 配置 barParam 即可
    // Param1：6=HP 血量, 7=Pleasure 高潮, 8=Desire 欲望
    public class HudStatBarHoverProvider : BaseUIHoverProvider
    {
        [SerializeField]
        int _barParam = 6;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.Main3Ball,
                Param1 = _barParam,
            };
        }
    }
}
