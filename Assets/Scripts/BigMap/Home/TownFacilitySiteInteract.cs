using System.Collections.Generic;

using My.Config;

using My.UI.Home;

using UnityEngine;



namespace My.Home

{

    // 挂在城镇设施静态 prefab 上；设施管理鸟瞰开启时可被点选。

    public class TownFacilitySiteInteract : MonoBehaviour, ISceneInteractable

    {

        public int SiteId;

        public Transform HintPivot;



        public string ShowName

        {

            get

            {

                var site = TownFacilitySiteCatalog.Get(SiteId);

                if (site == null)

                {

                    return "设施";

                }



                var def = FacilityDevelopmentCatalog.GetDefinition(site.FacilityCfgId);

                return def?.DisplayName ?? site.FacilityCfgId;

            }

        }



        public Vector2 Pos => transform.position;



        public bool WithInteractDetail => true;



        public bool InteractFocused { get; set; }



        public bool IsInteractDetail { get; set; }



        public bool CanInteractEnable()

        {

            return HomeTownViewController.IsFacilityManagementViewActive

                   && SiteId > 0;

        }



        public bool TriggerInteract(int selectionId, int playerId)

        {

            TownFacilityInteractUtil.OpenDetailBySite(SiteId);

            return true;

        }



        public Vector3 GetHintAnchorPosition()

        {

            return HintPivot != null ? HintPivot.position : transform.position;

        }



        public float GetHintOffsetInfos()

        {

            return 0f;

        }



        public List<SceneInteractSelection> GetInteractSelections()

        {

            var glm = MainGameManager.Instance?.gameLogicManager;

            var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);

            int level = glm?.townFacilityDevelopmentSystem?.GetFacilityDevelopmentLevel(logicAreaId, SiteId) ?? 0;

            return new List<SceneInteractSelection>

            {

                new SceneInteractSelection

                {

                    SelectId = TownFacilityInteractUtil.SelectManageFacility,

                    SelectContent = level > 0 ? "管理" : "建造",

                },

            };

        }



        public bool IsAutoInteract()

        {

            return false;

        }

    }

}

