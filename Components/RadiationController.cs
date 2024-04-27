using Radiation.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	public sealed class RadiationController : MonoBehaviour
	{
		public static RadiationController I;
		private float dangerLevel = 0.7f;

		public void Awake()
		{
			I = this;
		}

		public void Start()
		{
			// Set noise seed.
			Noise.Seed = mainscript.M.seed;
		}

		/// <summary>
		/// Get the radiation level for a local position.
		/// </summary>
		/// <param name="pos">Local position</param>
		/// <returns>Radation level as float between 0 and 1</returns>
		public float GetRadiationLevel(Vector3 pos)
		{
			// Convert local to global position.
			pos = GameUtilities.GetGlobalObjectPosition(pos);

			// Background radiation.
			float radiation = Noise.GetNoiseMap(pos.x, pos.z, 0.009f);
			return Mathf.Clamp01(radiation);
		}

		/// <summary>
		/// Check if radiation is at a dangerous level.
		/// </summary>
		/// <param name="radiation">Radiation level</param>
		/// <returns>True if dangerous, otherwise false</returns>
		public bool IsRadiationDangerous(float radiation)
		{
			if (radiation >= dangerLevel) return true;
			return false;
		}
	}
}
