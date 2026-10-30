# Changelog

## Unreleased

- Skills：新增 `SkillCooldownMode`（`OnCast` / `OnDeactivate` / `Manual`），默认 `OnCast`，与旧行为一致
- Skills：`SkillRuntime` 新增宿主控制接口 `StartCooldown(seconds)`、`SetCooldown(remaining)`、`SetCaster(unit)`、`SetToggled(on)`
- Skills：施法流程调整为先扣资源 → 翻转开关 → 按策略判定冷却（默认路径结果不变）
- Tests：新增 `SkillRuntimeTests` 覆盖三种冷却策略与宿主控制接口

## 0.1.0

首个骨架版本。

- Core：`BattleClock`、`BattleRandom`、`SlotTable<T>`、`BattleBufferPool`、`BattleScheduler`、`BattleServiceRegistry`、`ISpatialIndex` / `UniformGridSpatialIndex`
- Attributes：`AttributeRegistry`、`AttributeDefinition`、`AttributeModifier`、`IAttributeFormula`、`AttributeContainer`
- Effects：`EffectDefinition`、`EffectInstance`、`EffectContainer`、`IEffectBehavior` 及 4 个内置行为
- Combat：`DamageRequest` / `DamageResult` / `DamageContext`、`DamagePipeline` 及 6 个内置处理器
- Units：`ICombatUnit`、`ResourceContainer`、`ResourceCost`、`IUnitSpawner`
- Factions：`FactionSystem`、`FactionRelation`
- Skills：`SkillDefinition`、`SkillRuntime`、4 个动作、4 个目标选择器
- Sessions：`BattleSession`、`BattleRules`、`IVictoryCondition`、`BattleSnapshot`
- AI：`ITactic`、`TacticScheduler`、`UnitCommand`
- Events：`BattleEventBus` 与标准事件
