using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;
using My;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using My.Map;
using My.UI;
using My.Map.Fight;
using My.Config;
using My.SecretBase;
using My.Dungeon;
using cfg.demo;

public class ConsoleGM : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.BackQuote; // ~ 或 `
    public bool visible = false;

    [Header("UI")]
    public float height = 260f;         // 控制台窗口高度
    public int fontSize = 14;
    public int maxLogLines = 200;
    public Color bgColor = new Color(0, 0, 0, 0.8f);
    public Color inputBgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color hintColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color paramColor = new Color(0.6f, 0.9f, 0.6f, 1f);
    public Color errorColor = new Color(1f, 0.5f, 0.5f, 1f);

    private string input = "";
    private Vector2 scroll;
    private GUIStyle logStyle;
    private GUIStyle inputStyle;
    private GUIStyle hintStyle;

    private readonly List<string> logs = new();
    private readonly List<string> history = new();
    private int historyIndex = -1;

    // 命令注册
    private readonly Dictionary<string, Command> commands = new(StringComparer.OrdinalIgnoreCase);

    // 自动完成候选
    private List<string> candidates = new();
    private int candidateIndex = 0;

    // 多候选已顶到公共前缀时再按 Tab：循环整词（-1 表示尚未循环）
    int _tabCycleIndex = -1;
    string _autocompleteHeadSnapshot;

    // 参数提示缓存
    private string paramHint = "";

    void Awake()
    {
        Application.logMessageReceived += OnUnityLog;

        // 样例命令注册
        Register("help", "显示所有命令或查看某命令帮助",
            new[] { new CmdParam("cmd", "可选，命令名") },
            args =>
            {
                if (args.Count == 0)
                {
                    Log("可用命令：");
                    foreach (var kv in commands.OrderBy(k => k.Key))
                        Log($"  {kv.Key} - {kv.Value.Description}");
                }
                else
                {
                    var name = args[0];
                    if (commands.TryGetValue(name, out var cmd))
                        Log($"{name} {cmd.ParamUsage()} - {cmd.Description}");
                    else
                        LogError($"未知命令 {name}");
                }
            });

        Register("set_time_scale", "设置时间缩放",
            new[] { new CmdParam("scale", "float，时间缩放，例如 0.5") },
            args =>
            {
                if (args.Count < 1) { LogError("用法：set_time_scale <scale>"); return; }
                if (float.TryParse(args[0], out var s)) { Time.timeScale = s; Log($"已设置 Time.timeScale = {s}"); }
                else LogError("参数格式错误，需 float");
            });

        Register("tp", "传送到坐标",
            new[] { new CmdParam("x", "float"), new CmdParam("y", "float") },
            args =>
            {
                if (args.Count < 2) { LogError("用法：tp <x> <y>"); return; }
                if (Camera.main == null) { LogError("无主相机"); return; }
                var player = FindAnyObjectByType<Rigidbody2D>();
                if (player == null) { LogError("未找到 Rigidbody2D 作为玩家示例"); return; }
                if (float.TryParse(args[0], out var x) && float.TryParse(args[1], out var y))
                {
                    player.position = new Vector2(x, y);
                    Log($"玩家传送至 ({x}, {y})");
                }
                else LogError("参数需为 float");
            });

        Register("set_variable", "设置变量",
            new[] { new CmdParam("name", "string，变量名") },
            args =>
            {
                if (args.Count < 1) { LogError("用法：set_variable <id>"); return; }
                var id = args[0];
                
                Log($"设置变量 id={id}");

                MainGameManager.Instance.gameLogicManager.playerDataManager?.SetVariable(id);

            });

        Register("add_alert", "Adds GameLogicManager.AlertVal (NOT AreaAlertValue). For wanted_guard alert tier use: area_alert_add",
            new[] { new CmdParam("val", "int，值") },
            args =>
            {
                if (args.Count < 1) { LogError("usage: add_alert <int_delta> (GameLogicManager.AlertVal only)"); return; }
                var val = int.Parse(args[0]);

                Log($"add_alert val={val}");

                MainGameManager.Instance.gameLogicManager.AddAlertVal(val);

            });

        Register("f1", "扣衣服20点",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerClothes, -20000, false, FightStruct.EDmgFlag.None, null);
            });
        Register("hungry", "饿了",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.ApplyResourceChange(AttrIdConsts.PlayerHunger, -100000, false, FightStruct.EDmgFlag.None, null);
            });
        Register("m1", "刷怪",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.AddNewEntityRecord(new LogicEntityRecord4Npc()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "h_sprite",
                    Position = MainGameManager.Instance.playerScenePresenter.transform.position + new Vector3(2, 2, 0),
                    FactionId = EFactionId.HSprite,

                    IsPeace = false,
                    MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.Hunting,
                    EnmityConfId = "default_monster",
                });
            });
        Register("m2", "刷守卫",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.AddNewEntityRecord(new LogicEntityRecord4Npc()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "default_guard_01",
                    Position = MainGameManager.Instance.playerScenePresenter.transform.position + new Vector3(2, 2, 0),
                    FactionId = EFactionId.Citizen,

                    IsPeace = false,
                    MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                    EnmityConfId = "default_guard",
                });
            });

        Register("m3", "刷小鬼",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.AddNewEntityRecord(new LogicEntityRecord4Npc()
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "evil_child",
                    Position = MainGameManager.Instance.playerScenePresenter.transform.position + new Vector3(2, 2, 0),
                    FactionId = EFactionId.Citizen,

                    IsPeace = false,
                    MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                    EnmityConfId = "default_monster",
                });
            });
        

        Register("dump_player_attr", "Print all registered numeric and resource attributes on the player",
            null,
            args =>
            {
                var glm = MainGameManager.Instance?.gameLogicManager;
                var player = glm?.playerLogicEntity;
                if (player == null)
                {
                    LogError("playerLogicEntity is null");
                    return;
                }

                Log("[GM] === player attributes (registered in AttributeStore) ===");
                player.DebugGmEnumerateAllAttributes(
                    (id, val) => { Log($"{id} = {val}"); Debug.Log($"{id} = {val}"); }
                    );
                Log("[GM] === end ===");
            });

        Register("shop", "开shop",
            new[] { new CmdParam("shop id", "int，值") },
            args =>
            {
                var id = int.Parse(args[0]);
                var shopInfo = MainGameManager.Instance.gameLogicManager.shopDataManager.GetShop(id);
                if(shopInfo == null)
                {
                    return;
                }
                UIOrchestrator.Instance.ShowShop(shopInfo);
            });

        Register("talent_ui", "Toggle PlayerProgressionHub Talents tab",
            null,
            _ =>
            {
                PlayerProgressionHubPanel.ToggleTalents();
            });

        Register("rune_ui", "Toggle PlayerProgressionHub Runes tab",
            null,
            _ =>
            {
                PlayerProgressionHubPanel.ToggleRunes();
            });

        Register("grant_rune", "获得指定符文 grant_rune <rune_id>",
            new[] { new CmdParam("runeId", "string，rune_id") },
            args =>
            {
                if (args.Count < 1)
                {
                    LogError("用法：grant_rune <rune_id>");
                    return;
                }

                var pdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
                if (pdm == null)
                {
                    LogError("playerDataManager null");
                    return;
                }

                if (pdm.TryGrantRune(args[0]))
                {
                    Log($"Granted rune: {args[0]}");
                }
                else
                {
                    LogError($"Failed to grant rune: {args[0]}");
                }
            });

        Register("gc", "加gc",
            new[] { new CmdParam("val", "int，值") },
            args =>
            {
                var val = int.Parse(args[0]) * 1000;
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

                player.ApplyResourceChange(AttrIdConsts.PlayerPleasure, val, false, FightStruct.EDmgFlag.None, null);
            });

        Register("faqing", "强制发情",
            null,
            args =>
            {
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

                player.ApplyResourceChange(AttrIdConsts.PlayerEstrusProgrss, 40_000, false, FightStruct.EDmgFlag.None, null);
            });

        Register("knockdown_add", "增加玩家推倒进度，100=满条",
            new[] { new CmdParam("val", "int，刻度值，100=满条") },
            args =>
            {
                if (args.Count < 1) { LogError("用法：knockdown_add <val>"); return; }
                if (!int.TryParse(args[0], out var rawVal)) { LogError("参数需为 int"); return; }

                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                if (player == null) { LogError("player not found"); return; }

                var val = rawVal * 1000;
                player.ApplyResourceChange(AttrIdConsts.PlayerKnockDown, val, true, FightStruct.EDmgFlag.None, null);
                Log($"PlayerKnockDown +{val}, current={player.GetAttr(AttrIdConsts.PlayerKnockDown)}");
            });

        Register("knockdown_full", "填满推倒条并触发被推倒",
            null,
            args =>
            {
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                if (player == null) { LogError("player not found"); return; }

                var max = player.GetResourceMax(AttrIdConsts.PlayerKnockDown);
                var cur = player.GetAttr(AttrIdConsts.PlayerKnockDown);
                var delta = max - cur;
                if (delta <= 0)
                {
                    Log("PlayerKnockDown already full or above max");
                    return;
                }

                player.ApplyResourceChange(AttrIdConsts.PlayerKnockDown, delta, true, FightStruct.EDmgFlag.None, null);
                Log($"PlayerKnockDown filled to max, current={player.GetAttr(AttrIdConsts.PlayerKnockDown)}");
            });


        Register("gptest", "创建测试用gather point",
            null,
            args =>
            {
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

                Vector2 pos = player.Pos + UnityEngine.Random.insideUnitCircle * 1f;

                var rec = new LogicEntityRecord();
                rec.Id = GameLogicManager.LogicEntityIdInst++;
                rec.Position = pos;
                rec.EntityType = EEntityType.GatherPoint;
                rec.CfgId = "berry_01";

                MainGameManager.Instance.gameLogicManager.AddNewEntityRecord(rec);
            });

        Register("fish_settlement", "推进一次世界结算日并刷新垂钓点容量（按 Luban restock_every_n_days）",
            null,
            args =>
            {
                Log("fish_settlement: day=" + MainGameManager.Instance.gameLogicManager.SettlementDayIndex);
            });

        Register("hptest", "创建测试用hide point",
            null,
            args =>
            {
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;

                Vector2 pos = player.Pos + UnityEngine.Random.insideUnitCircle * 1f;

                var rec = new LogicEntityRecord4InteractPoint();
                rec.Id = GameLogicManager.LogicEntityIdInst++;
                rec.Position = pos;
                rec.EntityType = EEntityType.InteractPoint;
                rec.CfgId = "hide_point_01";

                MainGameManager.Instance.gameLogicManager.AddNewEntityRecord(rec);
            });

        Register("dmg", "造成伤害",
            new[] { new CmdParam("val", "int，值") },
            args =>
            {
                var val = int.Parse(args[0]) * 1000;
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                player.ApplyResourceChange(AttrIdConsts.HP, -val, false, FightStruct.EDmgFlag.None, null);
            });
        Register("san", "san 变化",
            new[] { new CmdParam("val", "int，值") },
            args =>
            {
                var val = int.Parse(args[0]) * 1000;
                var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
                player.ApplyResourceChange(AttrIdConsts.PlayerSanity, val, false, FightStruct.EDmgFlag.None, null);
            });
        Register("give_item", "给item",
            new[] { new CmdParam("itemId", "string，变量名"),
             new CmdParam("count", "string，变量名")},
            args =>
            {
                string itemId = args[0];
                var val = int.Parse(args[1]);

                MainGameManager.Instance.gameLogicManager.playerDataManager.GiveItemToPlayer(itemId, val);

            });

        RegisterSecretBaseLevelCommands();
        RegisterSecretBaseNpcFavorCommands();

        Register("rr", "撤退",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.TryStartRetreating();
            });

        Register("hit", "hitt",
            null,
            args =>
            {
                var ctx = MapSceneEffectManager.Instance.ShowSceneEffect(MainGameManager.Instance.gameLogicManager.playerLogicEntity.Pos, 0.2f, "Hit/player_shield", MainGameManager.Instance.gameLogicManager.playerLogicEntity.Id);
                if (ctx != null)
                {
                    ctx.BindingUnitVec = new Vector2(0, 0.555f);
                }
            });

        Register("chain_bind", "测试锁链捆缚：对附近最近单位施加 chain_bind",
            new[] { new CmdParam("duration", "可选，默认 3 秒") },
            args =>
            {
                float dur = 3f;
                if (args.Count > 0 && float.TryParse(args[0], out var parsed))
                {
                    dur = parsed;
                }

                var glm = MainGameManager.Instance.gameLogicManager;
                var player = glm.playerLogicEntity;
                long targetId = player.Id;
                float bestDist = float.MaxValue;

                //foreach (var entity in player.FindEntityInRange(player.Pos, 8f))
                //{
                //    if (entity.Id == player.Id || entity is not My.Map.BaseUnitLogicEntity)
                //    {
                //        continue;
                //    }

                //    float dist = (entity.Pos - player.Pos).sqrMagnitude;
                //    if (dist < bestDist)
                //    {
                //        bestDist = dist;
                //        targetId = entity.Id;
                //    }
                //}

                glm.globalBuffManager.RequestAddBuff(targetId, "chain_bind", overrideDuration: dur);
                Log($"chain_bind applied to entity {targetId}, duration={dur}s");
            });

        Register("orb_skill_summon", "Debug：召唤 orb SkillProxy 跟随球",
            null,
            args =>
            {
                var glm = MainGameManager.Instance.gameLogicManager;
                var player = glm?.playerLogicEntity;
                if (player == null)
                {
                    LogError("player not found");
                    return;
                }

                const string skillId = "orb_skill_summon";
                if (!player.ablilityManager.SkillRuntimes.ContainsKey(skillId))
                {
                    if (!player.ablilityManager.RegisterSkill(skillId))
                    {
                        LogError($"RegisterSkill failed: {skillId}");
                        return;
                    }
                }

                bool ok = player.ablilityManager.UseSkill(skillId);
                Log(ok ? $"UseSkill ok: {skillId}" : $"UseSkill failed: {skillId}");
            });

        Register("wanted", "通缉",
            null,
            args =>
            {
                MainGameManager.Instance.gameLogicManager.WantedManager.DebugAddWantedVal(50000);
            });

        Register("wanted_behave", "按 Luban 通缉行为加星（含 max_add_once）",
            new[] { new CmdParam("name", "StealSmall | StealValuable | AssaultCitizen") },
            args =>
            {
                if (args.Count < 1)
                {
                    LogError("usage: wanted_behave StealSmall");
                    return;
                }

                if (!System.Enum.TryParse<EWantedBehaveType>(args[0], true, out var b) || b == EWantedBehaveType.None)
                {
                    LogError("invalid EWantedBehaveType: " + args[0]);
                    return;
                }

                var wm = MainGameManager.Instance.gameLogicManager.WantedManager;
                wm.AddWantedForBehavior(b);
                Log($"wanted_behave {b} -> star {wm.GetWantedStarLevel()} val {wm.CurrentWantedVal}");
            });

        void LogWantedGuardStatus()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                LogError("gameLogicManager null");
                return;
            }

            var wm = glm.WantedManager;
            var am = glm.AreaManager;
            var sp = glm.WantedGuardSpawner;
            string tierLine = sp != null
                ? sp.DebugFormatSelectedTier(out _, out _)
                : "WantedGuardSpawner null";
            Log("[wanted_guard] tick_period_s=" + WantedDynamicGuardController.TickPeriodSeconds
                + " | spawns only in spots outside player FOV (visionSenser.SimpleCanSee)");
            Log($"[wanted_guard] wanted_star={wm?.GetWantedStarLevel() ?? -1} wanted_scaled_max_channel={wm?.CurrentWantedVal ?? -1}");
            Log($"[wanted_guard] area_alert_value={am?.AreaAlertValue ?? -1} alert_pressure_tier={am?.GetAlertPressureTier() ?? -1} (thresholds 2500/5000/8000 on AreaAlertValue+pending)");
            Log($"[wanted_guard] game_alert_val={glm.AlertVal} (add_alert command only)");
            Log($"[wanted_guard] selected_tier: {tierLine}");
        }

        Register("wanted_guard_status", "Print wanted star, area alert tier, and selected WantedGuardSpawnTier row (same logic as refresh)",
            null,
            _ => LogWantedGuardStatus());

        Register("wg_status", "Alias of wanted_guard_status",
            null,
            _ => LogWantedGuardStatus());

        Register("wanted_clear", "Clear all wanted channels",
            null,
            _ =>
            {
                MainGameManager.Instance.gameLogicManager.WantedManager.ClearAllWanted();
                Log("WantedManager.ClearAllWanted done");
            });

        Register("wanted_guard_clear", "Destroy all dynamic wanted-pressure guards tracked by WantedGuardSpawner",
            null,
            _ =>
            {
                MainGameManager.Instance.gameLogicManager.WantedGuardSpawner?.ClearAll();
                Log("WantedGuardSpawner.ClearAll done");
            });

        Register("area_alert_add", "Add to GameLogicAreaManager.AreaAlertValue (drives GetAlertPressureTier). NOT add_alert.",
            new[] { new CmdParam("delta", "long, e.g. 3000") },
            args =>
            {
                if (args.Count < 1 || !long.TryParse(args[0], out var delta))
                {
                    LogError("usage: area_alert_add <delta_long>");
                    return;
                }

                var am = MainGameManager.Instance.gameLogicManager.AreaManager;
                am.DebugAddAreaAlert(delta);
                Log($"area_alert_add {delta} -> AreaAlertValue={am.AreaAlertValue} alert_pressure_tier={am.GetAlertPressureTier()}");
            });

        Register("dungeon_test", "Enter procedural test cave overlay. Usage: dungeon_test [seed]",
            new[] { new CmdParam("seed", "optional int seed") },
            args =>
            {
                int seed;
                if (args.Count >= 1 && int.TryParse(args[0], out var parsed))
                {
                    seed = parsed;
                }
                else
                {
                    seed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
                }

                DungeonSession.SetPendingSeed(seed);
                var glm = MainGameManager.Instance.gameLogicManager;
                if (glm == null)
                {
                    LogError("gameLogicManager null");
                    return;
                }

                glm.PreparePlayerSwitchArea(DungeonPresentation.TestCaveOverlayId, true);
                Log($"dungeon_test switching to {DungeonPresentation.TestCaveOverlayId} seed={seed}");
            });

        Register("spawn_investigation_npc", "Spawn default_guard_01; kind=pressure_behavior 0-3 on NpcRecord (post-Search policy applied by WantedGuardSpawner); optional immediate 0|1 (default: 1 if kind>0 else 0)",
            new[] { new CmdParam("kind", "0-3 pressure_behavior / NpcRecord.PostInvestigationResolveKind"), new CmdParam("immediate", "optional 0|1") },
            args =>
            {
                if (args.Count < 1 || !int.TryParse(args[0], out var kind) || kind < 0 || kind > 3)
                {
                    LogError("usage: spawn_investigation_npc <kind_0_3> [immediate_0_1]");
                    return;
                }

                int immediate = (args.Count >= 2 && int.TryParse(args[1], out var im)) ? im : (kind > 0 ? 1 : 0);
                if (immediate != 0 && immediate != 1)
                {
                    LogError("immediate must be 0 or 1");
                    return;
                }

                var glm = MainGameManager.Instance.gameLogicManager;
                var pos = MainGameManager.Instance.playerScenePresenter.transform.position + new Vector3(2, 2, 0);
                glm.AddNewEntityRecord(new LogicEntityRecord4Npc
                {
                    Id = GameLogicManager.LogicEntityIdInst++,
                    EntityType = EEntityType.Npc,
                    CfgId = "default_guard_01",
                    Position = pos,
                    FactionId = EFactionId.Citizen,
                    IsPeace = false,
                    MoveBehaveType = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                    EnmityConfId = "default_guard",
                    PostInvestigationResolveKind = kind,
                    PostInvestigationPatrolPickN = 3,
                    SpawnWithImmediateInvestigation = immediate == 1,
                });
                Log($"spawn_investigation_npc pressure_behavior={kind} immediate={immediate} at player+2,2");
            });

        Register("add_quest_value", "开shop",
            new[] { new CmdParam("quest_id", "int，值"),
            new CmdParam("amount", "int，值")},
            args =>
            {
                var questId = int.Parse(args[0]);
                var amount = int.Parse(args[1]);

                var quest = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.GetQuest(questId);
                if(quest == null)
                {
                    return;
                }
                if(quest.ActiveStep == null)
                {
                    return;
                }

                foreach(var obj in quest.ActiveStep.ObjectiveRuntimes)
                {
                    obj.ProgressVal += amount;
                }

                MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.RaiseQuestObjUpdateEvent(quest.cacheCfg.QuestId);
            });

        Register("finish_quest", "直接完成任务",
            new[] { new CmdParam("quest_id", "int，值") },
            args =>
            {
                var questId = int.Parse(args[0]);
                MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.ForceFinishQuest(questId);
            });
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnUnityLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
            if (visible) DisableInput();
            else EnableInput();
        }

        if (!visible) return;

        // 基础键盘交互（不依赖 IMGUI focus）
        if (Input.GetKeyDown(KeyCode.UpArrow)) BrowseHistory(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) BrowseHistory(+1);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DoAutoComplete();
        }

        UpdateHints();
    }

    void OnGUI()
    {
        if (!visible) return;

        var e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == toggleKey)
            {
                visible = !visible;
                if (visible) DisableInput(); else EnableInput();
                e.Use();
                if (!visible) return; // 关掉后就不再绘制
            }
            // 回车保留给输入执行（你已有处理）
            // 其它编辑键（Backspace/左右箭头）让 IMGUI 正常处理
        }

        EnsureStyles();

        var rect = new Rect(0, 0, Screen.width, height);
        // 背景
        DrawRect(rect, bgColor);

        GUILayout.BeginArea(rect);
        GUILayout.Space(6);

        // 日志区域
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        foreach (var line in logs)
        {
            GUILayout.Label(line, logStyle);
        }
        GUILayout.EndScrollView();

        // 输入区域背景
        var inputRect = new Rect(8, height - 36, Screen.width - 16, 28);
        DrawRect(inputRect, inputBgColor);

        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUI.SetNextControlName("ConsoleInput");
        input = GUILayout.TextField(input, inputStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
        GUI.FocusControl("ConsoleInput");

        if (GUILayout.Button("执行", GUILayout.Width(60), GUILayout.Height(24)))
            ExecuteInput();
        GUILayout.Space(8);
        GUILayout.EndHorizontal();

        // 参数提示
        if (!string.IsNullOrEmpty(paramHint))
        {
            GUILayout.Space(2);
            var c = hintStyle.normal.textColor;
            hintStyle.normal.textColor = paramColor;
            GUILayout.Label(paramHint, hintStyle);
            hintStyle.normal.textColor = c;
        }
        else
        {
            // 占位，保持控件数量一致
            GUILayout.Space(0);
        }

        // 自动完成候选
        if (candidates.Count > 0)
        {
            GUILayout.Space(2);
            var c = hintStyle.normal.textColor;
            hintStyle.normal.textColor = hintColor;
            GUILayout.Label("候选：" + string.Join("  |  ", candidates.Select((s, i) => i == candidateIndex ? $"[{s}]" : s)), hintStyle);
            hintStyle.normal.textColor = c;
        }
        else
        {
            GUILayout.Space(0);
        }

        //// 处理回车
        //var e = Event.current;
        //if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        //{
        //    ExecuteInput();
        //    e.Use();
        //}

        

        GUILayout.EndArea();
    }

    void RegisterSecretBaseLevelCommands()
    {
        void Handler(List<string> args)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                LogError("gameLogicManager null");
                return;
            }

            if (args.Count == 0)
            {
                int lv = glm.GetSecretBaseBuildLevel();
                var bounds = SecretBaseScrollBounds.Get(lv);
                Log($"SecretBaseBuildLevel={lv}, scroll X [{bounds.minX}, {bounds.maxX}]");
                var table = CfgMgr.Cfgs?.TbSecretBaseBuildLevel;
                if (table?.DataList != null)
                {
                    foreach (var row in table.DataList)
                    {
                        if (row != null)
                        {
                            Log($"  Lv{row.Level}: [{row.ScrollMinX}, {row.ScrollMaxX}]");
                        }
                    }
                }

                return;
            }

            if (!int.TryParse(args[0], out var target))
            {
                LogError("usage: secret_base_level [level]  (level must be int >= 1)");
                return;
            }

            if (target < 1)
            {
                LogError("level must be >= 1");
                return;
            }

            glm.SetSecretBaseBuildLevel(target);
            int applied = glm.GetSecretBaseBuildLevel();
            var appliedBounds = SecretBaseScrollBounds.Get(applied);
            Log($"Set SecretBaseBuildLevel={applied}, scroll X [{appliedBounds.minX}, {appliedBounds.maxX}]");
        }

        var parameters = new[] { new CmdParam("level", "optional int，TbSecretBaseBuildLevel.level") };
        Register("secret_base_level", "隐秘据点建设等级：无参查看配置与当前值，有参升级/降级并刷新卷轴边界",
            parameters, Handler);
        Register("sb_level", "alias of secret_base_level", parameters, Handler);
    }

    void RegisterSecretBaseNpcFavorCommands()
    {
        var parameters = new[]
        {
            new CmdParam("character_key", "TbCharacterInfo.key"),
            new CmdParam("delta", "optional int favor delta"),
        };

        void Handler(List<string> args)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                LogError("gameLogicManager null");
                return;
            }

            if (args.Count == 0)
            {
                Log("usage: secret_base_npc_favor <character_key> [delta]");
                return;
            }

            string key = args[0];
            var registry = glm.worldPersistState?.NpcCharacters;
            if (registry == null)
            {
                LogError("NpcCharacters registry null");
                return;
            }

            if (args.Count >= 2)
            {
                if (!int.TryParse(args[1], out var delta))
                {
                    LogError("delta must be int");
                    return;
                }

                registry.AddFavorValue(key, delta);
            }

            int favor = registry.GetFavorValue(key);
            int level = registry.GetFavorLevel(key);
            int given = registry.GetGiftsGivenToday(key, glm.SettlementDayIndex);
            Log($"secret_base_npc_favor key={key} favor={favor} level={level} giftsToday={given}");
        }

        Register("secret_base_npc_favor", "查看/调整据点 NPC 好感（CharacterKey）", parameters, Handler);
    }

    // =============== 命令系统 ===============

    private void Register(string name, string desc, CmdParam[] parameters, Action<List<string>> handler)
    {
        commands[name] = new Command(name, desc, parameters?.ToList() ?? new List<CmdParam>(), handler);
    }

    private void ExecuteInput()
    {
        var line = input.Trim();
        if (string.IsNullOrEmpty(line)) return;

        Log($"> {line}");
        history.Add(line);
        historyIndex = history.Count;
        input = "";
        candidates.Clear();
        paramHint = "";
        _tabCycleIndex = -1;
        _autocompleteHeadSnapshot = null;

        // 解析命令
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;

        var cmdName = tokens[0];
        tokens.RemoveAt(0);

        if (!commands.TryGetValue(cmdName, out var cmd))
        {
            LogError($"未知命令：{cmdName}（输入 help 查看）");
            return;
        }

        try
        {
            cmd.Handler(tokens);
        }
        catch (Exception ex)
        {
            LogError($"命令执行错误：{ex.Message}");
        }
    }

    private List<string> Tokenize(string line)
    {
        // 简单分词：支持双引号包裹的参数
        var list = new List<string>();
        bool inQuote = false;
        var cur = new System.Text.StringBuilder();
        foreach (char ch in line)
        {
            if (ch == '"') { inQuote = !inQuote; continue; }
            if (!inQuote && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0) { list.Add(cur.ToString()); cur.Clear(); }
            }
            else cur.Append(ch);
        }
        if (cur.Length > 0) list.Add(cur.ToString());
        return list;
    }

    private void UpdateHints()
    {
        var prefix = input;
        // 构建命令提示和参数提示
        var tokens = Tokenize(prefix);
        if (tokens.Count == 0)
        {
            if (!string.IsNullOrEmpty(_autocompleteHeadSnapshot))
            {
                _tabCycleIndex = -1;
                _autocompleteHeadSnapshot = "";
            }

            candidates = commands.Keys.OrderBy(k => k).ToList();
            candidateIndex = 0;
            paramHint = "";
            return;
        }

        if (input.EndsWith(" "))
        {
            // 用户正在输入下一个参数
            tokens.Add("");
        }

        if (tokens.Count == 1)
        {
            string head = tokens[0];
            if (head != _autocompleteHeadSnapshot)
            {
                _tabCycleIndex = -1;
                _autocompleteHeadSnapshot = head;
            }

            // 命令名自动完成
            candidates = commands.Keys.Where(k => k.StartsWith(head, StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(k => k).ToList();
            candidateIndex = (_tabCycleIndex >= 0 && _tabCycleIndex < candidates.Count)
                ? _tabCycleIndex
                : 0;

            // 参数提示：显示该命令的参数签名
            if (commands.TryGetValue(head, out var cmdExact))
            {
                paramHint = $"{cmdExact.Name} {cmdExact.ParamUsage()} - {cmdExact.Description}";
            }
            else
            {
                // 最近的公共前缀命中显示第一个候选的签名
                if (candidates.Count > 0 && commands.TryGetValue(candidates[0], out var cmd))
                    paramHint = $"{cmd.Name} {cmd.ParamUsage()} - {cmd.Description}";
                else
                    paramHint = "";
            }
        }
        else
        {
            _autocompleteHeadSnapshot = null;

            // 已确定命令名，给参数提示
            string cmdName = tokens[0];
            if (commands.TryGetValue(cmdName, out var cmd))
            {
                paramHint = $"{cmd.Name} {cmd.ParamUsage()}";
            }
            else paramHint = "";

            // 不对参数做自动补全（也可在此做自定义补全）
            candidates.Clear();
        }
    }

    private void DoAutoComplete()
    {
        bool addingSpace = input.EndsWith(" ");
        var tokens = Tokenize(input);

        if (tokens.Count > 1 || (tokens.Count == 1 && addingSpace))
        {
            return;
        }

        string head = tokens.Count == 0 ? "" : tokens[0];
        var matches = commands.Keys
            .Where(k => k.StartsWith(head, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

        candidates = matches;
        candidateIndex = 0;

        if (matches.Count == 1)
        {
            input = matches[0] + " ";
            _tabCycleIndex = -1;
            return;
        }

        string lcp = LongestCommonPrefixIgnoreCase(matches);
        if (lcp.Length > head.Length)
        {
            input = lcp;
            _tabCycleIndex = -1;
            return;
        }

        _tabCycleIndex = (_tabCycleIndex + 1) % matches.Count;
        candidateIndex = _tabCycleIndex;
        input = matches[_tabCycleIndex] + " ";
    }

    // 忽略大小写比较，返回用首个候选的 casing 表示的公共前缀
    static string LongestCommonPrefixIgnoreCase(IReadOnlyList<string> strings)
    {
        if (strings == null || strings.Count == 0)
        {
            return "";
        }

        if (strings.Count == 1)
        {
            return strings[0];
        }

        int minLen = int.MaxValue;
        foreach (var s in strings)
        {
            if (s.Length < minLen)
            {
                minLen = s.Length;
            }
        }

        var first = strings[0];
        for (int i = 0; i < minLen; i++)
        {
            char c0 = first[i];
            for (int j = 1; j < strings.Count; j++)
            {
                if (char.ToLowerInvariant(strings[j][i]) != char.ToLowerInvariant(c0))
                {
                    return first.Substring(0, i);
                }
            }
        }

        return first.Substring(0, minLen);
    }

    private void BrowseHistory(int delta)
    {
        if (history.Count == 0) return;
        historyIndex = Mathf.Clamp(historyIndex + delta, 0, history.Count);
        if (historyIndex >= 0 && historyIndex < history.Count) input = history[historyIndex];
        else input = "";
        // 更新提示
        UpdateHints();
    }

    // =============== UI/日志 ===============

    private void EnsureStyles()
    {
        if (logStyle == null)
        {
            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.fontSize = fontSize;
            logStyle.normal.textColor = Color.white;

            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = fontSize;

            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = fontSize - 1;
            hintStyle.normal.textColor = hintColor;
        }
    }

    private void DrawRect(Rect r, Color c)
    {
        var prev = GUI.color;
        GUI.color = c;
        GUI.Box(r, GUIContent.none);
        GUI.color = prev;
    }

    private void Log(string msg)
    {
        logs.Add(msg);
        TrimLogs();
        scroll.y = float.MaxValue;
    }

    private void LogError(string msg)
    {
        logs.Add(Colorize(msg, errorColor));
        TrimLogs();
        scroll.y = float.MaxValue;
    }

    private void TrimLogs()
    {
        if (logs.Count > maxLogLines)
            logs.RemoveRange(0, logs.Count - maxLogLines);
    }

    private string Colorize(string text, Color c)
    {
        var col = ColorUtility.ToHtmlStringRGBA(c);
        return $"<color=#{col}>{text}</color>";
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            LogError(condition);
        else if (type == LogType.Warning)
            Log(Colorize(condition, new Color(1f, 0.9f, 0.6f)));
        else
            Log(condition);
    }

    private void EnableInput()
    {
        MainGameManager.Instance.inputBinder.GlobalLock = false;
    }

    private void DisableInput()
    {
        MainGameManager.Instance.inputBinder.GlobalLock = true;
    }

    // =============== 数据结构 ===============
    private class Command
    {
        public string Name;
        public string Description;
        public List<CmdParam> Params;
        public Action<List<string>> Handler;

        public Command(string name, string desc, List<CmdParam> @params, Action<List<string>> handler)
        {
            Name = name;
            Description = desc;
            Params = @params ?? new List<CmdParam>();
            Handler = handler;
        }

        public string ParamUsage()
        {
            if (Params == null || Params.Count == 0) return "";
            return string.Join(" ", Params.Select(p => p.Optional ? $"[{p.Name}]" : $"<{p.Name}>"));
        }
    }

    private class CmdParam
    {
        public string Name;
        public string Hint;
        public bool Optional;

        public CmdParam(string name, string hint = "", bool optional = false)
        {
            Name = name;
            Hint = hint;
            Optional = optional;
        }
    }
}
