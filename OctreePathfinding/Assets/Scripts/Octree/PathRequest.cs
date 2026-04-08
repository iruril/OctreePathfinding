using UnityEngine;

namespace Octrees
{
    public readonly struct PathRequest
    {
        public readonly OctreeNode startNode;
        public readonly OctreeNode endNode;
        public readonly OctreeAgent agent;

        public PathRequest(OctreeNode startNode, OctreeNode endNode, OctreeAgent agent)
        {
            this.startNode = startNode;
            this.endNode = endNode;
            this.agent = agent;
        }
    }
}
