using cfg.demo;
using DG.Tweening;
using My.Map;
using My.Map.Entity;
using UnityEngine;
using static My.GameLogicManager;

namespace My.UI
{
    public partial class OverworldHUDPanel
    {
        public void Update()
        {
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            if (player != null)
            {
                // 血量文本
                long hp = player.GetAttr(AttrIdConsts.HP);
                if (PlayerHpText != null)
                {
                    PlayerHpText.text = ((int)(hp * 0.001f)).ToString();
                }

                // 血量进度条（HPBar → 血量）
                if (HpBar != null)
                {
                    long hpMax = player.GetAttr(AttrIdConsts.HP_MAX);
                    HpBar.fillAmount = hpMax > 0
                        ? Mathf.Clamp01((float)hp / hpMax)
                        : 0f;
                }

                // 高潮进度条（PleasureBar → PlayerPleasure，0-100000 对应 0-1）
                if (PleasureBar != null)
                {
                    PleasureBar.fillAmount = Mathf.Clamp01(
                        player.GetAttr(AttrIdConsts.PlayerPleasure) * 0.001f / 100f);
                }

                // 发情值进度条（DesireBar → PlayerEstrusProgrss，max level 5 对应 100000）
                if (DesireBar != null)
                {
                    const float EstrusMaxRaw = 100000f;
                    DesireBar.fillAmount = Mathf.Clamp01(
                        player.GetAttr(AttrIdConsts.PlayerEstrusProgrss) / EstrusMaxRaw);
                }

                // 精浴层数显隐
                var jingyuVal = player.GetAttr(AttrIdConsts.PlayerJingYu);
                int layer = (int)(jingyuVal / 1000);
                if (PlayerBallMap.TryGetValue(AttrIdConsts.PlayerJingYu, out var jingyuBall) && jingyuBall.Root != null)
                {
                    jingyuBall.Root.gameObject.SetActive(layer > 0);
                }

                RefreshPropBallBar(AttrIdConsts.PlayerHunger, player);
                RefreshPropBallBar(AttrIdConsts.PlayerSanity, player);
                RefreshPropBallBar(AttrIdConsts.PlayerClothes, player);
                RefreshPropBallBar(AttrIdConsts.PlayerOriginPower, player);
            }

            if (WantedIndicator != null)
            {
                if (MainGameManager.Instance.gameLogicManager.GameSession.IsInfiltrationRun && MainGameManager.Instance.gameLogicManager.AreaManager.cacheMapOverlayCfg.IsCivilArea)
                {
                    WantedIndicator.gameObject.SetActive(true);
                    WantedIndicator.RefreshView();
                }
                else
                {
                    WantedIndicator.gameObject.SetActive(false);
                }
            }

            if (HudMode == EHudMode.PreviewSkill)
            {
                overworldSkillPreviewUI.TickPreviewState();
            }

            AlertHintIndicator?.RefreshView();
            RetreatHintIndicator?.RefreshView();
            ExposeSkillIndicator?.RefreshView();
            RefreshHoldCancelHint();

            CheckDisguiseState();

            UpdateEstrusStateHint();

            EstrusIndicator?.CheckEstrusUpdate();
        }

        // 检查伪装状态切换时的属性球显示
        private void CheckDisguiseState()
        {
            if (disguiseSwitchTween != null) return;
            bool disguising = false;
            var lgm = MainGameManager.Instance.gameLogicManager;

            if (!lgm.PlayerHumanMode)
            {
                if (lgm.AreaManager.cacheMapOverlayCfg != null
                    && lgm.AreaManager.cacheMapOverlayCfg.IsCivilArea
                    && !lgm.playerLogicEntity.IsExposed)
                {
                    disguising = true;
                }
            }

            if (isUIDisguiseMode == disguising)
            {
                return;
            }

            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(false);
            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(false);

            if (!MainGameManager.Instance.gameLogicManager.playerDataManager.FuncOpenSystem.FuncOpenSet.Contains(EFuncOpenType.Clothes))
            {
                return;
            }
            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(true);
            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(true);

            if (disguising)
            {
                PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(true);
                PlayerBallMap[AttrIdConsts.PlayerClothes].CG.alpha = 0;

                disguiseSwitchTween = DG.Tweening.DOTween.Sequence()
                        .Append(PlayerBallMap[AttrIdConsts.PlayerClothes].CG.DOFade(1, 0.3f))
                        .Append(PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(false);
                            isUIDisguiseMode = disguising;

                        }).SetLink(gameObject);
            }
            else
            {
                PlayerBallMap[AttrIdConsts.PlayerOriginPower].Root.gameObject.SetActive(true);
                PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.alpha = 0;

                disguiseSwitchTween = DG.Tweening.DOTween.Sequence()
                        .Append(PlayerBallMap[AttrIdConsts.PlayerOriginPower].CG.DOFade(1, 0.3f))
                        .Append(PlayerBallMap[AttrIdConsts.PlayerClothes].CG.DOFade(0, 0.3f))
                        .OnComplete(() =>
                        {
                            disguiseSwitchTween = null;
                            PlayerBallMap[AttrIdConsts.PlayerClothes].Root.gameObject.SetActive(false);
                            isUIDisguiseMode = disguising;

                        }).SetLink(gameObject);
            }
        }

        private void UpdateEstrusStateHint()
        {
        }
    }
}
