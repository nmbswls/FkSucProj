# 通用品级（ECommonGrade）

项目级通用品级枚举，供入梦路人、掉落、展示色等复用。  
**不是**料理专用的 `ECookingRarity`（获取难度语义不同）。

| 值 | 名 | 说明 |
|---|---|---|
| 0 | None | 未指定 |
| 1 | Common | 凡品 |
| 2 | Uncommon | 良品 |
| 3 | Rare | 稀品 |
| 4 | Epic | 上品 |
| 5 | Legendary | 极品 |

入梦 `dream_passerby.grade` 使用本枚举。后续其它系统若需通用品级，优先引用此处，勿再平行发明一套。
