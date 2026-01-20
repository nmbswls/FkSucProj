using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using My.MiniGame;
using My.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.RuleTile.TilingRuleOutput;


namespace My.Map.Scene
{
    public class SceneNpcPresenter : SceneUnitPresenter, ISceneInteractable
    {
        public Vector2 Pos => transform.position;
        public string ShowName {
            get
            {
                return NpcEntity.cacheCfg.ShowName;
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

        public static int EnterDetailMode = 98;
        public static int DeepAbsorbInteractGoodId = 99;
        public static int DeepAbsorbInteractBadId = 100;
        public static int PickDropInteractId = 101;
        public static int BackHit = 102;

        public bool InteractDetailMode { get; set; }

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

        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            UnitEntity.EventOnAnimLayerUpdate += OnEventAnimLayerUpdate;

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};



            //UnitEntity.onNewDashIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.dashDir.normalized * intent.dashSpeed;
            //};

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            UnitEntity.EventOnAnimLayerUpdate -= OnEventAnimLayerUpdate;
        }


        public override bool CheckCanActiveMove()
        {
            var ret = base.CheckCanActiveMove();
            if(!ret)
            {
                return ret;
            }

            if(InteractDetailMode)
            {
                return false;
            }

            return true;
        }

        public void OnEventAnimLayerUpdate()
        {
            if (UnitEntity.AnimLayers.Count > 0)
            {
                var firstAnim = UnitEntity.AnimLayers[0];

                if (string.IsNullOrEmpty(firstAnim.Name) && _Animancer != null)
                {
                    _animCacheDict.TryGetValue(firstAnim.Name, out var clip);
                    if(clip != null)
                    {
                        _Animancer.Play(clip);
                    }
                }
            }
        }

        public bool CanInteractEnable()
        {

            if (NpcEntity.IsAttaching) return false;

            if(UnitEntity.IsDead)
            {
                return true;
            }

            // 针对晕眩类型 如果可榨取
            if (UnitEntity.MarkUnsensored)
            {
                return true;
            }

            do
            {
                if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
                {
                    if (NpcEntity.InteractComp.IsInteracting)
                    {
                        break;
                    }

                    if (UnitEntity.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
                    {
                        break;
                    }

                    var logicInts = NpcEntity.InteractComp.InteractInfos;
                    int enableOne = 0;
                    foreach (var i in logicInts)
                    {
                        if (i.Passive)
                        {
                            continue;
                        }
                        bool canInt = NpcEntity.InteractComp.CheckTriggerInteract(i.InteractId);
                        if (canInt || !i.HideWhenFail)
                        {
                            enableOne += 1;
                        }
                    }

                    if (enableOne > 0)
                    {
                        return true;
                    }
                }
            }
            while (false);

            if (!UnitEntity.IsDead && !UnitEntity.MarkNoLogic && !MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, NpcEntity.CurrentLook, MainGameManager.Instance.playerScenePresenter.transform.position, 6.0f, 150f))
            {
                return true;
            }

            return false;
        }

        public void TriggerInteract(int selectionId)
        {
            if(selectionId == EnterDetailMode)
            {
                if(UnitEntity.CombatState == NpcCombatStateComp.ECombatState.NotCombat)
                {
                    InteractDetailMode = true;
                    NpcEntity.RegisterGaze("Interact", UnitEntity.LogicManager.playerLogicEntity.Id, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Override, 0);

                    // todo 抛出事件
                    if(SceneInteractMenuPanel.Instance != null)
                    {
                        SceneInteractMenuPanel.Instance.ResetRefreshSelection();
                    }
                }
                return;
            }

            if (selectionId < 50 && UnitEntity.CombatState == NpcCombatStateComp.ECombatState.NotCombat)
            {
                NpcEntity.InteractComp.TryTriggerInteract(selectionId);
                return;
            }

            if(selectionId == DeepAbsorbInteractGoodId)
            {
               DeepAbsorbPanel.Show(0, 5, 3);
                //MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("deep_zhaqu", target: NpcEntity);
            }
            else if(selectionId == PickDropInteractId)
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
            else if(selectionId == BackHit)
            {
                //if (selectionId == 1)
                //{
                //    MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("deep_zhaqu", target: NpcEntity);
                //}
                //else if (selectionId == 2)
                //{
                //    if (MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, MainGameManager.Instance.playerScenePresenter.transform.position, NpcEntity.FaceDir, 1.0f, 60f))
                //    {
                //        return;
                //    }

                //    if (NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) == 0)
                //    {
                //        //return;
                //    }

                //    // 显示层事件
                //    MainGameManager.Instance.gameLogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
                //    {
                //        Name = "AbsorbDizzy",
                //        Param3 = this.Id,
                //    });

                //    MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("zhaqu", target: NpcEntity);
                //}

                // 显示层事件
                MainGameManager.Instance.gameLogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
                {
                    Name = "AbsorbDizzy",
                    Param3 = this.Id,
                });

                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("zhaqu", target: NpcEntity);
            }

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

            if (UnitEntity.IsDead)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = PickDropInteractId,
                    SelectContent = "搜刮",
                    Selectable = true
                }); 
            }


            if (UnitEntity.MarkUnsensored)
            {
                if (UnitEntity.GetAttr(AttrIdConsts.DeepZhaChance) != 0)
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = DeepAbsorbInteractGoodId,
                        SelectContent = "深度榨取",
                        Selectable = true
                    });
                }
                else
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = DeepAbsorbInteractBadId,
                        SelectContent = "深度榨取(无）",
                        Selectable = false
                    });
                }
            }

           


            do
            {



                if (!MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
                {
                    break;
                }

                if(UnitEntity.IsDead)
                {
                    break;
                }

                if (UnitEntity.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
                {
                    break;
                }

                if (NpcEntity.InteractComp.IsInteracting)
                {
                    break;
                }

                if (!InteractDetailMode)
                {
                    var logicInts = NpcEntity.InteractComp.InteractInfos;
                    bool hasEnabled = false;
                    foreach (var i in logicInts)
                    {
                        if (i.Passive)
                        {
                            continue;
                        }
                        bool canInt = NpcEntity.InteractComp.CheckTriggerInteract(i.InteractId);
                        if (canInt || !i.HideWhenFail)
                        {
                            hasEnabled = true; 
                            break;
                        }
                    }

                    if(hasEnabled)
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = EnterDetailMode,
                            SelectContent = "互动",
                            Selectable = true
                        });
                    }
                }
                else
                {
                    var logicInts = NpcEntity.InteractComp.InteractInfos;
                    foreach (var i in logicInts)
                    {
                        if (i.Passive)
                        {
                            continue;
                        }
                        bool canInt = NpcEntity.InteractComp.CheckTriggerInteract(i.InteractId);
                        if (canInt || !i.HideWhenFail)
                        {
                            ret.Add(new SceneInteractSelection()
                            {
                                SelectId = i.InteractId,
                                SelectContent = i.Label,
                                Selectable = true
                            });
                        }
                    }
                }
            }
            while (false);

            if (!UnitEntity.IsDead && !UnitEntity.MarkNoLogic && !MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, NpcEntity.CurrentLook, MainGameManager.Instance.playerScenePresenter.transform.position, 6.0f, 150f))
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = BackHit,
                    SelectContent = "被刺",
                    Selectable = true
                }); ;
            }

            return ret;
        }

        
    }
}

/// <summary>
/// 场景单位 基类
/// </summary>

