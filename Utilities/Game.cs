using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	}
}
