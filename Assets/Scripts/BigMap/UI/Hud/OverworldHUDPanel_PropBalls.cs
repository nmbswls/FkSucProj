using System.Collections.Generic;
using cfg.demo;
using DG.Tweening;
using My.Map;
using My.Map.Entity;
using My.Quest;
using UnityEngine;
using UnityEngine.UI;
using static My.GameLogicManager;

namespace My.UI
{
    public partial class OverworldHUDPanel
    {
        public class PlayerPropBall
        {
            public string AttrId;
            public RectTransform Root;
            public CanvasGroup CG;
            public Image BarValue;
        }

        public RectTransform PropLineContainer;
        Dictionary<string, PlayerPropBall> PlayerBallMap = new();

        bool isUIDisguiseMode = false;
        Tween disguiseSwitchTween = null;

        void InitializePropBalls()
        {
            var hungerGo = PropLineContainer.Find("PlayerHunger");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerHunger;
                ball.Root = hungerGo as RectTransform;
                ball.CG = hungerGo.GetComponent<CanvasGroup>();
                ball.BarValue = hungerGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerHunger, ball);
                ball.Root.gameObject.SetActive(false);
            }
            var sanGo = PropLineContainer.Find("PlayerSan");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerSanity;
                ball.Root = sanGo as RectTransform;
                ball.CG = sanGo.GetComponent<CanvasGroup>();
                ball.BarValue = sanGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerSanity, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var clothesGo = PropLineContainer.Find("PlayerClothes");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerClothes;
                ball.Root = clothesGo as RectTransform;
                ball.CG = clothesGo.GetComponent<CanvasGroup>();
                ball.BarValue = clothesGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerClothes, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var cexposeGo = PropLineContainer.Find("PlayerExpose");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerOriginPower;
                ball.Root = cexposeGo as RectTransform;
                ball.CG = cexposeGo.GetComponent<CanvasGroup>();
                ball.BarValue = cexposeGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerOriginPower, ball);

                ball.Root.gameObject.SetActive(false);
            }
            var jingyuGo = PropLineContainer.Find("PlayerJingYu");
            {
                var ball = new PlayerPropBall();
                ball.AttrId = AttrIdConsts.PlayerJingYu;
                ball.Root = jingyuGo as RectTransform;
                ball.CG = jingyuGo.GetComponent<CanvasGroup>();
                ball.BarValue = jingyuGo.Find("Bar").GetComponent<Image>();
                PlayerBallMap.Add(AttrIdConsts.PlayerJingYu, ball);

                ball.Root.gameObject.SetActive(false);
            }
        }

        void ShowBallAppearEffect(string attrId)
        {
            if (PlayerBallMap.TryGetValue(attrId, out var ball))
            {
                ball.CG.alpha = 0;
                ball.CG.DOFade(1.0f, 1.0f);
            }
        }

        void HandleOnPlayerFuncOpen(PlayerFuncUnlockEvent e)
        {
            if (e.OpenType == EFuncOpenType.Hunger)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerHunger);
            }
            else if (e.OpenType == EFuncOpenType.Desire)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerSanity);
            }
            else if (e.OpenType == EFuncOpenType.Clothes)
            {
                ShowBallAppearEffect(AttrIdConsts.PlayerClothes);
            }
        }
    }
}
