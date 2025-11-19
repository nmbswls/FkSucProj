using System.Collections.Generic;
using System.Drawing;
using Map.Logic.Events;
using My.Map.Logic;
using UnityEngine;
using static My.Map.MapControlEventManager.MapControlEvent;

namespace My.Map
{
    public class MapControlEventManager
    {
        public GameLogicManager logicManager;

        public class MapControlAction
        {
            public enum EActionType
            {
                None,
                SpawnShouWei
            }
            public EActionType ActionType;

            public int Param1;
            public int Param2;
            public string Param3;
            public string Param4;
        }


        public class MapControlEvent
        {

            public enum EControlTriggerType
            {
                None = 0,
                JingJie = 1,
            }

            public EControlTriggerType TriggerType;
            public int TriggerP1;

            public List<MapControlAction> Actions = new();
        }

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
                    switch(e.TriggerType)
                    {
                        case EControlTriggerType.JingJie:
                            {
                                var realEv = (MLECommonGameEvent)logicEvent;
                                if (realEv.Name == "AlertTrigger")
                                {
                                    int p = realEv.Param1;

                                    if (e.Actions != null)
                                    {
                                        foreach (var action in e.Actions)
                                        {
                                            switch (action.ActionType)
                                            {
                                                case MapControlAction.EActionType.SpawnShouWei:
                                                    {
                                                        Debug.Log("OnMapLogicEvent");

                                                        var points = logicManager.AreaManager.emptyGuardSpawners;
                                                        var guardName = action.Param3;

                                                        var record = new LogicEntityRecord4UnitBase();
                                                        record.Id = GameLogicManager.LogicEntityIdInst++;
                                                        record.EntityType = EEntityType.Npc;
                                                        record.CfgId = guardName;

                                                        record.Position = Vector2.zero;
                                                        record.FaceDir = Vector2.down;

                                                        record.MoveBehaveType = BaseUnitLogicEntity.EMoveBehaveType.NoMove;

                                                        logicManager.CreateNewEntityRecord(record);
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                                break;
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
                oneEvent.TriggerType = EControlTriggerType.JingJie;
                oneEvent.TriggerP1 = 100;

                oneEvent.Actions.Add(new MapControlAction()
                {
                    ActionType = MapControlAction.EActionType.SpawnShouWei,
                    Param1 = 3,
                    Param3 = "default_shouwei",
                });

                events.Add(oneEvent);

                if (oneEvent.TriggerType == EControlTriggerType.JingJie)
                {
                    if(!_eventListeners.TryGetValue(EMapLogicEventType.Common, out var ll))
                    {
                        ll = new();
                        _eventListeners[EMapLogicEventType.Common] = ll;
                    }

                    ll.Add(oneEvent);
                }
            }
        }
    }

    
}