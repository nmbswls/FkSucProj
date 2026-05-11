using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using My.MiniGame;
using My.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace My.Map.Scene
{
    public class SceneNpcPresenter : SceneUnitPresenter, ISceneInteractable
    {
        public Vector2 Pos => transform.position;
        public string ShowName {
            get
            {
                return NpcEntity.NpcConfig.Name;
            } 
        }


        public NpcUnitLogicEntity NpcEntity
        {
            get
            {
                return (NpcUnitLogicEntity)_logic;
            }
        }

        public List<AnimationClip> SpecialAnimClips;
        private Dictionary<string, AnimationClip> _animCacheDict = new();

        public static int ID_NormalDialog = 51;
        public static int ID_DeepAbsorbEnable = 99;
        public static int ID_DeepAbsorbDisable = 100;
        public static int ID_PickDropInteractId = 101;
        //public static int ID_BackHit = 102;
        public static int ID_ForcePushDown = 103; // 强推
        public static int ID_CarryBody = 104;      // 搬运尸体/昏迷单位
        public static int ID_SneakBackstab = 105;  // 蹲伏偷袭

        public HighlightCtrl highlightCtrl;
        public bool InteractDetailMode { get; set; }

        // 猎杀模式 + 附着欲望结晶：从 Resources 实例化占位预制体（见 Prefab/SceneEffect/desire_crystal_hunting_placeholder）
        public const string DesireCrystalHuntingFxPrefabResourcePath = "Prefab/SceneEffect/desire_crystal_hunting_placeholder";

        Transform _desireCrystalHuntingFxRoot;

        private int _faqingEffectUId { get; set; }

        public bool InteractFocused
        {
            get { return interactFocused; }
            set
            {
                interactFocused = value;

                if (interactFocused)
                {
                    highlightCtrl?.SetHighlightStatus(true, "InteractFocused");
                }
                else
                {
                    //highlightCtrl?.SetHighlight("InteractFocused");
                    highlightCtrl?.SetHighlightStatus(false, "InteractFocused");
                }
            }
        }

        private bool interactFocused;
        public bool IsInteractDetail
        {
            get { return isInteractDetail; }
            set
            {
                isInteractDetail = value;

                if (isInteractDetail)
                {
                    NpcEntity.RegisterGaze("InteractDetail", UnitEntity.LogicManager.playerLogicEntity.Id, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Interact);
                }
                else
                {
                    NpcEntity.UnregisterGazeBySourceTag("InteractDetail");
                }
            }
        }

        private bool isInteractDetail;

        public bool WithInteractDetail
        {
            get
            {
                // 猎杀模式 不可展开
                if (OverworldHUDPanel.Instance.IsHunterMode)
                {
                    return false;
                }

                if(MainGameManager.Instance.gameLogicManager.playerLogicEntity.IsSpecialCrouchStance)
                {
                    return false;
                }

                return true;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (SpecialAnimClips != null)
            {
                foreach(var clip in SpecialAnimClips)
                {
                    _animCacheDict[clip.name] = clip;
                }
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            TickDesireCrystalHuntingFx();

            if(NpcEntity.IsFaQing)
            {
                if(_faqingEffectUId == 0)
                {
                    // duration < 0：不自动清理，由 IsFaQing==false 时 ForceDestroy
                    var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(PivotHeader.transform.position, -1, "SceneFaQing", NpcEntity.Id, Vector2.right);
                    if (fxCtx == null)
                    {
                        return;
                    }
                    _faqingEffectUId = fxCtx.UniqId;
                }
            }
            else
            {
                if (_faqingEffectUId != 0)
                {
                    MapSceneEffectManager.Instance.ForceDestroy(_faqingEffectUId);
                    _faqingEffectUId = 0;
                }
            }
        }

        void TickDesireCrystalHuntingFx()
        {
            if (_desireCrystalHuntingFxRoot == null)
            {
                return;
            }

            bool hunter = OverworldHUDPanel.Instance != null && OverworldHUDPanel.Instance.IsHunterMode;
            bool hasCrystal = NpcEntity != null && NpcEntity.HasAttachedDesireCrystal;
            _desireCrystalHuntingFxRoot.gameObject.SetActive(hunter && hasCrystal);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            EnsureDesireCrystalHuntingFxBuilt();
            if (_desireCrystalHuntingFxRoot != null)
            {
                _desireCrystalHuntingFxRoot.gameObject.SetActive(false);
            }
        }

        public override void Unbind()
        {
            DestroyDesireCrystalHuntingFx();
            base.Unbind();
        }

        void DestroyDesireCrystalHuntingFx()
        {
            if (_desireCrystalHuntingFxRoot != null)
            {
                Destroy(_desireCrystalHuntingFxRoot.gameObject);
                _desireCrystalHuntingFxRoot = null;
            }
        }

        void EnsureDesireCrystalHuntingFxBuilt()
        {
            if (_desireCrystalHuntingFxRoot != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(DesireCrystalHuntingFxPrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"SceneNpcPresenter: Resources.Load failed '{DesireCrystalHuntingFxPrefabResourcePath}'");
                return;
            }

            Transform parent = PivotHeader != null ? PivotHeader : transform;
            var inst = Instantiate(prefab, parent);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            _desireCrystalHuntingFxRoot = inst.transform;
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            //UnitEntity.EventOnAnimLayerUpdate += OnEventAnimLayerUpdate;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            //UnitEntity.EventOnAnimLayerUpdate -= OnEventAnimLayerUpdate;
        }


        public override bool CheckCanActiveMove()
        {
            var ret = base.CheckCanActiveMove();
            if (!ret)
            {
                return ret;
            }

            if (InteractDetailMode)
            {
                return false;
            }

            return true;
        }


        public bool CanInteractEnable()
        {
            if (OverworldHUDPanel.Instance == null)
            {
                return false;
            }

            if (NpcEntity.IsAttaching) return false;

            if (OverworldHUDPanel.Instance.IsHunterMode)
            {
                if (!UnitEntity.IsDead && !UnitEntity.MarkUnsensored)
                {
                    return true;
                }
            }
            else
            {
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                if (player != null && PlayerGamePlayRule.CanPlayerSneakThisNpc(player, NpcEntity))
                {
                    return true;
                }

                // 拾取相关
                if (UnitEntity.IsDead)
                {
                    return true;
                }

                // 可开启深度榨取
                if (UnitEntity.MarkUnsensored)
                {
                    return true;
                }

                // 可开启普通对话
                if (CheckNpcPeaceDialog())
                {
                    return true;
                }
            }

            //if (CheckCanBackHit())
            //{
            //    return true;
            //}

            return false;
        }


        private bool CheckCanBackHit()
        {
            if (OverworldHUDPanel.Instance == null) return false;
            if (!OverworldHUDPanel.Instance.IsHunterMode) return false;

            if (NpcEntity.CheckHasState(AttrIdConsts.NoInteract)) return false;


            if (NpcEntity.CheckHasState(AttrIdConsts.Charmed)) return true;

            // 背刺
            if (!MainGameManager.Instance.VisionSenser2D.SimpleCanSee(transform.position, NpcEntity.CurrentLook, MainGameManager.Instance.playerScenePresenter.transform.position, 6.0f, 150f))
            {
                return true;
            }

            return false;
        }




        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CheckNpcPeaceDialog()
        {
            if(!MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                return false;
            }

            if(UnitEntity.IsDead || UnitEntity.MarkDestroyed | UnitEntity.MarkUnsensored)
            {
                return false;
            }

            if (UnitEntity.IsInCombat)
            {
                return false;
            }

            if (NpcEntity.CheckHasState(AttrIdConsts.NoSelect))
            {
                return false;
            }

            return true;
        }


        public bool TriggerInteract(int selectionId)
        {
            //if(selectionId == EnterDetailMode)
            //{
            //    if(!UnitEntity.IsInCombat)
            //    {
            //        InteractDetailMode = true;
            //        NpcEntity.RegisterGaze("Interact", UnitEntity.LogicManager.playerLogicEntity.Id, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Override, 0);

            //        // todo 抛出事件
            //        if(SceneInteractMenuPanel.Instance != null)
            //        {
            //            SceneInteractMenuPanel.Instance.ResetRefreshSelection();
            //        }
            //    }
            //    return false;
            //}

            if (selectionId < 50)
            {
                //NpcEntity.InteractComp.TryTriggerInteract(selectionId);
                return true;
            }

            if(selectionId == ID_NormalDialog)
            {
                var entryId = NpcUnitLogicEntity.NpcDialogHubId;
                if (!string.IsNullOrEmpty(entryId))
                {
                    NpcEntity.LogicManager.viewer.PlayDialog(entryId, srcEntityId: Id);
                }
                return true;
            }
            else if (selectionId == ID_DeepAbsorbEnable)
            {
               DeepAbsorbPanel.Show(UnitEntity.Id, 5, 3);
            }
            else if(selectionId == ID_PickDropInteractId)
            {
                var container = NpcEntity.GetLootItemContainer();
                if (container != null)
                {
                    MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("use_loot_point", target: NpcEntity,
                    overrideParams: new Dictionary<string, string>()
                    {
                        ["PhaseExecutingTime"] = "0"
                    }, phaseOverrideAnims: new Dictionary<string, string>()
                    {
                        ["Executing"] = string.Empty,
                    });
                }
            }
            else if (selectionId == ID_CarryBody)
            {
                PlayerNpcCarryService.TryStartCarryInteract(NpcEntity);
            }
            else if (selectionId == ID_SneakBackstab)
            {
                var pl = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                if (pl != null && PlayerGamePlayRule.CanPlayerSneakThisNpc(pl, NpcEntity))
                {
                    pl.abilityController.TryUseAbility("player_sneak_backstab",
                        target: NpcEntity,
                        overrideParams: new Dictionary<string, string>()
                        {
                            ["InteractTime"] = "0.38",
                        },
                        phaseOverrideAnims: new Dictionary<string, string>()
                        {
                            ["Interacting"] = string.Empty,
                        });
                }
            }
            else if(selectionId == ID_ForcePushDown)
            {
                PlayerHCalculater.CheckHForceDownSuccess(MainGameManager.Instance.gameLogicManager, NpcEntity);
            }

            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position + new Vector3(0, 0.25f, 0);
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        /// <summary>
        /// 1 shendu
        /// 2 吸
        /// </summary>
        /// <returns></returns>
        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            if (NpcEntity.IsAttaching) return ret;

            if(OverworldHUDPanel.Instance.IsHunterMode)
            {
                //if (!UnitEntity.IsDead && !UnitEntity.MarkUnsensored)
                //{
                //    ret.Add(new SceneInteractSelection()
                //    {
                //        SelectId = ID_ForcePushDown,
                //        SelectContent = "强推",
                //        Selectable = true
                //    });
                //}
            }
            else
            {
                var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
                if (player != null && PlayerGamePlayRule.CanPlayerSneakThisNpc(player, NpcEntity))
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = ID_SneakBackstab,
                        SelectContent = "\u5077\u88ad",
                        Selectable = true
                    });
                }

                if (player == null || !player.IsSpecialCrouchStance)
                {
                    if (CheckNpcPeaceDialog())
                    {
                        if (!string.IsNullOrEmpty(NpcEntity.GetCurrentDialogId()))
                        {
                            ret.Add(new SceneInteractSelection()
                            {
                                SelectId = ID_NormalDialog,
                                SelectContent = "交谈",
                                Selectable = true
                            });
                        }
                    }
                }

                if (UnitEntity.IsDead || UnitEntity.MarkUnsensored)
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = ID_PickDropInteractId,
                        SelectContent = "搜刮",
                        Selectable = true
                    });
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = ID_CarryBody,
                        SelectContent = "搬运单位",
                        Selectable = !PlayerNpcCarryService.IsCarrying,
                    });
                }

                if (UnitEntity.MarkUnsensored)
                {
                    if (UnitEntity.GetAttr(AttrIdConsts.DeepZhaChance) != 0)
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = ID_DeepAbsorbEnable,
                            SelectContent = "榨干",
                            Selectable = true
                        });
                    }
                    else
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = ID_DeepAbsorbDisable,
                            SelectContent = "榨干(无）",
                            Selectable = false
                        });
                    }
                }
            }

            //if(CheckCanBackHit())
            //{
            //    ret.Add(new SceneInteractSelection()
            //    {
            //        SelectId = ID_BackHit,
            //        SelectContent = "被刺",
            //        Selectable = true
            //    }); ;
            //}

            return ret;
        }


        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

/// <summary>
/// 场景单位 基类
/// </summary>

