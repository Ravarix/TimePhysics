using UnityEngine;

namespace Unitilities
{
    public static class ExtensionMethods
    {
        public static Vector3 With(this Vector3 v3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? v3.x, y ?? v3.y, z ?? v3.z);
        }

        public static Vector3 Add(this Vector3 v3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(
                x != null ? v3.x + (float)x : v3.x, 
                y != null ? v3.y + (float)y : v3.y,
                z != null ? v3.z + (float)z : v3.z);
        }
    }
}