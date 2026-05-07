

using System;
using cfg.demo;
using My.Map;
using My.Map.Entity;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.EventSystems;
using static My.Map.Entity.MapAbilitySpecConfig;
using static My.UI.OverworldHUDPanel;

namespace My.UI
{
    public class OverworldSkillPreviewUI : MonoBehaviour
    {

        public OverworldHUDPanel HUDPanel;


        public GameObject PreviewCirclePrefab; 
        public GameObject PreviewLinePrefab;
        public GameObject PreviewCastRangePrefab;

        public TextMeshProUGUI HintText;
        protected EntitySkillData skillCfg;
        protected MapAbilitySpecConfig mainAbilityCfg;


        protected SceneRangeWarnCtrl PreviewCircle;
        protected SceneRangeWarnCtrl PreviewRect;

        protected GameObject PreviewCastRange;

        protected Action<bool> cbOnConfirm;

        private void Awake()
        {
            var go1 = GameObject.Instantiate(PreviewCirclePrefab, MainGameManager.Instance.SceneEffectLayer);
            {
                PreviewCircle = go1.GetComponent<SceneRangeWarnCtrl>();
            }
            var go2 = GameObject.Instantiate(PreviewLinePrefab, MainGameManager.Instance.SceneEffectLayer);
            {
                PreviewRect = go2.GetComponent<SceneRangeWarnCtrl>();
            }

            {
                var go = GameObject.Instantiate(PreviewCastRangePrefab, MainGameManager.Instance.SceneEffectLayer); ;
                PreviewCastRange = go;
            }
        }

        public void Initialize(string skillId, Action<bool> onConfirm = null)
        {
            //this.PreviewSkillName = skillId;
            this.skillCfg = SkillLibrary.GetSkillConfig(skillId);
            this.mainAbilityCfg = skillCfg != null ? AbilityLibrary.GetAbilityConfig(skillCfg.MainAbilityId) : null;

            this.cbOnConfirm = onConfirm;

            PreviewCircle.gameObject.SetActive(false);
            PreviewRect.gameObject.SetActive(false);
            PreviewCastRange.SetActive(false);

            if (skillCfg == null || mainAbilityCfg == null)
            {
                HUDPanel.CancelSkillCast();
                return;
            }

            if (mainAbilityCfg.CastType == ECastType.NoTarget)
            {
                HUDPanel.CancelSkillCast();
                return;
            }


            if (mainAbilityCfg.CastType == ECastType.Point)
            {
                if(mainAbilityCfg.Range1 > 1e-1)
                {
                    PreviewCastRange.SetActive(true);
                    PreviewCastRange.transform.localScale = Vector3.one * mainAbilityCfg.Range1;
                }
                PreviewCircle.gameObject.SetActive(true);
                PreviewCircle.transform.localScale = Vector3.one * 0.1f;
            }
            else if(mainAbilityCfg.CastType == ECastType.Circle)
            {
                if (mainAbilityCfg.Range1 > 1e-1)
                {
                    PreviewCastRange.SetActive(true);
                    PreviewCastRange.transform.localScale = Vector3.one * mainAbilityCfg.Range1;
                }
                
                PreviewCircle.gameObject.SetActive(true);
                PreviewCircle.transform.localScale = Vector3.one * mainAbilityCfg.Range2;
            }
            else if(mainAbilityCfg.CastType == ECastType.Directional)
            {
                PreviewRect.gameObject.SetActive(true);
                PreviewRect.transform.localScale = new Vector3(mainAbilityCfg.Range1, 0.1f, 1);
            }
        }

        public void Clear()
        {
            PreviewCircle.gameObject.SetActive(false);
            PreviewRect.gameObject.SetActive(false);
            PreviewCastRange.SetActive(false);

            skillCfg = null;

            mainAbilityCfg = null;

            cbOnConfirm = null;
        }


        public void TickPreviewState()
        {
            if (skillCfg == null || mainAbilityCfg == null)
            {
                return;
            }

            var playerPos = MainGameManager.Instance.playerScenePresenter.transform.position;
            playerPos.z = 0;

            if (!LogicTime.paused)
            {
                if(PreviewCastRange.activeSelf)
                {
                    PreviewCastRange.transform.position = playerPos;
                }

                if(PreviewRect.gameObject.activeSelf)
                {
                    if(mainAbilityCfg.CastType == ECastType.Directional)
                    {
                        PreviewRect.transform.position = playerPos;
                    }
                }

                //if(PreviewCircle.gameObject.activeSelf)
                //{
                //    if (SkillConfig.TargetType == ECastType.NoTarget
                //        && SkillConfig.Range1 > 1e-1)
                //    {
                //        PreviewCircle.transform.position = playerPos;
                //    }
                //}
            }

            // 不在ui上时 移动
            if (!EventSystem.current.IsPointerOverGameObject() && !LogicTime.paused)
            {
                Vector3 sp = new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, 1);
                Vector3 wp = Camera.main.ScreenToWorldPoint(sp);
                wp.z = 0; // 将 z 固定到你的世界平面（例如 0）


                switch (mainAbilityCfg.CastType)
                {
                    case ECastType.Point:
                    case ECastType.Circle:
                        {
                            // 施法距离
                            var dist = mainAbilityCfg.Range1;

                            if (dist < (wp - playerPos).magnitude)
                            {
                                wp = playerPos + (wp - playerPos).normalized * dist;
                            }

                            PreviewCircle.transform.position = wp;
                        }
                        break;

                }
            }
        }


        public void ConfirmSkillCast(Vector2 mousePos)
        {
            if (skillCfg == null || mainAbilityCfg == null)
            {
                return;
            }
            Vector2 wp = Camera.main.ScreenToWorldPoint(mousePos);
            switch (mainAbilityCfg.CastType)
            {
                case ECastType.Point:
                case ECastType.Circle:
                    {
                        // 施法距离
                        var dist = mainAbilityCfg.Range1;
                        var playerPos = MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos;
                        if (dist < (wp - playerPos).magnitude)
                        {
                            wp = playerPos + (wp - playerPos).normalized * dist;
                        }
                    }
                    break;

            }

            MainGameManager.Instance.playerScenePresenter.PlayerEntity.ablilityManager.UseSkill(skillCfg.SkillId, castVec: wp);

            cbOnConfirm?.Invoke(true);

            HUDPanel.UpdateHudMode(EHudMode.Normal);
        }
    }
}