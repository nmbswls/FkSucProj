using Map.Logic.Events;
using My.Player.Bag;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static My.Input.QuickPlayerInputBinder;
using static UnityEngine.Rendering.DebugUI;

namespace My.UI
{

    public enum UIAppState
    {
        Boot,
        Overworld,
        Battle,
        PauseMenu,
        Dialog,
        Loading
    }


    public class UIRegister
    { 
        public static void RegisterPanels()
        {
            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "OverworldHUD",
                resourcePath = "UI/Prefabs/OverworldHUD",
                defaultLayer = UILayer.HUD,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "LoadingOverlay",
                resourcePath = "UI/Prefabs/LoadingOverlay",
                defaultLayer = UILayer.System,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "PlayerBag",
                resourcePath = "UI/Prefabs/PlayerBag",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "LootPoint",
                resourcePath = "UI/Prefabs/LootPoint",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "SceneMask",
                resourcePath = "UI/Prefabs/SceneMask",
                defaultLayer = UILayer.Scene,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "InteractMenu",
                resourcePath = "UI/Prefabs/InteractMenu",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "SmallIconLayer",
                resourcePath = "UI/Prefabs/SmallIconLayer",
                defaultLayer = UILayer.Scene,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "ItemDragDrop",
                resourcePath = "UI/Prefabs/ItemDragDrop",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });
            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "ItemPopup",
                resourcePath = "UI/Prefabs/ItemPopup",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "DeepZhaQuMiniGame",
                resourcePath = "UI/Prefabs/DeepZhaQuMiniGame",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "BeFckedWindow",
                resourcePath = "UI/Prefabs/BeFckedWindow",
                defaultLayer = UILayer.Overlay,
                pooled = true,
            });

        }

        public static void RegisterGroups()
        {
            {
                var bagPolicy = new UIOrchestrator.UIGroupPolicy()
                {
                    groupName = "bag",
                    singleInGroup = false,
                    panelIds = new() { "PlayerBag" },
                    isExclusive = false,
                };
                UIOrchestrator.Instance.AddGroupPolicy(bagPolicy);
            }

            {
                var lootPolicy = new UIOrchestrator.UIGroupPolicy()
                {
                    groupName = "looting",
                    singleInGroup = false,
                    panelIds = new() { "Looting"},
                    isExclusive = true,
                };
                UIOrchestrator.Instance.AddGroupPolicy(lootPolicy);
            }
        }
    }
    

    public class UIOrchestrator : MonoBehaviour
    {
        public static UIOrchestrator Instance { get; private set; }

        [SerializeField] private UIAppState current = UIAppState.Boot;
        private UIAppState previous;


        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            UIRegister.RegisterGroups();
        }

        void Start()
        {
            BuildGroupIndex();
        }

        #region 互斥组处理

        [Serializable]
        public class UIGroupPolicy
        {
            public string groupName;                // 组名，如 "Interaction", "Inventory", "Fullscreen"
            public bool singleInGroup = false;      // 是否同组内互斥（true=同组只能留一个）
            public List<string> panelIds = new();   // 组内面板清单
            public bool isExclusive = false;
        }

        [Header("UI Groups (Mutual Exclusion)")]
        [SerializeField] private List<UIGroupPolicy> groupPolicies = new();

        // 运行时索引
        private readonly Dictionary<string, string> panelToGroup = new();     // panelId -> groupName
        private readonly Dictionary<string, HashSet<string>> groupActive = new(); // groupName -> active panelIds

        public void AddGroupPolicy(UIGroupPolicy policy)
        {
            groupPolicies.Add(policy);
        }

        private void BuildGroupIndex()
        {
            panelToGroup.Clear();
            groupActive.Clear();
            foreach (var gp in groupPolicies)
            {
                if (string.IsNullOrEmpty(gp.groupName)) continue;
                if (!groupActive.ContainsKey(gp.groupName))
                    groupActive[gp.groupName] = new HashSet<string>();
                foreach (var pid in gp.panelIds)
                {
                    if (!string.IsNullOrEmpty(pid))
                        panelToGroup[pid] = gp.groupName;
                }
            }
        }

        private UIGroupPolicy FindGroupByName(string groupName)
        {
            return groupPolicies.Find(g => g.groupName == groupName);
        }

        private UIGroupPolicy FindGroupByPanel(string panelId, out string groupName)
        {
            groupName = null;
            if (panelToGroup.TryGetValue(panelId, out var g))
            {
                groupName = g;
                return FindGroupByName(g);
            }
            return null;
        }

        // 对外：按组规则显示面板（互斥组生效）
        public void ShowInGroup(string panelId, object ctx = null, UILayer? layerOverride = null)
        {
            // 查找面板所属组
            var policy = FindGroupByPanel(panelId, out var myGroup);
            if (policy != null && !string.IsNullOrEmpty(myGroup))
            {
                if(policy.isExclusive)
                {
                    // 1) 关闭其他组所有活动面板
                    foreach (var kv in groupActive)
                    {
                        var groupName = kv.Key;
                        if (groupName == myGroup) continue;
                        // 复制集合避免遍历修改
                        var toClose = new List<string>(kv.Value);
                        foreach (var pid in toClose)
                        {
                            UIManager.Instance.HidePanel(pid);
                            kv.Value.Remove(pid);
                        }
                    }
                }
                
                // 2) 若同组单例，关闭同组已打开面板
                if (policy.singleInGroup && groupActive.TryGetValue(myGroup, out var setInMyGroup))
                {
                    var toCloseMy = new List<string>(setInMyGroup);
                    foreach (var pid in toCloseMy)
                    {
                        if (pid != panelId)
                        {
                            UIManager.Instance.HidePanel(pid);
                            setInMyGroup.Remove(pid);
                        }
                    }
                }
            }

            // 3) 打开目标面板
            UIManager.Instance.ShowPanel(panelId, ctx, layerOverride);

            // 4) 登记活动关系
            if (!string.IsNullOrEmpty(myGroup))
            {
                if (!groupActive.TryGetValue(myGroup, out var set))
                    groupActive[myGroup] = set = new HashSet<string>();
                set.Add(panelId);
            }
        }

        // 对外：按组规则关闭面板（更新索引）
        public void HideInGroup(string panelId)
        {
            UIManager.Instance.HidePanel(panelId);
            if (panelToGroup.TryGetValue(panelId, out var g) && groupActive.TryGetValue(g, out var set))
            {
                set.Remove(panelId);
            }
        }

        // 辅助：关闭整个组（可用于“回到世界”时清空交互相关）
        public void CloseGroup(string groupName)
        {
            if (!groupActive.TryGetValue(groupName, out var set)) return;
            var toClose = new List<string>(set);
            foreach (var pid in toClose)
            {
                UIManager.Instance.HidePanel(pid);
                set.Remove(pid);
            }
        }

        // 查询：某组是否有活动面板
        public bool IsGroupActive(string groupName)
        {
            return groupActive.TryGetValue(groupName, out var set) && set.Count > 0;
        }

        #endregion


        public async Task SetStateAsync(UIAppState next, object ctx = null)
        {
            if (current == next) return;
            previous = current;
            current = next;

            switch (next)
            {
                case UIAppState.Overworld:
                    await EnterOverworldAsync(ctx);
                    break;
                //case UIAppState.Battle:
                //    await EnterBattleAsync(ctx);
                //    break;
                //case UIAppState.PauseMenu:
                //    await EnterPauseAsync();
                //    break;
                //case UIAppState.Dialog:
                //    await EnterDialogAsync(ctx);
                //    break;
            }
        }

        private async Task EnterOverworldAsync(object ctx)
        {
            // 关闭战斗相关
            UIManager.Instance.ShowPanel("OverworldHUD");
            UIManager.Instance.ShowPanel("SceneMask");
            UIManager.Instance.ShowPanel("SmallIconLayer");
            UIManager.Instance.ShowPanel("InteractMenu");
            
            MainGameManager.Instance.inputBinder.ApplyInputMode(InputMode.Overworld);
            await Task.CompletedTask;
        }

        //private async Task EnterBattleAsync(object ctx)
        //{
        //    UIManager.Instance.ShowLoading("Entering Battle...");
        //    // 关闭世界 HUD
        //    UIManager.Instance.HidePanel("OverworldHUD");
        //    // 打开战斗 HUD
        //    UIManager.Instance.ShowPanel("BattleHUD", ctx, UILayer.HUD);
        //    UIManager.Instance.ApplyInputMode(UIInputMode.Battle);
        //    UIManager.Instance.HideLoading();
        //    await Task.CompletedTask;
        //}

        //private async Task EnterPauseAsync()
        //{
        //    // 打开暂停菜单（示例，你可以做一个 PauseMenuPanel，放在 Popup/Overlay 层）
        //    UIManager.Instance.ShowPanel("PauseMenu", null, UILayer.Overlay);
        //    // 菜单态切 UI 输入
        //    UIManager.Instance.ApplyInputMode(UIInputMode.Menu);
        //    await Task.CompletedTask;
        //}

        //private async Task EnterDialogAsync(object dialogCtx)
        //{
        //    // 打开对话面板（示例）
        //    UIManager.Instance.ShowPanel("DialogPanel", dialogCtx, UILayer.Popup);
        //    UIManager.Instance.ApplyInputMode(UIInputMode.Dialog);
        //    await Task.CompletedTask;
        //}

        //private async Task EnterLoadingAsync(string tip)
        //{
        //    UIManager.Instance.ShowLoading(tip);
        //    // Loading 状态通常只是展示遮罩，吞掉输入；结束时由调用方再跳到下一个状态
        //    await Task.CompletedTask;
        //}

        //// 常用编排：从任意状态进入战斗
        //public async Task GoToBattleAsync(object battleCtx)
        //{
        //    await SetStateAsync(UIAppState.Loading, "Matchmaking...");
        //    // 资源加载/场景切换...
        //    await SetStateAsync(UIAppState.Battle, battleCtx);
        //}


        #region 具体逻辑部分

        public void TryEnterLootDetailMode(ILootableObj lootObj)
        {
            // 打开lootpoint
            ShowInGroup("LootPoint", lootObj);
            ShowInGroup("PlayerBag");
            ShowInGroup("PlayerBag");


            //if(UIManager.Instance.IsPanelVisible())
            {

            }
            //ItemPopupMenu.Instance.Close();
        }


        #endregion


        private MapLogicEventAdapter adapter;
        private List<MapLogicSubscription> subs = new();
        /// <summary>
        /// 逻辑事件处理
        /// </summary>
        public void InitGameLogicEventListener()
        {
            if(adapter == null)
            {
                adapter = new(OnMapLogicEvent);
            }

            if (subs.Count > 0)
            {
                foreach(var sub in subs)
                {
                    MainGameManager.Instance.gameLogicManager.LogicEventBus.Unsubscribe(sub);
                }
                subs.Clear();
            }

            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.Common, adapter));
            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.OnHit, adapter));
            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.AddBuff, adapter));
        }

        public void OnMapLogicEvent(IMapLogicEvent ev)
        {
            switch (ev.Type)
            {
                case EMapLogicEventType.AddBuff:
                    {
                        var addBuffEv = (MLEApplyBuff)ev;
                        if(addBuffEv.BuffId == "be_fcked")
                        {
                            BeFckedWindowPanel.ShowFckedWindow(addBuffEv.CasterId, 100);
                        }
                    }
                    break;
            }
        }
    }
}




//public class UIOrchestrator : MonoBehaviour
//{

//    public enum UITopMode { Overworld, Battle, Menu, Dialog }


//    public static UIOrchestrator Instance;
//    public Camera UICamera;
//    public Canvas RootCanvas;

//    public Transform HintFloatingPanel;

//    public UISceneInteractMenu SceneInteractMenu;

//    private void Awake()
//    {
//        Instance = this;

//        InteractHintPrefab.SetActive(false);
//        //InventoryUICtrl.gameObject.SetActive(false);
//        //LootUICtrl.gameObject.SetActive(false);
//    }

//    private void Update()
//    {
//        if(Input.GetKeyDown(KeyCode.B))
//        {
//            if(!InventoryUICtrl.gameObject.activeSelf)
//            {
//                InventoryUICtrl.InitilaizeView();
//                InventoryUICtrl.gameObject.SetActive(true);
//            }
//        }
//    }

//    private void LateUpdate()
//    {
//        //OnRefreshInteractInfo();
//    }

//    public void SetTopMode(UITopMode mode)
//    {
//        if (currentMode == mode) return;
//        currentMode = mode;

//        // 切换 Map
//        mapOverworld?.Disable();
//        mapBattle?.Disable();
//        mapUI?.Disable();

//        switch (currentMode)
//        {
//            case UIMode.Overworld: mapOverworld?.Enable(); break;
//            case UIMode.Battle: mapBattle?.Enable(); break;
//            case UIMode.Menu:
//            case UIMode.Dialog: mapUI?.Enable(); break;
//        }

//        RequestRefresh(mode);
//    }


//    // 便捷流程：战斗切换 + Loading
//    public void EnterBattleUI(object battleCtx)
//    {

//        ShowPanel(loadingPanelId, "Entering Battle...", UILayer.System);
//        SetMode(UIMode.Battle);
//        HidePanel("OverworldHUD");
//        ShowPanel("BattleHUD", battleCtx, UILayer.HUD);
//        HidePanel(loadingPanelId);
//    }

//    public void ExitBattleUI()
//    {
//        ShowPanel(loadingPanelId, "Leaving Battle...", UILayer.System);
//        HidePanel("BattleHUD");
//        SetMode(UIMode.Overworld);
//        ShowPanel("OverworldHUD", null, UILayer.HUD);
//        HidePanel(loadingPanelId);
//    }

//    public void ShowLoading(string text = "Loading...") { ShowPanel(loadingPanelId, text, UILayer.System); }
//    public void HideLoading() { HidePanel(loadingPanelId); }

//    public void InitGameLogicEventListener()
//    {
//        MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(new CommonGameEventAdapter((ev) =>
//        {
//            switch(ev.Name)
//            {
//                case "Death":
//                    {
//                        var guy = ev.Param3;
//                        var presenter = SceneAOIManager.Instance.GetActivePresentation(guy);
//                        if (presenter != null)
//                        {
//                            FakeHintTextManager.ShowWorld("imdead", presenter.GetWorldPosition(), Camera.main);
//                        }
//                    }
//                    break;

//            }

//        }));
//    }

    
//    public GameObject InteractHintPrefab;
//    private Dictionary<long, SceneInteractUIHinter> sceneInteractHintDicts = new(0);
//    private Queue<SceneInteractUIHinter> _hintPool = new();

//    public void OnScenePresentationBinded(IScenePresentation scenePresentation)
//    {
//        if(scenePresentation is ISceneInteractable interactPoint)
//        {
//            SceneInteractUIHinter hint = null;
//            if(_hintPool.Count > 0)
//            {
//                hint = _hintPool.Dequeue();
//            }
//            else
//            {
//                var newHintGo = GameObject.Instantiate(InteractHintPrefab, HintFloatingPanel);
//                hint = newHintGo.GetComponent<SceneInteractUIHinter>();
//            }
//            hint.InitBind(interactPoint);

//            hint.BindInteractPoint = interactPoint;
//            hint.gameObject.SetActive(true);
//            sceneInteractHintDicts[interactPoint.Id] = hint;

//            hint.transform.position = scenePresentation.GetWorldPosition();
//            hint.transform.localPosition = new Vector3(hint.transform.localPosition.x, hint.transform.localPosition.y, 0);
//        }
//    }

//    public void OnScenePresentationUbbind(IScenePresentation scenePresentation)
//    {
//        if (scenePresentation is ISceneInteractable interactPoint)
//        {
//            sceneInteractHintDicts.TryGetValue(scenePresentation.Id, out var hintItem);
//            if(hintItem != null)
//            {
//                hintItem.Clear();
//                hintItem.gameObject.SetActive(false);
//                sceneInteractHintDicts.Remove(scenePresentation.Id);

//                if(_hintPool.Count < 10)
//                {
//                    _hintPool.Enqueue(hintItem);
//                }
//                else
//                {
//                    GameObject.Destroy(hintItem.gameObject);
//                }
//            }
//        }
//    }


//    public BottomProgressUICtrl ProgressUICtrl;

//    public long ShowBottomProgress(string hintText, float progressTime)
//    {
//        return ProgressUICtrl.InitProgressInfo(hintText, progressTime);
//    }

//    public void TryCancelProgressComplete(long showId)
//    {
//        ProgressUICtrl.TryCancelProgressComplete(showId);
//    }

//    public InventoryUIController InventoryUICtrl;
//    public LootPointUIController LootUICtrl;

//    public bool IsLootingMode;



//    public void TryQuitLootDetailMode()
//    {
//        if (IsLootingMode)
//        {
//            IsLootingMode = false;

//            LootUICtrl.gameObject.SetActive(false);
//            InventoryUICtrl.gameObject.SetActive(false);

//            ItemPopupMenu.Instance.Close();
//        }
//    }
//}
