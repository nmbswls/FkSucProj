using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.Rendering.DebugManager;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using My.Input;

namespace My.UI
{

    public class UIManager : MonoBehaviour, IUiRouter
    {
        public static UIManager Instance { get; private set; }

        [Header("Roots")]
        [SerializeField] public Canvas RootCanvas;
        public Camera UICamera;
        [SerializeField] private Transform sceneLayerRoot;
        [SerializeField] private Transform hudLayerRoot;
        [SerializeField] private Transform popupLayerRoot;
        [SerializeField] private Transform overlayLayerRoot;
        [SerializeField] private Transform systemLayerRoot;
        [SerializeField] private EventSystem eventSystem;

        [Header("Resources Catalog")]
        [SerializeField] private List<PanelResource> panelCatalog;
        [SerializeField] private string loadingPanelId = "LoadingOverlay";

        [Header("Input (New Input System) - Optional")]
        [SerializeField] private InputActionAsset inputAsset;
        

        [Header("Debug")]
        [SerializeField] private bool logConsumption = false;

        // 运行时
        private readonly Dictionary<string, PanelResource> catalogMap = new();
        private readonly Dictionary<string, IPanel> activePanels = new();
        private readonly Dictionary<string, PanelPool> pools = new();
        private readonly SortedDictionary<int, List<IPanel>> layerPanels = new();


        // 输入缓存
        private InputActionMap mapOverworld, mapBattle, mapUI;
        private InputAction uiConfirm, uiCancel, uiNavigate;
        private readonly List<(InputAction action, Action<InputAction.CallbackContext> handler)> handlers = new();


        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            UIResigter.RegisterPanels();

            foreach (var pr in panelCatalog) catalogMap[pr.panelId] = pr;
            if (!eventSystem) eventSystem = FindFirstObjectByType<EventSystem>();

            InitPools();
        }

        void OnDestroy()
        {
            UnhookInput();
            foreach (var kv in activePanels) kv.Value?.Teardown();
            activePanels.Clear();
            layerPanels.Clear();
            pools.Clear();
        }

        public void RegisterPanel(PanelResource rsc)
        {

        }

        private void InitPools()
        {
            foreach (var pr in panelCatalog)
            {
                if (pr.pooled && pr.poolSize > 0)
                {
                    pools[pr.panelId] = new PanelPool { parent = GetLayerRoot(pr.defaultLayer) };
                    for (int i = 0; i < pr.poolSize; i++)
                    {
                        var inst = InstantiatePanel(pr, pr.defaultLayer);
                        (inst as PanelBase)?.Hide();
                        pools[pr.panelId].pool.Enqueue(inst);
                    }
                }
            }
        }

        private Transform GetLayerRoot(UILayer layer)
        {
            return layer switch
            {
                UILayer.Scene => hudLayerRoot,
                UILayer.HUD => hudLayerRoot,
                UILayer.Popup => popupLayerRoot,
                UILayer.Overlay => overlayLayerRoot,
                UILayer.System => systemLayerRoot,
                _ => hudLayerRoot
            };
        }

        private void UnhookInput()
        {
            foreach (var h in handlers)
            {
                if (h.action != null && h.handler != null)
                {
                    h.action.performed -= h.handler;
                }
            }
            handlers.Clear();
        }

        

        // 输入冒泡分发
        private bool TryConsumeByLayers(Func<IInputConsumer, bool> call)
        {
            for (int layer = (int)UILayer.System; layer >= (int)UILayer.HUD; layer--)
            {
                if (!layerPanels.TryGetValue(layer, out var list) || list.Count == 0) continue;
                list.Sort((a, b) => GetPriority(b).CompareTo(GetPriority(a)));
                foreach (var p in list)
                {
                    if (!p.IsVisible) continue;
                    if (p is IFocusable f && !f.CanFocus) continue;
                    if (p is IInputConsumer c)
                    {
                        bool consumed = false;
                        try { consumed = call(c); }
                        catch (Exception e) { Debug.LogError($"Input error on {p.PanelId}: {e}"); }
                        if (consumed)
                        {
                            if (logConsumption) Debug.Log($"[UI Input] consumed by {p.PanelId} (layer {layer})");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private int GetPriority(IPanel p) => (p is IFocusable f) ? f.FocusPriority : 0;

        // 面板管理
        public IPanel ShowPanel(string panelId, object data = null, UILayer? layerOverride = null)
        {
            if (activePanels.TryGetValue(panelId, out var existing))
            {
                existing.Setup(data);
                existing.Show();
                return existing;
            }
            if (!catalogMap.TryGetValue(panelId, out var res))
            {
                Debug.LogError($"Panel {panelId} not in catalog");
                return null;
            }

            var layer = layerOverride ?? res.defaultLayer;
            var panel = GetOrCreatePanel(res, layer);
            if (panel == null) return null;

            panel.Setup(data);
            panel.Show();
            activePanels[panelId] = panel;
            RegisterPanel(panel);
            return panel;
        }

        public void HidePanel(string panelId, bool recycleIfPooled = true)
        {
            if (!activePanels.TryGetValue(panelId, out var panel)) return;
            panel.Hide();
            panel.Teardown();
            activePanels.Remove(panelId);
            UnregisterPanel(panel);

            if (catalogMap.TryGetValue(panelId, out var res) && res.pooled && recycleIfPooled)
            {
                if (!pools.TryGetValue(panelId, out var pool)) pools[panelId] = new PanelPool { parent = GetLayerRoot(res.defaultLayer) };
                pools[panelId].pool.Enqueue(panel);
            }
            else
            {
                var mb = panel as MonoBehaviour;
                if (mb) GameObject.Destroy(mb.gameObject);
            }
        }

        public bool IsPanelVisible(string panelId) => activePanels.TryGetValue(panelId, out var p) && p.IsVisible;

        public IPanel GetShowingPanel(string panelId)
        {
            activePanels.TryGetValue(panelId, out var p);
            return p;
        }  

        private void RegisterPanel(IPanel panel)
        {
            if (!layerPanels.TryGetValue(panel.Layer, out var list))
            {
                list = new List<IPanel>();
                layerPanels[panel.Layer] = list;
            }
            list.Add(panel);
        }

        private void UnregisterPanel(IPanel panel)
        {
            if (layerPanels.TryGetValue(panel.Layer, out var list)) list.Remove(panel);
        }



        private IPanel GetOrCreatePanel(PanelResource res, UILayer layer)
        {
            if (res.pooled && pools.TryGetValue(res.panelId, out var pool) && pool.pool.Count > 0)
            {
                var p = pool.pool.Dequeue();
                var mb = p as MonoBehaviour;
                mb.transform.SetParent(GetLayerRoot(layer), false);
                return p;
            }
            return InstantiatePanel(res, layer);
        }

        private IPanel InstantiatePanel(PanelResource res, UILayer layer)
        {
            var prefab = Resources.Load<GameObject>(res.resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Resources.Load failed: {res.resourcePath}");
                return null;
            }
            var parent = GetLayerRoot(layer);
            var go = GameObject.Instantiate(prefab, parent, false);
            var panel = go.GetComponent<IPanel>();
            if (panel == null)
            {
                Debug.LogError($"Prefab {res.resourcePath} missing IPanel component");
                return null;
            }
            (panel as PanelBase)?.Hide(); // 初始隐藏
            return panel;
        }

        // 刷新
        public void RequestRefresh(InputMode mode)
        {
            foreach (var kv in activePanels)
            {
                if (kv.Value.IsVisible && kv.Value is IRefreshable r) r.Refresh();
            }
        }

        // Loading 快捷
        public void ShowLoading(string text = "Loading...") { ShowPanel(loadingPanelId, text, UILayer.System); }
        public void HideLoading() { HidePanel(loadingPanelId); }

        public bool DispatchConfirm()
        {
            if (TryConsumeByLayers(c => c.OnConfirm())) return true;
            return false;
        }

        public bool DispatchCancel()
        {
            if (TryConsumeByLayers(c => c.OnCancel())) return true;
            return false;
        }

        public bool DispatchNavigate(Vector2 dir)
        {
            if (TryConsumeByLayers(c => c.OnNavigate(dir))) return true;
            return false;
        }

        public bool DispatchScroll(float deltaY)
        {
            if (TryConsumeByLayers(c => c.OnScroll(deltaY))) return true;
            return false;
        }

        public bool DispatchHotkey(int index) 
        { 
            if (TryConsumeByLayers(c => c.OnHotkey(index))) return true;
            return false;
        }
    }
}