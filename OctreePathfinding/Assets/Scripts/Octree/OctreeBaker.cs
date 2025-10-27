using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        GameObject[] objects;
        public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph waypoints = new();

        private void Awake()
        {
            objects = GameObject.FindGameObjectsWithTag("Level");
            ot = new Octree(objects, minNodeSize, waypoints);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            ot.root.DrawNode();
            ot.graph.DrawGraph();
        }
    }
}
