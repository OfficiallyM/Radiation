using UnityEngine;

namespace Radiation.Extensions
{
    internal static class VectorExtensions
    {
        /// <summary>
        /// Round to nearest int.
        /// </summary>
        /// <param name="v">Vector3 to round</param>
        /// <returns>Vector3 with rounded coordinates</returns>
        public static Vector3 Round(this Vector3 v)
        {
            v.x = Mathf.Round(v.x);
            v.y = Mathf.Round(v.y);
            v.z = Mathf.Round(v.z);
            return v;
        }
    }
}
