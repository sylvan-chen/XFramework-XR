using System;
using System.Collections.Generic;

namespace XGame.Core
{
    public abstract class StateMachineBase
    {
        internal abstract void Update(float deltaTime, float unscaleDeltaTime);
        internal abstract void Shutdown();
    }

    /// <summary>
    /// 状态机
    /// </summary>
    /// <typeparam name="T">状态机的所有者类型</typeparam>
    public sealed class StateMachine<T> : StateMachineBase, ICache where T : class
    {
        private readonly Dictionary<Type, StateBase<T>> _stateDict = new();
        private string _name;
        private T _owner;
        private StateBase<T> _currentState;
        private float _currentStateTime = 0f;
        private bool _isDestroyed = false;

        /// <summary>
        /// 状态机名称
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// 状态机所有者
        /// </summary>
        public T Owner
        {
            get { return _owner; }
        }

        /// <summary>
        /// 状态机的状态数量
        /// </summary>
        public int StateCount
        {
            get { return _stateDict.Count; }
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public StateBase<T> CurrentState
        {
            get { return _currentState; }
        }

        /// <summary>
        /// 当前状态已持续时间
        /// </summary>
        /// <remarks>
        /// 单位：秒，切换时重置为 0。
        /// </remarks>
        public float CurrentStateTime
        {
            get { return _currentStateTime; }
        }

        public bool IsDestroyed
        {
            get { return _isDestroyed; }
        }

        internal static StateMachine<T> Create(string name, T owner, params StateBase<T>[] states)
        {
            var fsm = CachePool.Spawn<StateMachine<T>>();
            fsm._name = name ?? throw new ArgumentNullException(nameof(name), $"Create StateMachine failed. Name cannot be null.");
            fsm._owner = owner ?? throw new ArgumentNullException(nameof(owner), $"Create StateMachine failed. Owner cannot be null.");
            foreach (StateBase<T> state in states)
            {
                if (state == null)
                {
                    throw new ArgumentNullException(nameof(states), $"Create StateMachine failed. The state in initial states cannot be null.");
                }
                if (fsm._stateDict.ContainsKey(state.GetType()))
                {
                    throw new ArgumentException($"Create StateMachine failed. The state of type {state.GetType().FullName} is already created.", nameof(states));
                }
                fsm._stateDict.Add(state.GetType(), state);
                state.OnInit(fsm);
            }
            fsm._isDestroyed = false;
            return fsm;
        }

        internal override void Update(float deltaTime, float unscaleDeltaTime)
        {
            if (!CheckStarted() || _isDestroyed)
            {
                return;
            }
            _currentStateTime += unscaleDeltaTime;
            _currentState.OnUpdate(this, deltaTime, unscaleDeltaTime);
        }

        internal override void Shutdown()
        {
            _currentState?.OnExit(this);
            foreach (StateBase<T> state in _stateDict.Values)
            {
                state.OnShutdown(this);
                state.Destroy();
            }
            _isDestroyed = true;
            CachePool.Unspawn(this);
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState">启动时的状态类型</typeparam>
        public void Startup<TState>() where TState : StateBase<T>
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Start StateMachine {Name} failed. It has already been destroyed.");
            }
            if (CheckStarted())
            {
                throw new InvalidOperationException($"Start StateMachine {Name} failed. It has already been started, don't start it again.");
            }

            if (_stateDict.TryGetValue(typeof(TState), out StateBase<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Start StateMachine {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
            }
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <param name="startStateType">启动时的状态类型</param>
        public void Startup(Type startStateType)
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Start StateMachine {Name} failed. It has already been destroyed.");
            }
            if (CheckStarted())
            {
                throw new InvalidOperationException($"Start StateMachine {Name} failed. It has already been started, don't start it again.");
            }
            CheckTypeCompilance(startStateType);

            if (_stateDict.TryGetValue(startStateType, out StateBase<T> state))
            {
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Start StateMachine {Name} failed. State of type {startStateType.FullName} not found.", nameof(startStateType));
            }
        }

        public TState GetState<TState>() where TState : StateBase<T>
        {
            if (_stateDict.TryGetValue(typeof(TState), out StateBase<T> state))
            {
                return state as TState ?? throw new InvalidOperationException($"Get state of StateMachine {Name} failed.");
            }
            return null;
        }

        public StateBase<T> GetState(Type stateType)
        {
            CheckTypeCompilance(stateType);

            if (_stateDict.TryGetValue(stateType, out StateBase<T> state))
            {
                return state;
            }
            return null;
        }

        public bool HasState<TState>() where TState : StateBase<T>
        {
            return _stateDict.ContainsKey(typeof(TState));
        }

        public bool HasState(Type stateType)
        {
            CheckTypeCompilance(stateType);

            return _stateDict.ContainsKey(stateType);
        }

        public void ChangeState<TState>() where TState : StateBase<T>
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Change state of StateMachine {Name} failed. The FSM has already been destroyed.");
            }
            if (!CheckStarted())
            {
                throw new InvalidOperationException($"Change state of StateMachine {Name} failed. The FSM didn't start yet.");
            }
            if (_stateDict.TryGetValue(typeof(TState), out StateBase<T> state))
            {
                _currentState.OnExit(this);
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Change state of StateMachine {Name} failed. State of type {typeof(TState).FullName} not found.", nameof(TState));
            }
        }

        public void ChangeState(Type stateType)
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Change state of StateMachine {Name} failed. The FSM has already been destroyed.");
            }
            if (!CheckStarted())
            {
                throw new InvalidOperationException($"Change state of StateMachine {Name} failed. The FSM didn't start yet.");
            }
            CheckTypeCompilance(stateType);

            if (_stateDict.TryGetValue(stateType, out StateBase<T> state))
            {
                _currentState.OnExit(this);
                _currentState = state;
                _currentStateTime = 0;
                _currentState.OnEnter(this);
            }
            else
            {
                throw new ArgumentException($"Change state of StateMachine {Name} failed. State of type {stateType.FullName} not found.", nameof(stateType));
            }
        }

        public StateBase<T>[] GetAllStates()
        {
            if (_isDestroyed)
            {
                throw new InvalidOperationException($"Get all states of StateMachine {Name} failed. The StateMachine has already been destroyed.");
            }
            if (_stateDict.Count == 0)
            {
                return new StateBase<T>[0];
            }
            var result = new StateBase<T>[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        public void Clear()
        {
            _stateDict.Clear();
            _name = null;
            _owner = null;
            _currentState = null;
            _currentStateTime = 0f;
        }

        private bool CheckStarted()
        {
            return _currentState != null;
        }

        private void CheckTypeCompilance(Type type)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type), $"Check type complience of FSM {Name} failed. State type cannot be null.");
            }
            if (!type.IsClass || type.IsAbstract)
            {
                throw new ArgumentException($"Check type complience of StateMachine {Name} failed. State type {type.FullName} must be a non-abstract class.", nameof(type));
            }
            if (!typeof(StateBase<T>).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Check type complience of StateMachine {Name} failed. State type {type.FullName} must be a subclass of {typeof(StateBase<T>).Name}.", nameof(type));
            }
#endif
        }
    }
}