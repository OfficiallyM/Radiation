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
        private static readonly int[] _radiationStages = new int[] { 0, 1, 2 };
        private static readonly int _minRadiationStage = 0;
        private static readonly int _maxRadiationStage = _radiationStages.Length - 1;
        private int _radiationStage = 0;
        private float _maxRadiationLevel = 1 * _radiationStages.Length;
        private float _radiationLevel = 0;
		private float _damageLevel = 0.7f + _maxRadiationStage;
		private float _poisonMultiplier = 0.0085f;
		private float _dissipationMultiplier = 0.005f;
        private CameraEffect grain;

		// Radiation away variables.
		private float _radiationAway = 0;
		private bool _radiationAwayAppliedWhenDangerous = false;
		private CameraEffect sickness;

		// Radiation resist variables.
		private int _radiationResistanceStacks = 0;
		private float _radiationResistance = 0f;
		private float _radiationResistanceLength = 0f;
		private survivalscript _survival = null;
		private float _defaultFoodLoss = 0f;

		public void Awake()
		{
			I = this;
		}

		private void Init()
		{
			_survival = gameObject.GetComponent<survivalscript>();
			if (_survival != null)
			{
				_defaultFoodLoss = _survival.foodLoss;
			}

			PoisonData poison = Save.GetPoisonData(0);
            if (poison != null)
            {
                // Load existing data.
                _radiationLevel = poison.RadiationLevel;
                _radiationAway = poison.RadAway;
                _radiationResistance = poison.RadResist;
                _radiationResistanceLength = poison.RadResistLength;
                _radiationResistanceStacks = poison.RadResistStacks;

                Logger.Log($"Loading poison save data:\nRads: {_radiationLevel}\nRadAway: {_radiationAway}\nRadResist: {_radiationResistance}\nRadResist Length: {_radiationResistanceLength}\nRadResist stacks: {_radiationResistanceStacks}");

                if (_survival != null && _radiationResistanceStacks > 0)
                {
                    // Water loss is based off foodLoss, the waterLoss property is unused.
                    _survival.foodLoss *= 5.5f / _radiationResistanceStacks;
                }
            }

			_started = true;
		}

		public void Update()
		{
            // Can't load save data during Start() as it's too early so load on first update tick.
			if (!_started)
			{
                Init();
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
			float change = -(_dissipationMultiplier * (_radiationStage + 1)) - radaway;
			if (RadiationController.I.IsRadiationDangerous(radiation))
			{
                // Radiation level is dangerous, increase the player radiation levels.
                float poisonAmount = _poisonMultiplier;
				// Factor in RadResist.
				if (_radiationResistance > 0)
                    poisonAmount -= _radiationResistance;
                change = radiation * poisonAmount;

                change = Mathf.Clamp(change, 0, _maxRadiationLevel);
			}
			else if (_radiationLevel == 0)
				change = 0;

			bool dataUpdate = false;

            if (change != 0)
            {
                _radiationLevel = Mathf.Clamp(_radiationLevel + change * Time.deltaTime, 0, _maxRadiationLevel);
                dataUpdate = true;
            }

            // Calculate radiation stage.
            int nextStage = _radiationStage;
            if (_radiationLevel > _radiationStage + 1)
                nextStage++;
            if (_radiationLevel < _radiationStage)
                nextStage--;
            nextStage = Mathf.Clamp(nextStage, _minRadiationStage, _maxRadiationStage);

            // TODO: Decide on consequences for stage 2 & 3 radiation.

            switch (nextStage)
            {
                case 0:
                    if (grain != null)
                        DestroyImmediate(grain);
                    break;
                case 1:
                    if (grain == null)
                        grain = ApplyShader(Radiation.GrainShader);

                    grain.GetMaterial().SetFloat("_Strength", _radiationLevel / _maxRadiationLevel * 100 / 5.5f / 100);
                    break;
                case 2:
                    if (grain == null)
                        grain = ApplyShader(Radiation.GrainShader);

                    grain.GetMaterial().SetFloat("_Strength", _radiationLevel / _maxRadiationLevel * 100 / 5 / 100);
                    break;
            }

            _radiationStage = nextStage;

            // Decrease radiationaway level.
            if (_radiationAway > 0)
			{
				_radiationAway = Mathf.Clamp(_radiationAway - 0.01f * Time.deltaTime, 0, _radiationAway);
				dataUpdate = true;
			}

            // Tick down resistance length.
			if (_radiationResistanceLength > 0)
			{
				_radiationResistanceLength = Mathf.Clamp(_radiationResistanceLength - (1f * Time.deltaTime), 0, _radiationResistanceLength);
                dataUpdate = true;
            }

			// RadResist has ran out, decrease the value.
			if (_radiationResistanceLength == 0 && _radiationResistance > 0)
            {
				_radiationResistance = Mathf.Clamp01(_radiationResistance -= 0.05f * Time.deltaTime);
                dataUpdate = true;
            }

			if (sickness != null)
				// TODO: Not quite happy with the effect, it's a sharp cut off when it ends.
				sickness.GetMaterial().SetFloat("_Intensity", Mathf.Clamp01(1f - (_radiationAway * 10)));

			if (_radiationAway == 0 && sickness != null)
			{
				_radiationAwayAppliedWhenDangerous = false;
				DestroyImmediate(sickness);
			}

			if (_radiationResistance == 0 && _radiationResistanceStacks > 0)
			{
				_radiationResistanceStacks = 0;

				if (_survival != null)
				{
					_survival.foodLoss = _defaultFoodLoss;
				}
                dataUpdate = true;
            }

			if (dataUpdate) 
                SaveData();

			if (_radiationLevel >= _damageLevel)
			{
				// Radiation levels are high, start dropping player health.
				if (_survival == null) return;
				_survival.DamageInstant(_radiationLevel / _radiationStage / 35 * Time.deltaTime, true);
			}
		}

		public void OnGUI()
		{
			if (Radiation.debug)
			{
                PoisonData saveData = Save.GetPoisonData(0);
				float y = 0;
				GUI.Button(new Rect(0, y, 300, 20), $"Found geiger counter: {(Radiation.hasFoundGeigerCounter ? "Yes" : "No")}");
				y += 20f;
				GUI.Button(new Rect(0, y, 300, 20), $"Rads: {Math.Round(_radiationLevel * 100, 2)}/{Math.Round(_maxRadiationLevel * 100, 2)} (Stage: {_radiationStage + 1}) ({Math.Round(saveData.RadiationLevel * 100, 2)} saved)");
				y += 20f;
				GUI.Button(new Rect(0, y, 300, 20), $"Level: {Math.Round((double)RadiationController.I.GetRadiationLevel(gameObject.transform.position) * 100, 2)}");
				y += 20f;
                if (grain != null)
                {
                    GUI.Button(new Rect(0, y, 300, 20), $"Grain strength: {Math.Round(grain.GetMaterial().GetFloat("_Strength"), 2)}");
                    y += 20f;
                }
                if (_radiationAway != 0)
                {
				    GUI.Button(new Rect(0, y, 300, 20), $"RadAway: {Math.Round(_radiationAway * 100, 2)}");
				    y += 20f;
                }
				if (sickness != null)
				{
					GUI.Button(new Rect(0, y, 300, 20), $"Sickness intensity: {Math.Round(sickness.GetMaterial().GetFloat("_Intensity"), 2)}");
					y += 20f;
				}
				if (_radiationResistance != 0)
				{
					GUI.Button(new Rect(0, y, 300, 20), $"RadResist: {Math.Round(_radiationResistance * 10000, 2)}");
					y += 20f;
					GUI.Button(new Rect(0, y, 300, 20), $"RadResist length: {Math.Round(_radiationResistanceLength, 2)}");
					y += 20f;
				}
				GUI.Button(new Rect(0, y, 300, 20), $"Tracked object/building count: {RadiationController.I.radioactives.Count}");
                if (Radiation.debugShowNearbyRadioactives)
                {
                    y += 40f;
                    GUI.Button(new Rect(0, y, 300, 20), $"Nearby radioactives:");
                    y += 20f;
                    foreach (var radioactive in RadiationController.I.radioactives.Where(r => !r.IsSafe() && r.GetRadiationLevel(gameObject.transform.position) > 0))
                    {
                        GUI.Button(new Rect(0, y, 300, 20), $"{radioactive.name.Replace("(Clone)", string.Empty)} - {Math.Round((double)radioactive.GetRadiationLevel(gameObject.transform.position) * 100, 2)}");
                        y += 20f;
                    }
                }
            }
		}

        /// <summary>
        /// Save poison data.
        /// </summary>
        private void SaveData()
        {
            Save.Upsert(new PoisonData()
            {
                // Use Id 0 to store the player data.
                Id = 0,
                Type = "poison",
                RadiationLevel = _radiationLevel,
                RadAway = _radiationAway,
                RadResist = _radiationResistance,
                RadResistLength = _radiationResistanceLength,
                RadResistStacks = _radiationResistanceStacks,
            });
        }

        /// <summary>
        /// Apply shader to player camera.
        /// </summary>
        /// <param name="shader">Shader to apply</param>
        /// <returns>Camera effect</returns>
        private CameraEffect ApplyShader(Shader shader)
        {
            CameraEffect effect = gameObject.GetComponent<fpscontroller>().Cam.gameObject.AddComponent<CameraEffect>();
            effect.SetMaterial(new Material(shader));
            return effect;
        }

		/// <summary>
		/// Set radiation poisoning multiplier.
		/// </summary>
		/// <param name="poisonMultiplier">New radiation poisoning multiplier</param>
		public void SetPoisonMultiplier(float poisonMultiplier)
		{
			//_poisonMultiplier = poisonMultiplier;
		}

		/// <summary>
		/// Set radiation dissipation multiplier.
		/// </summary>
		/// <param name="dissipationMultiplier">New radiation dissipation multiplier</param>
		public void SetDissipationMultiplier(float dissipationMultiplier)
		{
			//_dissipationMultiplier = dissipationMultiplier;
			//_dissipationLevel = -(1 * _dissipationMultiplier);
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

            sickness = ApplyShader(Radiation.RadiationAwaySicknessShader);

            SaveData();
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
            {
                if (_radiationResistanceStacks == 1)
                {
                    // No existing stacks, nullify the effect.
                    _radiationResistance = 0;
                    _radiationResistanceLength = 0;
                    _radiationResistanceStacks = 0;
                }
                else
                {
                    // Has existing stacks, half the effect.
                    _radiationResistance /= 4;
                    _radiationResistanceLength /= 2;
                }
            }

			// Apply food and water loss multiplier if survival is enabled.
			if (_survival != null && _radiationResistanceStacks > 0)
			{
				// Water loss is based off foodLoss, the waterLoss property is unused.
				_survival.foodLoss *= 5.5f / _radiationResistanceStacks;
			}

            SaveData();
        }
	}
}
