using System.Linq;
using Unitilities;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hitbox
{
    
    public class HitboxBody : MonoBehaviour
    {
        [SerializeField] private int snapshotFrequency = 1;
        public int SnapshotFrequency => snapshotFrequency;
        [SerializeField] private Collider _proximityCollider;
        public Collider ProximityCollider => _proximityCollider;
        [SerializeField] private Transform[] transforms;
        public Transform[] Transforms { get { return transforms; } set { transforms = value; }}
        public int CurrentSnapshotFrame { get; private set; } = -1;
        public HitboxSnapshot[] Snapshots { get; private set; }

        private bool _isRewound;
        private HitboxSnapshot _restoreSnapshot;

        private void Awake()
        {
            Snapshots = new HitboxSnapshot[TimePhysics.NumSnapshots];
            
            if (_proximityCollider == null)
            {
                Debug.LogError("Hitbox Body Must have proximity.");
                return;
            }

            // Validate all the target hitboxes are populated.
            if (transforms.Any(entry => entry == null))
            {
                Debug.LogError("At least one hitbox in hitbox body is null: " + name, this);
                transforms = transforms.Where(entry => entry != null).ToArray();
                return;
            }

            // Initialize Snapshot space
            for (var index = 0; index < TimePhysics.NumSnapshots; index++)
                Snapshots[index] = new HitboxSnapshot(transforms.Length);
            
            _restoreSnapshot = new HitboxSnapshot(transforms.Length);
        }

        private void OnValidate()
        {
            if (_proximityCollider == null)
                Debug.LogError("Must populate ProximityHitbox with Collider");
        }

        private void OnEnable()
        {
            if(_proximityCollider != null)
                TimePhysics.RegisterHitboxBody(this);
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
                
                if (frame % snapshotFrequency == 0)
                {
                    snapShot.Real = true;
                    snapShot.ProximityBounds = _proximityCollider.bounds;

                    //grab LTW for body parts
                    for (int hitboxIndex = 0; hitboxIndex < transforms.Length; hitboxIndex++)
                        snapShot.LocalToWorld[hitboxIndex] = transforms[hitboxIndex].localToWorldMatrix;
                }
                else
                {
                    snapShot.Real = false;
                }
            }
            else
                Debug.LogError($"Requesting snapshot frame <= current snapshot frame: {frame}, Current : {CurrentSnapshotFrame}", this);
        }

        private void TakeRestoreSnapshot()
        {
            if (_isRewound)
                return;

            //TODO tie this to snapshot index to lazy-populate?
            for (int hitboxIndex = 0; hitboxIndex < transforms.Length; hitboxIndex++)
                _restoreSnapshot.LocalToWorld[hitboxIndex] = transforms[hitboxIndex].localToWorldMatrix;
        }

        private void RestoreSnapshot()
        {
            if (!_isRewound)
                return;
            
            for (int i = 0; i < transforms.Length; i++)
                transforms[i].SetPositionAndRotation(
                    MatrixUtils.ExtractTranslationFromMatrix(ref _restoreSnapshot.LocalToWorld[i]),
                    MatrixUtils.ExtractRotationFromMatrix(ref _restoreSnapshot.LocalToWorld[i]));
        }

        /// <summary>
        /// Sets the hitboxes on this body to a certain frame.
        /// </summary>
        private void SetActiveSnapshot(int index)
        {
            var snapShot = Snapshots[index];

            // Set the current position/rotation of each hitbox to be back in time.
            for (int hitboxIndex = 0; hitboxIndex < transforms.Length; hitboxIndex++)
                transforms[hitboxIndex].SetPositionAndRotation(
                    MatrixUtils.ExtractTranslationFromMatrix(ref snapShot.LocalToWorld[hitboxIndex]),
                    MatrixUtils.ExtractRotationFromMatrix(ref snapShot.LocalToWorld[hitboxIndex]));
        }

        private void SetActiveSnapshotLerp(int index1, int index2, float lerpVal)
        {
            var snapShot1 = Snapshots[index1];
            var snapShot2 = Snapshots[index2];
            
            // Set the current position/rotation of each hitbox to be back in time lerped.
            for (int i = 0; i < transforms.Length; i++)
            {
                var matrix1 = snapShot1.LocalToWorld[i];
                var matrix2 = snapShot2.LocalToWorld[i];
                Vector3 pos;
                Quaternion rot;
                MatrixUtils.LerpMatrix(ref matrix1, ref matrix2, lerpVal, out pos, out rot);
                
                transforms[i].SetPositionAndRotation(pos, rot);
            }
        }

        public bool OverlapBounds(ref Bounds bounds)
        {
            if(!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % snapshotFrequency == 0)
                return InternalOverlapBounds(TimePhysics.WorldRewindState.Frame % TimePhysics.NumSnapshots, ref bounds);
            
            int index1, index2;
            var lerpVal = LerpFrame(out index1, out index2);
            return InternalOverlapBounds(index1, index2, lerpVal, ref bounds);
        }

        public bool Raycast(ref Ray ray, float maxDistance)
        {
            if (!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % snapshotFrequency == 0)
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
                return LerpFrame(TimePhysics.WorldRewindState.Frame, TimePhysics.WorldRewindState.Frame2, TimePhysics.WorldRewindState.LerpVal,
                    out index1, out index2);
            }
            return LerpFrame(TimePhysics.WorldRewindState.Frame, out index1, out index2);
        }

        public float LerpFrame(int frame, out int index1, out int index2)
        {
            //TODO FIX
            var frameVal = (float) frame % TimePhysics.NumSnapshots / snapshotFrequency;
            var floor = Mathf.FloorToInt(frameVal);
            index1 = floor * snapshotFrequency;
            index2 = Mathf.CeilToInt(frameVal) * snapshotFrequency;
            if (index2 >= TimePhysics.NumSnapshots)
                index2 %= TimePhysics.NumSnapshots;
            float lerpVal = (frameVal - floor) + (TimePhysics.WorldRewindState.LerpVal / snapshotFrequency);

            return lerpVal;
        }
        
        public float LerpFrame(int frame1, int frame2, float frameLerp, out int index1, out int index2)
        {
            //TODO FIX
            var frameVal = (float) frame1 % TimePhysics.NumSnapshots / snapshotFrequency;
            var floor = Mathf.FloorToInt(frameVal);
            index1 = floor * snapshotFrequency;
            var frame2Val = (float) frame2 % TimePhysics.NumSnapshots / snapshotFrequency;
            index2 = Mathf.CeilToInt(frame2Val) * snapshotFrequency;
            if (index2 >= TimePhysics.NumSnapshots)
                index2 %= TimePhysics.NumSnapshots;
            float lerpVal = (frameVal - floor) + (frameLerp / snapshotFrequency);

            return lerpVal;
        }
        
        public bool Rewind()
        {
            if (_isRewound || !TimePhysics.IsWorldRewound)
                return false;
            
            if (!TimePhysics.WorldRewindState.Lerp && TimePhysics.WorldRewindState.Frame % snapshotFrequency == 0)
            {
                InternalRewind(TimePhysics.WorldRewindState.Frame % TimePhysics.NumSnapshots);
            }
            else
            {
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

    }
}