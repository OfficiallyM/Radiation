using UnityEngine;

namespace Radiation.Utilities
{
	internal static class GameUtilities
	{
		/// <summary>
		/// Get global position of an object.
		/// </summary>
		/// <param name="objPos">Object to get global position of</param>
		/// <returns>Vector3 global object position</returns>
		public static Vector3 GetGlobalObjectPosition(Vector3 objPos)
		{
			return new Vector3((float)(-mainscript.M.mainWorld.coord.x + objPos.x), (float)(-mainscript.M.mainWorld.coord.y + objPos.y), (float)(-mainscript.M.mainWorld.coord.z + objPos.z));
		}

        /// <summary>
		/// Get object local position from global.
		/// </summary>
		/// <param name="globalPos">Current global position</param>
		/// <returns>Vector3 local object position</returns>
		public static Vector3 GetLocalObjectPosition(Vector3 globalPos)
        {
            return new Vector3((float)-(-mainscript.M.mainWorld.coord.x - globalPos.x), (float)-(-mainscript.M.mainWorld.coord.y - globalPos.y), (float)-(-mainscript.M.mainWorld.coord.z - globalPos.z));
        }

        /// <summary>
        /// Get the distance between two positions ignoring Y axis.
        /// </summary>
        /// <param name="from">Position 1</param>
        /// <param name="to">Position 2</param>
        /// <returns>Distance between both vectors</returns>
        public static float Distance2D(Vector3 from, Vector3 to) => Mathf.Abs(from.x - to.x) + Mathf.Abs(from.z - to.z);

		/// <summary>
		/// Get the distance between two positions including Y axis.
		/// </summary>
		/// <param name="from">Position 1</param>
		/// <param name="to">Position 2</param>
		/// <returns>Distance between both vectors</returns>
		public static float Distance3D(Vector3 from, Vector3 to) => Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) + Mathf.Abs(from.z - to.z);
	}
}
