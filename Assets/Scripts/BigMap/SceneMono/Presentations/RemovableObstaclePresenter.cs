using My.Map.Entity;
using UnityEngine;
using UnityEngine.AI;

namespace My.Map.Scene
{
    public class RemovableObstaclePresenter : InteractPointPresenter
    {
        LogicEntityRemovableObstacle RemovableLogic =>
            (LogicEntityRemovableObstacle)_logic;

        public override string ShowName =>
            RemovableLogic?.RemovableCfg != null && !string.IsNullOrEmpty(RemovableLogic.RemovableCfg.ShowName)
                ? RemovableLogic.RemovableCfg.ShowName
                : RemovableLogic?.CfgId ?? string.Empty;

        public override float GetHintOffsetInfos()
        {
            if (RemovableLogic?.RemovableCfg != null)
            {
                return RemovableLogic.RemovableCfg.NameOffset;
            }

            return base.GetHintOffsetInfos();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            ValidateMainBlockSetup();
        }
#endif

        void Awake()
        {
            ValidateMainBlockSetup();
        }

        void ValidateMainBlockSetup()
        {
            if (MainBlock == null)
            {
                return;
            }

            if (MainBlock.GetComponentInChildren<Collider2D>(true) == null)
            {
                Debug.LogWarning($"RemovableObstaclePresenter: MainBlock has no Collider2D, go={name}");
            }

            if (MainBlock.GetComponentInChildren<NavMeshObstacle>(true) == null)
            {
                Debug.LogWarning($"RemovableObstaclePresenter: MainBlock has no NavMeshObstacle, go={name}");
            }
        }
    }
}
