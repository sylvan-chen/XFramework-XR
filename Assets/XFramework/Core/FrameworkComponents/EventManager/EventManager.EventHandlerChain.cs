using System;

namespace XGame.Core
{
    public sealed partial class EventManager
    {
        /// <summary>
        /// 事件委托链
        /// </summary>
        private class EventHandlerChain : ICache
        {
            // 用链表实现事件委托链而不是直接用 +=
            private readonly XLinkedList<Action<IEvent>> _handlers = new();

            public static EventHandlerChain Create()
            {
                EventHandlerChain handlerChain = CachePool.Spawn<EventHandlerChain>();
                return handlerChain;
            }

            public void Destroy()
            {
                CachePool.Unspawn(this);
            }

            public int Count
            {
                get { return _handlers.Count; }
            }

            public void AddHandler(Action<IEvent> handler)
            {
                _handlers.AddLast(handler);
            }

            public void RemoveHandler(Action<IEvent> handler)
            {
                _handlers.Remove(handler);
            }

            public void Fire(IEvent evt)
            {
                foreach (Action<IEvent> handler in _handlers)
                {
                    handler?.Invoke(evt);
                }
            }

            public void Clear()
            {
                _handlers.Clear();
            }
        }
    }
}