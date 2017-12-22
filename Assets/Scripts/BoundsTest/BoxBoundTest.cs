using Unitilities;
using UnityEngine;

namespace Demo
{
    [RequireComponent(typeof(BoxCollider))]
    public class BoxBoundTest : MonoBehaviour
    {
        private BoxCollider _boxCollider;
        private BoxCollider boxCollider => _boxCollider ?? (_boxCollider = GetComponent<BoxCollider>());

        private void OnDrawGizmos()
        {
            var bounds = new Bounds(boxCollider.center, boxCollider.size);
            var matrix = transform.localToWorldMatrix;
            var worldBounds = MatrixUtils.LocalToWorld(ref bounds, ref matrix);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}