using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        public GameObject[] objects;
        public float minNodeSize = 1f;
        Octree ot;

        private void Awake() => ot = new Octree(objects, minNodeSize);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            ot.root.DrawNode();
        }
    }
}
