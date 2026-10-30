# UGS.Battle

与具体玩法解耦的通用战斗框架。

## 特性

- **玩法无关**：包内不出现任何具体游戏的名词（生命 / 法力 / 骑兵 / 盾牌…），属性、效果、行为全部基于可扩展的 key/id。
- **可扩展**：属性公式、效果行为、技能动作、目标选择器、伤害处理器、AI 战术全部为接口 + 注册表。
- **场景无关**：`Realtime` / `RoundBased` 两种 Tick 模式共用同一套逻辑。
- **确定性**：内置 `BattleRandom`（xorshift64*）与显式执行顺序，支持回放与后台补算。
- **逻辑/表现分离**：运行时只产出数据与事件，不触碰 GameObject / 动画 / UI。
- **零分配目标**：热路径遵循数组化、注册期预排序、池化、空容器早退等约束。

## 快速开始

```csharp
// 1. 启动期：定义属性（资源型属性用 isResource 声明）+ 注册扩展
var hp     = AttributeRegistry.Define("hp", 100f, 0f, isResource: true);
var attack = AttributeRegistry.Define("attack", 0f, 0f);

// 2. 创建会话
var session = new BattleSession(new BattleRules
{
    TickMode = BattleTickMode.Realtime,
    RandomSeed = 12345,
});
session.Factions.CreateFaction(1);
session.Factions.CreateFaction(2);
session.Factions.SetRelation(1, 2, FactionRelation.Hostile);

// 3. 注册单位（实现 ICombatUnit）
session.RegisterUnit(1, myKnight);
session.RegisterUnit(2, enemyArcher);

// 4. 推进
session.Start();
session.SetReady();
session.Tick(deltaTime);

// 5. 订阅事件（表现层）
session.Events.Subscribe<DamageDealtEvent>(e => ShowFloatText(e.Request.Target, e.Result.FinalAmount));
```

## 目录

| 目录 | 内容 |
|------|------|
| `Runtime/Core` | 时钟、确定性随机、调度器、Slot 表、缓冲池、扩展点注册表、空间索引 |
| `Runtime/Attributes` | 属性定义、修正器、公式、属性容器 |
| `Runtime/Effects` | 效果定义、实例、容器、行为接口与内置行为 |
| `Runtime/Skills` | 技能定义、运行时、动作、目标选择器、触发器 |
| `Runtime/Combat` | 伤害请求/结果/上下文、伤害管线与内置处理器 |
| `Runtime/Units` | `ICombatUnit`、资源容器、召唤接口 |
| `Runtime/Factions` | 阵营关系图与查询 |
| `Runtime/Sessions` | 战斗会话、阶段、规则、胜利条件、快照 |
| `Runtime/AI` | 战术接口与调度器 |
| `Runtime/Events` | 事件总线与标准事件 |

## 文档

详见 [Documentation/设计文档.md](Documentation/设计文档.md)。

## 约定

- `Runtime` 不引用 `UnityGameFramework`，也不引用任何游戏程序集。
- 战斗逻辑中禁止使用 `UnityEngine.Random` / `System.Random`，统一走 `BattleRandom`。
- 热路径禁止 LINQ、闭包、字符串拼接（详见设计文档第 16 章）。
