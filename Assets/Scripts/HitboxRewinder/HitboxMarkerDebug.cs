using System;
using Unitilities;
using UnityEngine;

namespace Hitbox
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class HitboxMarkerDebug : MonoBehaviour
    {
        public enum Shape
        {
            Unknown = 0,
            Box,
            Sphere
        }

        //DAT CA$HE
        public Collider Collider { get; private set; }
        public Shape shape { get; private set; }
        public BoxCollider BoxCollider { get; private set; }
        public SphereCollider SphereCollider { get; private set; }
        public Transform Trans;

        private void Awake()
        {
            Trans = transform;
            Collider = GetComponent<Collider>();
            var box = Collider as BoxCollider;
            if (box != null)
            {
                shape = Shape.Box;
                BoxCollider = box;
            }
            var sphere = Collider as SphereCollider;
            if (sphere != null)
            {
                shape = Shape.Sphere;
                SphereCollider = sphere;
            }
        }
    }
}
