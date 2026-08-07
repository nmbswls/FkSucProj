# Dialogue 与 Cutscene 工作流记录

## 当前 Dialogue Command 支持情况

- 纯文本对话：支持 `DialogCommandData4Text`，可以批量播放多行台词。
- 选项与跳转：支持 `Choice`、`JumpTo`、`SwitchDialogSegment`、动态 NPC 选项。
- 图片：
  - `SetImage` 偏立绘槽位，现有实现依赖 `PortraitManager` 的 Left/Center/Right/Background 槽位。
  - `ShowImage` 已补充为无条件 CG 图片命令，运行时在 `DialogueUI` 下创建全屏图片层，不依赖 prefab 预配槽位。
- 场景角色移动：支持 `MoveEntity`，通过 `StaticName` 找动态实体，并要求实体实现 `IDialogueActor`。等待逻辑依赖 `DialogMoveFinished`。
- Timeline / cutscene：支持加载 additive cutscene scene、播放 `PlayableDirector`、等待 Timeline signal、恢复 Timeline。
- 角色动画：`DialogCommandData4ActorAnim` 已由 `DialoguePlayer` 执行；当前命令作用于玩家逻辑实体，并通过 `DoDialogAnimation` 播放指定逻辑动画名。
- 头顶表情：当前 DialoguePlayer 没有专门 command。项目里有气泡/头顶 UI 相关系统，但还没有被对话命令统一调度。

## 是否适合直接在原场景播放日式 RPG 演出

适合的场景：

- 只需要让已有地图中的角色短距离走位、停住、对话。
- 角色都已经是地图动态实体，并且实现了 `IDialogueActor`。
- 演出不依赖复杂镜头、临时道具、大量一次性布景或精确逐帧调度。

不适合的场景：

- 需要多个演员、临时道具、表情气泡、镜头和 BGM 按时间轴精确配合。
- 需要复用主场景的地图素材，但又要隔离玩家输入、AI、刷新、碰撞状态。
- 需要策划或美术反复预览，不希望每次都进完整主流程触发。

## Cutscene 现状问题

- Cutscene 支持 additive scene，但目前更像“额外过场场景”，没有成熟的“复用当前主场景素材”的制作流程。
- `LoadCutsceneSceneRoutine` 里隐藏主场景的逻辑仍是注释状态，active scene 切到 cutscene 后，主场景对象与 cutscene 对象的关系不够明确。
- Timeline 通过场景对象名查找 `PlayableDirector`，缺少统一的 cutscene prefab/asset 注册入口。
- Dialogue command 与 Timeline 可以互相等待，但对地图实体绑定、临时演员生成、头顶表情、相机目标绑定还没有统一数据层。

## 建议优化方向

- 做一个 `CutsceneStage` prefab/scene 约定：包含 VCam、TimelineRoot、ActorBindingRoot、PropRoot、SignalBridge。
- 引入 `CutsceneActorBinding`：用逻辑实体 uniq name、player、临时 prefab 三类来源绑定 Timeline track，避免 Timeline 直接依赖场景层级路径。
- 增加 Dialogue commands：
  - `ActorEmote`：在指定 actor 头顶播放图标/气泡。
  - `ActorAnim` 执行逻辑：按 actor static name 或 binding id 播放动画状态。
  - `CameraFollowActor` / `CameraLookAtActor`：把镜头目标绑定到逻辑演员。
- 对于复用主场景素材的演出，优先做“原场景轻量 cutscene mode”：暂停玩家输入和 AI，锁定必要实体，使用主场景相机和地图对象，只把 Timeline/临时道具 additive 进来。
- 对于大型独立演出，继续使用 additive cutscene scene，但需要明确进入/退出时主场景显示、逻辑暂停、相机优先级和资源卸载规则。
