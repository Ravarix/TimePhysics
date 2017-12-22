using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace CBG {

    [CustomEditor(typeof(NetRewinder))]
    public class NetRewinderEditor : Editor {
        List<int> problemTransforms;
        List<int> problemTransformChildren;
        NetRewinder rewinder;

        public override void OnInspectorGUI() {
            // Get NetRewinder reference
            rewinder = (NetRewinder)target;
            // Check for problems with transform list.  Needs to happen before inspector GUI starts being drawn.
            RebuildProblemTransformList();
            // Draw the default inspector
            DrawDefaultInspector();
            // If no hitboxes defined in rewinder, warn and offer to find them
            if (rewinder.hitBoxes==null || rewinder.hitBoxes.Length == 0) {
                EditorGUILayout.HelpBox("You don't have any hitboxes selected.  Manually add them, or click \"Find Hitboxes\" to add all child colliders as hitboxes.", MessageType.Warning);
                if (GUILayout.Button("Find Hitboxes")) {
                    FindHitboxes();
                }
            }
            // If there are problems...
            if (problemTransforms != null && problemTransforms.Count > 0) {
                for (int i = 0; i < problemTransforms.Count; i++) {
                    // display an appropriate warning or error for each problem
                    // Errors will cause definite problems with functionality.
                    // Warnings won't cause anything obviously incorrect to happen, but are not optimal.
                    if (rewinder.hitBoxes[problemTransforms[i]] == null) {
                        // Show error for null transform and offer to fix.
                        EditorGUILayout.HelpBox("Hitbox " + HitboxString(problemTransforms[i]) +
                            " is null.  Select a valid transform or click \"Fix Problems\" to delete the entry.", MessageType.Error);
                    } else if (!rewinder.hitBoxes[problemTransforms[i]].IsChildOf(rewinder.transform)) {
                        // Show warning for a transform outside the gameobject and offer to fix
                        EditorGUILayout.HelpBox("Hitbox " + HitboxString(problemTransforms[i]) +
                            " is not a child of " + rewinder.name + ".  Select a valid transform or click \"Fix Problems\" to delete the entry.", MessageType.Warning);
                    } else if (rewinder.hitBoxes[problemTransforms[i]] == rewinder.hitBoxes[problemTransformChildren[i]]) {
                        // Show warning for duplicate hitbox entry and offer to fix
                        EditorGUILayout.HelpBox("Hitbox " + HitboxString(problemTransforms[i]) + " is a duplicate of hitbox " +
                            HitboxString(problemTransformChildren[i]) + ".  Hitboxes should only be entered once.", MessageType.Warning);
                    } else {
                        // Show error for hitboxes in incorrect order and offer to fix.
                        EditorGUILayout.HelpBox("Hitbox " + HitboxString(problemTransforms[i]) +
                            " is a parent of hitbox " + HitboxString(problemTransformChildren[i]) +
                            " but is later in the hitbox list.  Parents must be before their children.", MessageType.Error);
                    }
                }
                if (GUILayout.Button("Fix Problems")) {
                    Undo.RecordObject(rewinder, "Repair transform issues");
                    // while there are problems...
                    while (problemTransforms.Count > 0) {
                        // fix the first one and rebuild the problem list (single fix may fix multiples, eg root transform being at bottom of list)
                        FixProblem(problemTransforms[0], problemTransformChildren[0]);
                        RebuildProblemTransformList();
                    }
                }
            }
        }

        // find all colliders in children of the NetRewinder gameobject and add them as hitboxes
        void FindHitboxes() {
            rewinder.hitBoxes = GetHitboxes(rewinder.transform).ToArray();
        }

        // recursive function to return a list of all child transforms with colliders, and optionally self as well
        List<Transform> GetHitboxes(Transform trans, bool childrenOnly = true) {
            List<Transform> hitboxes = new List<Transform>();
            // Check self for transform if childrenOnly not set, and add to list if true
            if (!childrenOnly && HasCollider(trans)) {
                hitboxes.Add(trans);
            }
            // Call self on all child transforms.
            // Using the for loop instead of getchildren will process disabled children as well.
            for (int i=0;i<trans.childCount;i++) {
                hitboxes.AddRange(GetHitboxes(trans.GetChild(i), false));
            }
            return hitboxes;
        }

        // Convenience test for collider
        bool HasCollider(Transform t) {
            return t.GetComponent<Collider>() != null;
        }

        // fix the problem related to the two provided transform indices
        void FixProblem(int parentIndex, int childIndex) {
            // if parent is null, or not a child of the NetRewinder's gameobject, or both transforms are identical...
            if (rewinder.hitBoxes[parentIndex] == null || !rewinder.hitBoxes[parentIndex].IsChildOf(rewinder.transform) || rewinder.hitBoxes[parentIndex] == rewinder.hitBoxes[childIndex]) {
                // Remove the null or invalid or duplicate transform from list
                for (int i = parentIndex + 1; i < rewinder.hitBoxes.Length; i++) {
                    rewinder.hitBoxes[i - 1] = rewinder.hitBoxes[i];
                }
                Array.Resize(ref rewinder.hitBoxes, rewinder.hitBoxes.Length - 1);
            } else {
                // otherwise, the cure is to swap elements
                Transform t = rewinder.hitBoxes[parentIndex];
                rewinder.hitBoxes[parentIndex] = rewinder.hitBoxes[childIndex];
                rewinder.hitBoxes[childIndex] = t;
            }
        }

        // find any problems with the current list of transforms
        void RebuildProblemTransformList() {
            problemTransforms = new List<int>();
            problemTransformChildren = new List<int>();
            if (rewinder.hitBoxes != null) {
                for (int i = 0; i < rewinder.hitBoxes.Length; i++) {
                    Transform hitbox = rewinder.hitBoxes[i];
                    // Test for null or invalid (not childed to NetRewinder gameobject) transforms
                    if (hitbox == null || !hitbox.IsChildOf(rewinder.transform)) {
                        problemTransforms.Add(i);
                        problemTransformChildren.Add(i);
                    } else {
                        // check all hitboxes higher in list than self
                        for (int j = 0; j < i; j++) {
                            // If any are children...
                            if (IsChild(rewinder.hitBoxes[j], hitbox)) {
                                // ...add them to the problem list
                                problemTransforms.Add(i);
                                problemTransformChildren.Add(j);
                            }
                        }
                    }
                }
            }
        }

        // Convenience function to build name for referenced hitbox.  Use index and name if possible
        string HitboxString(int id) {
            Transform t = rewinder.hitBoxes[id];
            string result = id.ToString();
            if (t != null) {
                result += " (" + t.name + ")";
            }
            return result;
        }

        // Convenience function to test for child/parent relationship between transforms
        bool IsChild(Transform t1, Transform t2) {
            if (t1 == null) return false;
            return t1.IsChildOf(t2);
        }

    }

}