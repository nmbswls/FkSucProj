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
            PlayerBallMap.Clear();

            var manager = PropLineContainer != null
                ? PropLineContainer.GetComponent<PropBallsDataManager>()
                : null;

            if (manager != null)
            {
                foreach (var def in manager.AllBalls)
                {
                    if (def.Root == null || string.IsNullOrEmpty(def.AttrId))
                    {
                        continue;
                    }
                    RegisterPropBall(def.AttrId, def.Root);
                }
            }
            else
            {
                RegisterBallByFind(PropLineContainer, "PlayerHunger", AttrIdConsts.PlayerHunger);
                RegisterBallByFind(PropLineContainer, "PlayerSan", AttrIdConsts.PlayerSanity);
                RegisterBallByFind(PropLineContainer, "PlayerClothes", AttrIdConsts.PlayerClothes);
                RegisterBallByFind(PropLineContainer, "PlayerExpose", AttrIdConsts.PlayerOriginPower);
            }

            // JingYu 位于 HUD 根节点下，独立查找
            RegisterBallByFind(transform, "PlayerJingYu", AttrIdConsts.PlayerJingYu);
        }

        void RegisterBallByFind(Transform parent, string childName, string attrId)
        {
            if (parent == null)
            {
                return;
            }
            var go = parent.Find(childName);
            if (go == null)
            {
                Debug.LogWarning($"[PropBalls] Child '{childName}' not found under '{parent.name}'");
                return;
            }
            RegisterPropBall(attrId, go as RectTransform);
        }

        void RegisterPropBall(string attrId, RectTransform root)
        {
            var ball = CreatePropBall(attrId, root);
            if (ball == null)
            {
                return;
            }
            PlayerBallMap[attrId] = ball;
            ball.Root.gameObject.SetActive(false);
        }

        // 从 Root 自动解析 CG 与 Bar 子节点
        static PlayerPropBall CreatePropBall(string attrId, RectTransform root)
        {
            if (root == null)
            {
                return null;
            }

            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                Debug.LogWarning($"[PropBalls] CanvasGroup missing on '{root.name}' (attrId={attrId})");
            }

            Image barValue = null;
            var barTr = root.Find("Bar");
            if (barTr != null)
            {
                barValue = barTr.GetComponent<Image>();
            }
            else
            {
                Debug.LogWarning($"[PropBalls] Bar child missing on '{root.name}' (attrId={attrId})");
            }

            return new PlayerPropBall
            {
                AttrId = attrId,
                Root = root,
                CG = cg,
                BarValue = barValue,
            };
        }

        void RefreshPropBallBar(string attrId, LogicEntityBase player)
        {
            if (player == null
                || !PlayerBallMap.TryGetValue(attrId, out var ball)
                || ball.BarValue == null
                || ball.Root == null
                || !ball.Root.gameObject.activeSelf)
            {
                return;
            }

            long max = player.GetResourceMax(attrId);
            if (max <= 0)
            {
                return;
            }

            ball.BarValue.fillAmount = Mathf.Clamp01((float)player.GetAttr(attrId) / max);
        }

        void ShowBallAppearEffect(string attrId)
        {
            if (!PlayerBallMap.TryGetValue(attrId, out var ball) || ball.CG == null)
            {
                return;
            }
            ball.CG.alpha = 0;
            ball.CG.DOFade(1.0f, 1.0f);
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
