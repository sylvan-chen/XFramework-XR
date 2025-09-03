using System;

namespace XGame.Core
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 延迟事件包装类
        /// </summary>
        private class DelayEventWrapper : ICache
        {
            public IEvent Event;
            public EventHandlerChain HandlerChain;
            public float DelaySeconds;

            public static DelayEventWrapper Create(IEvent evt, EventHandlerChain handlerChain, float delaySeconds)
            {
                var wrapper = CachePool.Spawn<DelayEventWrapper>();
                wrapper.Event = evt ?? throw new ArgumentNullException(nameof(evt), "Spawn DelayEventWrapper failed. Args is null.");
                wrapper.HandlerChain = handlerChain ?? throw new ArgumentNullException(nameof(handlerChain), "Spawn DelayEventWrapper failed. HandlerChain is null.");
                wrapper.DelaySeconds = delaySeconds;
                return wrapper;
            }

            public void Destroy()
            {
                CachePool.Unspawn(this);
            }

            public void Clear()
            {
                Event.Destroy();
                Event = null;
                HandlerChain = null;
                DelaySeconds = 0;
            }
        }
    }
}