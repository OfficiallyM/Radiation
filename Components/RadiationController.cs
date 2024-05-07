using Radiation.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	public sealed class RadiationController : MonoBehaviour
	{
		public static RadiationController I;
		private const float DangerLevel = 0.7f;
		internal List<Radioactive> radioactives = new List<Radioactive>();

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
		/// <returns>Radiation level as float between 0 and 1</returns>
		public float GetRadiationLevel(Vector3 pos)
		{
			// Convert local to global position.
			pos = GameUtilities.GetGlobalObjectPosition(pos);

			// Background radiation.
			float backgroundRadiation = Noise.GetNoiseMap(pos.x, pos.z, 0.009f);
			float radiation = 0;
			foreach (Radioactive radioactive in radioactives)
			{
				float? rads = radioactive.GetRadiationLevel(pos);
				if (rads == null) continue;

				// Remove background radiation in safe zones.
				if (radioactive.IsSafe()) backgroundRadiation = 0;

				radiation += rads.Value;
			}

			radiation += backgroundRadiation;

			return Mathf.Clamp01(radiation);
		}

		/// <summary>
		/// Check if radiation is at a dangerous level.
		/// </summary>
		/// <param name="radiation">Radiation level</param>
		/// <returns>True if dangerous, otherwise false</returns>
		public bool IsRadiationDangerous(float radiation)
		{
			if (radiation >= DangerLevel) return true;
			return false;
		}
	}
}
