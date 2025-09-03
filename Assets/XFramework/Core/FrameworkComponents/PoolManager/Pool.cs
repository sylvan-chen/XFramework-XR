using System;
using System.Collections.Generic;

namespace XGame.Core
{
    public abstract class PoolBase
    {
        public abstract Type ObjectType { get; }
        public abstract bool AllowMultiReference { get; }
        public abstract int Capacity { get; set; }
        public abstract float ObjectExpiredTime { get; set; }
        public abstract float AutoClearInterval { get; set; }
        public abstract float AutoClearTimer { get; }
        public abstract int Count { get; }

        internal abstract void Update(float deltaTime, float unscaledDeltaTime);
        internal abstract void Destroy();

        public abstract PoolObjectInfo[] GetAllPoolObjectInfos();
        public abstract void Squeeze();
        public abstract void DicardAllUnused();
        public abstract void DiscardAllExpired();
    }

    public sealed class Pool<T> : PoolBase where T : class
    {
        private readonly bool _allowMultiReference;        // 是否允许多重引用
        private int _capacity;                             // 容量
        private float _objectExpiredTime;                  // 对象过期时间
        private float _autoClearInterval;                  // 自动清理间隔
        private float _autoClearTimer = 0f;
        private readonly Dictionary<T, PoolObject> _poolObjectDict = new();
        private readonly List<PoolObject> _cachedDiscardablePoolObjects = new();

        public Pool(bool allowMultiReference, int capacity, float objectExpiredTime, float autoClearInterval)
        {
            _allowMultiReference = allowMultiReference;
            _capacity = capacity;
            _objectExpiredTime = objectExpiredTime;
            _autoClearInterval = autoClearInterval;
        }

        public override Type ObjectType
        {
            get => typeof(T);
        }

        public override bool AllowMultiReference
        {
            get => _allowMultiReference;
        }

        public override float AutoClearInterval
        {
            get => _autoClearInterval;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentException("Set AutoSqueezeInterval failed. AutoSqueezeInterval must be greater than or equal to 0.", nameof(value));
                }
                _autoClearInterval = value;
            }
        }

        public override float AutoClearTimer
        {
            get => _autoClearTimer;
        }

        public override float ObjectExpiredTime
        {
            get => _objectExpiredTime;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentException("Set PoolObjectSurvivalTime failed. PoolObjectSurvivalTime must be greater than or equal to 0.", nameof(value));
                }
                _objectExpiredTime = value;
            }
        }

        public override int Capacity
        {
            get => _capacity;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Set Capacity failed. Capacity must be greater than or equal to 0.", nameof(value));
                }
                _capacity = value;
                Squeeze();
            }
        }

        public override int Count
        {
            get => _poolObjectDict.Count;
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            _autoClearTimer += unscaledDeltaTime;
            if (_autoClearTimer >= _autoClearInterval)
            {
                DiscardAllExpired();
                _autoClearTimer = 0f;
            }
        }

        internal override void Destroy()
        {
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                poolObject.Destroy();
            }
        }

        /// <summary>
        /// 注册一个对象到池中
        /// </summary>
        public void Register(T target, Action<T> onSpawn = null, Action<T> onUnspawn = null, Action<T> onDiscard = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "TargetObject cannot be null.");
            }
            PoolObject poolObject = PoolObject.Create(target);
            poolObject.OnSpawn = onSpawn == null ? null : () => onSpawn?.Invoke(target);
            poolObject.OnUnspawn = onUnspawn == null ? null : () => onUnspawn?.Invoke(target);
            poolObject.OnDiscard = onDiscard == null ? null : () => onDiscard?.Invoke(target);
            poolObject.ReferenceCount = 1;
            _poolObjectDict.Add(target, poolObject);
        }

        public override PoolObjectInfo[] GetAllPoolObjectInfos()
        {
            PoolObjectInfo[] poolObjectInfos = new PoolObjectInfo[_poolObjectDict.Count];
            int index = 0;
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                poolObjectInfos[index++] = new PoolObjectInfo
                (
                    poolObject.Locked,
                    poolObject.IsInUse,
                    poolObject.ReferenceCount,
                    poolObject.LastUseUtcTime.ToLocalTime()
                );
            }
            return poolObjectInfos;
        }

        public T Spawn()
        {
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                if (_allowMultiReference || !poolObject.IsInUse)
                {
                    return poolObject.Spawn().Target as T;
                }
            }
            return null;
        }

        public void Unspawn(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }

            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Unspawn();
                if (Count > Capacity && poolObject.ReferenceCount <= 0)
                {
                    Squeeze();
                }
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Unspawn failed. Target not found in pool.");
            }
        }

        public void Lock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Locked = true;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] Lock failed. Target not found in pool.");
            }
        }

        public void Unlock(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                poolObject.Locked = false;
            }
            else
            {
                Log.Error($"[XFramework] [Pool<{typeof(T).Name}>] UnLock failed. Target not found in pool.");
            }
        }

        public bool Discard(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target), "Target cannot be null.");
            }
            // 通过 target 反向获取对应的池对象
            if (_poolObjectDict.TryGetValue(target, out PoolObject poolObject))
            {
                return Discard(poolObject);
            }
            return false;
        }


        internal bool Discard(PoolObject poolObject)
        {
            if (poolObject == null)
            {
                throw new ArgumentNullException(nameof(poolObject), "PoolObject cannot be null.");
            }

            if (poolObject.IsInUse || poolObject.Locked)
            {
                return false;
            }

            _poolObjectDict.Remove(poolObject.Target as T);
            poolObject.Destroy();
            return true;
        }

        /// <summary>
        /// 清理对象池中所有未使用的对象
        /// </summary>
        public override void DicardAllUnused()
        {
            UpdateDiscardablePoolObjectsWithoutExpiredCheck();
            foreach (PoolObject poolObject in _cachedDiscardablePoolObjects)
            {
                Discard(poolObject);
            }
            _cachedDiscardablePoolObjects.Clear();
        }

        /// <summary>
        /// 清理对象池中所有过期的对象
        /// </summary>
        public override void DiscardAllExpired()
        {
            UpdateDiscardablePoolObjects();
            foreach (PoolObject poolObject in _cachedDiscardablePoolObjects)
            {
                Discard(poolObject);
            }
            _cachedDiscardablePoolObjects.Clear();
        }

        /// <summary>
        /// 收缩，使得池中对象数量不要超出容量限制
        /// </summary>
        public override void Squeeze()
        {
            int discardCount = Count - Capacity;
            if (discardCount <= 0)
            {
                return;
            }
            UpdateDiscardablePoolObjectsWithoutExpiredCheck();
            _cachedDiscardablePoolObjects.Sort((a, b) => b.LastUseUtcTime.CompareTo(a.LastUseUtcTime));
            for (int i = 0; i < _cachedDiscardablePoolObjects.Count; i++)
            {
                Discard(_cachedDiscardablePoolObjects[i]);
            }
            _cachedDiscardablePoolObjects.Clear();
        }

        /// <summary>
        /// 更新可丢弃对象缓存列表，包括未使用、未锁定且过期的对象
        /// </summary>
        /// <returns></returns>
        private void UpdateDiscardablePoolObjects()
        {
            _cachedDiscardablePoolObjects.Clear();
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                if (poolObject.IsInUse || poolObject.Locked)
                {
                    continue;
                }
                double remainingTime = (poolObject.LastUseUtcTime - DateTime.MinValue).TotalSeconds + _objectExpiredTime;
                // 如果过期时间为无穷大，则认为该对象永不过期
                if (remainingTime.CompareTo(float.MaxValue) >= 0)
                {
                    continue;
                }
                DateTime expiredTime = poolObject.LastUseUtcTime.AddSeconds(_objectExpiredTime);
                if (DateTime.UtcNow > expiredTime)
                {
                    _cachedDiscardablePoolObjects.Add(poolObject);
                }
            }
        }

        /// <summary>
        /// 更新可丢弃对象缓存列表，包括未使用、未锁定的对象，不检查过期时间
        /// </summary>
        private void UpdateDiscardablePoolObjectsWithoutExpiredCheck()
        {
            _cachedDiscardablePoolObjects.Clear();
            foreach (PoolObject poolObject in _poolObjectDict.Values)
            {
                if (poolObject.IsInUse || poolObject.Locked)
                {
                    continue;
                }
                _cachedDiscardablePoolObjects.Add(poolObject);
            }
        }

    }
}