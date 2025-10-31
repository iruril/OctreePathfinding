using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class PathfindingContextPool
    {
        private readonly Stack<PathfindingContext> _pool = new();
        private readonly object _lock = new();
        private int nodeCount = 0;

        public void Init(int nodeCount, int initCount)
        {
            _pool.Clear(); 
            this.nodeCount = nodeCount;
            for (int i = 0; i < initCount; i++)
            {
                _pool.Push(new PathfindingContext(nodeCount));
            }
        }

        public PathfindingContext Rent()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                    return _pool.Pop();
                else
                {
                    PathfindingContext ctx = new PathfindingContext(nodeCount);
                    _pool.Push(ctx);
                    return _pool.Pop();
                }
            }
        }

        public void Return(PathfindingContext ctx)
        {
            lock (_lock)
            {
                _pool.Push(ctx);
            }
        }
    }
}
