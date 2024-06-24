using Radiation.Core;
using Radiation.Utilities;
using System;
using System.Linq;
using UnityEngine;
using Logger = Radiation.Utilities.Logger;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	internal sealed class RadiationPoison : MonoBehaviour
	{
		public static RadiationPoison I;
		private bool _started = false;

		// Radiation poison variables.
		private float _radiationLevel = 0;
		private float _defaultMaxRadiation = 1;
		private float _maxRadiation = 0;
		private float _dangerLevel = 0.7f;
		private float _dissipationLevel = 0;
		private float _poisonMultiplier = 0.1f;
		private float _dissipationMultiplier = 0.05f;

		// Radiation away variables.
		private float _radiationAway = 0;
		private bool _radiationAwayAppliedWhenDangerous = false;
		RadiationAwaySickness sickness;

		// Radiation resist variables.
		private int _radiationResistanceStacks = 0;
		private float _radiationResistance = 0f;
		private float _radiationResistanceLength = 0f;
		private bool _radiationResistanceAppliedWhenDangerous = false;
		private survivalscript _survival = null;
		private float _defaultFoodLoss = 0f;

		public void Awake()
		{
			I = this;
		}

		public void Start()
		{
			SetMaxRadiation(_defaultMaxRadiation);
			_dissipationLevel = -(_maxRadiation * _dissipationMultiplier);

			PoisonData poison = Save.GetPoisonData(0);
			if (poison != null)
			{
				// Load existing data.
				_radiationLevel = poison.RadiationLevel;
				_radiationAway = poison.RadAway;
			}

			_survival = gameObject.GetComponent<survivalscript>();
			if (_survival != null)
			{
				_defaultFoodLoss = _survival.foodLoss;
			}

			_started = true;
		}

		public void Update()
		{
			if (!_started)
			{
				Start();
				return;
			}

			if (Radiation.disableForPlayer)
				return;

			float radiation = RadiationController.I.GetRadiationLevel(gameObject.transform.position);

			float radaway = _radiationAway;
			// Radiationaway applied during danger, don't offer any benefit
			// but still apply the side effects.
			if (_radiationAwayAppliedWhenDangerous)
				radaway = 0;
			float change = _dissipationLevel - radaway;
			if (RadiationController.I.IsRadiationDangerous(radiation))
			{
				// Radiation level is dangerous, increase the player radiation levels.
				change = radiation * _poisonMultiplier;

				// Factor in RadResist if not applied during danger.
				if (!_radiationResistanceAppliedWhenDangerous && _radiationResistance > 0)
					change *= -_radiationResistance;
			}
			else if (_radiationLevel == 0)
				change = 0;

			bool dataUpdate = false;

			if (change != 0)
			{
				_radiationLevel = Mathf.Clamp(_radiationLevel + change * Time.deltaTime, 0, _maxRadiation);
				dataUpdate = true;
			}

			// Decrease radiationaway level.
			if (_radiationAway > 0)
			{
				_radiationAway = Mathf.Clamp(_radiationAway - 0.01f * Time.deltaTime, 0, _radiationAway);
				dataUpdate = true;
			}

			if (_radiationResistanceLength > 0)
			{
				_radiationResistanceLength = Mathf.Clamp(_radiationResistanceLength - (1f * Time.deltaTime), 0, _radiationResistanceLength);
			}

			// RadResist has ran out, decrease the value.
			if (_radiationResistanceLength == 0 && _radiationResistance > 0)
				_radiationResistance = Mathf.Clamp01(_radiationResistance -= 0.05f * Time.deltaTime);

			if (sickness != null)
				// TODO: Not quite happy with the effect, it's a sharp cut off when it ends.
				sickness.material.SetFloat("_Intensity", Mathf.Clamp01(1f - (_radiationAway * 10)));

			if (_radiationAway == 0 && sickness != null)
			{
				_radiationAwayAppliedWhenDangerous = false;
				DestroyImmediate(sickness);
			}

			if (_radiationResistance == 0 && _radiationResistanceStacks > 0)
			{
				_radiationResistanceStacks = 0;
				_radiationResistanceAppliedWhenDangerous = false;

				if (_survival != null)
				{
					_survival.foodLoss = _defaultFoodLoss;
				}
			}

			if (dataUpdate) 
			{ 
				Save.SetPoisonData(new PoisonData()
				{
					// Use Id 0 to store the player data.
					Id = 0,
					RadiationLevel = _radiationLevel,
					RadAway = _radiationAway,
				});
			}

			if (_radiationLevel >= _dangerLevel)
			{
				// Radiation levels are high, start dropping player health.
				if (_survival == null) return;
				_survival.DamageInstant(_radiationLevel / 25 * Time.deltaTime, true);
			}
		}

		public void OnGUI()
		{
			if (Radiation.debug)
			{
				float y = 0;
				GUI.Button(new Rect(0, y, 300, 20), $"Found geiger counter: {(Radiation.hasFoundGeigerCounter ? "Yes" : "No")}");
				y += 20f;
				GUI.Button(new Rect(0, y, 300, 20), $"Rads: {Math.Round(_radiationLevel * 100, 2)}");
				y += 20f;
				GUI.Button(new Rect(0, y, 300, 20), $"Level: {Math.Round((double)RadiationController.I.GetRadiationLevel(gameObject.transform.position) * 100, 2)}");
				y += 20f;
				GUI.Button(new Rect(0, y, 300, 20), $"RadAway: {Math.Round(_radiationAway * 100, 2)}");
				y += 20f;
				if (sickness != null)
				{
					GUI.Button(new Rect(0, y, 300, 20), $"Sickness intensity: {Math.Round(sickness.material.GetFloat("_Intensity"), 2)}");
					y += 20f;
				}
				if (_radiationResistance != 0)
				{
					GUI.Button(new Rect(0, y, 300, 20), $"RadResist: {Math.Round(_radiationResistance * 100, 2)}");
					y += 20f;
					GUI.Button(new Rect(0, y, 300, 20), $"RadResist length: {Math.Round(_radiationResistanceLength, 2)}");
					y += 20f;
				}
				GUI.Button(new Rect(0, y, 300, 20), $"Tracked object/building count: {RadiationController.I.radioactives.Count}");
			}
		}

		/// <summary>
		/// Set maximum radiation level.
		/// </summary>
		/// <param name="maxRadiation">New maximum radiation level</param>
		public void SetMaxRadiation(float maxRadiation)
		{
			_maxRadiation = maxRadiation;
		}

		/// <summary>
		/// Reset maximum radiation level to default.
		/// </summary>
		public void ResetMaxRadiation()
		{
			_maxRadiation = _defaultMaxRadiation;
		}

		/// <summary>
		/// Set radiation poisoning multiplier.
		/// </summary>
		/// <param name="poisonMultiplier">New radiation poisoning multiplier</param>
		public void SetPoisonMultiplier(float poisonMultiplier)
		{
			_poisonMultiplier = poisonMultiplier;
		}

		/// <summary>
		/// Set radiation dissipation multiplier.
		/// </summary>
		/// <param name="dissipationMultiplier">New radiation dissipation multiplier</param>
		public void SetDissipationMultiplier(float dissipationMultiplier)
		{
			_dissipationMultiplier = dissipationMultiplier;
			_dissipationLevel = -(_defaultMaxRadiation * _dissipationMultiplier);
		}

		/// <summary>
		/// Set radiation away offset to increase dissipation.
		/// </summary>
		/// <param name="radiationAway">Radiation away amount</param>
		public void SetRadiationAway(float radiationAway)
		{
			_radiationAway = radiationAway;

			// Track if radiation away was applied during danger.
			if (RadiationController.I.IsRadiationDangerous(RadiationController.I.GetRadiationLevel(gameObject.transform.position)))
				_radiationAwayAppliedWhenDangerous = true;

			sickness = gameObject.GetComponent<fpscontroller>().Cam.gameObject.AddComponent<RadiationAwaySickness>();
		}

		/// <summary>
		/// Set radiation resistance to decrease radiation intake.
		/// </summary>
		/// <param name="radiationResistance">Radiation resistance level</param>
		/// <param name="radiationResistanceLength">Radiation resistance length in seconds</param>
		public void SetRadiationResist(float radiationResistance, float radiationResistanceLength)
		{
			_radiationResistanceStacks++;
			// Allow effect to be stackable.
			_radiationResistance = Mathf.Clamp01(_radiationResistance += radiationResistance);

			if (_radiationResistanceLength < radiationResistanceLength)
				_radiationResistanceLength = radiationResistanceLength;

			// Track if radiation resistance was applied during danger.
			if (RadiationController.I.IsRadiationDangerous(RadiationController.I.GetRadiationLevel(gameObject.transform.position)))
				_radiationResistanceAppliedWhenDangerous = true;

			// Apply food and water loss multiplier if survival is enabled.
			if (_survival != null)
			{
				// Water loss is based off foodLoss, the waterLoss property is unused.
				_survival.foodLoss *= 5.5f / _radiationResistanceStacks;
			}
		}
	}
}
