using Unitilities;
using UnityEngine;

namespace Demo
{
    public class SphereCastBoundsTest : MonoBehaviour
    {
        private SphereCollider[] _sphereColliders;
        private SphereCollider[] sphereColliders => _sphereColliders ?? (_sphereColliders = GetComponentsInChildren<SphereCollider>());

        private void Reset()
        {
            int spheres = GetComponentsInChildren<SphereCollider>().Length;
            for (int i = 0; i < 2 - spheres; i++)
                new GameObject("Sphere", typeof(SphereCollider)).transform.SetParent(transform);
        }

        private void OnDrawGizmos()
        {
            var origin = sphereColliders[0].transform.position + sphereColliders[0].center;
            var destination = sphereColliders[1].transform.position + sphereColliders[1].center;
            var direction = destination - origin;
            float radius = sphereColliders[0].radius;
            float maxDistance = direction.magnitude;
            direction = direction.normalized;
            
            var bounds1 = new Bounds(origin, radius * 2f * Vector3.one);
            var bounds2 = new Bounds(origin + direction * maxDistance, radius * 2f * Vector3.one);
            
            bounds1.Encapsulate(bounds2);
            Gizmos.DrawWireCube(bounds1.center, bounds1.size);
        }
    }
}