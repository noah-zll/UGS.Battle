namespace UGS.Battle.Core
{
    /// <summary>
    /// 确定性随机源（xorshift64*）。
    /// 战斗逻辑中禁止使用 UnityEngine.Random / System.Random，统一走本类。
    /// </summary>
    public sealed class BattleRandom
    {
        private const ulong DefaultSeed = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        public BattleRandom(ulong seed)
        {
            _state = seed == 0UL ? DefaultSeed : seed;
        }

        /// <summary>当前内部状态，用于快照。</summary>
        public ulong State => _state;

        public void Restore(ulong state)
        {
            _state = state == 0UL ? DefaultSeed : state;
        }

        public ulong NextULong()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>[0,1) 浮点。</summary>
        public float NextFloat()
        {
            return (NextULong() >> 40) * (1f / (1 << 24));
        }

        /// <summary>[0,maxExclusive) 整数。</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>[minInclusive,maxExclusive) 整数。</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        /// <summary>[min,max) 浮点。</summary>
        public float Range(float min, float max)
        {
            if (max <= min) return min;
            return min + NextFloat() * (max - min);
        }

        /// <summary>以给定概率返回 true。</summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }
    }
}
