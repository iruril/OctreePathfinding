using PriorityQueue;
using System;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public class PathfindingContext
    {
        public float[] f, g, h;
        public Node[] from;
        public bool[] closed;
        public int[] stamp;
        private int currentStamp = 1; 
        public PriorityQueue<Node, float> openQueue;

        public PathfindingContext(int nodeCount)
        {
            f = new float[nodeCount];
            g = new float[nodeCount];
            h = new float[nodeCount];
            from = new Node[nodeCount];
            closed = new bool[nodeCount];
            stamp = new int[nodeCount]; 
            openQueue = new PriorityQueue<Node, float>(nodeCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginNewSearch()
        {
            currentStamp++;
            if(currentStamp == int.MaxValue)
            {
                Array.Fill(stamp, 0);
                currentStamp = 0;
            }
            openQueue.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive(int id) => stamp[id] == currentStamp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Activate(int id)
        {
            stamp[id] = currentStamp;
            f[id] = float.MaxValue;
            g[id] = float.MaxValue;
            h[id] = 0f;
            closed[id] = false;
            from[id] = null;
        }
    }
}
