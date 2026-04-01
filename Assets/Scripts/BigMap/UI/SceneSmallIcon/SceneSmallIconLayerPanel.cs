
using System;
using System.Collections.Generic;
using System.Linq;
using My.Map;
using My.Map.Entity;
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


        private Dictionary<ISceneInteractable, SceneInteractUIHinter> sceneInteractHintDicts = new();
        private Queue<SceneInteractUIHinter> _hintPool = new();


        private Dictionary<long, SceneEvilAlertUIItem> _activeEvilAlerts = new Dictionary<long, SceneEvilAlertUIItem>();
        private Queue<SceneEvilAlertUIItem> _evilAlertPool = new Queue<SceneEvilAlertUIItem>();


        private Dictionary<long, SceneNPCHStatUIStruct> _activeNpcHStat = new Dictionary<long, SceneNPCHStatUIStruct>();
        private Queue<SceneNPCHStatUIStruct> _npcHStatPool = new Queue<SceneNPCHStatUIStruct>();

        public void Awake()
        {
            InteractHintPrefab.gameObject.SetActive(false);
            EvilAlertPrefab.gameObject.SetActive(false);
            if (NPCHStatPrefab != null) NPCHStatPrefab.gameObject.SetActive(false);

            TopCanvas = GetComponentInParent<Canvas>();
            _mainCam = Camera.main;
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
            
        }

        private float _screenWidth;
        private float _screenHeight;
        private float _bufferX;
        private float _bufferY;


        protected void UpdateSceneSmallIconBind()
        {
            // 缓存屏幕尺寸和 10% 的防抖缓冲
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _bufferX = _screenWidth * 0.1f;
            _bufferY = _screenHeight * 0.1f;

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
            }
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

            bool hasActiveUI = sceneInteractHintDicts.ContainsKey(interactblePresenter);
            Vector3 worldPos = Vector3.zero;
            bool isVisible = false;
            do
            {
                if(interactblePresenter.CanInteractEnable())
                {
                    break;
                }

                if(OverworldHUDPanel.Instance != null && OverworldHUDPanel.Instance.IsHunterMode)
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
            if (OverworldHUDPanel.Instance == null || !OverworldHUDPanel.Instance.IsHunterMode)
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

            uiItem.UpdateView();
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
        }

        public override void Hide()
        {
            base.Hide();

            foreach(var key in sceneInteractHintDicts.Keys.ToList())
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

            DebugIconsShower.Clear();
        }
    }

}
