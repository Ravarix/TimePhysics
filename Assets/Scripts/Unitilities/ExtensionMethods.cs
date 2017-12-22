using UnityEngine;

namespace Unitilities
{
    public static class ExtensionMethods
    {
        public static float MaxComponent(this Vector3 v3)
        {
            return Mathf.Max(v3.x, v3.y, v3.z);
        }

        public static Vector3 Inflate(this Vector3 v3)
        {
            var f = v3.MaxComponent();
            return new Vector3(f, f, f);
        }
    }
}