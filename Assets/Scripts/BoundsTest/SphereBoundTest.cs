using Unitilities;
using UnityEngine;

namespace Demo
{
    [RequireComponent(typeof(SphereCollider))]
    public class SphereBoundTest : MonoBehaviour
    {
        private SphereCollider _sphereCollider;
        private SphereCollider sphereCollider => _sphereCollider ?? (_sphereCollider = GetComponent<SphereCollider>());

        private void OnDrawGizmos()
        {
            var bounds = new Bounds(sphereCollider.center, sphereCollider.radius * 2f * Vector3.one);
            var matrix = transform.localToWorldMatrix;
            var worldBounds = MatrixUtils.LocalToWorld(ref bounds, ref matrix);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}