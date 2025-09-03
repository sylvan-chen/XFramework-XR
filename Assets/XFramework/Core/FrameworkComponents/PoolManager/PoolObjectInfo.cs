using System;

namespace XGame.Core
{
    public readonly struct PoolObjectInfo
    {
        public readonly bool Locked;
        public readonly bool IsInUse;
        public readonly int ReferenceCount;
        public readonly DateTime LastUseTime;

        public PoolObjectInfo(bool locked, bool isInUse, int referenceCount, DateTime lastUseTime)
        {
            Locked = locked;
            IsInUse = isInUse;
            ReferenceCount = referenceCount;
            LastUseTime = lastUseTime;
        }
    }
}