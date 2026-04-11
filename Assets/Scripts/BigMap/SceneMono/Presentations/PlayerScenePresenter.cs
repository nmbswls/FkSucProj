using Config;
using Map.Entity;
using Map.Logic;
using Map.Scene;
using My.Map.Entity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Rendering.CameraUI;


namespace My.Map.Scene
{

    public class PlayerAimHelper
    {
        private Collider2D[] hits = new Collider2D[16];


        // 配置参数：根据手感调整这些值

        private const float SearchRadius = 5.0f; // 技能的最大射程（硬限制）
        private const float MaxAimAngle = 60f;   // 索敌扇形角度的一半（60度 = 总共120度扇形）
        private const float MouseHoverThreshold = 0.8f; // 鼠标精准悬停的判定半径

        public long GetSmartMainTargetId(Vector2 playerPos, Vector2 mouseWorldPos)
        {
            int layerMask = 1 << LayerMask.NameToLayer("MapTarget");

            // 1. 获取射程内所有目标
            var count = Physics2D.OverlapCircleNonAlloc(playerPos, SearchRadius, hits, layerMask);

            long bestTargetId = 0;
            float bestScore = float.MinValue;

            // 计算玩家到鼠标的“朝向向量”
            Vector2 aimDir = (mouseWorldPos - playerPos).normalized;

            for (int i = 0; i < count; i++)
            {
                var col = hits[i];
                if (col == null) continue;

                var presentation = col.GetComponentInParent<IScenePresentation>();
                if (presentation == null) continue;
                var logic = presentation.GetLogicEntity();
                if (logic is not BaseUnitLogicEntity unitEntity) continue;
                if (unitEntity.FactionId == EFactionId.Player || unitEntity.IsDead) continue;

                if (unitEntity.CheckHasState(AttrIdConsts.NoSelect)) continue;
                Vector2 targetPos = unitEntity.Pos;

                // --- 核心评分逻辑修改 ---

                //// 1. 优先判定：是否鼠标直接点在怪身上？(最高优先级)
                //float distToMouse = Vector2.Distance(mouseWorldPos, targetPos);
                //if (distToMouse <= MouseHoverThreshold)
                //{
                //    // 如果鼠标直接悬停，直接给最大分，距离越近分越高，确保多个重叠时选最准的
                //    float manualScore = 1000f - distToMouse;
                //    if (manualScore > bestScore)
                //    {
                //        bestScore = manualScore;
                //        bestTargetId = unitEntity.Id;
                //    }
                //    continue; // 既然直接悬停了，就不用走下面的常规逻辑了
                //}

                // 2. 常规判定：基于玩家距离和朝向
                Vector2 toTargetDir = targetPos - playerPos;
                float distToPlayer = toTargetDir.magnitude;

                // 归一化方向用于计算角度
                Vector2 toTargetDirNorm = toTargetDir / distToPlayer; // 简单的归一化

                // 计算夹角 (0度表示正对鼠标方向，180度表示在背后)
                float angle = Vector2.Angle(aimDir, toTargetDirNorm);

                // 【筛选 1】角度剔除：如果怪在侧面或背后（超出扇形），直接不考虑
                // 除非怪非常非常近（贴脸），为了防止漏怪，可以允许贴脸怪无视角度
                if (angle > MaxAimAngle && distToPlayer > 1.0f)
                {
                    continue;
                }

                // 【评分】离玩家越近，分数越高 (这是你要的核心逻辑)
                // 基础分是 (射程 - 距离)，距离越小分越高
                float score = SearchRadius - distToPlayer;

                // 【微调】角度越正，稍微加一点点分 (防止两个怪距离一样时，选歪的那个)
                // 这里的权重给很小 (0.2f)，保证主要还是看距离
                score += (1.0f - (angle / MaxAimAngle)) * 0.5f;

                //if (unitEntity.IsBoss) score += 2.0f; // Boss 依然稍微优先

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTargetId = unitEntity.Id;
                }
            }

            System.Array.Clear(hits, 0, count);
            return bestTargetId;
        }
    }


    public class PlayerScenePresenter : SceneUnitPresenter, ISceneInteractable, IAnimancerPrewarmable
    {

        public GhostTrailSpawner MoveTrailSpawner;

        public MapGlobalNoiseEmitter NoiseEmitter;

        public PlayerAimHelper AimHelper;

        protected override void Awake()
        {
            base.Awake();

            AimHelper = new();
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

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }

        public Vector2? LastValidMovePos { get; set; }
        public bool IsAdjustingFromForbidden { get; set; }


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

            CheckTickZoneArea();

            CheckTriggerTeleporter();

            TickForbiddenAreaMove();
        }

        private void TickForbiddenAreaMove()
        {
            if(IsAdjustingFromForbidden)
            {

                if (infoProvider.IsForbidden)
                {
                    bool passed = true;
                    foreach (var cond in infoProvider.EnableCondition)
                    {
                        if (!MainGameManager.Instance.gameLogicManager.CheckCommonCond(cond))
                        {
                            passed = false;
                            break;
                        }
                    }

                    if (!passed)
                    {
                        isForbiddenPos = true;
                    }
                }

                Vector2 diff = new Vector2(transform.position.x, transform.position.y) - LastValidMovePos.Value;
                transform.localPosition = LastValidMovePos.Value;
                IsAdjustingFromForbidden = false;

                Debug.Log("forbit move adjust");


                return;
            }
            else
            {
                int count = Physics2D.OverlapPointNonAlloc(PlayerEntity.Pos, zoneTriggerCache, 1 << LayerMask.NameToLayer("Zone"));
                for (int i = 0; i < count; i++)
                {
                    var col = zoneTriggerCache[i];
                    if (col == null) continue;

                    var forbidChecker = col.GetComponentInParent<ForbidZoneChecker>();
                    if (forbidChecker == null) continue;

                }
            }

            
            

        }


        private float _checkTeleporterTimer = 0;
        private Collider2D[] hits = new Collider2D[16];
        private void CheckTriggerTeleporter()
        {
            if (LogicTime.time - _checkTeleporterTimer < 0.5f)
            {
                return;
            }

            _checkTeleporterTimer = LogicTime.time;

            int cnt = Physics2D.OverlapCircleNonAlloc(transform.position, 0.5f, hits, 1 << LayerMask.NameToLayer("Trigger"));
            for (int i = 0; i < cnt; i++)
            {
                var teleporter = hits[i].GetComponentInParent<SceneTeleporterPresenter>();
                if(teleporter == null)
                {
                    continue;
                }
                Debug.Log("Trigger switch");

                teleporter.TryTriggerTeleport();
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

            // 初始化position
            //LastValidMovePos = logic.Pos;
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
            if (PlayerEntity.IsInStealth())
            {
                return true;
            }

            if (PlayerEntity.AtttachingObjList.Count > 0)
            {
                return true;
            }

            return false;
        }

        public bool TriggerInteract(int selectionId)
        {
            if (selectionId == 1)
            {
                PlayerEntity.EndStealth();
                return true;
            }
            else if (selectionId == 2)
            {
                PlayerEntity.abilityController.TryUseAbility("hit_attach");
                return true;
            }

            return false;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
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

            PlayerEntity.EventOnRequestAimHelper += OnRequestAimHelper;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            PlayerEntity.EventOnAttachmentUpdate -= OnEventAttachmentUpdate;
            PlayerEntity.EventOnRequestAimHelper -= OnRequestAimHelper;
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

        private float _checkAreaTiker = 0;
        private Collider2D[] zoneTriggerCache = new Collider2D[16];

        public bool IsInBusyZone = false;
        
        /// <summary>
        /// 高频检查 保证精度
        /// </summary>
        private void CheckTickZoneArea()
        {
            if (LogicTime.time - _checkAreaTiker < 1.0f)
            {
                return;
            }
            _checkAreaTiker = LogicTime.time;

            bool isInBusy = false;
            bool isInAlert = false;

            int count = Physics2D.OverlapPointNonAlloc(PlayerEntity.Pos, zoneTriggerCache, 1 << LayerMask.NameToLayer("Zone"));
            for (int i = 0; i < count; i++)
            {
                var col = zoneTriggerCache[i];
                if(col == null) continue;

                ZoneInfoProvider infoProvider = col.GetComponentInParent<ZoneInfoProvider>();
                if (infoProvider == null) continue;

                if((infoProvider.ZoneType & ZoneInfoProvider.EZoneFlag.BusyZone) != 0)
                {
                    isInBusy = true;
                }

            }

            if(isInAlert)
            {
                MainGameManager.Instance.gameLogicManager.AreaManager.PlayerInAlertArea = true;
            }
            else
            {
                MainGameManager.Instance.gameLogicManager.AreaManager.PlayerInAlertArea = false;
            }

            IsInBusyZone = isInBusy;

            if(isForbiddenPos)
            {
                IsAdjustingFromForbidden = true;
                if(LastValidMovePos == null)
                {
                    LastValidMovePos = new Vector2(0, 0);
                }
            }
            else
            {
                LastValidMovePos = transform.position;
            }
        }



        public bool IsAutoInteract()
        {
            return false;
        }

        private void OnRequestAimHelper()
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(MainGameManager.Instance.inputBinder.LastPos);
            var mainTargetId = AimHelper.GetSmartMainTargetId(PlayerEntity.Pos, mouseWorldPos);

            Debug.Log($"OnRequestAimHelper update targetId:{mainTargetId}" );

            PlayerEntity.UpdateSupportTargetId(mainTargetId);
        }

        public AnimationClip[] preLoadAnimClips;


        public AnimationClip[] GetClipsToPrewarm()
        {
            return preLoadAnimClips;
        }
    }

}


