using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU
{
    public class FixedQueue<T> : IEnumerable<T>
    {
        private Queue<T> m_queue;

        public int Count => m_queue.Count;

        public int Capacity { get; private set; }

        public FixedQueue(int capacity)
        {
            Capacity = capacity;
            m_queue = new Queue<T>(capacity);
        }

        public void Enqueue(T item)
        {
            m_queue.Enqueue(item);

            if (Count > Capacity) Dequeue();
        }

        public void Clear() => m_queue.Clear();

        public T Dequeue() => m_queue.Dequeue();

        public T Peek() => m_queue.Peek();

        public T[] ToArray() => m_queue.ToArray();

        public IEnumerator<T> GetEnumerator() => m_queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_queue.GetEnumerator();
    }

    public class CashTransform
    {
        public Vector3 LocalPosiiton { get => m_localPosition; }

        public Vector3 LocalScale { get => m_localScale; }

        public Quaternion LocalRotation { get => m_localRotation; }

        public CashTransform(Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
        {
            m_localPosition = localPosition;
            m_localRotation = localRotation;
            m_localScale = localScale;
        }

        private Vector3 m_localPosition;
        private Vector3 m_localScale;
        private Quaternion m_localRotation;
    }
}
