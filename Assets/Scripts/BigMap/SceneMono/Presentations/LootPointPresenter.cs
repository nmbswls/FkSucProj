using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class LootPointPresenter : ScenePresentationBase<LootPointLogicEntity>, ISceneInteractable
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;


        public Vector2 Pos => transform.position;

        public event Action<bool> EventOnInteractStateChanged;

        public string ShowName => LootEntity.cacheConfig.ShowName;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }

        public LootPointLogicEntity LootEntity { get { return (LootPointLogicEntity)_logic; } }
        public override void ApplyState(object state)
        {
            if (state is InteractPointState s)
            {
                transform.position = s.Position;
                if (icon != null) icon.enabled = s.IsEnabled;
                //if (highlightFx != null) highlightFx.SetActive(s.IsEnabled && _logic.IsInAOI);
            }
        }

        public Vector3 GetHintAnchorPosition()
        {
            return GetWorldPosition() + new Vector3(0, 0.1f, 0);
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        /// <summary>
        /// 触发交互
        /// </summary>
        /// <param name="triggerIdx"></param>
        public bool TriggerInteract(int selectionId)
        {
            // 只有一个触发点
            if (selectionId == 1)
            {
                // 尝试解锁
                if (!LootEntity.IsLocked)
                {
                    MainGameManager.Instance.ShowFakeFxEffect("没锁呀", _logic.Pos);
                    return true;
                }
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("unlock_loot_point", target: LootEntity); ;
            }
            else if (selectionId == 2)
            {
                // 尝试解锁
                if (LootEntity.IsLocked)
                {
                    MainGameManager.Instance.ShowFakeFxEffect("locked", _logic.Pos);
                    return true;
                }

                float useTime = LootEntity.cacheConfig.LootOpenTime;
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("use_loot_point", target: LootEntity,
                    overrideParams: new Dictionary<string, string>()
                    {
                        ["PhaseExecutingTime"] = useTime.ToString()
                    }, phaseOverrideAnims: new Dictionary<string, string>()
                    {
                        ["Executing"] = LootEntity.cacheConfig.LootOverrideAnim
                    });
            }
            return false;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            if (LootEntity.IsLocked)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "unlock",
                });
            }
            else
            {

                bool canLoot = LootEntity.CanLoot();

                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 2,
                    SelectContent = "loot",
                    Selectable = canLoot,
                });
            }

            return ret;

        }

        public bool CanInteractEnable()
        {
            return true;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

