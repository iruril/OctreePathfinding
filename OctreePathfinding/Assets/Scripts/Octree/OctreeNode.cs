using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class OctreeNode
    {
        public List<OctreeObject> objects = new();

        static int nextId;
        public readonly int id;

        public Bounds bounds;
        Bounds[] childBounds = new Bounds[8];
        public OctreeNode[] children;
        public bool IsLeaf => children == null;

        float minNodeSize;

        public OctreeNode(Bounds bounds, float minNodeSize)
        {
            this.bounds = bounds;
            this.minNodeSize = minNodeSize;
            Vector3 newSize = this.bounds.size * 0.5f; //halved size
            Vector3 centerOffset = this.bounds.size * 0.25f; //quater offset
            Vector3 parentCenter = this.bounds.center;

            for(int i = 0; i < 8; i++)
            {
                Vector3 childCenter = parentCenter;
                childCenter.x += centerOffset.x * ((i & 1) == 0 ? -1 : 1);
                childCenter.y += centerOffset.y * ((i & 2) == 0 ? -1 : 1);
                childCenter.z += centerOffset.z * ((i & 4) == 0 ? -1 : 1);
                childBounds[i] = new Bounds(childCenter, newSize);
            }
        }

        public void DrawNode()
        {
            Gizmos.color = Color.cyan;
            foreach(var childBound in childBounds)
            {
                Gizmos.DrawWireCube(childBound.center, childBound.size * 0.95f);
            }
        }
    }
}
