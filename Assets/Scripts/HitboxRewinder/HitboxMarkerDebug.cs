using System;
using Unitilities;
using UnityEngine;

namespace Hitbox
{
    public enum ColliderShape
    {
        Unknown = 0,
        Box,
        Sphere,
        Mesh,
        Capsule,
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class HitboxMarkerDebug : MonoBehaviour
    {

        public static bool RenderCapsulesAsMeshes = false;

        //DAT CA$HE
        public Collider Collider { get; private set; }
        public ColliderShape Shape { get; private set; }
        public BoxCollider BoxCollider { get; private set; }
        public SphereCollider SphereCollider { get; private set; }
        
        public Mesh Mesh { get; private set; }
        public Vector3 Pos { get; private set; }
        public Quaternion Rot { get; private set; }
        public Vector3 Scale { get; private set; }
        
        public Vector3 Point1 { get; private set; }
        public Vector3 Point2 { get; private set; }
        public float Radius { get; private set; }
        public int Direction { get; private set; }
        
        public Transform Trans { get; private set; }

        private void Awake()
        {
            Trans = transform;
            Collider = GetComponent<Collider>();
            if (!Collider)
                return;
            var box = Collider as BoxCollider;
            if (box)
            {
                Shape = ColliderShape.Box;
                BoxCollider = box;
                return;
            }
            
            var sphere = Collider as SphereCollider;
            if (sphere)
            {
                Shape = ColliderShape.Sphere;
                SphereCollider = sphere;
                return;
            }

            var mesh = Collider as MeshCollider;
            if (mesh)
            {
                Shape = ColliderShape.Mesh;
                Mesh = GetComponent<MeshFilter>().sharedMesh;
                Pos = Vector3.zero;
                Rot = Quaternion.identity;
                Scale = Vector3.zero;
                return;
            }

            var capsule = Collider as CapsuleCollider;
            if (capsule)
            {
                Shape = RenderCapsulesAsMeshes ? ColliderShape.Mesh : ColliderShape.Capsule;
                Mesh = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Capsule);
                Pos = capsule.center;
                Direction = capsule.direction;
                Radius = capsule.radius;
                Scale = new Vector3(capsule.radius * 2f, capsule.height * .5f, capsule.radius * 2f);

                var offset = Mathf.Max(capsule.height / 2 - capsule.radius, 0f);
                switch (capsule.direction)
                {
                    case 0: //x-axis
                        Rot = Quaternion.Euler(0f, 0f, 90f);
                        Point1 = capsule.center.Add(x: offset);
                        Point2 = capsule.center.Add(x: -offset);
                        break;
                    case 1: //y-axis
                        Rot = Quaternion.identity;
                        Point1 = capsule.center.Add(y: offset);
                        Point2 = capsule.center.Add(y: -offset);
                        break;
                    case 2: //z-axis
                        Rot = Quaternion.Euler(90f, 0f, 0f);
                        Point1 = capsule.center.Add(z: offset);
                        Point2 = capsule.center.Add(z: -offset);
                        break;
                }
            }
        }
    }
}
