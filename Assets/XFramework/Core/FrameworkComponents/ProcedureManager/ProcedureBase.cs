namespace XGame.Core
{
    /// <summary>
    /// 所有流程的基类
    /// </summary>
    /// <remarks>
    /// 流程实际上就是一系列的状态。
    /// </remarks>
    public abstract class ProcedureBase : StateBase<ProcedureManager>
    {
        /// <summary>
        /// 流程初始化时
        /// </summary>
        /// <param name="fsm">流程管理器的状态机</param>
        public override void OnInit(StateMachine<ProcedureManager> fsm)
        {
            base.OnInit(fsm);
        }

        /// <summary>
        /// 进入流程时
        /// </summary>
        /// <param name="fsm">流程管理器的状态机</param>
        public override void OnEnter(StateMachine<ProcedureManager> fsm)
        {
            base.OnEnter(fsm);
            Log.Debug($"[Procedure] Enter {GetType().Name}...");
        }

        /// <summary>
        /// 离开流程时
        /// </summary>
        /// <param name="fsm">流程管理器的状态机</param>
        public override void OnExit(StateMachine<ProcedureManager> fsm)
        {
            base.OnExit(fsm);
            Log.Debug($"[Procedure] Exit {GetType().Name}...");
        }

        /// <summary>
        /// 流程销毁时
        /// </summary>
        /// <param name="fsm">流程管理器的状态机</param>
        public override void OnShutdown(StateMachine<ProcedureManager> fsm)
        {
            base.OnShutdown(fsm);
        }

        /// <summary>
        /// 流程更新时
        /// </summary>
        /// <param name="fsm">流程管理器的状态机</param>
        /// <param name="logicSeconds">逻辑时间</param>
        /// <param name="realSeconds">真实时间</param>
        public override void OnUpdate(StateMachine<ProcedureManager> fsm, float logicSeconds, float realSeconds)
        {
            base.OnUpdate(fsm, logicSeconds, realSeconds);
        }
    }
}