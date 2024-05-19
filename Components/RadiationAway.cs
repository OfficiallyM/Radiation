using UnityEngine;
using Logger = Radiation.Utilities.Logger;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	public sealed class RadiationAway : MonoBehaviour
	{
		private GameObject _radAway;
		public void Start()
		{
			tosaveitemscript save = gameObject.GetComponent<tosaveitemscript>();
			if (save != null)
			{
				// 1 in 3 chance of replacing with RadAway.
				if (IsRadAway(save.idInSave))
				{
					gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
					_radAway = Instantiate(Radiation.SyringePrefab);
					_radAway.transform.SetParent(transform, false);
					_radAway.transform.localScale = Radiation.SyringePrefab.transform.localScale;
					_radAway.transform.localPosition = Vector3.zero;
					_radAway.transform.localEulerAngles = Vector3.zero;
					Destroy(gameObject.GetComponent<ediblescript>());
					Destroy(gameObject.GetComponent<Radioactive>());
					save.P.invImg = Radiation.RadiationAwaySprite;
					return;
				}
			}
			this.enabled = false;
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
			RadiationPoison.I.SetRadiationAway(0.07f);
			gameObject.GetComponent<tosaveitemscript>().removeFromMemory = true;
			DestroyImmediate(gameObject);
		}

		public static bool IsRadAway(int id) => new System.Random(id).Next(3) == 1;
	}
}
