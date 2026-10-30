using System.Collections.Generic;
using UGS.Battle.Sessions;
using UnityEngine;

namespace UGS.Battle.AI
{
    /// <summary>AI 产出的单位指令基类。包内只做决策，不执行移动/攻击。</summary>
    public abstract class UnitCommand
    {
        public int UnitId;
    }

    public sealed class AttackCommand : UnitCommand
    {
        public int TargetUnitId;
    }

    public sealed class MoveCommand : UnitCommand
    {
        public Vector2 Destination;
    }

    public sealed class CastSkillCommand : UnitCommand
    {
        public int SkillId;
        public int TargetUnitId;
    }

    public sealed class HoldCommand : UnitCommand
    {
    }

    public sealed class RetreatCommand : UnitCommand
    {
        public Vector2 Destination;
    }

    /// <summary>战术输出通道。维护"已被占用"的单位集合，避免多战术争抢。</summary>
    public interface ITacticOutput
    {
        void Issue(UnitCommand command);

        bool IsClaimed(int unitId);
    }

    /// <summary>
    /// 战术。按 <see cref="Priority"/> 降序执行，高优先级先占用单位。
    /// </summary>
    public interface ITactic
    {
        string Name { get; }

        int Priority { get; }

        int FactionId { get; }

        void Execute(BattleSession session, float deltaTime, ITacticOutput output);
    }
}
