# Town NPC Prefab/View Boundary

城镇 NPC 的逻辑壳子和外观变体必须分离。

`civil_shell.prefab` 负责 Presenter、碰撞、移动、交互、阴影、武器控制以及稳定的 `view/agent` 挂点。它不承载具体市民外观。

`civil_view_01.prefab`、`civil_view_02.prefab`、`civil_view_03.prefab` 只负责 `agent` 及其 SpriteRenderer。随机表配置这些 view prefab 的名称，运行时只替换 `view/agent`，不替换 NPC 外层壳子。

重要 NPC 不写入随机表，继续使用固定的完整 prefab，因此不会执行外观替换。
