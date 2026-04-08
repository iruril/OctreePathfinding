using System;

namespace PriorityQueue
{
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private (TElement Element, TPriority Priority)[] elements;
        private int size;

        public int Count => size;

        // 생성자: 노드 개수를 미리 받아 배열을 할당 (GC 방지)
        public PriorityQueue(int capacity)
        {
            elements = new (TElement, TPriority)[capacity];
            size = 0;
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (size == elements.Length)
            {
                // 공간이 부족하면 2배로 늘림 (일반적으로 노드 최대 개수로 초기화하면 발생하지 않음)
                Array.Resize(ref elements, elements.Length * 2);
            }

            elements[size] = (element, priority);
            MoveUp(size);
            size++;
        }

        public TElement Dequeue()
        {
            if (size == 0) throw new InvalidOperationException("Queue is empty.");

            TElement root = elements[0].Element;
            size--;
            elements[0] = elements[size];
            elements[size] = default; // 참조 해제

            if (size > 0)
            {
                MoveDown(0);
            }

            return root;
        }

        public void Clear()
        {
            Array.Clear(elements, 0, size);
            size = 0;
        }

        private void MoveUp(int index)
        {
            var item = elements[index];
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                var parent = elements[parentIndex];

                if (item.Priority.CompareTo(parent.Priority) >= 0) break;

                elements[index] = parent;
                index = parentIndex;
            }
            elements[index] = item;
        }

        private void MoveDown(int index)
        {
            var item = elements[index];
            while (true)
            {
                int leftChild = index * 2 + 1;
                int rightChild = index * 2 + 2;
                if (leftChild >= size) break;

                int minChild = leftChild;
                if (rightChild < size && elements[rightChild].Priority.CompareTo(elements[leftChild].Priority) < 0)
                {
                    minChild = rightChild;
                }

                if (item.Priority.CompareTo(elements[minChild].Priority) <= 0) break;

                elements[index] = elements[minChild];
                index = minChild;
            }
            elements[index] = item;
        }
    }
}