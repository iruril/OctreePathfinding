using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public static class PathfindingContextPool
    {
        private static readonly Stack<PathfindingContext> _pool = new();
        private static readonly object _lock = new();
        private const int MaxContexts = 20;

        public static PathfindingContext Rent(int nodeCount)
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                    return _pool.Pop();
                return new PathfindingContext(nodeCount);
            }
        }

        public static void Return(PathfindingContext ctx)
        {
            lock (_lock)
            {
                if (_pool.Count < MaxContexts)
                    _pool.Push(ctx);
            }
        }
    }
}
