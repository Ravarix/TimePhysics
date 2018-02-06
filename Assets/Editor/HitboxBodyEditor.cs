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
                .Where(col => col.gameObject != hitboxBody.gameObject) //only children
                .Select(col => col.transform)
                .ToArray();
        }
    }
}