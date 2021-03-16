﻿/*MIT License

Copyright(c) 2018 Vili Volčini / viliwonka

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections.Generic;

namespace DataStructures.ViliWonka.Heap {

    public class MaxHeap : BaseHeap {

        public MaxHeap(int initialSize = 2048) : base(initialSize) {


        }

        public override void PushValue(float h) {

            // if heap array is full
            if(nodesCount == maxSize) {

                UpsizeHeap();
            }

            nodesCount++;
            heap[nodesCount] = h;
            BubbleUpMax(nodesCount);
        }

        public override float PopValue() {

            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            float result = heap[1];

            heap[1] = heap[nodesCount];
            nodesCount--;
            BubbleDownMax(1);

            return result;
        }
    }

    // generic version
    public class MaxHeap<T> : MaxHeap {

        T[] objs; // objects
        private Dictionary<T, int> currentIndices;

        public MaxHeap(int maxNodes) : base(maxNodes) {
            objs = new T[maxNodes + 1];
            currentIndices = new Dictionary<T, int>(maxNodes);
        }

        public T     HeadHeapObject { get { return objs[1]; } }

        T tempObjs;
        protected override void Swap(int A, int B) {

            tempHeap = heap[A];
            tempObjs = objs[A];

            heap[A] = heap[B];
            objs[A] = objs[B];
            currentIndices[objs[A]] = A;

            heap[B] = tempHeap;
            objs[B] = tempObjs;
            currentIndices[objs[B]] = B;
        }

        public void BubbleUp(T obj)
        {
            BubbleUpMax(currentIndices[obj]);
        }

        public void BubbleDown(T obj)
        {
            BubbleDownMax(currentIndices[obj]);
        }

        public void SetValue(T obj, float h)
        {
            if(currentIndices.ContainsKey(obj))
            {
                var index = currentIndices[obj];
                var previous = heap[index];
                heap[index] = h;
                if (h > previous) BubbleUpMax(index);
                else BubbleDownMax(index);
            }
        }

        public override void PushValue(float h) {
            throw new System.ArgumentException("Use PushObj(T, float)!");
        }

        public override float PopValue() {
            throw new System.ArgumentException("Use Push(T, float)!");
        }

        public void PushObj(T obj, float h) {

            // if heap array is full
            if(nodesCount == maxSize) {
                UpsizeHeap();
            }

            nodesCount++;
            heap[nodesCount] = h;
            objs[nodesCount] = obj;
            currentIndices[obj] = nodesCount;

            BubbleUpMax(nodesCount);
        }

        public T PopObj() {

            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            T result = objs[1];
            currentIndices.Remove(result);

            heap[1] = heap[nodesCount];
            objs[1] = objs[nodesCount];
            currentIndices[objs[1]] = 1;

            objs[nodesCount] = default(T);

            nodesCount--;
            BubbleDownMax(1);

            return result;
        }


        public T PopObj(ref float heapValue) {

            if(nodesCount == 0)
                throw new System.ArgumentException("Heap is empty!");

            heapValue = heap[1];
            T result = PopObj();

            return result;
        }

        protected override void UpsizeHeap() {

            maxSize *= 2;
            System.Array.Resize(ref heap, maxSize + 1);
            System.Array.Resize(ref objs, maxSize + 1);
        }

        //flush internal results, returns ordered data
        public void FlushResult(List<T> resultList, List<float> heapList = null) {

            int count = nodesCount + 1;

            if(heapList == null) {

                for(int i = 1; i < count; i++) {
                    resultList.Add(PopObj());
                }
            }
            else {

                float h = 0f;

                for(int i = 1; i < count; i++) {
                    resultList.Add(PopObj(ref h));
                    heapList.Add(h);
                }
            }
        }

        public IEnumerable<T> FlushResult()
        {
            int count = nodesCount + 1;
            for(int i = 1; i < count; i++) {
                yield return PopObj();
            }
        }
    }
}