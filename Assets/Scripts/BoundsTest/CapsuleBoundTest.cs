using Unitilities;
using UnityEngine;

namespace Demo
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class CapsuleBoundTest : MonoBehaviour
    {
        private CapsuleCollider _capsuleCollider;
        private CapsuleCollider capsuleCollider => _capsuleCollider ?? (_capsuleCollider = GetComponent<CapsuleCollider>());

        private void OnDrawGizmos()
        {
            var bounds = new Bounds(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2f, capsuleCollider.height, capsuleCollider.radius * 2f));
            var matrix = transform.localToWorldMatrix;
            var worldBounds = MatrixUtils.LocalToWorld(ref bounds, ref matrix);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}