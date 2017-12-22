using UnityEngine;

namespace Hitbox
{
    public class HitboxSnapshot
    {
        public Bounds ProximityBounds;
        public Matrix4x4[] LocalToWorld;
        public bool Real;

        public HitboxSnapshot (int numHitboxes)
        {
            ProximityBounds = new Bounds();
            LocalToWorld = new Matrix4x4[numHitboxes];
        }
    }  
}
