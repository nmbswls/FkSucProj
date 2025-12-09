using System.Collections.Generic;
using System.Drawing;
using Map.Logic.Events;
using My.Map.Logic;
using UnityEngine;
using static My.Map.MapControlEvent;

namespace My.Map
{
    public class MapControlEventManager
    {
        public GameLogicManager logicManager;

        
        public class InnerListener : IMapLogicEventHandler
        {
            private MapControlEventManager controlEventManager;
            public InnerListener(MapControlEventManager controlEventManager)
            {
                this.controlEventManager = controlEventManager;
            }

            public void Handle(in IMapLogicEvent evt)
            {
                controlEventManager.OnMapLogicEvent(evt);
            }
        }

        public MapControlEventManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;

            innerListener = new(this);
        }
        public InnerListener innerListener;

        private List<MapLogicSubscription> subs = new();

        public void Initialize()
        {
            foreach (var sub in subs)
            {
                logicManager.LogicEventBus.Unsubscribe(sub);
            }
            subs.Clear();

            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Common, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.Attract, innerListener);
                subs.Add(sub);
            }
            {
                var sub = logicManager.LogicEventBus.Subscribe(EMapLogicEventType.OnDie, innerListener);
                subs.Add(sub);
            }

            RegisterMapControlEvents();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="logicEvent"></param>
        public void OnMapLogicEvent(IMapLogicEvent logicEvent)
        {
            var evType = logicEvent.Type;

            _eventListeners.TryGetValue(evType, out var ll);
            if (ll != null)
            {
                foreach(var e in ll)
                {
                    if(!e.CheckMatch(logicEvent))
                    {
                        return;
                    }

                    Debug.Log($"OnMapLogicEvent e {logicEvent}");

                    if (e.Actions != null)
                    {
                        foreach (var action in e.Actions)
                        {
                            ExecuteMapControlAction(action);
                        }
                    }
                }
            }
        }

        public List<MapControlEvent> events = new();

        protected Dictionary<EMapLogicEventType, List<MapControlEvent>> _eventListeners = new();

        /// <summary>
        /// ×¢²áÊÂ¼þ
        /// </summary>
        public void RegisterMapControlEvents()
        {
            {
                MapControlEvent oneEvent = new();
                oneEvent.TriggerType = ETriggerType.JingJie;
                oneEvent.TriggerP1 = 1;

                oneEvent.Actions.Add(new MapControlAction()
                {
                    ActionType = MapControlAction.EActionType.SpawnShouWei,
                    Param1 = 3,
                    Param3 = "default_shouwei",
                });

                events.Add(oneEvent);
            }

            //{
            //    // 
            //    MapControlEvent oneEvent = new();
            //    oneEvent.TriggerType = ETriggerType.JingJie;
            //    oneEvent.TriggerP1 = 1;

            //    oneEvent.Actions.Add(new MapControlAction()
            //    {
            //        ActionType = MapControlAction.EActionType.SpawnShouWei,
            //        Param1 = 3,
            //        Param3 = "default_shouwei",
            //    });

            //    events.Add(oneEvent);
            //}

            foreach (var ev in events)
            {
                EMapLogicEventType listenType = EMapLogicEventType.Invalid;

                switch (ev.TriggerType)
                {
                    case ETriggerType.JingJie:
                        {
                            listenType = EMapLogicEventType.Common;
                        }
                        break;
                }

                if(listenType != EMapLogicEventType.Invalid)
                {
                    if (!_eventListeners.TryGetValue(listenType, out var ll))
                    {
                        ll = new();
                        _eventListeners[listenType] = ll;
                    }

                    ll.Add(ev);
                }
            }
            
        }

        /// <summary>
        /// Ö´ÐÐaction
        /// </summary>
        /// <param name="action"></param>
        public void ExecuteMapControlAction(MapControlAction action)
        {

            switch(action.ActionType)
            {
                case MapControlAction.EActionType.SpawnShouWei:
                    {
                        Debug.Log("OnMapLogicEvent");

                        var points = logicManager.AreaManager.emptyGuardSpawners;
                        var guardName = action.Param3;

                        var record = new LogicEntityRecord4Npc();
                        record.Id = GameLogicManager.LogicEntityIdInst++;
                        record.EntityType = EEntityType.Npc;
                        record.CfgId = guardName;

                        record.Position = Vector2.zero;
                        record.FaceDir = Vector2.down;

                        record.MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove;

                        logicManager.AddNewEntityRecord(record);
                    }
                    break;
            }
        }
    }

    
}