using System.Collections;
using System.Collections.Generic;

namespace XGame.Core
{
    public sealed partial class XLinkedList<T> : ICollection<T>, ICollection
    {
        /// <summary>
        /// 对 LinkedList<T>.Enumerator 的封装
        /// </summary>
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private LinkedList<T>.Enumerator _enumerator;

            internal Enumerator(LinkedList<T> linkedList)
            {
                _enumerator = linkedList!.GetEnumerator();
            }

            public T Current
            {
                get { return _enumerator.Current; }
            }

            readonly object IEnumerator.Current
            {
                get { return (_enumerator as IEnumerator).Current; }
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            readonly void IEnumerator.Reset()
            {
                (_enumerator as IEnumerator).Reset();
            }
        }
    }
}