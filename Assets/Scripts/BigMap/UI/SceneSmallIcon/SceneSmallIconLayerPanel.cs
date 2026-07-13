
using System;
using System.Collections.Generic;
using System.Linq;
using My.Map;
using My.Map.Entity;
using My.Map.Hunting;
using My.Map.Scene;
using TMPro;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static QuickDebugShow;


namespace My.UI
{

    public class SceneSmallIconLayerPanel : PanelBase, IRefreshable
    {
        public static SceneSmallIconLayerPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("SmallIconLayer");
                if (panel != null && panel is SceneSmallIconLayerPanel sceneSmallIconLayer)
                {
                    return sceneSmallIconLayer;
                }
                return null;
            }
        }

        public QuickDebugShow DebugIconsShower;

        private Camera _mainCam;
        public Canvas TopCanvas;


        public override void Setup(object data = null)
        {
            //BottomProgressPanel.Setup();
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel() => false;
        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(int index) => false;

        public SceneInteractUIHinter InteractHintPrefab;
        public SceneEvilAlertUIItem EvilAlertPrefab;
        public SceneNPCHStatUIStruct NPCHStatPrefab; // 需要在 Inspector 中拖拽 Prefab
        public SceneUnitBuffHeadHintItem BuffHeadHintPrefab;

        [SerializeField]
        SceneUnitHpBarItem HpBarPrefab;
        [SerializeField]
        SceneUnitChargeBarItem ChargeBarPrefab;
        [SerializeField]
        float hpBarShowDuration = 3f;
        [SerializeField]
        float hpBarScreenOffsetY = 12f;
        [SerializeField]
        float chargeBarScreenOffsetY = -10f;

        const float BuffHeadHintScreenOffsetY = 28f;


        private Dictionary<ISceneInteractable, SceneInteractUIHinter> sceneInteractHintDicts = new();
        private Queue<SceneInteractUIHinter> _hintPool = new();


        private Dictionary<long, SceneEvilAlertUIItem> _activeEvilAlerts = new Dictionary<long, SceneEvilAlertUIItem>();
        private Queue<SceneEvilAlertUIItem> _evilAlertPool = new Queue<SceneEvilAlertUIItem>();


        private Dictionary<long, SceneNPCHStatUIStruct> _activeNpcHStat = new Dictionary<long, SceneNPCHStatUIStruct>();
        private Queue<SceneNPCHStatUIStruct> _npcHStatPool = new Queue<SceneNPCHStatUIStruct>();

        private Dictionary<long, SceneUnitBuffHeadHintItem> _activeBuffHeadHints = new Dictionary<long, SceneUnitBuffHeadHintItem>();
        private Queue<SceneUnitBuffHeadHintItem> _buffHeadHintPool = new Queue<SceneUnitBuffHeadHintItem>();
        private HashSet<long> _buffHeadHintSeenThisFrame = new HashSet<long>();

        struct UnitHpBarTrackState
        {
            public long LastHp;
            public bool Initialized;
            public float ShowUntil;
        }

        readonly Dictionary<long, UnitHpBarTrackState> _hpBarTracks = new Dictionary<long, UnitHpBarTrackState>();
        readonly Dictionary<long, SceneUnitHpBarItem> _activeHpBars = new Dictionary<long, SceneUnitHpBarItem>();
        readonly Queue<SceneUnitHpBarItem> _hpBarPool = new Queue<SceneUnitHpBarItem>();

        readonly Dictionary<long, SceneUnitChargeBarItem> _activeChargeBars = new Dictionary<long, SceneUnitChargeBarItem>();
        readonly Queue<SceneUnitChargeBarItem> _chargeBarPool = new Queue<SceneUnitChargeBarItem>();

        bool _bindingsSuspended;

        public void Awake()
        {
            InteractHintPrefab.gameObject.SetActive(false);
            EvilAlertPrefab.gameObject.SetActive(false);
            if (NPCHStatPrefab != null) NPCHStatPrefab.gameObject.SetActive(false);
            if (BuffHeadHintPrefab != null) BuffHeadHintPrefab.gameObject.SetActive(false);
            if (HpBarPrefab != null) HpBarPrefab.gameObject.SetActive(false);
            if (ChargeBarPrefab != null) ChargeBarPrefab.gameObject.SetActive(false);

            TopCanvas = GetComponentInParent<Canvas>();
            _mainCam = Camera.main;
        }

        void OnEnable()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null)
            {
                glm.EventOnHardAreaClearStarting += OnHardAreaClearStarting;
            }
        }

        void OnDisable()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null)
            {
                glm.EventOnHardAreaClearStarting -= OnHardAreaClearStarting;
            }
        }

        void OnHardAreaClearStarting()
        {
            ClearAllSceneSmallIcons();
            _bindingsSuspended = true;
        }

        public void ClearAllSceneSmallIcons()
        {
            foreach (var key in sceneInteractHintDicts.Keys.ToList())
            {
                RecycleInteractHintUI(key);
            }

            foreach (var key in _activeEvilAlerts.Keys.ToList())
            {
                RecycleEvilAlertUI(key);
            }

            foreach (var key in _activeNpcHStat.Keys.ToList())
            {
                RecycleNpcHStatUI(key);
            }

            foreach (var key in _activeBuffHeadHints.Keys.ToList())
            {
                RecycleBuffHeadHintUI(key);
            }

            foreach (var key in _activeHpBars.Keys.ToList())
            {
                RecycleHpBarUI(key);
            }

            foreach (var key in _activeChargeBars.Keys.ToList())
            {
                RecycleChargeBarUI(key);
            }

            _hpBarTracks.Clear();
            _buffHeadHintSeenThisFrame.Clear();
            DebugIconsShower?.Clear();
            MapSpeechBubbleManager.Instance?.ForceClearAll();
        }

        public override void Show()
        {
            base.Show();

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null && glm.IsInSecretBaseContext())
            {
                ClearAllSceneSmallIcons();
                _bindingsSuspended = true;
                return;
            }

            _bindingsSuspended = false;
        }

        public void Update()
        {
            LowFreqCleanInvalidEntry();
        }

        public void LateUpdate()
        {
            if (_mainCam == null) _mainCam = Camera.main;

            UpdateSceneSmallIconBind();
        }

        private float _lowFreqCleanInvalidTimer;
        private List<long> _lowFreqCleanCaches = new List<long>();

        private void LowFreqCleanInvalidEntry()
        {
            if(LogicTime.time - _lowFreqCleanInvalidTimer < 1.0f)
            {
                return;
            }

            _lowFreqCleanInvalidTimer = LogicTime.time;

            {
                _lowFreqCleanCaches.Clear();

                foreach (var kv in _activeEvilAlerts)
                {
                    if (_activeEvilAlerts[kv.Key].BindingNpc == null)
                    {
                        _lowFreqCleanCaches.Add(kv.Key);
                        continue;
                    }
                }

                foreach (var oneId in _lowFreqCleanCaches)
                {
                    RecycleEvilAlertUI(oneId);
                }
            }

            foreach (var key in sceneInteractHintDicts.Keys.ToList())
            {
                if (key is not UnityEngine.Object obj || obj == null)
                {
                    RecycleInteractHintUI(key);
                }
            }

            _lowFreqCleanCaches.Clear();
            foreach (var kv in _activeNpcHStat)
            {
                if (_activeNpcHStat[kv.Key].bindingNpc == null)
                {
                    _lowFreqCleanCaches.Add(kv.Key);
                }
            }

            foreach (var oneId in _lowFreqCleanCaches)
            {
                RecycleNpcHStatUI(oneId);
            }

            _lowFreqCleanCaches.Clear();
            foreach (var kv in _activeBuffHeadHints)
            {
                if (_activeBuffHeadHints[kv.Key].BindingUnit == null)
                {
                    _lowFreqCleanCaches.Add(kv.Key);
                }
            }

            foreach (var oneId in _lowFreqCleanCaches)
            {
                RecycleBuffHeadHintUI(oneId);
            }

            _lowFreqCleanCaches.Clear();
            foreach (var kv in _activeHpBars)
            {
                if (kv.Value.Binding == null || !kv.Value.Binding.CheckValid())
                {
                    _lowFreqCleanCaches.Add(kv.Key);
                }
            }

            foreach (var oneId in _lowFreqCleanCaches)
            {
                RecycleHpBarUI(oneId);
                _hpBarTracks.Remove(oneId);
            }

            _lowFreqCleanCaches.Clear();
            foreach (var kv in _activeChargeBars)
            {
                if (kv.Value.Binding == null || !kv.Value.Binding.CheckValid())
                {
                    _lowFreqCleanCaches.Add(kv.Key);
                }
            }

            foreach (var oneId in _lowFreqCleanCaches)
            {
                RecycleChargeBarUI(oneId);
            }
        }

        private float _screenWidth;
        private float _screenHeight;
        private float _bufferX;
        private float _bufferY;


        protected void UpdateSceneSmallIconBind()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (_bindingsSuspended
                || glm == null
                || glm.IsInSecretBaseContext()
                || glm.playerLogicEntity == null)
            {
                return;
            }

            // 缓存屏幕尺寸和 10% 的防抖缓冲
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _bufferX = _screenWidth * 0.1f;
            _bufferY = _screenHeight * 0.1f;

            _buffHeadHintSeenThisFrame.Clear();

            var activePresenters = SceneAOIManager.Instance.GetAllActivePresentation();
            foreach(var p in activePresenters)
            {
                long entityId = p.Id;
                if(!p.CheckValid())
                {
                    continue;
                }

                var innerEntity = p.GetLogicEntity();
                if (innerEntity == null || innerEntity.MarkDestroyed || innerEntity.MarkDespawn)
                    continue;


                CheckUpdateSceneUnitAlert(p);

                CheckUpdateSceneInteracbleHint(p);

                CheckUpdateSceneNpcHStat(p);

                CheckUpdateSceneUnitBuffHeadHint(p);

                CheckUpdateSceneUnitHpBar(p);

                CheckUpdateSceneUnitChargeBar(p);
            }

            RecycleStaleBuffHeadHints();
        }

        #region Alert UI 更新与按需分配

        protected void CheckUpdateSceneUnitAlert(IScenePresentation presenter)
        {
            if (presenter is not SceneNpcPresenter npcPresenter) return;

            bool hasActiveUI = _activeEvilAlerts.ContainsKey(npcPresenter.Id);

            bool isVisible = false;
            Vector3 screenPos = Vector3.zero;

            if (!npcPresenter.NpcEntity.IsEvilAlert)
            {
                isVisible = false;
            }
            else
            {
                screenPos = _mainCam.WorldToScreenPoint(npcPresenter.Pos);
                isVisible = screenPos.z > 0 &&
                screenPos.x >= -_bufferX && screenPos.x <= _screenWidth + _bufferX &&
                                 screenPos.y >= -_bufferY && screenPos.y <= _screenHeight + _bufferY;
            }

            if (isVisible)
            {
                SceneEvilAlertUIItem uiItem;
                // 在屏幕内，如果没有分配UI，则从池中取
                if (!hasActiveUI)
                {
                    uiItem = AllocateEvilAlertUI(npcPresenter);
                }
                else
                {
                    uiItem = _activeEvilAlerts[presenter.Id];
                }

                // 2. 更新 UI 位置
                UpdateSceneAlertUI(uiItem, screenPos);
            }
            else
            {
                // 3. 在屏幕外，如果占用了 UI，立刻回收
                if (hasActiveUI)
                {
                    RecycleEvilAlertUI(presenter.Id);
                }
            }
        }

        private SceneEvilAlertUIItem AllocateEvilAlertUI(SceneNpcPresenter npcPresenter)
        {
            SceneEvilAlertUIItem uiItem;
            if (_evilAlertPool.Count > 0)
            {
                uiItem = _evilAlertPool.Dequeue();
            }
            else
            {
                uiItem = Instantiate(EvilAlertPrefab, transform);
            }

            uiItem.Bind(npcPresenter); // 调用外部组件的绑定方法
            _activeEvilAlerts[npcPresenter.Id] = uiItem;
            return uiItem;
        }

        private void RecycleEvilAlertUI(long entityId)
        {
            if (_activeEvilAlerts.TryGetValue(entityId, out var uiItem))
            {
                uiItem.Unbind(); // 内部解绑并隐藏
                _evilAlertPool.Enqueue(uiItem);
                _activeEvilAlerts.Remove(entityId);
            }
        }

        private void UpdateSceneAlertUI(SceneEvilAlertUIItem uiItem, Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas.worldCamera, // 注意 Canvas 模式，如果是 Overlay 这里传 null
                out Vector2 uiLocalPos
            );
            uiLocalPos += Vector2.up * 20f;
            uiItem.transform.localPosition = uiLocalPos;
        }
        #endregion


        #region ui 交互更新

        protected void CheckUpdateSceneInteracbleHint(IScenePresentation presenter)
        {
            if (presenter is not ISceneInteractable interactblePresenter)
            {
                return;
            }

            if(presenter.GetLogicEntity().Type != EEntityType.InteractPoint)
            {
                return;
            }

            // 休眠交互点（Dormant）未显形时不可交互，也不显示屏幕 InteractHint
            if (presenter is InteractPointPresenter pointPresenter
                && !pointPresenter.RealLogic.IsLogicInteractAvailable)
            {
                if (sceneInteractHintDicts.ContainsKey(interactblePresenter))
                {
                    RecycleInteractHintUI(interactblePresenter);
                }
                return;
            }

            bool hasActiveUI = sceneInteractHintDicts.ContainsKey(interactblePresenter);
            Vector3 worldPos = Vector3.zero;
            bool isVisible = false;
            do
            {
                if(interactblePresenter.CanInteractEnable())
                {
                    break;
                }

                if(HuntingHudPanel.Instance != null && HuntingHudPanel.Instance.IsHunterMode)
                {
                    break;
                }

                // 1. 判断是否在屏幕可视范围内（带缓冲区域）
                worldPos = presenter.GetWorldPosition();
                var viewportPos = _mainCam.WorldToViewportPoint(worldPos);
                isVisible = viewportPos.z > 0 &&
                                 viewportPos.x >= -0.1f && viewportPos.x <= 1.1f &&
                                 viewportPos.y >= -0.1f && viewportPos.y <= 1.1f;
            }
            while (false);

            if (isVisible)
            {
                SceneInteractUIHinter hintItem;
                // 在屏幕内，如果没有分配UI，则从池中取
                if (!hasActiveUI)
                {
                    hintItem = AllocateInteractHintUI(interactblePresenter);
                }
                else
                {
                    hintItem = sceneInteractHintDicts[interactblePresenter];
                }

                // 2. 更新 UI 位置
                UpdateInteractUIPosition(hintItem, worldPos);
            }
            else
            {
                // 3. 在屏幕外，如果占用了 UI，立刻回收
                if (hasActiveUI)
                {
                    RecycleInteractHintUI(interactblePresenter);
                }
            }
        }

        private SceneInteractUIHinter AllocateInteractHintUI(ISceneInteractable interactPoint)
        {
            SceneInteractUIHinter hint;
            if (_hintPool.Count > 0)
            {
                hint = _hintPool.Dequeue();
            }
            else
            {
                var newHintGo = Instantiate(InteractHintPrefab, transform);
                hint = newHintGo.GetComponent<SceneInteractUIHinter>();
            }

            hint.Bind(interactPoint);
            hint.sceneInteract = interactPoint;
            hint.gameObject.SetActive(true);
            sceneInteractHintDicts[interactPoint] = hint;

            return hint;
        }

        private void RecycleInteractHintUI(ISceneInteractable interactPoint)
        {
            if (sceneInteractHintDicts.TryGetValue(interactPoint, out var hintItem))
            {
                hintItem.Unbind();
                hintItem.gameObject.SetActive(false);
                _hintPool.Enqueue(hintItem);
                sceneInteractHintDicts.Remove(interactPoint);
            }
        }

        private void UpdateInteractUIPosition(SceneInteractUIHinter hintItem, Vector3 worldPos)
        {
            var innerInteract = hintItem.sceneInteract;

            var hintPos = innerInteract.GetHintAnchorPosition();
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hintPos);

            // 如果是 Screen Space - Camera 或 World Space，用 RectTransformUtility：
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                UIManager.Instance.RootCanvas.transform as RectTransform,
                screenPos,
                UIManager.Instance.UICamera,   // Screen Space - Camera 用摄像机；Overlay 模式传 null
                out Vector2 localPos
            );
            hintItem.transform.localPosition = localPos;
        }

        #endregion


        #region NPC HStat UI 更新与按需分配

        protected void CheckUpdateSceneNpcHStat(IScenePresentation presenter)
        {
            // 强转判断是否为 NPC
            if (presenter is not SceneNpcPresenter npcPresenter) return;

            bool hasActiveUI = _activeNpcHStat.ContainsKey(npcPresenter.Id);

            bool isVisible;
            Vector3 screenPos = Vector3.zero;
            if (HuntingHudPanel.Instance == null || !HuntingHudPanel.Instance.IsHunterMode)
            {
                isVisible = false;
            }
            else
            {
                // 1. 判断是否在屏幕可视范围内（增加 -0.1 到 1.1 的缓冲防抖区域）
                screenPos = _mainCam.WorldToScreenPoint(npcPresenter.Pos);
                isVisible = screenPos.z > 0 &&
                screenPos.x >= -_bufferX && screenPos.x <= _screenWidth + _bufferX &&
                                 screenPos.y >= -_bufferY && screenPos.y <= _screenHeight + _bufferY;
            }

            

            if (isVisible)
            {
                SceneNPCHStatUIStruct uiItem;
                // 在屏幕内，如果没有分配UI，则从池中取
                if (!hasActiveUI)
                {
                    uiItem = AllocateNpcHStatUI(npcPresenter);
                }
                else
                {
                    uiItem = _activeNpcHStat[npcPresenter.Id];
                }

                // 2. 更新 UI 位置
                UpdateNpcHStatUIPosition(uiItem, screenPos);

                // 3. (可选) 在这里更新 UI 上的文本、血条进度等实时表现数据
                // uiItem.SJProgressText.text = "...";
            }
            else
            {
                // 3. 在屏幕外，如果占用了 UI，立刻回收
                if (hasActiveUI)
                {
                    RecycleNpcHStatUI(npcPresenter.Id);
                }
            }
        }

        private SceneNPCHStatUIStruct AllocateNpcHStatUI(SceneNpcPresenter npcPresenter)
        {
            SceneNPCHStatUIStruct uiItem;
            if (_npcHStatPool.Count > 0)
            {
                uiItem = _npcHStatPool.Dequeue();
            }
            else
            {
                uiItem = Instantiate(NPCHStatPrefab, transform);
            }

            uiItem.Bind(npcPresenter); // 调用外部组件的绑定方法
            _activeNpcHStat[npcPresenter.Id] = uiItem;
            return uiItem;
        }

        private void RecycleNpcHStatUI(long entityId)
        {
            if (_activeNpcHStat.TryGetValue(entityId, out var uiItem))
            {
                uiItem.Unbind(); // 内部解绑并隐藏
                _npcHStatPool.Enqueue(uiItem);
                _activeNpcHStat.Remove(entityId);
            }
        }

        private void UpdateNpcHStatUIPosition(SceneNPCHStatUIStruct uiItem, Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas.worldCamera, // 注意 Canvas 模式，如果是 Overlay 这里传 null
                out Vector2 uiLocalPos
            );

            // 根据需要调整高度偏移，避免和 Alert / Interact UI 重叠
            uiLocalPos += Vector2.up * 10f;
            uiItem.transform.localPosition = uiLocalPos;

            uiItem.SetFocusScale(GetNpcHStatFocusScale(uiItem.bindingNpc));
            uiItem.UpdateView();
        }

        float GetNpcHStatFocusScale(SceneNpcPresenter npcPresenter)
        {
            if (npcPresenter == null)
            {
                return 1f;
            }

            var hunt = HuntingModeManager.Instance;
            if (hunt == null || !hunt.Active)
            {
                return 1f;
            }

            if (hunt.PinnedNpc != null && hunt.PinnedNpc.Id == npcPresenter.Id)
            {
                return 1.4f;
            }

            if (hunt.HoverNpc != null && hunt.HoverNpc.Id == npcPresenter.Id)
            {
                return 1.25f;
            }

            return 1f;
        }

        #endregion

        #region Buff 头顶图标

        protected void CheckUpdateSceneUnitBuffHeadHint(IScenePresentation presenter)
        {
            if (BuffHeadHintPrefab == null)
            {
                return;
            }

            if (presenter is not SceneUnitPresenter unitPresenter)
            {
                return;
            }

            if (presenter.GetLogicEntity() is not IEntityBuffOwner buffOwner)
            {
                return;
            }

            _buffHeadHintSeenThisFrame.Add(unitPresenter.Id);

            var headBuff = BuffHeadHintUtil.ResolveTopHeadHintBuff(buffOwner);
            var hasActiveUI = _activeBuffHeadHints.ContainsKey(unitPresenter.Id);

            if (headBuff == null)
            {
                if (hasActiveUI)
                {
                    RecycleBuffHeadHintUI(unitPresenter.Id);
                }

                return;
            }

            var anchor = unitPresenter.PivotHeader != null
                ? unitPresenter.PivotHeader.position
                : unitPresenter.GetWorldPosition();
            var screenPos = _mainCam.WorldToScreenPoint(anchor);
            var isVisible = screenPos.z > 0 &&
                            screenPos.x >= -_bufferX && screenPos.x <= _screenWidth + _bufferX &&
                            screenPos.y >= -_bufferY && screenPos.y <= _screenHeight + _bufferY;

            if (!isVisible)
            {
                if (hasActiveUI)
                {
                    RecycleBuffHeadHintUI(unitPresenter.Id);
                }

                return;
            }

            var icon = BuffHeadHintUtil.ResolveBuffIcon(headBuff);
            SceneUnitBuffHeadHintItem uiItem;
            if (!hasActiveUI)
            {
                uiItem = AllocateBuffHeadHintUI(unitPresenter, icon, headBuff.InstanceId);
                if (uiItem == null)
                {
                    return;
                }
            }
            else
            {
                uiItem = _activeBuffHeadHints[unitPresenter.Id];
                if (uiItem.BoundBuffInstanceId != headBuff.InstanceId)
                {
                    uiItem.Bind(unitPresenter, icon, headBuff.InstanceId);
                }
                else
                {
                    uiItem.SetIcon(icon);
                }
            }

            UpdateBuffHeadHintUIPosition(uiItem, screenPos);
        }

        SceneUnitBuffHeadHintItem AllocateBuffHeadHintUI(
            SceneUnitPresenter unitPresenter,
            Sprite icon,
            long buffInstanceId)
        {
            if (BuffHeadHintPrefab == null)
            {
                return null;
            }

            SceneUnitBuffHeadHintItem uiItem;
            if (_buffHeadHintPool.Count > 0)
            {
                uiItem = _buffHeadHintPool.Dequeue();
            }
            else
            {
                uiItem = Instantiate(BuffHeadHintPrefab, transform);
            }

            uiItem.Bind(unitPresenter, icon, buffInstanceId);
            _activeBuffHeadHints[unitPresenter.Id] = uiItem;
            return uiItem;
        }

        void RecycleBuffHeadHintUI(long entityId)
        {
            if (_activeBuffHeadHints.TryGetValue(entityId, out var uiItem))
            {
                uiItem.Unbind();
                _buffHeadHintPool.Enqueue(uiItem);
                _activeBuffHeadHints.Remove(entityId);
            }
        }

        void RecycleStaleBuffHeadHints()
        {
            if (_activeBuffHeadHints.Count == 0)
            {
                return;
            }

            _lowFreqCleanCaches.Clear();
            foreach (var kv in _activeBuffHeadHints)
            {
                if (!_buffHeadHintSeenThisFrame.Contains(kv.Key))
                {
                    _lowFreqCleanCaches.Add(kv.Key);
                }
            }

            foreach (var entityId in _lowFreqCleanCaches)
            {
                RecycleBuffHeadHintUI(entityId);
            }
        }

        void UpdateBuffHeadHintUIPosition(SceneUnitBuffHeadHintItem uiItem, Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas != null ? TopCanvas.worldCamera : null,
                out Vector2 uiLocalPos
            );
            uiLocalPos += Vector2.up * BuffHeadHintScreenOffsetY;
            uiItem.transform.localPosition = uiLocalPos;
        }

        #endregion

        #region Unit HP Bar

        static bool TryReadHpBarStats(ILogicEntity entity, out long current, out long max)
        {
            current = 0;
            max = 0;
            if (entity == null)
            {
                return false;
            }

            max = entity.GetResourceMax(AttrIdConsts.HP);
            if (max <= 0)
            {
                max = entity.GetAttr(AttrIdConsts.HP_MAX);
            }

            if (max <= 0)
            {
                return false;
            }

            current = entity.GetAttr(AttrIdConsts.HP);
            if (entity is BaseUnitLogicEntity unit && (unit.IsDead || current <= 0))
            {
                current = 0;
            }

            return true;
        }

        static void ReadRawHp(ILogicEntity entity, out long hp, out long hpMax)
        {
            hp = entity != null ? entity.GetAttr(AttrIdConsts.HP) : 0;
            hpMax = entity != null ? entity.GetResourceMax(AttrIdConsts.HP) : 0;
            if (hpMax <= 0 && entity != null)
            {
                hpMax = entity.GetAttr(AttrIdConsts.HP_MAX);
            }
        }

        static bool DidLoseHealth(long hp, long hpMax, ref UnitHpBarTrackState track)
        {
            if (!track.Initialized)
            {
                return false;
            }

            return hpMax > 0 && hp < track.LastHp;
        }

        static Vector3 GetHpBarAnchor(IScenePresentation presenter)
        {
            if (presenter.PivotHeader != null)
            {
                return presenter.PivotHeader.position;
            }

            return presenter.GetWorldPosition();
        }

        static bool IsOnScreen(Vector3 screenPos, float screenWidth, float screenHeight, float bufferX, float bufferY)
        {
            return screenPos.z > 0
                   && screenPos.x >= -bufferX && screenPos.x <= screenWidth + bufferX
                   && screenPos.y >= -bufferY && screenPos.y <= screenHeight + bufferY;
        }

        void CheckUpdateSceneUnitHpBar(IScenePresentation presenter)
        {
            var entity = presenter?.GetLogicEntity();
            if (HpBarPrefab == null || entity == null || !TryReadHpBarStats(entity, out var current, out var max))
            {
                return;
            }

            if (MainGameManager.Instance?.gameLogicManager?.AreaManager?.BossEncounters?.IsBossEntity(entity.Id) == true)
            {
                if (_activeHpBars.ContainsKey(entity.Id))
                {
                    RecycleHpBarUI(entity.Id);
                }
                return;
            }

            ReadRawHp(entity, out var hp, out var hpMax);

            long entityId = presenter.Id;
            if (!_hpBarTracks.TryGetValue(entityId, out var track))
            {
                track = new UnitHpBarTrackState();
            }

            if (DidLoseHealth(hp, hpMax, ref track))
            {
                track.ShowUntil = LogicTime.time + hpBarShowDuration;
            }

            track.LastHp = hp;
            track.Initialized = true;
            _hpBarTracks[entityId] = track;

            bool shouldShow = LogicTime.time < track.ShowUntil;
            var anchor = GetHpBarAnchor(presenter);
            var screenPos = _mainCam.WorldToScreenPoint(anchor);
            bool onScreen = IsOnScreen(screenPos, _screenWidth, _screenHeight, _bufferX, _bufferY);

            if (!shouldShow || !onScreen)
            {
                if (_activeHpBars.ContainsKey(entityId))
                {
                    RecycleHpBarUI(entityId);
                }

                return;
            }

            if (!_activeHpBars.TryGetValue(entityId, out var uiItem))
            {
                uiItem = AllocateHpBarUI(presenter);
            }

            uiItem.SetFill(current, max);
            UpdateHpBarUIPosition(uiItem, screenPos);
        }

        SceneUnitHpBarItem AllocateHpBarUI(IScenePresentation presenter)
        {
            SceneUnitHpBarItem uiItem;
            if (_hpBarPool.Count > 0)
            {
                uiItem = _hpBarPool.Dequeue();
            }
            else
            {
                uiItem = Instantiate(HpBarPrefab, transform);
            }

            uiItem.Bind(presenter);
            _activeHpBars[presenter.Id] = uiItem;
            return uiItem;
        }

        void RecycleHpBarUI(long entityId)
        {
            if (!_activeHpBars.TryGetValue(entityId, out var uiItem))
            {
                return;
            }

            uiItem.Unbind();
            _hpBarPool.Enqueue(uiItem);
            _activeHpBars.Remove(entityId);
        }

        void UpdateHpBarUIPosition(SceneUnitHpBarItem uiItem, Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas != null ? TopCanvas.worldCamera : null,
                out Vector2 uiLocalPos);
            uiLocalPos += Vector2.up * hpBarScreenOffsetY;
            uiItem.transform.localPosition = uiLocalPos;
        }

        #endregion

        #region Charge Bar UI

        void CheckUpdateSceneUnitChargeBar(IScenePresentation presenter)
        {
            if (ChargeBarPrefab == null)
            {
                return;
            }

            long entityId = presenter.Id;
            var entity = presenter.GetLogicEntity() as BaseUnitLogicEntity;
            if (entity?.ablilityManager == null)
            {
                RecycleChargeBarUI(entityId);
                return;
            }

            if (!entity.ablilityManager.TryGetActiveHoldViewState(out var holdState) || !holdState.IsActive)
            {
                RecycleChargeBarUI(entityId);
                return;
            }

            var anchor = presenter.GetWorldPosition();
            var screenPos = _mainCam.WorldToScreenPoint(anchor);
            if (!IsOnScreen(screenPos, _screenWidth, _screenHeight, _bufferX, _bufferY))
            {
                RecycleChargeBarUI(entityId);
                return;
            }

            if (!_activeChargeBars.TryGetValue(entityId, out var uiItem))
            {
                uiItem = AllocateChargeBarUI(presenter);
            }

            uiItem.SetFill(holdState.Progress01);
            UpdateChargeBarUIPosition(uiItem, screenPos);
        }

        SceneUnitChargeBarItem AllocateChargeBarUI(IScenePresentation presenter)
        {
            SceneUnitChargeBarItem uiItem;
            if (_chargeBarPool.Count > 0)
            {
                uiItem = _chargeBarPool.Dequeue();
            }
            else
            {
                uiItem = Instantiate(ChargeBarPrefab, transform);
            }

            uiItem.Bind(presenter);
            _activeChargeBars[presenter.Id] = uiItem;
            return uiItem;
        }

        void RecycleChargeBarUI(long entityId)
        {
            if (!_activeChargeBars.TryGetValue(entityId, out var uiItem))
            {
                return;
            }

            uiItem.Unbind();
            _chargeBarPool.Enqueue(uiItem);
            _activeChargeBars.Remove(entityId);
        }

        void UpdateChargeBarUIPosition(SceneUnitChargeBarItem uiItem, Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                TopCanvas != null ? TopCanvas.worldCamera : null,
                out Vector2 uiLocalPos);
            uiLocalPos += Vector2.up * chargeBarScreenOffsetY;
            uiItem.transform.localPosition = uiLocalPos;
        }

        #endregion

        /// <summary>
        /// 强制解绑
        /// </summary>
        /// <param name="scenePresentation"></param>
        public void OnScenePresentationUbbind(IScenePresentation scenePresentation)
        {
            if (scenePresentation is ISceneInteractable interactable)
            {
                RecycleInteractHintUI(interactable);
            }

            RecycleEvilAlertUI(scenePresentation.Id);

            // 补充 HStat 的解绑回收
            RecycleNpcHStatUI(scenePresentation.Id);
            RecycleBuffHeadHintUI(scenePresentation.Id);
            RecycleHpBarUI(scenePresentation.Id);
            _hpBarTracks.Remove(scenePresentation.Id);
            RecycleChargeBarUI(scenePresentation.Id);
        }

        public override void Hide()
        {
            ClearAllSceneSmallIcons();
            _bindingsSuspended = true;
            base.Hide();
        }

        public override void Teardown()
        {
            ClearAllSceneSmallIcons();
            _bindingsSuspended = true;
            base.Teardown();
        }
    }

}
