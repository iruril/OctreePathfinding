using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        GameObject[] objects;
        public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph waypoints = new();
        [SerializeField] private bool drawNodeGizmos = false;
        [SerializeField] private bool drawPathGizmos = false;

        private void Awake()
        {
            Debug.Log("Finding Levels...");
            objects = GameObject.FindGameObjectsWithTag("Level");
            ot = new Octree(objects, minNodeSize, waypoints);
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            if(drawNodeGizmos) ot.root.DrawNode();
            if(drawPathGizmos) ot.graph.DrawGraph();
        }
    }
}