namespace UGS.Battle.Core
{
    /// <summary>
    /// 战斗时钟。所有计时（冷却、Buff 持续、DoT 间隔）都基于此，
    /// 而非 UnityEngine.Time，以保证暂停 / 倍速 / 回放一致。
    /// </summary>
    public sealed class BattleClock
    {
        /// <summary>累计战斗时间（秒）。</summary>
        public float Elapsed { get; private set; }

        /// <summary>本帧步长（已乘以 TimeScale）。</summary>
        public float DeltaTime { get; private set; }

        /// <summary>时间缩放。0 等价于暂停。</summary>
        public float TimeScale { get; set; } = 1f;

        /// <summary>是否暂停。</summary>
        public bool Paused { get; set; }

        /// <summary>推进时钟。</summary>
        public void Advance(float realDeltaTime)
        {
            if (Paused || TimeScale <= 0f || realDeltaTime <= 0f)
            {
                DeltaTime = 0f;
                return;
            }

            DeltaTime = realDeltaTime * TimeScale;
            Elapsed += DeltaTime;
        }

        /// <summary>直接推进指定步长（RoundBased 批量结算使用）。</summary>
        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                DeltaTime = 0f;
                return;
            }

            DeltaTime = deltaTime;
            Elapsed += deltaTime;
        }

        /// <summary>快照/回放用。</summary>
        public void Restore(float elapsed)
        {
            Elapsed = elapsed;
            DeltaTime = 0f;
        }
    }
}
