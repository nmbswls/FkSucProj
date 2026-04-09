using DG.Tweening;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Rendering.CameraUI;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneTeleporterPresenter : ScenePresentationBase<LogicEntityTeleporter>
    {
        [SerializeField] private GameObject highlightFx;

        public Collider2D MainBlock;

        public LogicEntityTeleporter TeleporterEntity { get { return (LogicEntityTeleporter)_logic; } }


        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }


       public void TryTriggerTeleport()
       {
            string mapName = TeleporterEntity.TargetMapId;
            string namedP = TeleporterEntity.TargetNamedPoint;

            // Ô­µØ´«ËÍ
            if (string.IsNullOrEmpty(mapName) || mapName == TeleporterEntity.LogicManager.AreaManager.MapName)
            {
                if (string.IsNullOrEmpty(namedP))
                {
                    var p = TeleporterEntity.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(namedP);
                    if (p == null)
                    {
                        return;
                    }
                    TeleporterEntity.LogicManager.playerLogicEntity.TeleportTo(p.Value.Position);
                }
            }
            else
            {
                TeleporterEntity.LogicManager.PreparePlayerSwitchArea(mapName, false, namedP);
            }
       }


    }
}

