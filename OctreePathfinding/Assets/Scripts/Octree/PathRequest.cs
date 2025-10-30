using UnityEngine;

namespace Octrees
{
    public class PathRequest
    {
        public OctreeNode startNode;
        public OctreeNode endNode;
        public OctreeAgent agent;

        public PathRequest(OctreeNode startNode, OctreeNode endNode, OctreeAgent agent)
        {
            this.startNode = startNode;
            this.endNode = endNode;
            this.agent = agent;
        }
    }
}
