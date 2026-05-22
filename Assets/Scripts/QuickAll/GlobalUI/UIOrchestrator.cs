using Map.Logic.Events;
using My.Config;
using My.Map;
using My.Map.View;
using My.MiniGame;
using My.MiniGame.Dream;
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
                panelId = "StartupMenuPanel",
                resourcePath = "UI/Prefabs/Startup/StartupManuPanel",
                defaultLayer = UILayer.HUD,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "SavePointPanel",
                resourcePath = "UI/Prefabs/SavePointPanel",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });
            


            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "OverworldHUD",
                resourcePath = "UI/Prefabs/OverworldHUD",
                defaultLayer = UILayer.HUD,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PlayerHeadThrowQteHud.PanelIdConst,
                resourcePath = "UI/Prefabs/PlayerHeadQteHint",
                defaultLayer = UILayer.HUD,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PlayerHumanItemBarPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/PlayerHumanItemBarPanel",
                defaultLayer = UILayer.HUD,
                pooled = false,
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
                panelId = "WarehousePanel",
                resourcePath = "UI/Prefabs/WarehousePanel",
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
                panelId = "FishingMiniGamePanel",
                resourcePath = "UI/Prefabs/FishingMiniGamePanel",
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
                defaultLayer = UILayer.Overlay,
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
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "BeFckedWindow",
                resourcePath = "UI/Prefabs/BeFckedWindow",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "DialoguePanel",
                resourcePath = "UI/Prefabs/DialoguePanel",
                defaultLayer = UILayer.Overlay,
                pooled = true,
            });


            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "EncounterBattleHud",
                resourcePath = "UI/Prefabs/Battle/EncounterBattleHud",
                defaultLayer = UILayer.HUD,
                pooled = false,
            });


            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "ShopNormalPanel",
                resourcePath = "UI/Prefabs/ShopNormalPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "ItemCountChooseBox",
                resourcePath = "UI/Prefabs/Box/ItemCountChoose",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "YesNoMsgBox",
                resourcePath = "UI/Prefabs/Box/YesNoMsg",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PauseCloseupHTangleWindow.ID,
                resourcePath = "UI/Prefabs/PauseCloseupHTangleWindow",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PauseCloseupKaiYouWindow.ID,
                resourcePath = "UI/Prefabs/PauseCloseupKaiYouWindow",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PauseCloseupWindow.ID,
                resourcePath = "UI/Prefabs/PauseCloseupWindow",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "MapPlayerRadialMenu",
                resourcePath = "UI/Prefabs/MapPlayerRadialMenu",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = WorldMapRuntime.PanelId,
                resourcePath = "UI/Prefabs/WorldMapPanel",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = DreamInfiltrationIds.EntryPanel,
                resourcePath = "UI/Prefabs/DreamInfiltration/DreamEntryPanel",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });
            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = DreamInfiltrationIds.GameplayPanel,
                resourcePath = "UI/Prefabs/DreamInfiltration/DreamDodgeGameplayPanel",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });
            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = DreamInfiltrationIds.SettlementPanel,
                resourcePath = "UI/Prefabs/DreamInfiltration/DreamSettlementPanel",
                defaultLayer = UILayer.Overlay,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = UIBedroomDeployPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/UIBedroomDeploy",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = RumorIntelShopPanel.Pid,
                resourcePath = "UI/Prefabs/RumorIntelShopPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "MiniStaticAbsorbPanel",
                resourcePath = "UI/Prefabs/MiniStaticAbsorbPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "GainItemSideNtfPanel",
                resourcePath = "UI/Prefabs/Common/GainItemSideNtfPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });
            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "UIGainRewardCoordinator",
                resourcePath = "UI/Prefabs/Common/UIGainRewardCoordinator",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "BigMapFinishPanel",
                resourcePath = "UI/Prefabs/BigMapFinishPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "AmbientChatPanel",
                resourcePath = "UI/Prefabs/AmbientChatPanel",
                defaultLayer = UILayer.Scene,
                pooled = true,
            });


            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "QuestFloatingPanel",
                resourcePath = "UI/Prefabs/QuestFloatingPanel",
                defaultLayer = UILayer.HUD,
                pooled = false,
            });


            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = "RepairDetailPanel",
                resourcePath = "UI/Prefabs/RepairDetailRequirePanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = MapTownManagementPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/MapTownManagementPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = My.UI.SecretBaseHudPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/SecretBaseHudPanel",
                defaultLayer = UILayer.HUD,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = My.UI.SecretBaseBuildPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/SecretBaseBuildPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = My.UI.SecretBaseNpcHubPanel.PanelIdConst,
                resourcePath = "UI/Prefabs/SecretBaseNpcHubPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = Forge.ForgePanel.Pid,
                resourcePath = "UI/Prefabs/ForgePanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });

            UIManager.Instance.RegisterPanel(new PanelResource()
            {
                panelId = PlayerProgressionHubPanel.Pid,
                resourcePath = "UI/Prefabs/PlayerProgressionHubPanel",
                defaultLayer = UILayer.Popup,
                pooled = false,
            });
            
        }




        public static void RegisterGroups()
        {
            {
                var bagPolicy = new UIOrchestrator.UIGroupPolicy()
                {
                    groupName = "bag",
                    singleInGroup = false,
                    panelIds = new() { "PlayerBag", "WarehousePanel" },
                    isExclusive = false,
                };
                UIOrchestrator.Instance.AddGroupPolicy(bagPolicy);
            }

            {
                var progressionPolicy = new UIOrchestrator.UIGroupPolicy()
                {
                    groupName = "player_progression",
                    singleInGroup = false,
                    panelIds = new() { PlayerProgressionHubPanel.Pid },
                    isExclusive = false,
                };
                UIOrchestrator.Instance.AddGroupPolicy(progressionPolicy);
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

        #region ???????????

        [Serializable]
        public class UIGroupPolicy
        {
            public string groupName;                // ????????? "Interaction", "Inventory", "Fullscreen"
            public bool singleInGroup = false;      // ????????????true=??????????????
            public List<string> panelIds = new();   // ??????????
            public bool isExclusive = false;
        }

        [Header("UI Groups (Mutual Exclusion)")]
        [SerializeField] private List<UIGroupPolicy> groupPolicies = new();

        // ??????????
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

        // ?????????????????????????????????
        public void ShowInGroup(string panelId, object ctx = null, UILayer? layerOverride = null)
        {
            // ?????????????
            var policy = FindGroupByPanel(panelId, out var myGroup);
            if (policy != null && !string.IsNullOrEmpty(myGroup))
            {
                if(policy.isExclusive)
                {
                    // 1) ???????????????????
                    foreach (var kv in groupActive)
                    {
                        var groupName = kv.Key;
                        if (groupName == myGroup) continue;
                        // ?????????????????
                        var toClose = new List<string>(kv.Value);
                        foreach (var pid in toClose)
                        {
                            UIManager.Instance.HidePanel(pid);
                            kv.Value.Remove(pid);
                        }
                    }
                }
                
                // 2) ?????b????????????????
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

            // 3) ??????????
            UIManager.Instance.ShowPanel(panelId, ctx, layerOverride);

            // 4) ???????????
            if (!string.IsNullOrEmpty(myGroup))
            {
                if (!groupActive.TryGetValue(myGroup, out var set))
                    groupActive[myGroup] = set = new HashSet<string>();
                set.Add(panelId);
            }
        }

        // ??????????????????????????????
        public void HideInGroup(string panelId)
        {
            UIManager.Instance.HidePanel(panelId);
            if (panelToGroup.TryGetValue(panelId, out var g) && groupActive.TryGetValue(g, out var set))
            {
                set.Remove(panelId);
            }
        }

        // ????????????????????????????????????????????
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

        // ????????????????????
        public bool IsGroupActive(string groupName)
        {
            return groupActive.TryGetValue(groupName, out var set) && set.Count > 0;
        }

        #endregion


        public async Task SetStateAsync(UIAppState next, object ctx = null)
        {
            // 大地图切据点仍保持 Overworld，需刷新 HUD 显隐
            if (current == next && next != UIAppState.Overworld)
            {
                return;
            }

            if (current != next)
            {
                previous = current;
                current = next;
            }

            switch (next)
            {
                case UIAppState.Boot:
                    {
                        UIManager.Instance.HideAll("LoadingOverlay");
                    }
                    break;

                case UIAppState.Overworld:
                    await EnterOverworldAsync(ctx);
                    break;
                case UIAppState.Battle:
                    await EnterBattleAsync(ctx);
                    break;
                    //case UIAppState.PauseMenu:
                    //    await EnterPauseAsync();
                    //    break;
                    //case UIAppState.Dialog:
                    //    await EnterDialogAsync(ctx);
                    //    break;
            }
        }


        private void EnsureCommonUI()
        {
            UIManager.Instance.ShowPanel("GainItemSideNtfPanel");
            UIManager.Instance.ShowPanel("UIGainRewardCoordinator");
        }

        static bool IsInSecretBaseWorld()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            return glm != null && glm.IsInSecretBaseContext();
        }

        static readonly string[] OverworldMapOnlyPanelIds =
        {
            "OverworldHUD",
            PlayerHumanItemBarPanel.PanelIdConst,
            "SceneMask",
            "SmallIconLayer",
            "InteractMenu",
            "AmbientChatPanel",
            "QuestFloatingPanel",
        };

        static void ApplyOverworldMapPanelsVisibility(bool showOpenWorldHud)
        {
            foreach (var panelId in OverworldMapOnlyPanelIds)
            {
                if (showOpenWorldHud)
                {
                    UIManager.Instance.ShowPanel(panelId);
                }
                else
                {
                    UIManager.Instance.HidePanel(panelId);
                }
            }
        }

        private async Task EnterOverworldAsync(object ctx)
        {
            UIManager.Instance.HideAll("LoadingOverlay");

            ApplyOverworldMapPanelsVisibility(!IsInSecretBaseWorld());

            if (IsInSecretBaseWorld())
            {
                SecretBaseHudPanel.TryShow();
                PlayerHumanItemBarPanel.TryHide();
            }
            else
            {
                SecretBaseHudPanel.TryHide();
            }

            EnsureCommonUI();

            MainGameManager.Instance.inputBinder.ApplyInputMode(InputMode.Overworld);
            await Task.CompletedTask;
        }

        private async Task EnterBattleAsync(object ctx)
        {
            //UIManager.Instance.ShowLoading("Entering Battle...");
            //// ??????? HUD
            //UIManager.Instance.HidePanel("OverworldHUD");
            //// ????? HUD
            UIManager.Instance.ShowPanel("EncounterBattleHud", ctx, UILayer.HUD);
            //UIManager.Instance.ApplyInputMode(UIInputMode.Battle);
            //UIManager.Instance.HideLoading();

            EnsureCommonUI();

            MainGameManager.Instance.inputBinder.ApplyInputMode(InputMode.Battle);
            await Task.CompletedTask;
        }

        //private async Task EnterPauseAsync()
        //{
        //    // ????????????????????????? PauseMenuPanel?????? Popup/Overlay ??
        //    UIManager.Instance.ShowPanel("PauseMenu", null, UILayer.Overlay);
        //    // ?????? UI ????
        //    UIManager.Instance.ApplyInputMode(UIInputMode.Menu);
        //    await Task.CompletedTask;
        //}

        //private async Task EnterDialogAsync(object dialogCtx)
        //{
        //    // ?????????????
        //    UIManager.Instance.ShowPanel("DialogPanel", dialogCtx, UILayer.Popup);
        //    UIManager.Instance.ApplyInputMode(UIInputMode.Dialog);
        //    await Task.CompletedTask;
        //}

        //private async Task EnterLoadingAsync(string tip)
        //{
        //    UIManager.Instance.ShowLoading(tip);
        //    // Loading ?????????????????????????????????????????????????
        //    await Task.CompletedTask;
        //}

        //// ???????????????????????
        //public async Task GoToBattleAsync(object battleCtx)
        //{
        //    await SetStateAsync(UIAppState.Loading, "Matchmaking...");
        //    // ???????/????????...
        //    await SetStateAsync(UIAppState.Battle, battleCtx);
        //}


        #region ???????????

        public void EnsurePlayerBag()
        {
            ShowInGroup("ItemDragDrop");
            ShowInGroup("PlayerBag");
        }

        /// <summary>
        /// 切换右侧仓库面板；打开时确保拖拽层已显示。
        /// </summary>
        public void ToggleWarehousePanel()
        {
            if (UIManager.Instance.IsPanelVisible("WarehousePanel"))
            {
                UIManager.Instance.HidePanel("WarehousePanel");
                return;
            }

            ShowInGroup("ItemDragDrop");
            ShowInGroup("WarehousePanel");
        }


        public void TryEnterLootDetailMode(ILootableObj lootObj)
        {
            EnsurePlayerBag();
            // ??lootpoint
            ShowInGroup("LootPoint", lootObj);

            //if(UIManager.Instance.IsPanelVisible())
            {

            }
            //ItemPopupMenu.Instance.Close();
        }

        public void TryQuitLootDetailMode()
        {
            UIManager.Instance.HidePanel("ItemDragDrop");
            UIManager.Instance.HidePanel("PlayerBag");
            UIManager.Instance.HidePanel("WarehousePanel");
            UIManager.Instance.HidePanel("LootPoint");
        }

        public void ShowShop(IShopProvider shop)
        {
            EnsurePlayerBag();
            ShowInGroup("ShopNormalPanel", shop);
        }


        #endregion


        private MapLogicEventAdapter adapter;
        private List<MapLogicSubscription> subs = new();

        /// <summary>
        /// ??????????
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
            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.UnitDie, adapter));

            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.CostPendingAlert, adapter));
            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.PlayerFaQingStatusChange, adapter));
            subs.Add(MainGameManager.Instance.gameLogicManager.LogicEventBus.Subscribe(EMapLogicEventType.PlayerExposeStatusChange, adapter));

            MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.EventOnGainItem += (bagId, itemId, count) =>
            {
                HandleOnGainItem(itemId, (int)count);
            };
        }

        private void HandleOnGainItem(string itemId, int count)
        {
            if (UIGainSideNotifyPanel.Instance != null)
            {
                var itemCfg = CfgMgr.Cfgs.TbItemData.GetOrDefault(itemId);
                if(itemCfg != null)
                {
                    var sprite = SimpleResManager.Load<Sprite>("Sprites/Item/" + itemCfg.SpriteName);
                    UIGainSideNotifyPanel.Instance.EnqueueLog("+" + itemCfg.DisplayName + "*" + count, sprite);
                }
                
            }
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

                case EMapLogicEventType.CostPendingAlert:
                    {
                        var realEv = (MLECostPendingAlertEvent)ev;
                        if (OverworldHUDPanel.Instance != null)
                        {
                            OverworldHUDPanel.Instance.DoPendingAlertReduce(realEv.Value);
                        }
                    }
                    break;
                case EMapLogicEventType.PlayerFaQingStatusChange:
                    {
                        var realEv = (MLEPlayerFaQingStatusChangeEvent)ev;
                        var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                        
                        if(player.IsFaQing)
                        {
                            // ??????? ?????????
                        }


                        if (OverworldHUDPanel.Instance != null)
                        {
                            OverworldHUDPanel.Instance.SkilBar.Refresh(true);
                        }

                    }
                    break;

                case EMapLogicEventType.PlayerExposeStatusChange:
                    {
                        var realEv = (MLEPlayerExposeStatusChangeEvent)ev;
                        var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

                        if (OverworldHUDPanel.Instance != null)
                        {
                            OverworldHUDPanel.Instance.SkilBar.Refresh(true);
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

//        // ???? Map
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


//    // ??????????????? + Loading
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
