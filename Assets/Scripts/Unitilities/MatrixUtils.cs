using UnityEngine;

namespace Unitilities
{
    public class MatrixUtils
    {
        /// <summary>
        /// Zoinked from critias speed tree.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Bounds LocalToWorld(ref Bounds box, ref Matrix4x4 m)
        {
            Bounds newBox = new Bounds(Vector3.zero, Vector3.zero)
            {
                min = new Vector3(m.m03, m.m13, m.m23),
                max = new Vector3(m.m03, m.m13, m.m23)
            };

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {

                    float av = m[i, j] * box.min[j];
                    float bv = m[i, j] * box.max[j];

                    Vector3 min;
                    Vector3 max;
                    if (av < bv)
                    {
                        min = newBox.min;
                        max = newBox.max;

                        min[i] += av;
                        max[i] += bv;

                        newBox.min = min;
                        newBox.max = max;
                    }
                    else
                    {
                        min = newBox.min;
                        max = newBox.max;

                        min[i] += bv;
                        max[i] += av;

                        newBox.min = min;
                        newBox.max = max;
                    }
                }
            }

            return newBox;
        }

        // https://forum.unity3d.com/threads/how-to-assign-matrix4x4-to-transform.121966/
        /// <summary>
        /// Extract translation from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Translation offset.
        /// </returns>
        public static Vector3 ExtractTranslationFromMatrix(ref Matrix4x4 matrix)
        {
            return new Vector3(matrix.m03, matrix.m13, matrix.m23);
        }

        /// <summary>
        /// Extract rotation quaternion from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Quaternion representation of rotation transform.
        /// </returns>
        public static Quaternion ExtractRotationFromMatrix(ref Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(
                new Vector3(matrix.m02, matrix.m12, matrix.m22), //forwards
                new Vector3(matrix.m01, matrix.m11, matrix.m21)); //upwards
        }

        /// <summary>
        /// Extract scale from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Scale vector.
        /// </returns>
        public static Vector3 ExtractScaleFromMatrix(ref Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00,matrix.m10,matrix.m20,matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01,matrix.m11,matrix.m21,matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02,matrix.m12,matrix.m22,matrix.m32).magnitude;
            return scale;
        }

        /// <summary>
        /// Extract position, rotation and scale from TRS matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <param name="localPosition">Output position.</param>
        /// <param name="localRotation">Output rotation.</param>
        /// <param name="localScale">Output scale.</param>
        public static void DecomposeMatrix(ref Matrix4x4 matrix,out Vector3 localPosition,out Quaternion localRotation,out Vector3 localScale)
        {
            localPosition = ExtractTranslationFromMatrix(ref matrix);
            localRotation = ExtractRotationFromMatrix(ref matrix);
            localScale = ExtractScaleFromMatrix(ref matrix);
        }

        /// <summary>
        /// Set transform component from TRS matrix.
        /// </summary>
        /// <param name="transform">Transform component.</param>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        public static void SetTransformFromMatrix(Transform transform, ref Matrix4x4 matrix)
        {
            transform.localPosition = ExtractTranslationFromMatrix(ref matrix);
            transform.localRotation = ExtractRotationFromMatrix(ref matrix);
            transform.localScale = ExtractScaleFromMatrix(ref matrix);
        }

        /// <summary>
        /// Get translation matrix.
        /// </summary>
        /// <param name="offset">Translation offset.</param>
        /// <returns>
        /// The translation transform matrix.
        /// </returns>
        public static Matrix4x4 TranslationMatrix(Vector3 offset)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m03 = offset.x;
            matrix.m13 = offset.y;
            matrix.m23 = offset.z;
            return matrix;
        }
        
        public static Matrix4x4 LerpMatrix(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, float lerpVal)
        {
            Vector3 pos1, pos2;
            Quaternion rot1, rot2;
            Vector3 scale1, scale2;
            DecomposeMatrix(ref matrix1, out pos1, out rot1, out scale1);
            DecomposeMatrix(ref matrix2, out pos2, out rot2, out scale2);
            
            return Matrix4x4.TRS(
                Vector3.Lerp(pos1, pos2, lerpVal),
                Quaternion.Lerp(rot1, rot2, lerpVal),
                Vector3.Lerp(scale1, scale2, lerpVal));
        }

        //Faster version that doesnt lerp scale
        public static void LerpMatrixTR(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, float lerpVal, 
            out Vector3 position, out Quaternion rotation)
        {
            var pos1 = ExtractTranslationFromMatrix(ref matrix1);
            var pos2 = ExtractTranslationFromMatrix(ref matrix2);
            var rot1 = ExtractRotationFromMatrix(ref matrix1);
            var rot2 = ExtractRotationFromMatrix(ref matrix2);

            position = Vector3.Lerp(pos1, pos2, lerpVal);
            rotation = Quaternion.Lerp(rot1, rot2, lerpVal);
        }
        
        public static Matrix4x4 LerpMatrixTR(ref Matrix4x4 matrix1, ref Matrix4x4 matrix2, float lerpVal, Vector3 scale)
        {
            var pos1 = ExtractTranslationFromMatrix(ref matrix1);
            var pos2 = ExtractTranslationFromMatrix(ref matrix2);
            var rot1 = ExtractRotationFromMatrix(ref matrix1);
            var rot2 = ExtractRotationFromMatrix(ref matrix2);

            return Matrix4x4.TRS(
                Vector3.Lerp(pos1, pos2, lerpVal),
                Quaternion.Lerp(rot1, rot2, lerpVal),
                scale);
        }

        public static Bounds LerpBounds(ref Bounds bounds1, ref Bounds bounds2, float lerpVal)
        {
            return new Bounds(
                Vector3.Lerp(bounds1.center, bounds2.center, lerpVal),
                Vector3.Lerp(bounds1.size, bounds2.size, lerpVal));
        }
        
    }
}
