using DG.Tweening;
using Map.Entity;
using My.Map.Entity;
using My.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace My.Map.Scene
{
    public class SceneFacilityRuinPresenter : ScenePresentationBase<LogicEntityRepairPoint>, ISceneInteractable
    {
        public LogicEntityRepairPoint RepairPointEntity { get { return (LogicEntityRepairPoint)_logic; } }

        public string ShowName => "废墟";

        public Vector2 Pos => transform.position;

        public GameObject MainBlock;

        public Transform ViewRoot;
        [SerializeField]
        private SpriteRenderer[] _sprites;

        public Transform HintPivot;

        private bool interactFocused;
        public bool InteractFocused
        {
            get { return interactFocused; }
            set
            {
                interactFocused = value;

                if (interactFocused)
                {
                    var realPanel = UIManager.Instance.ShowPanel("RepairDetailPanel") as RepairDetailPanel;
                    if (realPanel != null)
                    {
                        realPanel.UpdateBind(this);
                    }
                }
                else
                {
                    UIManager.Instance.HidePanel("RepairDetailPanel");
                }
            }
        }

        public bool IsInteractDetail { get; set; }

        //public bool IsInteractDetail
        //{
        //    get { return isInteractDetail; }
        //    set
        //    {
        //        isInteractDetail = value;

        //        if(isInteractDetail)
        //        {
        //            var realPanel = UIManager.Instance.ShowPanel("RepairDetailPanel") as RepairDetailPanel;
        //            if(realPanel != null)
        //            {
        //                realPanel.UpdateBind(this);
        //            }
        //        }
        //        else
        //        {
        //            UIManager.Instance.HidePanel("RepairDetailPanel");
        //        }
        //    }
        //}

        //private bool isInteractDetail;

        public bool WithInteractDetail
        {
            get
            {
                return false;
            }
        }


        [ContextMenu("Auto Collect Child Sprites")]
        private void CollectSprites()
        {
            _sprites = ViewRoot.GetComponentsInChildren<SpriteRenderer>(true);

            Debug.Log($"已收集 {_sprites.Length} 个 SpriteRenderer");

            // 标记对象已修改，确保 Unity 保存这个列表，否则重启后会丢失
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            if(RepairPointEntity.IsRepaired)
            {
                if (_sprites != null)
                {
                    foreach (var view in _sprites)
                    {
                        view.color = new Color(view.color.r, view.color.g, view.color.b, 0);
                    }
                }

                if (MainBlock != null)
                {
                    MainBlock.gameObject.SetActive(false);
                }
            }
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            RepairPointEntity.EventOnRepaired += OnRuinRepaired;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            RepairPointEntity.EventOnRepaired -= OnRuinRepaired;
        }

        protected override void OnEventEntityDestroyed(long entityId)
        {

        }


        private void OnRuinRepaired()
        {

            MainGameManager.Instance.ShowFakeFxEffect("修好了", this.transform.position);

            if (_sprites != null)
            {
                foreach (var view in _sprites)
                {
                    view.DOFade(0, 0.3f);
                }
            }
            
            if (MainBlock != null)
            {
                MainBlock.gameObject.SetActive(false);
            }

            MainGameManager.Instance.gameLogicManager.PushPendingBlock(1.0f);
        }



        public bool CanInteractEnable()
        {
            if(RepairPointEntity.IsRepaired)
            {
                return false;
            }

            if(RepairPointEntity.Cfg.AutoRepair)
            {
                return false;
            }

            return true;
        }

        public bool TriggerInteract(int selectionId)
        {
            if (RepairPointEntity.Cfg.AutoRepair)
            {
                MainGameManager.Instance.ShowMapSpeachBubble(RepairPointEntity.Id, "没修好。", 1f);
                return true;
            }

            bool repairOpen = RepairPointEntity.CheckIsRepairOpen();

            if (!repairOpen)
            {
                if (RepairPointEntity.Cfg.ShwoWhenLocked)
                {
                    MainGameManager.Instance.ShowMapSpeachBubble(RepairPointEntity.Id, "没修好。", 1f);
                }
                return true;
            }

            // 材料足够 
            if (RepairPointEntity.CheckEnoughRepairMaterial())
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.abilityController.TryUseAbility("player_common_interact", overrideParams: new Dictionary<string, string>()
                {
                    ["InteractTime"] = "3.0",
                }, phaseOverrideAnims: new Dictionary<string, string>()
                {
                    ["Interacting"] = "repair"
                },
                onAbilityEnd: (complete) =>
                {
                    if (complete)
                    {
                        RepairPointEntity.TryManualRepair();
                    }
                });
            }
            // 材料不足
            else
            {
                // 放材料
                RepairPointEntity.TryPutInMaterial();
            }

            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            if(HintPivot != null)
            {
                return HintPivot.position;
            }
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();

            if (RepairPointEntity.IsRepaired)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "未知",
                });
                return ret;
            }

            if (RepairPointEntity.Cfg.AutoRepair)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "查看",
                });
                return ret;
            }

            bool enoughMat = RepairPointEntity.CheckEnoughRepairMaterial();
            if(enoughMat)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "修理",
                });
            }
            else
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "放入材料",
                });
            }

            return ret;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

