using System;

namespace XGame.Core
{
    /// <summary>
    /// 状态基类
    /// </summary>
    /// <typeparam name="T">状态机所有者的类型</typeparam>
    /// <remarks>
    /// 每一个状态类型代表状态机所有者的一种状态。
    /// </remarks>
    public abstract class StateBase<T> where T : class
    {
        /// <summary>
        /// 初始化状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnInit(StateMachine<T> fsm)
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "OnInit failed. FSM is null.");
            }
        }

        /// <summary>
        /// 进入状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnEnter(StateMachine<T> fsm)
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "OnEnter failed. FSM is null.");
            }
        }

        /// <summary>
        /// 退出状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnExit(StateMachine<T> fsm)
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "OnExit failed. FSM is null.");
            }
        }

        /// <summary>
        /// 更新状态时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        /// <param name="deltaTime">两帧之间的间隔时间</param>
        /// <param name="unscaledDeltaTime">不受时间缩放影响的两帧之间的间隔时间</param>
        public virtual void OnUpdate(StateMachine<T> fsm, float deltaTime, float unscaledDeltaTime)
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "OnUpdate failed. FSM is null.");
            }
        }

        /// <summary>
        /// 状态机关闭时
        /// </summary>
        /// <param name="fsm">所属状态机实例</param>
        public virtual void OnShutdown(StateMachine<T> fsm)
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "OnShutdown failed. FSM is null.");
            }
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="TState">目标状态类型</typeparam>
        /// <param name="fsm">所属状态机实例</param>
        protected virtual void ChangeState<TState>(StateMachine<T> fsm) where TState : StateBase<T>
        {
            if (fsm == null)
            {
                throw new ArgumentNullException(nameof(fsm), "ChangeState failed. FSM is null.");
            }
            fsm.ChangeState<TState>();
        }

        public virtual void Destroy()
        {
            Log.Debug($"[XFramework] [StateBase] Destroy State: {GetType().Name}...");
        }
    }
}