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

        public Collider2D MainCol;

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
            if (TeleporterEntity.LogicManager.IsLocalRoomTeleportLocked)
                return;

            string mapName = TeleporterEntity.TargetMapId;
            string namedP = TeleporterEntity.TargetNamedPoint;

            // 原地传送
            if (string.IsNullOrEmpty(mapName) || mapName == TeleporterEntity.LogicManager.AreaManager.AreaOverlayId)
            {
                if (!string.IsNullOrEmpty(namedP))
                {
                    var p = TeleporterEntity.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(namedP);
                    if (p == null)
                    {
                        return;
                    }
                    TeleporterEntity.LogicManager.RequestLocalRoomTeleport(p.Value.Position, () =>
                    {
                        //TeleporterEntity.LogicManager.globalBuffManager.AddBuff(
                        //    TeleporterEntity.LogicManager.playerLogicEntity.Id, "lock_move", overrideDuration: 0.6f);
                    });
                }
            }
            else
            {
                TeleporterEntity.LogicManager.PreparePlayerSwitchArea(mapName, false, namedP);
            }
       }


    }
}

