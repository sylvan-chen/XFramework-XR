using System;
using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 流程管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Procedure Manager")]
    public sealed class ProcedureManager : FrameworkComponent
    {
        private readonly ProcedureManagerSetting _setting;

        private StateMachine<ProcedureManager> _procedureStateMachine;
        private ProcedureBase _startupProcedure;

        public ProcedureBase CurrentProcedure => _procedureStateMachine?.CurrentState as ProcedureBase;
        public float CurrentProcedureTime => _procedureStateMachine?.CurrentStateTime ?? 0;

        public ProcedureManager(ProcedureManagerSetting setting)
        {
            _setting = setting;
        }

        internal override void Init()
        {
            base.Init();

            ProcedureBase[] procedures = new ProcedureBase[_setting.AvailableProcedureTypeNames.Length];
            // 注册所有流程为状态
            for (int i = 0; i < _setting.AvailableProcedureTypeNames.Length; i++)
            {
                string typeName = _setting.AvailableProcedureTypeNames[i];
                Type type = TypeHelper.GetTypeDeeply(typeName) ?? throw new InvalidOperationException($"ProcedureManager init failed. Type '{typeName}' not found.");
                procedures[i] = Activator.CreateInstance(type) as ProcedureBase;
                if (typeName == _setting.StartupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                throw new InvalidOperationException($"ProcedureManager init failed. Startup procedure '{_setting.StartupProcedureTypeName}' not found or failed to initialize.");
            }

            _procedureStateMachine = M.StateMachineManager.CreateFsm(this, procedures);
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            M.StateMachineManager.ShutdownFsm<ProcedureManager>();
            _procedureStateMachine = null;
            _startupProcedure = null;
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        public void StartProcedure()
        {
            _procedureStateMachine.Startup(_startupProcedure.GetType());
        }

        public T GetProcedure<T>() where T : ProcedureBase
        {
            return _procedureStateMachine.GetState<T>();
        }

        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return _procedureStateMachine.HasState<T>();
        }
    }
}