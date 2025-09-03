using System;

namespace XGame.Core
{
    /// <summary>
    /// 池对象
    /// </summary>
    /// <remarks>
    /// 对象池不直接管理实际对象，而是管理池对象，池对象中再包含实际对象。
    /// </remarks>
    public sealed class PoolObject : ICache
    {
        internal Action OnSpawn;
        internal Action OnUnspawn;
        internal Action OnDiscard;

        /// <summary>
        /// 实际管理的对象
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// 是否被锁定
        /// </summary>
        /// <remarks>
        /// 锁定的对象即使引用计数为 0 也不会被任何形式的自动丢弃机制释放，而是一直保留在对象池中，直到手动解锁。
        /// </remarks>
        public bool Locked { get; internal set; }

        /// <summary>
        /// 上次使用时间
        /// </summary>
        public DateTime LastUseUtcTime { get; internal set; }

        /// <summary>
        /// 引用计数
        /// </summary>
        public int ReferenceCount { get; internal set; }

        /// <summary>
        /// 是否正在使用
        /// </summary>
        public bool IsInUse
        {
            get => ReferenceCount > 0;
        }

        internal static PoolObject Create(object target, bool locked = false)
        {
            PoolObject poolObject = CachePool.Spawn<PoolObject>();
            poolObject.Target = target ?? throw new ArgumentNullException(nameof(target), "Target can not be null.");
            poolObject.Locked = locked;
            poolObject.LastUseUtcTime = DateTime.UtcNow;
            poolObject.ReferenceCount = 0;
            return poolObject;
        }

        /// <summary>
        /// 借出
        /// </summary>
        internal PoolObject Spawn()
        {
            ReferenceCount++;
            LastUseUtcTime = DateTime.UtcNow;
            OnSpawn?.Invoke();
            return this;
        }

        /// <summary>
        /// 归还
        /// </summary>
        internal void Unspawn()
        {
            OnUnspawn?.Invoke();
            LastUseUtcTime = DateTime.UtcNow;
            ReferenceCount--;
            if (ReferenceCount < 0)
            {
                throw new InvalidOperationException("SpawnCount can not be negative.");
            }
        }

        internal void Destroy()
        {
            OnDiscard?.Invoke();
            CachePool.Unspawn(this);
        }

        public void Clear()
        {
            OnSpawn = null;
            OnUnspawn = null;
            OnDiscard = null;
            Target = null;
            Locked = false;
            LastUseUtcTime = default;
            ReferenceCount = 0;
        }
    }
}
