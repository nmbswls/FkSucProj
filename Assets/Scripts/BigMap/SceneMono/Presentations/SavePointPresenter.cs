using My;
using My.Map;
using My.Map.Entity;
using My.UI;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public class SavePointPresenter : ScenePresentationBase<LogicEntitySavePoint>, ISceneInteractable
    {
        public LogicEntitySavePoint SaveEntity => (LogicEntitySavePoint)_logic;

        public string ShowName =>
            SaveEntity?.Cfg != null && !string.IsNullOrEmpty(SaveEntity.Cfg.DisplayName)
                ? SaveEntity.Cfg.DisplayName
                : "Save Point";

        public Vector2 Pos => transform.position;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => false;

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            RefreshVisibility();
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            RefreshVisibility();
        }

        void RefreshVisibility()
        {
            if (SaveEntity == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(SavePointUnlockHelper.ShouldShowOnMap(SaveEntity.LogicManager, SaveEntity.SavePointId));
        }

        public bool CanInteractEnable()
        {
            return SaveEntity != null
                   && !SaveEntity.MarkDestroyed
                   && SaveEntity.ShouldShowOnMap
                   && CanInteractSavePointNow();
        }

        bool CanInteractSavePointNow()
        {
            var glm = SaveEntity?.LogicManager;
            return glm != null && glm.CanInteractSavePoint;
        }

        public bool TriggerInteract(int selectionId)
        {
            if (selectionId != 1 || SaveEntity == null)
            {
                return false;
            }

            if (!CanInteractSavePointNow())
            {
                Debug.LogWarning("[SavePointPresenter] Save point interaction blocked: not in peace mode.");
                return false;
            }

            if (SaveEntity.IsActivated)
            {
                return OpenSavePanel();
            }

            if (SaveEntity.NeedsTribute)
            {
                if (!SavePointUnlockHelper.TrySubmitTribute(
                        SaveEntity.LogicManager, SaveEntity.SavePointId, out var tributeReason))
                {
                    Debug.LogWarning("[SavePointPresenter] Submit tribute failed: " + tributeReason);
                    return false;
                }

                return true;
            }

            if (SaveEntity.NeedsActivate)
            {
                if (!SavePointUnlockHelper.TryActivate(
                        SaveEntity.LogicManager, SaveEntity.SavePointId, out var activateReason))
                {
                    Debug.LogWarning("[SavePointPresenter] Activate failed: " + activateReason);
                    return false;
                }

                return true;
            }

            return false;
        }

        bool OpenSavePanel()
        {
            LogicTime.RequestPause("SavePoint");
            var panel = UIManager.Instance.ShowPanel("SavePointPanel") as SavePointPanel;
            if (panel == null)
            {
                Debug.LogError("[SavePointPresenter] SavePointPanel not registered or prefab missing.");
                LogicTime.ReleasePause("SavePoint");
                return false;
            }

            panel.BeginFlow(SaveEntity);
            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            if (SaveEntity == null)
            {
                return new List<SceneInteractSelection>();
            }

            string label;
            if (SaveEntity.IsActivated)
            {
                label = "Save";
            }
            else if (SaveEntity.NeedsTribute)
            {
                label = "Offer tribute";
            }
            else
            {
                label = "Activate";
            }

            return new List<SceneInteractSelection>
            {
                new SceneInteractSelection
                {
                    SelectId = 1,
                    SelectContent = label,
                    Selectable = CanInteractSavePointNow(),
                },
            };
        }

        public float GetHintOffsetInfos()
        {
            return -1f;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}
