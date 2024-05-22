using UnityEngine;
using Logger = Radiation.Utilities.Logger;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	public sealed class RadiationResist : MonoBehaviour
	{
		private GameObject _radResist;
		public void Start()
		{
			tosaveitemscript save = gameObject.GetComponent<tosaveitemscript>();
			if (save != null)
			{
				// 1 in 3 chance of replacing with RadResist.
				if (IsRadResist(save.idInSave))
				{
					gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
					_radResist = Instantiate(Radiation.RadResistPrefab);
					_radResist.transform.SetParent(transform, false);
					_radResist.transform.localScale = Radiation.RadResistPrefab.transform.localScale;
					_radResist.transform.localPosition = Vector3.zero;
					_radResist.transform.localEulerAngles = Vector3.zero;
					Destroy(gameObject.GetComponent<ediblescript>());
					Destroy(gameObject.GetComponent<Radioactive>());
					save.P.invImg = Radiation.RadiationResistSprite;
					return;
				}
			}
			enabled = false;
		}

		public void Update()
		{
			fpscontroller player = mainscript.M.player;

            if (player.pickedUp != null && player.pickedUp.gameObject == gameObject)
            {
				player.LMB = "Inject";
				player.BLMB = true;
				player.noLMBUse = true;

				if (player.input.lmbDown)
				{
					Use();
				}
            }
        }

		private void Use()
		{
			mainscript.PlayClipAtPoint(Radiation.RadiationAwayInjectClip, transform.position, 1f);
			RadiationPoison.I.SetRadiationResist(0.2f, 90f);
			gameObject.GetComponent<tosaveitemscript>().removeFromMemory = true;
			DestroyImmediate(gameObject);
		}

		public static bool IsRadResist(int id) => new System.Random(id).Next(3) == 2;
	}
}
