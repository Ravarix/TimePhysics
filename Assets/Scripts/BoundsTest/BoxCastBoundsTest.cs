using Unitilities;
using UnityEngine;

namespace Demo
{
    public class BoxCastBoundsTest : MonoBehaviour
    {
        private BoxCollider[] _boxColliders;
        private BoxCollider[] boxColliders => _boxColliders ?? (_boxColliders = GetComponentsInChildren<BoxCollider>());

        private void Reset()
        {
            int boxs = GetComponentsInChildren<BoxCollider>().Length;
            for (int i = 0; i < 2 - boxs; i++)
                new GameObject("Box", typeof(BoxCollider)).transform.SetParent(transform);
        }

        private void OnDrawGizmos()
        {
//            var bounds1 = new Bounds(boxColliders[0].center, boxColliders[0].size);
//            var matrix1 = boxColliders[0].transform.localToWorldMatrix;
//            var worldBounds1 = CoreMatrixUtils.LocalToWorld(ref bounds1, ref matrix1);
//            
//            var bounds2 = new Bounds(boxColliders[1].center, boxColliders[1].size);
//            var matrix2 = boxColliders[1].transform.localToWorldMatrix;
//            var worldBounds2 = CoreMatrixUtils.LocalToWorld(ref bounds2, ref matrix2);

            var worldBounds1 = boxColliders[0].bounds;
            var worldBounds2 = boxColliders[1].bounds;
            
            worldBounds1.Encapsulate(worldBounds2);
            Gizmos.DrawWireCube(worldBounds1.center, worldBounds1.size);
        }
    }
}