using System.Collections.Generic;
using UGS.Battle.Sessions;

namespace UGS.Battle.AI
{
    /// <summary>
    /// 单位指令的执行器。包只负责产出 <see cref="UnitCommand"/>，具体怎么执行由宿主实现。
    /// </summary>
    /// <remarks>
    /// 为什么放在宿主侧：把 <c>MoveCommand</c> 变成一次寻路、把 <c>AttackCommand</c> 变成一次状态切换，
    /// 必然要碰引擎的导航、动画、状态机，这些包里既没有也不该有。
    /// <para>
    /// 契约（实现方必须遵守）：
    /// <list type="bullet">
    /// <item><b>每轮一次</b>：每次战术结算结束回调一次，<b>包括空列表</b>。
    /// 空列表表示"本轮没有任何战术下达命令"，宿主应据此让上一轮的命令失效；</item>
    /// <item><b>列表只在回调期间有效</b>：包内复用同一个 <c>List</c>，下一轮会先 <c>Clear</c>。
    /// 需要跨帧保留的命令必须自行拷贝；</item>
    /// <item><b>不阻塞</b>：回调发生在会话 Tick 内，实现里不要做重活，也不要再次调用会话 Tick。</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICommandExecutor
    {
        /// <summary>消费本轮命令。</summary>
        /// <param name="session">产出这批命令的会话。</param>
        /// <param name="commands">本轮命令；仅回调期间有效，可能为空。</param>
        void Execute(BattleSession session, IReadOnlyList<UnitCommand> commands);
    }
}
