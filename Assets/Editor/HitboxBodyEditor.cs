using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hitbox
{
    [CustomEditor(typeof(HitboxBody))]
    public class HitboxBodyEditor : Editor
    {
        private HitboxBody hitboxBody;
        
        public override void OnInspectorGUI()
        {
            if(hitboxBody == null)
                hitboxBody = (HitboxBody) target;

            DrawDefaultInspector();

            if (hitboxBody.ProximityCollider == null)
            {
                EditorGUILayout.HelpBox("No Proximity Collider found!", MessageType.Error);
                return;
            }
            
            if (hitboxBody.Transforms == null || hitboxBody.Transforms.Length == 0)
            {
                EditorGUILayout.HelpBox("No Hitboxes found", MessageType.Warning);
                if(GUILayout.Button("Find Hitboxes"))
                    FindHitboxes();
            }
        }

        private void FindHitboxes()
        {
            hitboxBody.Transforms = hitboxBody.GetComponentsInChildren<Collider>()
                .Where(col => col.gameObject != hitboxBody.gameObject //only children
                              && col != hitboxBody.ProximityCollider) //not proximity
                .Select(col => col.transform)
                .ToArray();
        }
    }
}