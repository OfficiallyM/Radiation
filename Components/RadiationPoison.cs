using System;
using UnityEngine;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	internal sealed class RadiationPoison : MonoBehaviour
	{
		private float _radiationLevel = 0;
		private float _defaultMaxRadiation = 1;
		private float _maxRadiation = 0;
		private float _dangerLevel = 0;
		private float _dissipationLevel = 0;
		private float _poisonMultiplier = 0.1f;
		private float _dissipationMultiplier = 0.05f;

		public void Start()
		{
			SetMaxRadiation(_defaultMaxRadiation);
			_dangerLevel = _maxRadiation * 0.6f;
			_dissipationLevel = -(_maxRadiation * _dissipationMultiplier);
		}

		public void Update()
		{
			float radiation = RadiationController.I.GetRadiationLevel(gameObject.transform.position);

			float change = _dissipationLevel;
			if (RadiationController.I.IsRadiationDangerous(radiation))
			{
				// Radiation level is dangerous, increase the player radiation levels.
				change = radiation * _poisonMultiplier;
			}
			_radiationLevel = Mathf.Clamp(_radiationLevel + change * Time.deltaTime, 0, _maxRadiation);

			if (_dangerLevel > 0 && _radiationLevel >= _dangerLevel)
			{
				// Radiation levels are high, start dropping player health.
				survivalscript survival = gameObject.GetComponent<survivalscript>();
				if (survival == null) return;
				survival.DamageInstant(_radiationLevel / 25 * Time.deltaTime, true);
			}
		}

		public void OnGUI()
		{
			if (Radiation.debug)
			{
				GUI.Button(new Rect(0, 0, 300, 20), $"Rads: {Math.Round(_radiationLevel, 2)}");
				GUI.Button(new Rect(0, 20, 300, 20), $"Level: {Math.Round((double)RadiationController.I.GetRadiationLevel(gameObject.transform.position) * 100, 2)}");
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
			_dissipationLevel = -(_maxRadiation * _dissipationMultiplier);
		}
	}
}
