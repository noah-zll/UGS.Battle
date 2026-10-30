using System.Collections.Generic;
using UGS.Battle.Sessions;

namespace UGS.Battle.Combat
{
    /// <summary>
    /// 伤害结算上下文。在管线各处理器之间传递并累积计算结果。
    /// 由 <see cref="DamagePipeline"/> 池化复用。
    /// </summary>
    public sealed class DamageContext
    {
        public BattleSession Session;
        public DamageRequest Request;

        /// <summary>当前累积伤害值（处理器直接修改它）。</summary>
        public float Amount;

        /// <summary>短路标记：置为 true 后不再执行后续处理器。</summary>
        public bool Halted;

        public bool Critical;
        public bool Blocked;
        public bool Missed;
        public bool Killed;

        /// <summary>被护盾吸收的量。</summary>
        public float Absorbed;

        private readonly List<DamageStep> _steps = new List<DamageStep>(6);

        internal void Reset(BattleSession session, in DamageRequest request)
        {
            Session = session;
            Request = request;
            Amount = request.Amount;
            Halted = false;
            Critical = false;
            Blocked = false;
            Missed = false;
            Killed = false;
            Absorbed = 0f;
#if BATTLE_DIAGNOSTICS
            _steps.Clear();
#endif
        }

        public void AddStep(string processor, float before, float after, string note = null)
        {
#if BATTLE_DIAGNOSTICS
            _steps.Add(new DamageStep
            {
                Processor = processor,
                Before = before,
                After = after,
                Note = note,
            });
#endif
        }

        /// <summary>构建对外结果（在管线返回前调用，之后上下文会被回收）。</summary>
        public DamageResult ToResult()
        {
            var result = new DamageResult
            {
                FinalAmount = Amount > 0f ? Amount : 0f,
                Absorbed = Absorbed,
                Blocked = Blocked || Missed,
                Missed = Missed,
                Critical = Critical,
                Killed = Killed,
#if BATTLE_DIAGNOSTICS
                Steps = new List<DamageStep>(_steps),
#else
                Steps = null,
#endif
            };

            return result;
        }
    }
}
