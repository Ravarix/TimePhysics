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
        private readonly List<DebugHit> _tempList = new List<DebugHit>(32);
        
        public struct DebugHit : IEquatable<DebugHit>
        {
            public Matrix4x4 LocalToWorld;
            public readonly HitboxMarkerDebug HitboxMarkerDebug;
            public int Frame;

            public DebugHit(Matrix4x4 localToWorld, HitboxMarkerDebug hitboxMarkerDebug, int frame)
            {
                LocalToWorld = localToWorld;
                HitboxMarkerDebug = hitboxMarkerDebug;
                Frame = frame;
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
                return other.GetHashCode() == GetHashCode() && other.Frame == Frame;
            }
        }

        public void DebugHitRewind(HitboxMarkerDebug markerDebug, float duration)
        {
            DebugHits[new DebugHit(
                markerDebug.Trans.localToWorldMatrix,
                markerDebug,
                TimePhysics.WorldFrame)] = duration;
        }
        
        private void OnDrawGizmos()
        {
            foreach (var kvp in DebugHits)
                HitboxBodyDebug.DrawMarkerGizmo(kvp.Key.LocalToWorld, kvp.Key.HitboxMarkerDebug, DebugHitColor, false);
            
            _tempList.Clear();
            foreach (var kvp in DebugHits)
                _tempList.Add(kvp.Key);

            var dt = Time.deltaTime;
            foreach (var key in _tempList)
            {
                var time = DebugHits[key];
                time -= dt;
                if (time <= 0)
                    DebugHits.Remove(key);
                else
                    DebugHits[key] = time;
            }
            
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
        }
    }
}