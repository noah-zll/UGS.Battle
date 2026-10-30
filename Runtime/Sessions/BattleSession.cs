using System;
using System.Collections.Generic;
using UGS.Battle.AI;
using UGS.Battle.Combat;
using UGS.Battle.Combat.Processors;
using UGS.Battle.Core;
using UGS.Battle.Events;
using UGS.Battle.Factions;
using UGS.Battle.Skills;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Sessions
{
    /// <summary>
    /// 一场战斗的生命周期。纯逻辑对象，不依赖 MonoBehaviour，
    /// Realtime / RoundBased 两种推进模式共用同一套结算逻辑。
    /// </summary>
    public sealed class BattleSession
    {
        private static int _nextSessionId;

        private readonly SlotTable<ICombatUnit> _units = new SlotTable<ICombatUnit>(32);
        private readonly List<SimpleSkillState> _skills = new List<SimpleSkillState>(16);
        private readonly List<UnitCommand> _commands = new List<UnitCommand>(64);
        private readonly List<ICombatUnit> _tempUnits = new List<ICombatUnit>(64);

        private float _roundAccumulator;

        public int SessionId { get; }

        public BattleRules Rules { get; }

        public BattlePhase Phase { get; private set; } = BattlePhase.Created;

        public BattleClock Clock { get; }

        public BattleRandom Random { get; }

        public BattleEventBus Events { get; }

        public FactionSystem Factions { get; }

        public DamagePipeline Damage { get; }

        public BattleScheduler Scheduler { get; }

        public TacticScheduler Tactics { get; }

        public BattleServiceRegistry Services { get; }

        public SlotTable<ICombatUnit> UnitTable => _units;

        /// <summary>全部单位（数组含 null 空洞，遍历需判空）。</summary>
        public IReadOnlyList<ICombatUnit> Units => _units.Raw;

        public IReadOnlyList<SimpleSkillState> Skills => _skills;

        /// <summary>本帧产出的 AI 指令（供表现层消费）。</summary>
        public IReadOnlyList<UnitCommand> Commands => _commands;

        /// <summary>
        /// 命令执行器。包只产出 <see cref="UnitCommand"/>，怎么执行由宿主决定。
        /// 为 null 时不回调（默认行为，与没有战术产出完全一致）。
        /// </summary>
        /// <remarks>
        /// 注册后**每轮必回调一次**，空列表也会回调 —— 这是宿主判定"上一轮命令失效"的唯一信号。
        /// 回调里拿到的列表由包复用，下一轮会被清空，需要跨帧保留必须自行拷贝。
        /// </remarks>
        public ICommandExecutor CommandExecutor { get; set; }

        public event Action<BattlePhase, BattlePhase> PhaseChanged;

        public event Action<BattleEndReason, int> Ended;

        public BattleSession(BattleRules rules = null, BattleServiceRegistry services = null, int sessionId = 0)
        {
            SessionId = sessionId != 0 ? sessionId : ++_nextSessionId;
            Rules = rules ?? new BattleRules();
            Services = services ?? BattleServiceRegistry.Default;

            Clock = new BattleClock();
            Random = new BattleRandom(Rules.RandomSeed != 0UL ? Rules.RandomSeed : (ulong)SessionId);
            Events = new BattleEventBus(this);
            Factions = new FactionSystem(this);
            Damage = new DamagePipeline();
            Scheduler = new BattleScheduler();
            Tactics = new TacticScheduler();

            if (!Rules.SkipDefaultProcessors) RegisterDefaultProcessors();
            RegisterDefaultLayers();
        }

        // ---------------- 生命周期 ----------------

        public void Start()
        {
            if (Phase != BattlePhase.Created) return;
            TransitionTo(BattlePhase.Preparation);
        }

        public void SetReady()
        {
            if (Phase != BattlePhase.Preparation) return;
            TransitionTo(BattlePhase.InProgress);
        }

        public void Tick(float realDeltaTime)
        {
            if (Phase != BattlePhase.Preparation && Phase != BattlePhase.InProgress) return;

            Clock.Advance(realDeltaTime);
            if (Clock.DeltaTime > 0f)
            {
                if (Rules.TickMode == BattleTickMode.Realtime) Scheduler.Tick(Clock);
                else TickRoundBased(Clock.DeltaTime);
            }

            EvaluateVictory();
            Events.Flush();
        }

        public void Abort() => End(BattleEndReason.Aborted, -1);

        public void End(BattleEndReason reason, int winnerFactionId)
        {
            if (Phase == BattlePhase.Ended) return;

            TransitionTo(BattlePhase.Resolving);
            Ended?.Invoke(reason, winnerFactionId);

            Events.Publish(new BattleEndedEvent
            {
                Reason = reason,
                WinnerFactionId = winnerFactionId,
            });

            TransitionTo(BattlePhase.Ended);
            Events.Flush();
        }

        private void TransitionTo(BattlePhase next)
        {
            if (Phase == next) return;

            BattlePhase previous = Phase;
            Phase = next;

            PhaseChanged?.Invoke(previous, next);
            Events.Publish(new BattlePhaseChangedEvent { From = previous, To = next });
        }

        // ---------------- 注册 ----------------

        public int RegisterUnit(int factionId, ICombatUnit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            Factions.AddUnit(factionId, unit);
            return _units.Add(unit);
        }

        public bool UnregisterUnit(ICombatUnit unit)
        {
            if (unit == null) return false;

            for (int i = 0; i < _units.Capacity; i++)
            {
                if (ReferenceEquals(_units.Get(i), unit))
                {
                    _units.Remove(i);
                    break;
                }
            }

            return Factions.RemoveUnit(unit);
        }

        public void RegisterSkill(SimpleSkillState runtime)
        {
            if (runtime == null) throw new ArgumentNullException(nameof(runtime));
            if (!_skills.Contains(runtime)) _skills.Add(runtime);
        }

        public bool UnregisterSkill(SimpleSkillState runtime) => _skills.Remove(runtime);

        // ---------------- 查询 ----------------

        public int QueryHostileInRange(int factionId, Vector2 center, float radius, List<ICombatUnit> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            if (radius <= 0f) return 0;

            float radiusSqr = radius * radius;
            ISpatialIndex index = Rules.SpatialIndex;

            if (index != null && index.Count > 0)
            {
                index.Query(center, radius, -1, _tempUnits);
                for (int i = 0; i < _tempUnits.Count; i++)
                {
                    ICombatUnit unit = _tempUnits[i];
                    if (unit == null || !unit.IsAlive) continue;
                    if (!Factions.IsHostile(factionId, unit.FactionId)) continue;
                    if ((unit.Position - center).sqrMagnitude > radiusSqr) continue;

                    results.Add(unit);
                }

                return results.Count;
            }

            Factions.GetHostileUnits(factionId, _tempUnits);
            for (int i = 0; i < _tempUnits.Count; i++)
            {
                ICombatUnit unit = _tempUnits[i];
                if (unit == null || !unit.IsAlive) continue;
                if ((unit.Position - center).sqrMagnitude > radiusSqr) continue;

                results.Add(unit);
            }

            return results.Count;
        }

        public ICombatUnit FindNearestHostile(int factionId, Vector2 from, float maxRadius = float.MaxValue)
        {
            Factions.GetHostileUnits(factionId, _tempUnits);

            ICombatUnit best = null;
            float bestSqr = maxRadius * maxRadius;

            for (int i = 0; i < _tempUnits.Count; i++)
            {
                ICombatUnit unit = _tempUnits[i];
                if (unit == null || !unit.IsAlive) continue;

                float sqr = (unit.Position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = unit;
                }
            }

            return best;
        }

        // ---------------- 内部推进 ----------------

        private void TickRoundBased(float deltaTime)
        {
            float interval = Rules.RoundInterval > 0f ? Rules.RoundInterval : deltaTime;
            _roundAccumulator += deltaTime;

            int guard = 0;
            while (_roundAccumulator >= interval && guard < 4)
            {
                _roundAccumulator -= interval;
                RunOneRound(interval);
                guard++;
            }

            if (_roundAccumulator >= interval) _roundAccumulator = 0f;
        }

        private void RunOneRound(float step)
        {
            TickEffects(step);
            TickSkills(step);
            TickTactics(step);
        }

        private void TickEffects(float deltaTime)
        {
            if (_units.IsEmpty) return;

            ICombatUnit[] raw = _units.Raw;
            int count = _units.Capacity;

            for (int i = 0; i < count; i++)
            {
                ICombatUnit unit = raw[i];
                if (unit == null) continue;

                unit.Effects.Tick(deltaTime);
            }
        }

        private void TickSkills(float deltaTime)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                SimpleSkillState runtime = _skills[i];
                if (runtime == null || runtime.Caster == null || !runtime.Caster.IsAlive) continue;

                runtime.Tick(deltaTime);
            }
        }

        private void TickTactics(float deltaTime)
        {
            _commands.Clear();
            Tactics.Tick(this, deltaTime, _commands);

            // 命令的消费交给宿主：包只管产出，不管执行。
            // 执行器存在时每轮都回调（空列表也回调），宿主据此判定上一轮命令失效。
            ICommandExecutor executor = CommandExecutor;
            if (executor == null) return;

            executor.Execute(this, _commands);
        }

        private void EvaluateVictory()
        {
            if (Phase != BattlePhase.InProgress && Phase != BattlePhase.Preparation) return;

            IVictoryCondition condition = Rules.VictoryCondition;
            if (condition != null && condition.Evaluate(this, out int winner, out BattleEndReason reason))
            {
                End(reason, winner);
                return;
            }

            if (Rules.MaxDuration > 0f && Clock.Elapsed >= Rules.MaxDuration)
            {
                End(BattleEndReason.Timeout, -1);
            }
        }

        private void RegisterDefaultProcessors()
        {
            Damage.Add(new AccuracyProcessor());
            Damage.Add(new CriticalProcessor());
            Damage.Add(new MitigationProcessor());
            Damage.Add(new ShieldAbsorbProcessor());
            Damage.Add(new ClampProcessor());
            Damage.Add(new ApplyDamageProcessor());
        }

        private void RegisterDefaultLayers()
        {
            Scheduler.Schedule("effects", 0f, 0, TickEffects);
            Scheduler.Schedule("skills", 0f, 0, TickSkills);
            Scheduler.Schedule("tactics", Rules.TacticInterval, 0, TickTactics);
        }

        // ---------------- 快照 ----------------

        public BattleSnapshot CaptureSnapshot()
        {
            return BattleSnapshot.Capture(this);
        }

        public void RestoreSnapshot(BattleSnapshot snapshot)
        {
            snapshot?.RestoreTo(this);
        }
    }
}
