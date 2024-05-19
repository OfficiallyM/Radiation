﻿using Radiation.Core;
using Radiation.Utilities;
using System;
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

			_started = true;
		}

		public void Update()
		{
			if (!_started)
			{
				Start();
				return;
			}

			float radiation = RadiationController.I.GetRadiationLevel(gameObject.transform.position);

			if (Radiation.disableForPlayer)
				radiation = 0;

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
			}
			_radiationLevel = Mathf.Clamp(_radiationLevel + change * Time.deltaTime, 0, _maxRadiation);

			// Decrease radiationaway level.
			_radiationAway = Mathf.Clamp(_radiationAway - 0.01f * Time.deltaTime, 0, _radiationAway);

			if (sickness != null)
				// TODO: Not quite happy with the effect, it's a sharp cut off when it ends.
				sickness.material.SetFloat("_Intensity", Mathf.Clamp01(1f - (_radiationAway * 10)));

			if (_radiationAway == 0)
			{
				if (_radiationAwayAppliedWhenDangerous)
					_radiationAwayAppliedWhenDangerous = false;

				if (sickness != null)
					DestroyImmediate(sickness);
			}

			Save.SetPoisonData(new PoisonData()
			{
				// Use Id 0 to store the player data.
				Id = 0,
				RadiationLevel = _radiationLevel,
				RadAway = _radiationAway,
			});

			if (_radiationLevel >= _dangerLevel)
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
				GUI.Button(new Rect(0, 0, 300, 20), $"Rads: {Math.Round(_radiationLevel * 100, 2)}");
				GUI.Button(new Rect(0, 20, 300, 20), $"Level: {Math.Round((double)RadiationController.I.GetRadiationLevel(gameObject.transform.position) * 100, 2)}");
				GUI.Button(new Rect(0, 40, 300, 20), $"RadAway: {Math.Round(_radiationAway * 100, 2)}");
				if (sickness != null)
					GUI.Button(new Rect(0, 60, 300, 20), $"Sickness intensity: {Math.Round(sickness.material.GetFloat("_Intensity"), 2)}");
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
	}
}
