using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class PathfindingContextPool
    {
        private readonly ConcurrentStack<PathfindingContext> _pool = new();
        private int nodeCount = 0;
        private int MaxContexts = 8;

        public void Init(int nodeCount, int initCount)
        {
            this.nodeCount = nodeCount;
            MaxContexts = initCount;

            while (_pool.TryPop(out _)) { }

            for (int i = 0; i < initCount; i++)
                _pool.Push(new PathfindingContext(nodeCount));
        }

        public PathfindingContext Rent()
        {
            if (_pool.TryPop(out var ctx))
                return ctx;

            return new PathfindingContext(nodeCount);
        }

        public void Return(PathfindingContext ctx)
        {
            if (_pool.Count < MaxContexts)
                _pool.Push(ctx);
        }
    }
}
