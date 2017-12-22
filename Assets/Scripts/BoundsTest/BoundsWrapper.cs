using System.Linq;
using UnityEngine;

namespace Demo
{
    public class BoundsWrapper : MonoBehaviour
    {
        public Collider[] colliders;
        public bool findColliders;

        private void OnValidate()
        {
            if (findColliders)
            {
                findColliders = false;
                colliders = GetComponentsInChildren<Collider>();
            }
        }


        private void OnDrawGizmos()
        {
            if (!(colliders?.Length > 0)) 
                return;
            
            var bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);

            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}