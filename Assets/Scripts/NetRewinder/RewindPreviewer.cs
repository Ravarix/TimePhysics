using UnityEngine;
using System.Collections;

namespace CBG {
    public class RewindPreviewer : MonoBehaviour {
        // Prefab to use for hitbox previews
        [SerializeField]
        GameObject hitboxPrefab;

        // Time delay for preview
        // Can have any range (determined in NetRewinder), but clamped to 0-5 here
        [Range(0, 5)]
        public float timeDelay = 2;

        // The NetRewinder attached to the gameobject
        NetRewinder rewinder;
        // The hitbox preview objects for this rewinder
        Transform[] hitboxPreviews;
        // Local copy of hitboxCount
        int hitboxCount;
        // The hitbox objects in the NetRewinder
        Transform[] hitboxes;

        // Transform to be used by all previewers to hold hitbox previews
        static Transform previewHolder;

        // This procedure holds basic setup code for the previewer.  None of these steps are necessary to use NetRewinder.
        // The code here creates visible copies of the NetRewinder hitboxes - you wouldn't do this in a real scenario.
        void Start() {
            if (previewHolder == null) {
                previewHolder = new GameObject("HitboxPreviews").transform;
            }
            if (hitboxPrefab == null) {
                Debug.LogError("ERROR: No hitbox prefab specified for " + name);
            }
            rewinder = GetComponent<NetRewinder>();
            if (rewinder == null || hitboxPrefab == null) {
                this.enabled = false;
                return;
            }
            hitboxes = rewinder.hitBoxes;
            hitboxCount = rewinder.hitboxCount;
            hitboxPreviews = new Transform[hitboxCount];
            // for each hitbox object...
            for (int i = 0; i < hitboxCount; i++) {
                hitboxPreviews[i] = GameObject.Instantiate(hitboxPrefab).transform;
                Collider coll = hitboxes[i].GetComponent<Collider>();
                // if there is a collider...
                if (coll) {
                    // parent to ensure scale is correct when set below
                    hitboxPreviews[i].SetParent(hitboxes[i].parent);
                    Transform hitbox = hitboxPreviews[i].Find("Hitbox");
                    // set local scale to match
                    hitbox.localScale = hitboxes[i].localScale;
                    // set preview scale and position to match the collider's bounds
                    // approximate based on type
                    if (coll is BoxCollider) {
                        hitbox.localPosition = ((BoxCollider)coll).center;
                        Vector3 v1 = hitbox.localScale;
                        Vector3 v2 = ((BoxCollider)coll).size;
                        hitbox.localScale = new Vector3((v1.x * v2.x), (v1.y * v2.y), (v1.z * v2.z));
                    } else if (coll is SphereCollider) {
                        hitbox.localPosition = ((SphereCollider)coll).center;
                        Vector3 v1 = hitbox.localScale;
                        float diam = ((SphereCollider)coll).radius * 2;
                        hitbox.localScale = new Vector3(v1.x * diam, v1.y * diam, v1.z * diam);
                    } else if (coll is CapsuleCollider) {
                        hitbox.localPosition = ((CapsuleCollider)coll).center;
                        Vector3 v1 = hitbox.localScale;
                        Vector3 v2 = Vector3.zero;
                        switch (((CapsuleCollider)coll).direction) {
                        case 0: //x
                            v2 = new Vector3(
                                (((CapsuleCollider)coll).height),
                                (((CapsuleCollider)coll).radius * 2),
                                (((CapsuleCollider)coll).radius * 2));
                            break;
                        case 1: //y
                            v2 = new Vector3(
                                (((CapsuleCollider)coll).radius * 2),
                                (((CapsuleCollider)coll).height),
                                (((CapsuleCollider)coll).radius * 2));
                            break;
                        case 2: //z
                            v2 = new Vector3(
                                (((CapsuleCollider)coll).radius * 2),
                                (((CapsuleCollider)coll).radius * 2),
                                (((CapsuleCollider)coll).height));
                            break;
                        }
                        hitbox.localScale = new Vector3((v1.x * v2.x), (v1.y * v2.y), (v1.z * v2.z));
                    }
                    hitboxPreviews[i].SetParent(null);
                }
                hitboxPreviews[i].SetParent(previewHolder);
            }
            // Set initial positions to match gameobject
            SetPositionsAndRotations(hitboxPreviews, hitboxes);

        }

        // This is most reliable when done in LateUpdate to ensure the curren't frame's animations are complete.   This only matters if the time
        // being rewound to is more recent than the last snapshot taken (<20ms ago with default settings).  In most cases, this is optional.
        void LateUpdate() {
            // Set rewind target time
            // In this case, time.time-timeDelay
            float rewindTargetTime = Mathf.Max(0, Time.time - timeDelay);

            // Rewind hitboxes to the desired time
            if (rewinder.Rewind(rewindTargetTime)) {
                // NetRewinder.Rewind will return true on success.

                // Update preview elements
                // In a real scenario, you'd be doing your hitbox raycasts etc here
                SetPositionsAndRotations(hitboxPreviews, hitboxes);

                // Restore hitboxes to their original positions
                // It's important to do this during the same frame as the rewind
                rewinder.Restore();
                // NetRewinder.Restore also returns true on success, though it's not typically necessary to test for this.
                // Attempting to restore when not in a rewound state will return false, and add a warning to the debug log.
                // Note that you can perform multiple rewinds of the same object to different time points, then a single restore afterwards.
                // This can reduce performance impact if you receive multiple shot notifications from clients in the same frame

            } else {
                // NetRewinder.Rewind returns false on failure.  This will only happen if the rewind time is older
                // than the oldest time in the buffer (buffer length is set in seconds, default 10 s), or if the
                // rewind time is in the future.  In either case, NetRewinder.Rewind will show a detailed warning
                // in the log to help you figure out what went wrong.

                // You would insert any special logic here that you want to execute if the rewind is somehow invalid.
                return;
            }


        }

        // Set all hitboxPreview positions and rotations to match the provided transform list
        // If called after a rewind, this will show historical states
        // If called after a restore, this will show current states
        void SetPositionsAndRotations(Transform[] objs, Transform[] targets) {
            for (int i = 0; i < objs.Length; i++) {
                SetPositionAndRotation(objs[i], targets[i]);
            }
        }

        // Set the hitboxPreview position and rotation to match the supplied transform
        void SetPositionAndRotation(Transform obj, Transform target) {
            obj.position = target.position;
            obj.rotation = target.rotation;
        }
    }
}