using System;
using System.Collections;
using System.Collections.Generic;

namespace XGame.Core
{
    /// <summary>
    /// 对 C# LinkedList 的封装，增加了节点缓存机制，减少内存分配和 GC 以提升性能
    /// </summary>
    public sealed partial class XLinkedList<T> : ICollection<T>, ICollection
    {
        private readonly LinkedList<T> _linkedList;
        private readonly Queue<LinkedListNode<T>> _nodePool;

        public XLinkedList()
        {
            _linkedList = new LinkedList<T>();
            _nodePool = new Queue<LinkedListNode<T>>();
        }

        public int Count
        {
            get { return _linkedList.Count; }
        }

        public LinkedListNode<T> First
        {
            get { return _linkedList.First; }
        }

        public LinkedListNode<T> Last
        {
            get { return _linkedList.Last; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return (_linkedList as ICollection<T>).IsReadOnly; }
        }

        bool ICollection.IsSynchronized
        {
            get { return (_linkedList as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (_linkedList as ICollection).SyncRoot; }
        }

        void ICollection<T>.Add(T value)
        {
            AddLast(value);
        }

        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddAfter(node, newNode);
            return newNode;
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            _linkedList.AddAfter(node, newNode);
        }

        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddBefore(node, newNode);
            return newNode;
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            _linkedList.AddBefore(node, newNode);
        }

        public LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddFirst(newNode);
            return newNode;
        }

        public void AddFirst(LinkedListNode<T> node)
        {
            _linkedList.AddFirst(node);
        }

        public LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> newNode = AcquireNode(value);
            _linkedList.AddLast(newNode);
            return newNode;
        }

        public void AddLast(LinkedListNode<T> node)
        {
            _linkedList.AddLast(node);
        }

        public void Clear()
        {
            LinkedListNode<T> current = _linkedList.First;
            while (current is not null)
            {
                ReleaseNode(current);
                current = current.Next;
            }
            _linkedList.Clear();
        }

        /// <summary>
        /// 包括节点池缓存在内的完全清除
        /// </summary>
        public void ClearEntirely()
        {
            Clear();
            _nodePool.Clear();
        }

        public bool Contains(T value)
        {
            return _linkedList.Contains(value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _linkedList.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            (_linkedList as ICollection).CopyTo(array, index);
        }

        /// <summary>
        /// 查找第一个包含指定值的节点
        /// </summary>
        /// <param name="value">要查找的值</param>
        /// <returns>第一个包含指定值的节点</returns>
        public LinkedListNode<T> Find(T value)
        {
            return _linkedList.Find(value);
        }

        /// <summary>
        /// 查找最后一个包含指定值的节点
        /// </summary>
        /// <param name="value">要查找的值</param>
        /// <returns>最后一个包含指定值的节点</returns>
        public LinkedListNode<T> FindLast(T value)
        {
            return _linkedList.FindLast(value);
        }

        public bool Remove(T value)
        {
            LinkedListNode<T> node = _linkedList.Find(value);
            if (node is null)
            {
                return false;
            }
            ReleaseNode(node);
            _linkedList.Remove(node);
            return true;
        }

        public void Remove(LinkedListNode<T> node)
        {
            ReleaseNode(node);
            _linkedList.Remove(node);
        }

        public void RemoveFirst()
        {
            LinkedListNode<T> node = _linkedList.First;
            ReleaseNode(node);
            _linkedList.RemoveFirst();
        }

        public void RemoveLast()
        {
            LinkedListNode<T> node = _linkedList.Last;
            ReleaseNode(node);
            _linkedList.RemoveLast();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_linkedList);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 尝试从缓存中获取节点，如果缓存中没有节点，则创建一个新的节点
        /// </summary>
        /// <param name="value">节点值</param>
        /// <returns></returns>
        private LinkedListNode<T> AcquireNode(T value)
        {
            if (_nodePool.Count > 0)
            {
                LinkedListNode<T> node = _nodePool.Dequeue();
                node.Value = value;
                return node;
            }
            else
            {
                return new LinkedListNode<T>(value);
            }
        }

        /// <summary>
        /// 释放节点到缓存中
        /// </summary>
        private void ReleaseNode(LinkedListNode<T> node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node), $"ReleaseNode Failed: Node {nameof(node)} is null.");
            }
            node.Value = default;
            _nodePool.Enqueue(node);
        }
    }
}