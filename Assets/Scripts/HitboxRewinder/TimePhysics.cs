using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Hitbox
{
    public static class TimePhysics
    {
        // Usually 1 second of snapshots is more than enough
        public const int NumSnapshots = 60;
        // When within this margin of a frame, use that instead of Lerp
        public const float TimeErrorMargin = .001f;
        
        public static int WorldFrame { get; private set; } = -1;
        public static bool IsWorldRewound { get; private set; }
        public static RewindState WorldRewindState { get; private set; }

        private static readonly HashSet<HitboxBody> RegisteredHitboxBodies = new HashSet<HitboxBody>();
        private static readonly List<HitboxBody> RewoundHitboxBodies = new List<HitboxBody>(512);
        
        public const bool DebugMode
        #if UNITY_EDITOR  
            = true;
        #else
            = false;
        #endif

        //disposable access pattern
        public static RewindState RewindWorld(int frame) => new RewindState(frame);
        public static RewindState RewindFrames(int frames) => new RewindState(WorldFrame - frames);
        public static RewindState RewindSeconds(float seconds) => new RewindState(seconds); 
        
        public struct RewindState : IDisposable
        {
            public readonly bool Valid;
            public readonly bool Lerp;
            public readonly int Frame;
            public readonly int Frame2;
            public readonly float LerpVal;

            public RewindState(int frame)
            {
                Frame = frame;
                Lerp = false;
                Frame2 = frame;
                LerpVal = 0f;
                Valid = IsFrameRewindValid(frame);
                
                if(Valid)
                    BeginRewind(this);
                else
                    Debug.LogError("Error rewinding");
            }

            public RewindState(float seconds)
            {
                var lerpFrame = WorldFrame - seconds / Time.fixedDeltaTime;
                Frame = Mathf.FloorToInt(lerpFrame);
                Frame2 = Mathf.CeilToInt(lerpFrame);
                LerpVal = lerpFrame - Frame;
                Lerp = true;
                if (LerpVal <= TimeErrorMargin)
                {
                    Lerp = false;
                }
                if (LerpVal >= 1 - TimeErrorMargin)
                {
                    Frame = Frame2;
                    Lerp = false;
                }
                Valid = Lerp ? IsLerpRewindValid(lerpFrame) : IsFrameRewindValid(Frame);
                
                if(Valid)
                    BeginRewind(this);
                else
                    Debug.LogError("Error rewinding");
            }
            
            private static bool IsFrameRewindValid(int frame) => frame >= WorldFrame - NumSnapshots;
        
            private static bool IsLerpRewindValid(float lerpFrame) => lerpFrame >= WorldFrame - NumSnapshots;
        
            public void Dispose()
            {
                //Restore proximities and any rewound bodies
                if(Valid)
                    Restore();
            }

            public override string ToString()
            {
                if (!Valid)
                    return "INVALID";
                return Lerp ? $"{nameof(Frame)}: {Frame}, {nameof(Frame2)}: {Frame2}, {nameof(LerpVal)}: {LerpVal}" 
                            : $"{nameof(Frame)}: {Frame}";
            }
        }

        [RuntimeInitializeOnLoadMethod] private static void Init() { var go = Clock; } // needed to JIT?
        //shitty cached singleton pattern because we need a GO to get FixedUpdate and start recording.
        //call it a 'Global Behavior' to feel better.
        private static TimePhysicsClock _clock;
        public static TimePhysicsClock Clock =>
            _clock 
            ?? (_clock = Object.FindObjectOfType<TimePhysicsClock>()) 
            ?? (_clock = GetNewInstance()); 
        
        private static TimePhysicsClock GetNewInstance()
        {
            var inst = new GameObject("TimePhysicsClock").AddComponent<TimePhysicsClock>();
            Object.DontDestroyOnLoad(inst);
            return inst;
        }

        public static void TakeSnapshot()
        {            
            WorldFrame++;
            var index = WorldFrame % NumSnapshots;
            
            Profiler.BeginSample("Snapshot World");
            foreach (HitboxBody hb in RegisteredHitboxBodies)
                hb.TakeSnapshot (WorldFrame, index);
            Profiler.EndSample();
        }
        
        public static bool IsFrameValid(int frame)
        {
            return frame > WorldFrame - NumSnapshots && frame <= WorldFrame;
        }

        public static void RegisterHitboxBody (HitboxBody hitBoxBody)
        {
            RegisteredHitboxBodies.Add (hitBoxBody);
        }

        public static void UnregisterHitboxBody (HitboxBody hitBoxBody)
        {
            RegisteredHitboxBodies.Remove (hitBoxBody);
        }
    
        public static bool Raycast(Vector3 position, Vector3 direction, float maxDistance, out RaycastHit raycastHit, LayerMask layerMask)
        {
            RewindRayHits(position, direction, maxDistance);
            return Physics.Raycast(position, direction, out raycastHit, maxDistance, layerMask);
        }
    
        public static int RaycastNonAlloc(Vector3 position, Vector3 direction, float maxDistance, RaycastHit[] results, LayerMask layerMask)
        {
            RewindRayHits(position, direction, maxDistance);
            return Physics.RaycastNonAlloc(position, direction, results, maxDistance, layerMask);
        }
    
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, float maxDistance, 
            out RaycastHit raycastHit, LayerMask layerMask)
        {
            var bounds1 = new Bounds(origin, radius * 2f * Vector3.one);
            var bounds2 = new Bounds(origin + direction * maxDistance, radius * 2f * Vector3.one);
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.SphereCast(origin, radius, direction, out raycastHit, maxDistance, layerMask);
        }

        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction,
            RaycastHit[] results, float maxDistance, LayerMask layerMask)
        {
            var bounds1 = new Bounds(origin, radius * 2f * Vector3.one);
            var bounds2 = new Bounds(origin + direction * maxDistance, radius * 2f * Vector3.one);
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask);
        }
        
        public static bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, 
            float maxDistance, out RaycastHit raycastHit, LayerMask layerMask)
        {
            var bounds1 = new Bounds(center, halfExtents * 2f);
            var bounds2 = new Bounds(center + direction * maxDistance, halfExtents * 2f);
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.BoxCast(center, halfExtents, direction, out raycastHit, orientation, maxDistance, layerMask);
        }
        
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, 
            float maxDistance, RaycastHit[] results, LayerMask layerMask)
        {
            var bounds1 = new Bounds(center, halfExtents * 2f);
            var bounds2 = new Bounds(center + direction * maxDistance, halfExtents * 2f);
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask);
        }
        
        public static bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, 
            out RaycastHit raycastHit, LayerMask layerMask)
        {
            var pointDelta = point2 - point1;
            var bounds1 = new Bounds(
                point1 + pointDelta * .5f, 
                new Vector3(
                    Mathf.Abs(pointDelta.x) + radius * 2f, 
                    Mathf.Abs(pointDelta.y) + radius * 2f, 
                    Mathf.Abs(pointDelta.z) + radius * 2f));
            
            var endPoint1 = point1 + direction * maxDistance;
            
            var bounds2 = new Bounds(
                endPoint1 + pointDelta * .5f, 
                bounds1.size);
            
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.CapsuleCast(point1, point2, radius, direction, out raycastHit, maxDistance, layerMask);
        }
        
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, 
            RaycastHit[] results, LayerMask layerMask)
        {
            var pointDelta = point2 - point1;
            var bounds1 = new Bounds(
                point1 + pointDelta * .5f, 
                new Vector3(
                    Mathf.Abs(pointDelta.x) + radius * 2f, 
                    Mathf.Abs(pointDelta.y) + radius * 2f, 
                    Mathf.Abs(pointDelta.z) + radius * 2f));
            
            var endPoint1 = point1 + direction * maxDistance;
            
            var bounds2 = new Bounds(
                endPoint1 + pointDelta * .5f, 
                bounds1.size);
            
            bounds1.Encapsulate(bounds2);
            
            RewindBoundsHits(ref bounds1);
            return Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask);
        }
        
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, LayerMask layerMask)
        {
            var bounds = new Bounds(position, radius * 2f * Vector3.one);
            
            RewindBoundsHits(ref bounds);
            return Physics.OverlapSphereNonAlloc(position, radius, results, layerMask);
        }
        
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, LayerMask layerMask)
        {
            var bounds = new Bounds(center, halfExtents * 2f);
            
            RewindBoundsHits(ref bounds);
            return Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask);
        }

        public static int OverlapCapsuleNonAlloc(Vector3 point1, Vector3 point2, float radius,
            Collider[] results, LayerMask layerMask)
        {
            var bounds = new Bounds(
                point1 + (point2 - point1) * .5f, 
                new Vector3(radius * 2f, Mathf.Abs(point2.y - point1.y) + radius * 2f, radius * 2f));
            
            RewindBoundsHits(ref bounds);
            return Physics.OverlapCapsuleNonAlloc(point1, point2, radius, results, layerMask);
        }

        private static void RewindRayHits(Vector3 position, Vector3 direction, float maxDistance)
        {
            if (!IsWorldRewound)
                return;
            Profiler.BeginSample("Ray Checking");
            var ray = new Ray(position, direction);

            foreach(var body in RegisteredHitboxBodies)
                if (body.Raycast(ref ray, maxDistance))
                    RewindBody(body);

            Profiler.EndSample();
        }

        private static void RewindBoundsHits(ref Bounds bounds)
        {
            if (!IsWorldRewound)
                return;
            Profiler.BeginSample("Bounds Checking");
            foreach (var body in RegisteredHitboxBodies)
                if (body.OverlapBounds(ref bounds))
                    RewindBody(body);
            Profiler.EndSample();
        }

        private static void RewindBody(HitboxBody hitboxBody)
        {
            RewoundHitboxBodies.Add(hitboxBody);
            hitboxBody.Rewind();
        }
        
        private static void BeginRewind(RewindState rewindState)
        {
            WorldRewindState = rewindState;
            IsWorldRewound = true;
        }

        private static void Restore ()
        {           
            foreach (HitboxBody hb in RewoundHitboxBodies)
                hb.Restore();
            RewoundHitboxBodies.Clear();
            IsWorldRewound = false;
        }
    }
}