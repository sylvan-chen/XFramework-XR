using System;
using System.Collections.Generic;
using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 状态机管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/StateMachine Manager")]
    public sealed class StateMachineManager : FrameworkComponent
    {
        private readonly Dictionary<int, StateMachineBase> _stateMachines = new();

        private const string DefaultStateMachineName = "default";

        internal override void Init()
        {
            base.Init();
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            foreach (StateMachineBase stateMachine in _stateMachines.Values)
            {
                stateMachine.Shutdown();
            }
            _stateMachines.Clear();
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);

            foreach (StateMachineBase stateMachine in _stateMachines.Values)
            {
                stateMachine.Update(deltaTime, unscaledDeltaTime);
            }
        }

        public StateMachine<T> CreateFsm<T>(T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFsm(DefaultStateMachineName, owner, states.ToArray());
        }

        public StateMachine<T> CreateFsm<T>(T owner, params StateBase<T>[] states) where T : class
        {
            return CreateFsm(DefaultStateMachineName, owner, states);
        }

        public StateMachine<T> CreateFsm<T>(string name, T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFsm(name, owner, states.ToArray());
        }

        public StateMachine<T> CreateFsm<T>(string name, T owner, params StateBase<T>[] states) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Create StateMachine failed. Name cannot be null.");
            }
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Create StateMachine failed. Owner cannot be null.");
            }
            if (states == null || states.Length == 0)
            {
                throw new ArgumentNullException(nameof(states), "Create StateMachine failed. Initial states cannot be null or empty.");
            }
            int id = GetID(typeof(T), name);
            if (_stateMachines.ContainsKey(id))
            {
                throw new InvalidOperationException($"Create StateMachine failed. StateMachine with the same name ({name}) and same owner type ({typeof(T).Name}) already exists.");
            }

            var stateMachine = StateMachine<T>.Create(name, owner, states);
            _stateMachines.Add(id, stateMachine);
            return stateMachine;
        }

        public StateMachine<T> GetFsm<T>() where T : class
        {
            return GetFsm<T>(DefaultStateMachineName);
        }

        public StateMachine<T> GetFsm<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Get StateMachine failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_stateMachines.TryGetValue(id, out StateMachineBase stateMachine))
            {
                return stateMachine as StateMachine<T>;
            }
            return null;
        }

        public void ShutdownFsm<T>() where T : class
        {
            ShutdownFsm<T>(DefaultStateMachineName);
        }

        public void ShutdownFsm<T>(string name) where T : class
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name), "Destroy StateMachine failed. Name cannot be null.");
            }
            int id = GetID(typeof(T), name);
            if (_stateMachines.TryGetValue(id, out StateMachineBase stateMachine))
            {
                stateMachine.Shutdown();
                _stateMachines.Remove(id);
            }
        }

        private int GetID(Type type, string name)
        {
            return (type.Name + name).GetHashCode();
        }
    }
}