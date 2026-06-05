using Map.Entity;
using My.Map;
using My.Map.Entity;
using System;
using System.Collections.Generic;
using UnityEngine;
using My.UI;

namespace My.Map.Scene
{
    public class SceneFishingSpotPresenter : ScenePresentationBase<FishingSpotLogicEntity>, ISceneInteractable
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;

        public Vector2 Pos => transform.position;

        public event Action<bool> EventOnInteractStateChanged;

        public string ShowName => FishEntity.CacheCfg != null ? FishEntity.CacheCfg.DisplayName : gameObject.name;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => false;

        public FishingSpotLogicEntity FishEntity => (FishingSpotLogicEntity)_logic;

        public override void ApplyState(object state)
        {
            if (state is InteractPointState s)
            {
                transform.position = s.Position;
                if (icon != null) icon.enabled = s.IsEnabled;
            }
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        public bool CanInteractEnable()
        {
            return FishEntity.CanFishNow();
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "Fish",
                Selectable = FishEntity.CanFishNow()
            });
            return ret;
        }

        public bool TriggerInteract(int selectionId, int playerId)
        {
            if (selectionId != 1 || !FishEntity.CanFishNow())
            {
                return true;
            }

            FishEntity.SetMiniGameOpen(true);
            LogicTime.RequestPause("FishingMiniGame");
            var panel = UIManager.Instance.ShowPanel("FishingMiniGamePanel", new FishingMiniGamePanel.Ctx
            {
                FishingEntityId = FishEntity.Id
            }, UILayer.Popup) as FishingMiniGamePanel;

            if (panel == null)
            {
                Debug.LogError("[SceneFishingSpotPresenter] FishingMiniGamePanel missing.");
                FishEntity.SetMiniGameOpen(false);
                LogicTime.ReleasePause("FishingMiniGame");
            }

            return true;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}
