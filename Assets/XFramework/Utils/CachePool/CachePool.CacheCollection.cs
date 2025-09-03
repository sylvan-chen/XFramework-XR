using System;
using System.Collections.Generic;

namespace XGame.Core
{
    public static partial class CachePool
    {
        private class CacheCollection
        {
            private readonly Queue<ICache> _cache = new();

            public CacheCollection(Type cacheType)
            {
                CacheType = cacheType;
                UsingCount = 0;
                SpawnedCount = 0;
                UnspawnedCount = 0;
                CreatedCount = 0;
                DiscardedCount = 0;
            }

            public Type CacheType { get; private set; }

            public int UnusedCount
            {
                get { return _cache.Count; }
            }

            public int UsingCount { get; private set; }

            public int SpawnedCount { get; private set; }

            public int UnspawnedCount { get; private set; }

            public int CreatedCount { get; private set; }

            public int DiscardedCount { get; private set; }

            /// <summary>
            /// 从池中拿出一个缓存，如果池中没有则创建一个新的缓存
            /// </summary>
            public ICache Spawn()
            {
                SpawnedCount++;
                UsingCount++;
                lock (_cache)
                {
                    if (_cache.Count > 0)
                    {
                        return _cache.Dequeue();
                    }
                }
                CreatedCount++;
                return Activator.CreateInstance(CacheType) as ICache;
            }

            /// <summary>
            /// 放入一个缓存
            /// </summary>
            /// <param name="cache">将要放入的缓存</param>
            public void Unspawn(ICache cache)
            {
                if (cache == null)
                {
                    return;
                }
                if (_cache.Contains(cache))
                {
                    Log.Warning($"[XFramework] [CachePool] Unspawn cache failed. Cache {cache.GetType().Name} already exists in the pool.");
                    return;
                }

                cache.Clear();
                lock (_cache)
                {
                    _cache.Enqueue(cache);
                }
                UnspawnedCount++;
                UsingCount--;
            }

            /// <summary>
            /// 预先创建一部分缓存
            /// </summary>
            /// <param name="count">将要预留的缓存数量</param>
            public void Reserve(int count)
            {
                lock (_cache)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ICache newInstance = Activator.CreateInstance(CacheType) as ICache;
                        if (newInstance == null)
                        {
                            Log.Error($"[XFramework] [ReferencePool] Reserve reference failed. Reference type {CacheType.Name} is invalid.");
                            continue;
                        }
                        CreatedCount++;
                        _cache.Enqueue(newInstance);
                    }
                }
            }

            /// <summary>
            /// 丢弃一部分缓存
            /// </summary>
            /// <param name="count">将要丢弃的缓存数量</param>
            public void Discard(int count)
            {
                lock (_cache)
                {
                    if (count > _cache.Count)
                    {
                        count = _cache.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        _cache.Dequeue();
                        DiscardedCount++;
                    }
                }
            }

            /// <summary>
            /// 丢弃所有缓存
            /// </summary>
            public void DiscardAll()
            {
                lock (_cache)
                {
                    DiscardedCount += _cache.Count;
                    _cache.Clear();
                }
            }
        }
    }
}