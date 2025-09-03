using System;

namespace XGame.Core
{
    public readonly struct CacheCollectionInfo
    {
        /// <summary>
        /// 缓存类型
        /// </summary>
        public readonly Type CacheType;
        /// <summary>
        /// 未使用缓存数量
        /// </summary>
        public readonly int UnusedCount;
        /// <summary>
        /// 使用中的缓存数量
        /// </summary>
        public readonly int UsingCount;
        /// <summary>
        /// 借出缓存次数
        /// </summary>
        public readonly int SpawnedCount;
        /// <summary>
        /// 归还缓存次数
        /// </summary>
        public readonly int UnspawnedCount;
        /// <summary>
        /// 创建缓存次数
        /// </summary>
        public readonly int CreatedCount;
        /// <summary>
        /// 丢弃缓存次数
        /// </summary>
        public readonly int DiscardedCount;

        public CacheCollectionInfo(Type cacheType, int unusedCount, int usingCount, int spawnedCount, int unspawnedCount, int createdCount, int discardedCount)
        {
            CacheType = cacheType;
            UnusedCount = unusedCount;
            UsingCount = usingCount;
            SpawnedCount = spawnedCount;
            UnspawnedCount = unspawnedCount;
            CreatedCount = createdCount;
            DiscardedCount = discardedCount;
        }
    }
}