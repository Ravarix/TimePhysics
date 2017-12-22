using System;
using System.Collections.Generic;
using Hitbox;
using Unitilities;
using UnityEngine;

namespace Demo
{
    public class DemoShooterDebug : MonoBehaviour
    {
        public Color DebugHitColor = new Color(1f, 0f, 0f, .5f);
        public Dictionary<DebugHit, float> DebugHits = new Dictionary<DebugHit, float>(512);
        public List<DebugHit> DebugHitsList = new List<DebugHit>();
        public List<DebugHit> tempList = new List<DebugHit>(16);
        
        public struct DebugHit : IEquatable<DebugHit>
        {
            public Matrix4x4 LocalToWorld;
            public readonly HitboxMarkerDebug HitboxMarkerDebug;

            public DebugHit(Matrix4x4 localToWorld, HitboxMarkerDebug hitboxMarkerDebug)
            {
                LocalToWorld = localToWorld;
                HitboxMarkerDebug = hitboxMarkerDebug;
            }

            public override int GetHashCode()
            {
                return HitboxMarkerDebug.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj != null && Equals((DebugHit) obj);
            }

            public bool Equals(DebugHit other)
            {
                return other.GetHashCode() == GetHashCode();
            }
        }

        public void DebugHitRewind(HitboxMarkerDebug markerDebug, float duration)
        {
            DebugHits[new DebugHit(
                markerDebug.Trans.localToWorldMatrix,
                markerDebug)] = duration;

//            DebugHitsList.Add(new DebugHit(
//                markerDebug.Trans.localToWorldMatrix,
//                markerDebug));
        }
        
        private void OnDrawGizmos()
        {
            tempList.Clear();
            foreach (var key in DebugHits.Keys)
                tempList.Add(key);

            var dt = Time.deltaTime;
            foreach (var key in tempList)
            {
                var time = DebugHits[key];
                time -= dt;
                if (time <= 0)
                    DebugHits.Remove(key);
                else
                    DebugHits[key] = time;
            }

            foreach (var hit in DebugHits.Keys)
                HitboxBodyDebug.DrawMarkerGizmo(hit.LocalToWorld, hit.HitboxMarkerDebug, DebugHitColor, false);

//            foreach (var hit in DebugHitsList)
//                HitboxBodyDebug.DrawMarkerGizmo(hit.LocalToWorld, hit.HitboxMarkerDebug, DebugHitColor, false);
//            DebugHitsList.Clear();
            
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
    }
}