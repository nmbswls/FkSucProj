# 市集与人类武器词条系统

## 市集当前规则

市集基础拥有 3 个销售格。每个挂牌记录绑定 `building_id` 与 `logic_area_id`，因此同一玩家可以在不同城镇分别经营市集。市场结算只处理挂牌记录，不读取主角战斗属性。

建筑属性：

- `MarketSlotCount`：额外销售格数量。
- `MarketSaleChanceBonus`：每点使单格售出概率增加 5 个百分点。
- `MarketPriceBonus`：每点使最终价格增加 10%。

建议改造项共 5 个：

| 改造项 | 效果 |
| --- | --- |
| 扩建货架 I | `MarketSlotCount +1` |
| 扩建货架 II | `MarketSlotCount +1` |
| 公开竞价 | `MarketSaleChanceBonus +1` |
| 鉴定柜台 | `MarketPriceBonus +1` |
| 商会账房 | `MarketSlotCount +1`、`MarketSaleChanceBonus +1` |

这样最终基础 3 格可以成长到 6 格，概率和价格也分别有独立成长路径。

## 武器词条目标

`human_weapon` 物品继续是普通 Item；每个具体掉落的词条保存在 `ItemInstance4HumanWeapon`，不写入 Item 静态配置。主角装备和战斗系统完全忽略词条，武器在主角手中只使用既有的等级、技能和基础属性。

词条只服务三个消费者：

1. 市集价值：词条提供固定附加价值和稀有度乘数。
2. NPC需求：NPC可以要求某个词条、词条组或最低词条等级。
3. 任务与好感：任务目标和角色收礼规则可以匹配词条条件。

## 词条数据

新增独立配置表 `human_weapon_affix`：

- `affix_id`
- `display_name`
- `group_id`
- `tier`
- `market_value`
- `tags`
- `exclusive_group`

`group_id` 用于区分伤害、暴击、防御、资源、特殊效果等语义组；这些语义只用于展示、价值和条件匹配，不进入战斗计算。

新增 `human_weapon_affix_pool`：

- `pool_id`
- `affix_id`
- `weight`
- `min_weapon_level`
- `max_weapon_level`
- `min_count`
- `max_count`

具体武器实例通过鉴定 seed 从对应 pool 随机生成词条，实例保存 `affix_id` 与对应 tier 列表。词条数量、重复规则和互斥组由 pool 控制。

## 价值计算

```text
weapon_value = item.market_base_price
             + sum(affix.market_value)
             + max(0, highest_affix_tier - 1) * item.market_base_price * 10%
```

市场售卖时使用实例词条计算价值；没有实例信息的旧武器按无词条处理。这样旧存档和静态武器都能兼容。

## NPC 与好感需求

新增通用条件 `WeaponAffixRequirement`：

- `affix_id`
- `min_tier`
- `required_count`
- `match_mode`：拥有全部、拥有任一、至少满足数量

角色、任务和 NPC 需求都引用这个条件。角色收礼时的匹配顺序为：

1. 先匹配 `character_gift_rule` 的具体 Item 规则。
2. 再匹配词条条件规则。
3. 再按 Item 礼物 Tag 计算普通好感。

词条规则可覆盖基础好感、触发特殊对话和消耗规则。若没有匹配到词条规则，则保持现有礼物逻辑。

## 边界

- 词条不得进入 `PlayerProgression`、战斗属性聚合或主角武器伤害计算。
- 词条只能存在于实例，不修改 `ItemData` 的静态 Item 语义。
- NPC/任务条件读取实例词条；堆叠武器必须保持实例隔离，不能把带词条武器合并成普通堆。
- 市集挂牌记录必须持久化实例信息，售出或取消挂牌时一并清理。
