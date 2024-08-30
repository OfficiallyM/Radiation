using Radiation.Core;
using Radiation.Extensions;
using Radiation.Utilities;
using UnityEngine;
using Logger = Radiation.Utilities.Logger;

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
            _zoneType = gameObject.GetComponent<buildingscript>() != null ? RadiationZoneType.Zone : RadiationZoneType.Object;

            _type = type;
			if (distance.HasValue)
				_distance = distance.Value;
			if (radiationLevel.HasValue)
				_radiationLevel = radiationLevel.Value;


            // Zones will use -1 and so will be matched on position instead
            Save.Upsert(new RadioactiveData()
            {
                Id = GetSaveId(),
                Position = GameUtilities.GetGlobalObjectPosition(transform.position),
                Type = "radioactive",
                RadiationType = (int)_type,
                RadiationLevel = _radiationLevel,
                Distance = _distance,
            });

			_isRandom = false;
		}

		public void Start()
		{
			_zoneType = gameObject.GetComponent<buildingscript>() != null ? RadiationZoneType.Zone : RadiationZoneType.Object;

            // Attempt to load from save data.
            int saveId = GetSaveId();
            RadioactiveData saveData = Save.GetRadioactiveData(saveId, GameUtilities.GetGlobalObjectPosition(transform.position).Round());
            if (saveData != null)
            {
                _isRandom = false;

                _type = (RadiationType)saveData.RadiationType;
                _radiationLevel = saveData.RadiationLevel;
                _distance = saveData.Distance;
            }

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

					// 1/2 chance of zone being a safe zone.
					if (random.Next(0, 4) <= 1)
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

                // Divide radiation level by the number of child parts to average it across all of them.
                int childCount = 1;
                partconditionscript rootPart = gameObject.GetComponent<partconditionscript>();
                if (rootPart != null)
                    childCount = rootPart.childs.Count;

                _radiationLevel = (float)random.Next((int)levelMin * 100, (int)(levelMax * 100) + 1) / childCount / 100;
				_distance = random.Next((int)distanceMin, (int)distanceMax);

                // Apply radioactive to any object children.
                if (_zoneType == RadiationZoneType.Object)
                {
                    if (rootPart != null)
                    {
                        foreach (partconditionscript child in rootPart.childs)
                        {
                            if (child.gameObject.GetComponent<tosaveitemscript>() != null && child.gameObject.GetComponent<Radioactive>() == null)
                            {
                                Radioactive childRadioactive = child.gameObject.AddComponent<Radioactive>();
                                childRadioactive.Init(_type, _distance, _radiationLevel);
                            }
                        }
                    }
                }

                // Save data.
                // Zones will use -1 and so will be matched on position instead.
                saveId = GetSaveId();

                Save.Upsert(new RadioactiveData()
                {
                    Id = saveId,
                    Position = GameUtilities.GetGlobalObjectPosition(transform.position),
                    Type = "radioactive",
                    RadiationType = (int)_type,
                    RadiationLevel = _radiationLevel,
                    Distance = _distance,
                });
            }

			// Track all radioactive zones/objects.
			RadiationController.I.radioactives.Add(this);
		}

        /// <summary>
        /// Get save ID for object.
        /// </summary>
        /// <returns>Save ID</returns>
        private int GetSaveId()
        {
            return _zoneType == RadiationZoneType.Zone ? -1 : gameObject.GetComponent<tosaveitemscript>() != null ? gameObject.GetComponent<tosaveitemscript>().idInSave : -1;
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
			float distance = GameUtilities.Distance3D(position, GameUtilities.GetGlobalObjectPosition(transform.position));
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

        /// <summary>
        /// Get radiation level.
        /// </summary>
        /// <returns>Radiation level</returns>
        internal float GetRadiationLevel()
        {
            return _radiationLevel;
        }

        /// <summary>
        /// Get radiation distance.
        /// </summary>
        /// <returns>Radiation distance</returns>
        internal float GetDistance()
        {
            return _distance;
        }

        /// <summary>
        /// Get radiation type.
        /// </summary>
        /// <returns>Radiation type</returns>
        internal RadiationType GetRadiationType()
        {
            return _type;
        }
	}
}
