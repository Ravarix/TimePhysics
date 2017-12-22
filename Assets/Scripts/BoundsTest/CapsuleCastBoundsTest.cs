using Unitilities;
using UnityEngine;

namespace Demo
{
    public class CapsuleCastBoundsTest : MonoBehaviour
    {
        private SphereCollider[] _points;
        private SphereCollider[] points => _points ?? (_points = GetComponentsInChildren<SphereCollider>());

        private void Reset()
        {
            int spheres = GetComponentsInChildren<SphereCollider>().Length;
            for (int i = 0; i < 4 - spheres; i++)
                new GameObject("Point", typeof(SphereCollider)).transform.SetParent(transform);
        }

        private void OnDrawGizmos()
        {

            var point1 = points[0].transform.position + points[0].center;
            var point2 = points[1].transform.position + points[1].center;
            var radius = points[0].radius;
            var direction = points[2].transform.position - points[0].transform.position;
            var maxDistance = direction.magnitude;
            direction = direction.normalized;

            var pointDelta = point2 - point1;
            var bounds1 = new Bounds(point1 + pointDelta * .5f, 
                new Vector3(
                    Mathf.Abs(pointDelta.x) + radius * 2f, 
                    Mathf.Abs(pointDelta.y) + radius * 2f, 
                    Mathf.Abs(pointDelta.z) + radius * 2f));

            var endPoint1 = point1 + direction * maxDistance;
            var endPoint2 = point2 + direction * maxDistance;

            points[3].transform.position = endPoint2 - points[3].center;
            
            var bounds2 = new Bounds(
                endPoint1 + pointDelta * .5f, 
                bounds1.size);
            
            bounds1.Encapsulate(bounds2);
            Gizmos.DrawWireCube(bounds1.center, bounds1.size);
        }
    }
}