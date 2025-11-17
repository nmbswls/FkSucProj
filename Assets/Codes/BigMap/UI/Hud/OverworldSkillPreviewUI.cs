

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


        public GameObject PreviewCircle; 
        public GameObject PreviewLine;

        public TextMeshProUGUI HintText;
        public string PreviewAbilityName;
        protected MapAbilitySpecConfig AbilityConfig;


        public void Initialize(string abName)
        {
            this.PreviewAbilityName = abName;
            AbilityConfig = AbilityLibrary.GetAbilityConfig(abName);

            PreviewCircle.SetActive(false);
            PreviewLine.SetActive(false);

            if (AbilityConfig.TargetType == MapAbilitySpecConfig.ETargetType.Point)
            {
                //
            }
            else if(AbilityConfig.TargetType == MapAbilitySpecConfig.ETargetType.Circle)
            {
                PreviewCircle.SetActive(true);
                PreviewCircle.transform.localScale = Vector3.one * 100;
            }
        }

        public void TickPreviewState()
        {
            // 不在ui上时 移动
            if (!EventSystem.current.IsPointerOverGameObject() && !LogicTime.paused)
            {
                Vector3 sp = new Vector3(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y, 1);
                Vector3 wp = Camera.main.ScreenToWorldPoint(sp);
                wp.z = 0; // 将 z 固定到你的世界平面（例如 0）

                var dist = AbilityConfig.Range1;

                var playerPos = MainGameManager.Instance.playerScenePresenter.transform.position;
                playerPos.z = 0;
                if (dist < (wp - playerPos).magnitude)
                {
                    wp = playerPos + (wp - playerPos).normalized * dist;
                }

                PreviewCircle.transform.position = wp;

                if(UnityEngine.Input.GetMouseButtonDown(0))
                {
                    HUDPanel.ConfirmSkillCast(PreviewAbilityName, wp, Vector2.zero);
                }

                if(UnityEngine.Input.GetMouseButtonDown(1))
                {
                    HUDPanel.CancelSkillCast();
                }
            }
        }
    }
}