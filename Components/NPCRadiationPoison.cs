using Radiation.Core;
using Radiation.Utilities;
using System;
using System.Collections;
using System.Diagnostics.SymbolStore;
using UnityEngine;
using Logger = Radiation.Utilities.Logger;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	internal sealed class NPCRadiationPoison : MonoBehaviour
	{
		public static NPCRadiationPoison I;
		private tosaveitemscript _save = null;
		private bool _started = false;

		// Radiation poison variables.
		private float _radiationLevel = 0;
		private float _defaultMaxRadiation = 1;
		private float _maxRadiation = 0;
		private float _dangerLevel = 0.7f;
		private float _poisonMultiplier = 0.1f;
		private bool _appliedAIChanges = false;

		public void Awake()
		{
			I = this;
		}

		public void Start()
		{
			SetMaxRadiation(_defaultMaxRadiation);

			_save = gameObject.GetComponent<tosaveitemscript>();
			if (_save == null) return;

			PoisonData poison = Save.GetPoisonData(_save.idInSave);
			if (poison != null)
			{
				_radiationLevel = poison.RadiationLevel;
				if (poison.IsNPCTransformed)
				{
					StartCoroutine(MakeIrradiated(gameObject));
					_appliedAIChanges = true;
				}
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

			newAiScript ai = gameObject.GetComponent<newAiScript>();
			nyulscript nyulscript = gameObject.GetComponent<nyulscript>();

			// Return early if dead.
			if (ai != null && ai.died) return;
			if (nyulscript != null && !nyulscript.AI.alive) return;

			float radiation = RadiationController.I.GetRadiationLevel(gameObject.transform.position);

			if (RadiationController.I.IsRadiationDangerous(radiation))
			{
				// Radiation level is dangerous, increase the player radiation levels.
				float change = radiation * _poisonMultiplier;
				_radiationLevel = Mathf.Clamp(_radiationLevel + change * Time.deltaTime, 0, _maxRadiation);
			}

			if (_radiationLevel >= _dangerLevel && !_appliedAIChanges)
			{
				// Dangerous radiation levels, update AI settings.
				StartCoroutine(MakeIrradiated(gameObject));
				_appliedAIChanges = true;
			}

			Save.SetPoisonData(new PoisonData()
			{
				Id = _save.idInSave,
				RadiationLevel = _radiationLevel,
				IsNPCTransformed = _appliedAIChanges
			});
		}

		private IEnumerator MakeIrradiated(GameObject gameObject)
		{
			newAiScript ai = gameObject.GetComponent<newAiScript>();
			nyulscript nyulscript = gameObject.GetComponent<nyulscript>(); 

			// Munkas radiation transformation.
			if (ai != null)
			{
				SkinnedMeshRenderer renderer = null;
				foreach (SkinnedMeshRenderer meshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
				{
					if (meshRenderer.gameObject.name.Contains("munkas"))
						renderer = meshRenderer;
				}

				// Return early if we can't find the correct SkinnedMeshRenderer.
				if (renderer == null)
					yield return null;

				Color defaultColor = renderer.material.color;

				// Intentionally make the material colour brighter than
				// it's supposed to be as a transformation effect.
				renderer.material.color = new Color(0.32f, 221, 0.01f);

				yield return new WaitForSeconds(0.3f);

				for (float i = 1f; i <= 1.25f; i += 0.1f)
				{
					gameObject.transform.localScale *= i;
				}

				for (float j = 211f; j >= 1; j -= 5f)
				{
					renderer.material.color = new Color(0.32f, j, 0.01f);
					yield return null;
				}

				// Return to a more green default colour.
				defaultColor.g = 1.2f;
				renderer.material.color = defaultColor;

				ai.damage *= 2;
			}
			// Rabbit radiation transformation.
			else if (nyulscript != null)
			{
				SkinnedMeshRenderer renderer = null;
				foreach (SkinnedMeshRenderer meshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
				{
					if (meshRenderer.gameObject.name.Contains("Icosphere"))
						renderer = meshRenderer;
				}

				// Return early if we can't find the correct SkinnedMeshRenderer.
				if (renderer == null)
					yield return null;

				Color defaultColor = renderer.material.color;

				// Intentionally make the material colour brighter than
				// it's supposed to be as a transformation effect.
				renderer.material.color = new Color(0.32f, 221, 0.01f);

				yield return new WaitForSeconds(0.3f);

				for (float j = 211f; j >= 1; j -= 5f)
				{
					renderer.material.color = new Color(0.32f, j, 0.01f);
					yield return null;
				}

				// Return to a more green default colour.
				defaultColor.g = 1.2f;
				renderer.material.color = defaultColor;

				nyulscript.move.speed *= 1.5f;
				nyulscript.move.rotSpeed *= 2f;
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
	}
}
