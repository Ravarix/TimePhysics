using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CBG {
    public struct HitboxSnapshot {
        // Positions for each hitbox
        public Vector3[] positions;
        // Rotations for each hitbox
        public Quaternion[] rotations;
        // Hitbox count
        int hitboxCount;

        // Create an empty hitbox snapshot with the specified number of hitboxes
        public HitboxSnapshot(int newHitboxCount) {
            hitboxCount = newHitboxCount;
            positions = new Vector3[hitboxCount];
            rotations = new Quaternion[hitboxCount];
        }

        // Create a hitbox snapshot from the provided array of transforms
        public HitboxSnapshot(Transform[] newHitboxes) {
            hitboxCount = newHitboxes.Length;
            positions = new Vector3[hitboxCount];
            rotations = new Quaternion[hitboxCount];
            if ((newHitboxes == null) || newHitboxes.Length == 0) return;
            for (int i = 0; i < hitboxCount; i++) {
                positions[i] = newHitboxes[i].position;
                rotations[i] = newHitboxes[i].rotation;
            }
        }

        // Set the snapshot to match the transform array
        // Assuming the snapshot has the same number of hitboxes as the transform array,
        // this will have zero allocations (no GC)
        public void SetFrom(Transform[] source) {
            if (hitboxCount != source.Length) {
                hitboxCount = source.Length;
                positions = new Vector3[hitboxCount];
                rotations = new Quaternion[hitboxCount];
            }
            for (int i = 0; i < hitboxCount; i++) {
                positions[i] = source[i].position;
                rotations[i] = source[i].rotation;
            }
        }

        // Set the snapshot to match the provided snapshot
        // Assuming both snapshots have the same number of hitboxes,
        // this will be a zero-allocation operation (no GC)
        public void SetFrom(HitboxSnapshot source) {
            if (source.hitboxCount != hitboxCount) {
                hitboxCount = source.hitboxCount;
                positions = new Vector3[hitboxCount];
                rotations = new Quaternion[hitboxCount];
            }
            for (int i = 0; i < source.hitboxCount; i++) {
                positions[i] = source.positions[i];
                rotations[i] = source.rotations[i];
            }
        }
    }

    public class NetRewinder : MonoBehaviour {
        public Transform[] hitBoxes;

        // Settings for NetRewinder
        // Do not change during runtime - changes after Awake will cause errors
        [Tooltip("The length of recorded history in seconds")]
        [SerializeField]
        float historyLength = 10f;
        [Tooltip("The number of physics frames between snapshots (1 = every frame, 2 = every other frame, etc)")]
        [Range(1, 50)]
        [SerializeField]
        int snapshotInterval = 1;
        [Tooltip("Allowable error margin for timestep calculations - if a requested time is within this margin of a saved timestep, that timestep will be used directly instead of interpolating results")]
        [SerializeField]
        float timeErrorMargin = .001f;

        // time interval between snapshots (calculated from fixeddeltatime and snapshotInterval)
        float snapshotTimeInterval;
        // physics time of last snapshot - used to calculate which snapshots to interpolate between
        float lastSnapshotTime;
        // frame counter - used to honour snapshotInterval
        int framesSinceSnapshot;
        // maximum number of snapshots - calculated from historyLength and snapshotInterval
        int maxSnapshotCount;
        // saved snapshot to restore to
        HitboxSnapshot savedSnapshot;
        // snapshot used to calculate restore position
        HitboxSnapshot lerpSnapshot;
        // rewound flag - used to determine whether to save current state, and whether to restore to saved state, during operations
        bool rewound;
        // the snapshots array - accessed in a circular fashion via use of modulo (%) operator
        HitboxSnapshot[] snapshots;
        // index of the last recorded snapshot
        int nextSnapshotIndex = 0;

        // number of hitboxes attached to NetRewinder
        int _hitboxCount;
        public int hitboxCount { get { return _hitboxCount; } }

        void Awake() {
            // perform initial setup
            _hitboxCount = hitBoxes.Length;
            snapshotTimeInterval = Time.fixedDeltaTime * snapshotInterval;
            framesSinceSnapshot = snapshotInterval; // take first snapshot right away
            maxSnapshotCount = (int)(historyLength / snapshotTimeInterval) + 1;
            snapshots = new HitboxSnapshot[maxSnapshotCount];
            savedSnapshot = new HitboxSnapshot(hitboxCount);
            lerpSnapshot = new HitboxSnapshot(hitboxCount);
            if (timeErrorMargin < 0) timeErrorMargin = .001f;
            for (int i = 0; i < maxSnapshotCount; i++) {
                snapshots[i] = new HitboxSnapshot(hitboxCount);
            }
        }

        void FixedUpdate() {
            // Make sure we're not in a rewound state at the beginning of the frame
            if (rewound) {
                Debug.LogError("ERROR: " + name + " is in a rewound state at the beginning of FixedUpdate.  Always call Restore() in the same frame you call Rewind()");
                return;
            }
            framesSinceSnapshot++;
            // Take a snapshot if it's time
            if (framesSinceSnapshot >= snapshotInterval) {
                TakeSnapshot();
                framesSinceSnapshot = 0;
            }
        }

        void TakeSnapshot() {
            // If not rewound (should always be true)
            if (!rewound) {
                snapshots[(nextSnapshotIndex++)%maxSnapshotCount].SetFrom(hitBoxes);
                lastSnapshotTime = Time.fixedTime;
            } else {
                // Error if attempting to take a snapshot in rewound state
                // Shouldn't be possible to get here
                Debug.LogError("ERROR: Tried to take a snapshot while rewound!  Please report this to CodeBison Games");
            }
        }

        // Rewind all hitboxes to their position as of the requested time
        // Interpolate between snapshots as necessary
        public bool Rewind(float targetTime) {
            if (nextSnapshotIndex==0) { return false; }
            bool withinMargin = false;
            // calculate which snapshots we'll be interpolating between
            float targetSnapshotIndex = GetIndexAtTime(targetTime);
            // get nearest snapshot
            int roundedSnapshotIndex = (int)Mathf.Round(targetSnapshotIndex);
            // if point is within timeErrorMargin of snapshot, point at that snapshot directly
            if (Mathf.Abs(Mathf.Round(targetSnapshotIndex) - targetSnapshotIndex) <= timeErrorMargin && roundedSnapshotIndex <= nextSnapshotIndex - 1) {
                withinMargin = true;
                targetSnapshotIndex = roundedSnapshotIndex;
            }
            // if time point is outside snapshot list, return false (failure)
            bool tooEarly = targetSnapshotIndex < 0 || targetSnapshotIndex < nextSnapshotIndex - maxSnapshotCount;
            if (tooEarly || targetTime > Time.time) {
                Debug.LogWarning("WARNING: Tried to restore to snapshot at time " + targetTime + " with index " + targetSnapshotIndex);
                if (tooEarly) {
                    Debug.LogWarning("WARNING: You're trying to rewind by " + (Time.time - targetTime) + " seconds, but the NetRewinder is only set to remember " + historyLength + " seconds.");
                } else {
                    Debug.LogWarning("WARNING: You're trying to rewind to " + targetTime + " but the current time is only " + Time.time + ".  You need a DeLorean DMC-12 for that!");
                }
                return false;
            }
            if (withinMargin && roundedSnapshotIndex <= nextSnapshotIndex - 1) {
                // if snapshot is within error margin, just use it directly with no interpolation
                return RewindToSnapshot(snapshots[roundedSnapshotIndex % maxSnapshotCount]);
            } else {
                // if snapshot isn't within error margin, interpolate between the nearest two snapshots
                int lhsIndex = (int)targetSnapshotIndex;
                // set left snapshot
                HitboxSnapshot lhs = snapshots[lhsIndex % maxSnapshotCount];
                // set right snapshot
                HitboxSnapshot rhs;
                float lerpVal = 0;
                // if targetTime is more recent than last snapshot...
                if (lhsIndex == nextSnapshotIndex-1) {
                    // save current state if not already rewound
                    if (!rewound) {
                        savedSnapshot.SetFrom(hitBoxes);
                    }
                    // set interpolation value based on target time's relationship to most recent snapshot and "current" state
                    rhs = savedSnapshot;
                    lerpVal = (targetTime-lastSnapshotTime)/(Time.time-lastSnapshotTime);
                } else {
                    // Set interpolation value based on target time's relationship to before and after snapshots
                    rhs = snapshots[(lhsIndex + 1) % maxSnapshotCount];  // set right snapshot
                    lerpVal = targetSnapshotIndex - lhsIndex;  // set lerp value
                }
                // set the target snapshot's positions and rotations to interpolated values
                for (int i = 0; i < hitboxCount; i++) {
                    lerpSnapshot.positions[i] = Vector3.Lerp(lhs.positions[i], rhs.positions[i], lerpVal);
                    lerpSnapshot.rotations[i] = Quaternion.Lerp(lhs.rotations[i], rhs.rotations[i], lerpVal);
                }
                // rewind to the interpolated snapshot
                return RewindToSnapshot(lerpSnapshot);
            }
        }

        // Convenience function to get the index represented by a target time.  This will be
        // a non-integer if the time doesn't exactly correspond to a saved snapshot.
        float GetIndexAtTime(float targetTime) {
            return (nextSnapshotIndex - 1) - (lastSnapshotTime - targetTime) / snapshotTimeInterval;
        }

        // Rewind hitboxes to the saved state in the snapshot provided
        // Save current state first if not already in a rewound state
        bool RewindToSnapshot(HitboxSnapshot snapshot) {
            // if not rewound, save snapshot of current state for later restore
            if (!rewound) {
                savedSnapshot.SetFrom(hitBoxes);
            }
            LoadSnapshot(snapshot);
            rewound = true;
            return true;
        }

        // If in a rewound state, restore all hitboxes to their saved state
        public bool Restore() {
            if (rewound) {
                LoadSnapshot(savedSnapshot);
                ClearRewound();
                return true;
            }
            return false;
        }

        // Set all hitbox positions and rotations to the values in the snapshot
        void LoadSnapshot(HitboxSnapshot snapshot) {
            for (int i = 0; i < hitboxCount; i++) {
                hitBoxes[i].position = snapshot.positions[i];
                hitBoxes[i].rotation = snapshot.rotations[i];
            }
        }

        // Clear the rewound flag
        void ClearRewound() {
            rewound = false;
        }


    }
}