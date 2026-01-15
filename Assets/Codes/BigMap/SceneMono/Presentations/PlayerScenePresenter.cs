using Config;
using Map.Entity;
using Map.Logic;
using Map.Scene;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace My.Map.Scene
{
    public class PlayerScenePresenter : SceneUnitPresenter, ISceneInteractable
    {

        public GhostTrailSpawner MoveTrailSpawner;

        public MapGlobalNoiseEmitter NoiseEmitter;

        protected override void Awake()
        {
            base.Awake();
        }

        public PlayerLogicEntity PlayerEntity
        {
            get
            {
                return (PlayerLogicEntity)_logic;
            }
        }

        public string ShowName => "Self";

        public Vector2 Pos => transform.position;

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_logic == null) return;

            TryUpdateInRoomStatus(dt);

            TickMoveNoiseEffect(LogicTime.time, dt);

            if(PlayerEntity.controlledMoveCtx != null && PlayerEntity.controlledMoveCtx.WithEffect)
            {
                MoveTrailSpawner.IsShowing = true;
            }
            else
            {
                MoveTrailSpawner.IsShowing = false;
            }
        }


        public override void ApplyState(object state)
        {
            if (state is InteractPointState s)
            {
                transform.position = s.Position;
                //if (icon != null) icon.enabled = s.IsEnabled;
                //if (highlightFx != null) highlightFx.SetActive(s.IsEnabled && _logic.IsInAOI);
            }
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }



        private float _updateRoomStatusTimer = 0;

        private string? currRoomId = null;
        private string? lastRoomId = null;
        private float lastChangeRoomTime = 0;
        public void TryUpdateInRoomStatus(float dt)
        {
            _updateRoomStatusTimer -= dt;
            if (_updateRoomStatusTimer < 0)
            {
                var collides = Physics2D.OverlapPointAll(transform.position, 1 << LayerMask.NameToLayer("MapRoom"));
                if (collides.Length > 0)
                {
                    var infoProvider = collides.First().transform.GetComponentInParent<MapRoomProvider>();
                    if (currRoomId != infoProvider.RoomId)
                    {
                        lastRoomId = currRoomId;
                        currRoomId = infoProvider.RoomId;
                        OnRoomStatusChange();
                    }
                }
                else
                {
                    if (currRoomId != null)
                    {
                        lastRoomId = currRoomId;
                        currRoomId = null;
                        OnRoomStatusChange();
                    }
                }
                _updateRoomStatusTimer = 0.3f;
            }
        }

        public void OnRoomStatusChange()
        {
            MainGameManager.Instance.SceneFadeManager.RefreshCeilFadeEffect(currRoomId);
        }

        protected float lastHasSpeedTs = 0;
        protected float lastEmitNoiseTs = 0;

        /// <summary>
        /// 移动噪音
        /// </summary>
        /// <param name="now"></param>
        /// <param name="dt"></param>
        protected void TickMoveNoiseEffect(float now, float dt)
        {
            // 有速度时记录时间戳
            if (rb.velocity.magnitude > 0.1f)
            {
                lastHasSpeedTs = now;
            }

            if (lastHasSpeedTs > 0.1f)
            {
                float interval = 0.5f;
                float intensity = 0.5f;
                if (now - lastEmitNoiseTs > interval)
                {
                    MainGameManager.Instance.ShowNoiseEffect(intensity, transform.position);
                    lastEmitNoiseTs = now;
                }
            }
        }

        public bool CanInteractEnable()
        {
            if(PlayerEntity.IsInStealth())
            {
                return true;
            }

            if (PlayerEntity.AtttachingObjList.Count > 0)
            {
                return true;
            }

            return false;
        }

        public void TriggerInteract(int selectionId)
        {
            if (selectionId == 1)
            {
                PlayerEntity.EndStealth();
            }
            else if (selectionId == 2)
            {
                PlayerEntity.abilityController.TryUseAbility("hit_attach");
            }
        }

        public Vector3 GetHintAnchorPosition()
        {
            return GetWorldPosition();
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            List<SceneInteractSelection> ret = new();

            if (PlayerEntity.IsInStealth())
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "Leave",
                    Selectable = true,
                });
            }

            if(PlayerEntity.AtttachingObjList.Count > 0)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 2,
                    SelectContent = "挣扎",
                    Selectable = true,
                });
            }

            return ret;
        }


        protected override void OnEventUnitDie(long entityId)
        {
            base.OnEventUnitDie(entityId);

            MainGameManager.Instance.WaitingIntoDefeatedBattle();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            PlayerEntity.EventOnAttachmentUpdate += OnEventAttachmentUpdate;
            PlayerEntity.EventOnAttachmentUpdate += OnEventAttachmentUpdate;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            PlayerEntity.EventOnAttachmentUpdate -= OnEventAttachmentUpdate;
        }

        private void OnEventAttachmentUpdate(long e)
        {
            RefreshAttachmentView();
        }

        public Transform AttachmentRoot;

        private Dictionary<int, GameObject> AttachViewDict = new();
        public void RefreshAttachmentView()
        {
            foreach(var attach in PlayerEntity.AtttachingObjList)
            {
                if(!AttachViewDict.TryGetValue(attach.Id, out var showObj))
                {
                    var cfg = MapPlayerAttachObjCfgLoader.Get(attach.AttachId);
                    var prefab = Resources.Load<GameObject>($"Prefab/Attach/{attach.AttachId}");
                    var go = GameObject.Instantiate(prefab, AttachmentRoot);
                    go.SetActive(true);
                    go.transform.localPosition = Vector3.zero;
                    AttachViewDict[attach.Id] = go;
                }
            }

            foreach(var key in AttachViewDict.Keys.ToList())
            {
                if (PlayerEntity.AtttachingObjList.Find((item) => { return item.Id == key; }) == null)
                {
                    GameObject.Destroy(AttachViewDict[key].gameObject);
                    AttachViewDict.Remove(key); 
                }
            }

        }
    }

}


