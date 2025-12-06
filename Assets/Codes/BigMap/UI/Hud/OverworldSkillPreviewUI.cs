

using Config.Unit;
using My.Map;
using My.Map.Entity;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI
{
    public class OverworldSkillPreviewUI : MonoBehaviour
    {

        public OverworldHUDPanel HUDPanel;


        public GameObject PreviewCirclePrefab; 
        public GameObject PreviewLinePrefab;
        public GameObject PreviewCastRangePrefab;

        public TextMeshProUGUI HintText;
        public string PreviewSkillName;
        protected EntitySkillCfg SkillConfig;


        protected SceneRangeWarnCtrl PreviewCircle;
        protected SceneRangeWarnCtrl PreviewRect;

        protected GameObject PreviewCastRange;

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

        public void Initialize(string skillId)
        {
            this.PreviewSkillName = skillId;
            SkillConfig = SkillLibrary.GetSkillConfig(skillId);


            PreviewCircle.gameObject.SetActive(false);
            PreviewRect.gameObject.SetActive(false);
            PreviewCastRange.SetActive(false);

            if (SkillConfig.TargetType == EntitySkillCfg.ETargetType.NoTarget)
            {
                if(SkillConfig.Range1 > 1e-1)
                {
                    PreviewCastRange.SetActive(true);
                }
            }
            else if (SkillConfig.TargetType == EntitySkillCfg.ETargetType.Point)
            {
                if(SkillConfig.Range1 > 1e-1)
                {
                    PreviewCastRange.SetActive(true);
                    PreviewCastRange.transform.localScale = Vector3.one * SkillConfig.Range1;
                }
                PreviewCircle.gameObject.SetActive(true);
                PreviewCircle.transform.localScale = Vector3.one * 0.1f;
            }
            else if(SkillConfig.TargetType == EntitySkillCfg.ETargetType.Circle)
            {
                if (SkillConfig.Range1 > 1e-1)
                {
                    PreviewCastRange.SetActive(true);
                    PreviewCastRange.transform.localScale = Vector3.one * SkillConfig.Range1;
                }
                
                PreviewCircle.gameObject.SetActive(true);
                PreviewCircle.transform.localScale = Vector3.one * SkillConfig.Range2;
            }
            else if(SkillConfig.TargetType == EntitySkillCfg.ETargetType.Rect)
            {
                PreviewRect.gameObject.SetActive(true);
                PreviewRect.transform.localScale = new Vector3(SkillConfig.Range1, SkillConfig.Range2, 1);
            }
        }

        public void Clear()
        {
            PreviewCircle.gameObject.SetActive(false);
            PreviewRect.gameObject.SetActive(false);
            PreviewCastRange.SetActive(false);

            PreviewSkillName = null;
            SkillConfig = null;
        }


        public void TickPreviewState()
        {
            if (string.IsNullOrEmpty(PreviewSkillName))
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
                    if(SkillConfig.TargetType == EntitySkillCfg.ETargetType.Rect)
                    {
                        PreviewRect.transform.position = playerPos;
                    }
                }

                if(PreviewCircle.gameObject.activeSelf)
                {
                    if (SkillConfig.TargetType == EntitySkillCfg.ETargetType.NoTarget
                        && SkillConfig.Range1 > 1e-1)
                    {
                        PreviewCircle.transform.position = playerPos;
                    }
                }
            }

            // 不在ui上时 移动
            if (!EventSystem.current.IsPointerOverGameObject() && !LogicTime.paused)
            {
                Vector3 sp = new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, 1);
                Vector3 wp = Camera.main.ScreenToWorldPoint(sp);
                wp.z = 0; // 将 z 固定到你的世界平面（例如 0）

                

                switch (SkillConfig.TargetType)
                {
                    case EntitySkillCfg.ETargetType.Point:
                    case EntitySkillCfg.ETargetType.Circle:
                        {
                            // 施法距离
                            var dist = SkillConfig.Range1;

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
    }
}