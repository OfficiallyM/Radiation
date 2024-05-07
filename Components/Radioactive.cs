using Radiation.Utilities;
using UnityEngine;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	public sealed class Radioactive : MonoBehaviour
	{
		public enum RadiationType
		{
			Radioactive,
			Safe,
		}
		public enum RadiationZoneType
		{
			Object,
			Zone,
		}
		private RadiationType _type = 0;
		private RadiationZoneType? _zoneType = null;
		private float _radiationLevel = 0.5f;
		private float _distance = 20;
		private bool _isRandom = true;

		/// <summary>
		/// Initialise a new radiation zone.
		/// </summary>
		/// <param name="type">Zone type</param>
		/// <param name="distance">Radioactive/safe distance, measured in a circle around the object origin</param>
		/// <param name="radiationLevel">Radiation level</param>
		public void Init(RadiationType type, float? distance = null, float? radiationLevel = null)
		{
			_type = type;
			if (distance.HasValue)
				_distance = distance.Value;
			if (radiationLevel.HasValue)
				_radiationLevel = radiationLevel.Value;

			_isRandom = false;
		}

		public void Start()
		{
			_zoneType = gameObject.GetComponent<buildingscript>() != null ? RadiationZoneType.Zone : RadiationZoneType.Object;

			if (_isRandom)
			{
				// Radiation not set manually, randomise it.
				int id = 0;
				if (gameObject.GetComponent<tosaveitemscript>() != null)
					id = gameObject.GetComponent<tosaveitemscript>().idInSave;
				System.Random random = new System.Random(id);
				float levelMin = 0.4f;
				float levelMax = 1f;
				float distanceMin = 10f;
				float distanceMax = 30f;
				if (_zoneType == RadiationZoneType.Zone)
				{
					levelMin = 0.4f;
					distanceMin = 50f;
					distanceMax = 200f;

					// 1/4 chance of zone being a safe zone.
					if (random.Next(0, 4) == 0)
						_type = RadiationType.Safe;
				}
				else
				{
					bool metallic = false;
					foreach (MeshRenderer meshRenderer in gameObject.GetComponentsInChildren<MeshRenderer>())
					{
						string material = meshRenderer.material.name.ToLower();
						if (material.Contains("metal") || material.Contains("rust"))
							metallic = true; 
					}

					// Anything non-metallic is 1 in 25 chance of object being radioactive.
					int chance = 25;
					// Metallic items are a 1 in 10 chance of object being radioactive.
					if (metallic)
						chance = 10;

					if (random.Next(0, chance) >= 1)
					{
						levelMin = 0;
						levelMax = 0;
					}
				}

				_radiationLevel = (float)random.Next((int)levelMin * 100, (int)(levelMax * 100) + 1) / 100;
				_distance = random.Next((int)distanceMin, (int)distanceMax);
			}

			// Track all radioactive zones/objects.
			RadiationController.I.radioactives.Add(this);
		}

		public void OnDestroy()
		{
			// Clear radioactive zone/object on destroy.
			RadiationController.I.radioactives.Remove(this);
		}

		/// <summary>
		/// Get the radiation level from this zone.
		/// </summary>
		/// <param name="position">Position to check from</param>
		/// <returns>Radiation level between 0 and 1 or null if it should be ignored completely</returns>
		public float? GetRadiationLevel(Vector3 position)
		{
			float distance = GameUtilities.Distance3D(position, transform.position);
			// Outside of affected range, return null to ignore this zone.
			if (distance > _distance) return null;

			// Within a safe zone, return no radiation.
			if (IsSafe()) return 0;

			// Reduce the radiation level by the distance.
			return Mathf.Clamp01(_radiationLevel * (1f - (distance / _distance)));
		}

		public bool IsSafe()
		{
			return _type == RadiationType.Safe;
		}
	}
}
