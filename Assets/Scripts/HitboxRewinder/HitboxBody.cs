using System.Collections.Generic;
using System.Linq;
using Unitilities;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hitbox
{
    
    public class HitboxBody : MonoBehaviour
    {
        [Tooltip("Must be factor of TimePhysics.NumSnapshots. 1 = Every frame")]
        [Range(1, TimePhysics.NumSnapshots)]
        [SerializeField] private int _snapshotInterval = 1;
        public int SnapshotInterval => _snapshotInterval;
        [SerializeField] private Bounds _bounds = new Bounds(Vector3.up, Vector3.one * 2f);
        public Bounds Bounds => _bounds;
        [SerializeField] private Transform[] _transforms;
        public Transform[] Transforms { get { return _transforms; } set { _transforms = value; }}
        public int CurrentSnapshotFrame { get; private set; } = -1;
        public HitboxSnapshot[] Snapshots { get; private set; }

        private bool _isRewound;
        private HitboxSnapshot _restoreSnapshot;
        private int _startFrame;

        private void Awake()
        {
            Snapshots = new HitboxSnapshot[TimePhysics.NumSnapshots];

            // Validate all the target hitboxes are populated.
            if (_transforms.Any(entry => entry == null))
            {
                Debug.LogError($"At least one hitbox in hitbox body is null: {name}", this);
                _transforms = _transforms.Where(entry => entry != null).ToArray();
                return;
            }

            // Initialize Snapshot space
            for (var index = 0; index < TimePhysics.NumSnapshots; index++)
                Snapshots[index] = new HitboxSnapshot(_transforms.Length);
            
            _restoreSnapshot = new HitboxSnapshot(_transforms.Length);
        }

        private void OnValidate()
        {
            if (_snapshotInterval <= 0)
                _snapshotInterval = 1;
            // try and find a divisor so that snapshot frequency is a factor of NumSnapshots
            int tries = 0;
            while (TimePhysics.NumSnapshots % _snapshotInterval != 0 && ++tries <= 4)
                _snapshotInterval = TimePhysics.NumSnapshots / (TimePhysics.NumSnapshots / _snapshotInterval + 1);
        }

        private void OnEnable()
        {
            TimePhysics.RegisterHitboxBody(this);
            _startFrame = TimePhysics.WorldFrame;
        }

        private void OnDisable()
        {
            TimePhysics.UnregisterHitboxBody(this);
        }

        public void TakeSnapshot(int frame, int index)
        {
            if (frame > CurrentSnapshotFrame)
            {
                CurrentSnapshotFrame = frame;
                var snapShot = Snapshots[index];
                
                if (frame % _snapshotInterval == 0)
                {
                    snapShot.Real = true;
                    var ltw = transform.localToWorldMatrix;
                    snapShot.ProximityBounds = MatrixUtils.LocalToWorld(ref _bounds, ref ltw);

                    //grab LTW for body parts
                    for (int hitboxIndex = 0; hitboxIndex < _transforms.Length; hitboxIndex++)
                        snapShot.LocalToWorld[hitboxIndex] = _transforms[hitboxIndex].localToWorldMatrix;
                }
                else
                    snapShot.Real = false;
            }
            else
                Debug.LogError($"Requesting snapshot frame <= current snapshot frame: {frame}, Current: {CurrentSnapshotFrame}", this);
        }

        private void TakeRestoreSnapshot()
        {
            if (_isRewound)
                return;

            for (int hitboxIndex = 0; hitboxIndex < _transforms.Length; hitboxIndex++)
                _restoreSnapshot.LocalToWorld[hitboxIndex] = _transforms[hitboxIndex].localToWorldMatrix;
        }

        private void RestoreSnapshot()
        {
            if (!_isRewound)
                return;
            
            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i].SetPositionAndRotation(
                    MatrixUtils.ExtractTranslationFromMatrix(ref _restoreSnapshot.LocalToWorld[i]),
                    MatrixUtils.ExtractRotationFromMatrix(ref _restoreSnapshot.LocalToWorld[i]));
        }

        private void SetActiveSnapshot(int index)
        {
            var snapShot = Snapshots[index];

            // Set the current position/rotation of each hitbox to be back in time.
            for (int hitboxIndex = 0; hitboxIndex < _transforms.Length; hitboxIndex++)
                _transforms[hitboxIndex].SetPositionAndRotation(
                    MatrixUtils.ExtractTranslationFromMatrix(ref snapShot.LocalToWorld[hitboxIndex]),
                    MatrixUtils.ExtractRotationFromMatrix(ref snapShot.LocalToWorld[hitboxIndex]));
        }

        private void SetActiveSnapshotLerp(int index1, int index2, float lerpVal)
        {
            var snapShot1 = Snapshots[index1];
            var snapShot2 = Snapshots[index2];
            
            // Set the current position/rotation of each hitbox to be back in time lerped.
            for (int i = 0; i < _transforms.Length; i++)
            {
                var matrix1 = snapShot1.LocalToWorld[i];
                var matrix2 = snapShot2.LocalToWorld[i];
                Vector3 pos;
                Quaternion rot;
                MatrixUtils.LerpMatrixTR(ref matrix1, ref matrix2, lerpVal, out pos, out rot);
                
                _transforms[i].SetPositionAndRotation(pos, rot);
            }
        }

        public bool OverlapBounds(ref Bounds bounds)
        {
            if (TimePhysics.WorldRewindState.Frame <= _startFrame)
                return false;
            
            if(!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % _snapshotInterval == 0)
                return InternalOverlapBounds(TimePhysics.WorldRewindState.Frame % TimePhysics.NumSnapshots, ref bounds);
            
            int index1, index2;
            var lerpVal = LerpFrame(out index1, out index2);
            return InternalOverlapBounds(index1, index2, lerpVal, ref bounds);
        }

        public bool Raycast(ref Ray ray, float maxDistance)
        {
            if (TimePhysics.WorldRewindState.Frame <= _startFrame)
                return false;
            
            if (!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % _snapshotInterval == 0)
                return InternalRaycast(TimePhysics.WorldRewindState.Frame % TimePhysics.NumSnapshots, ref ray, maxDistance);
            
            int index1, index2;
            var lerpVal = LerpFrame(out index1, out index2);
            return InternalRaycast(index1, index2, lerpVal, ref ray, maxDistance);
        }

        private bool InternalOverlapBounds(int index, ref Bounds bounds)
        {
            return Snapshots[index].ProximityBounds.Intersects(bounds);
        }

        private bool InternalOverlapBounds(int index1, int index2, float lerpVal, ref Bounds bounds)
        {
            var bounds1 = Snapshots[index1].ProximityBounds;
            var bounds2 = Snapshots[index2].ProximityBounds;
            return MatrixUtils.LerpBounds(ref bounds1, ref bounds2, lerpVal).Intersects(bounds);
        }

        private bool InternalRaycast(int index, ref Ray ray, float maxDistance)
        {
            float distance;
            if (Snapshots[index].ProximityBounds.IntersectRay(ray, out distance))
                return distance <= maxDistance;
            return false;
        }

        private bool InternalRaycast(int index1, int index2, float lerpVal, ref Ray ray, float maxDistance)
        {
            var bounds1 = Snapshots[index1].ProximityBounds;
            var bounds2 = Snapshots[index2].ProximityBounds;
            var lerpBounds = MatrixUtils.LerpBounds(ref bounds1, ref bounds2, lerpVal);

            float distance;
            if (lerpBounds.IntersectRay(ray, out distance))
                return distance <= maxDistance;
            return false;
        }

        private float LerpFrame(out int index1, out int index2)
        {
            if (TimePhysics.WorldRewindState.Lerp)
            {
                return LerpFrame(TimePhysics.WorldRewindState.Frame, 
                                TimePhysics.WorldRewindState.Frame2, 
                                TimePhysics.WorldRewindState.LerpVal,
                                out index1, out index2);
            }
            return LerpFrame(TimePhysics.WorldRewindState.Frame, out index1, out index2);
        }

        public float LerpFrame(int frame, out int index1, out int index2)
        {
            var frameVal = ((float) frame % TimePhysics.NumSnapshots) / _snapshotInterval;
            var floor = Mathf.FloorToInt(frameVal);
            index1 = floor * _snapshotInterval;
            index2 = Mathf.CeilToInt(frameVal) * _snapshotInterval;
            if (index2 >= TimePhysics.NumSnapshots)
                index2 %= TimePhysics.NumSnapshots;
            float lerpVal = (frameVal - floor) + (TimePhysics.WorldRewindState.LerpVal / _snapshotInterval);

            return lerpVal;
        }
        
        public float LerpFrame(int frame1, int frame2, float frameLerp, out int index1, out int index2)
        {
            //need to remap the frame lerp to our own snapshot frequency lerp
            var frameVal = ((float) frame1 % TimePhysics.NumSnapshots) / _snapshotInterval;
            var floor = Mathf.FloorToInt(frameVal);
            index1 = floor * _snapshotInterval;
            var frame2Val = ((float) frame2 % TimePhysics.NumSnapshots) / _snapshotInterval;
            index2 = Mathf.CeilToInt(frame2Val) * _snapshotInterval;
            if (index2 >= TimePhysics.NumSnapshots)
                index2 %= TimePhysics.NumSnapshots;
            float lerpVal = (frameVal - floor) + (frameLerp / _snapshotInterval);
            //print($"Frame1: {frame1}, Frame2: {frame2}, FrameLerp: {frameLerp}, out id1 {index1}, id2 {index2}, lerpVal {lerpVal}");
            return lerpVal;
        }
        
        public bool Rewind()
        {
            if (_isRewound || !TimePhysics.IsWorldRewound)
                return false;
            
            if (!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % _snapshotInterval == 0)
                InternalRewind(TimePhysics.WorldRewindState.Frame % TimePhysics.NumSnapshots);
            else {
                int index1, index2;
                var lerpVal = LerpFrame(out index1, out index2);
                InternalRewindLerp(index1, index2, lerpVal);
            }
            _isRewound = true;
            return true;
        }

        private void InternalRewind(int index)
        {
            Profiler.BeginSample("Rewind Body");
            TakeRestoreSnapshot();
            SetActiveSnapshot(index);
            Profiler.EndSample();
        }

        private void InternalRewindLerp(int index1, int index2, float lerpVal)
        {
            Profiler.BeginSample("Rewind Body Lerp");
            TakeRestoreSnapshot();
            SetActiveSnapshotLerp(index1, index2, lerpVal);
            Profiler.EndSample();
        }

        public bool Restore()
        {
            if (!_isRewound)
                return false;
            
            Profiler.BeginSample("Restore Body");
            RestoreSnapshot();
            Profiler.EndSample();
            _isRewound = false;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, .8f, .2f, .4f);
            var ltw = transform.localToWorldMatrix;
            var bounds = MatrixUtils.LocalToWorld(ref _bounds, ref ltw);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}