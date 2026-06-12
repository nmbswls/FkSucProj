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
            if (MainGameManager.Instance.gameLogicManager.playerLogicEntity != null)
            {
                PlayerHpText.text = ((int)(MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.HP) * 0.001f)).ToString();

                PleasureBar.fillAmount = MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerPleasure) * 0.001f / 100;

                var jingyuVal = MainGameManager.Instance.gameLogicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerJingYu);
                int layer = (int)(jingyuVal / 1000);
                if (layer > 0)
                {
                    PlayerBallMap[AttrIdConsts.PlayerJingYu].Root.gameObject.SetActive(true);
                }
                else
                {
                    PlayerBallMap[AttrIdConsts.PlayerJingYu].Root.gameObject.SetActive(false);
                }
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
