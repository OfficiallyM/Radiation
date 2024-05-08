using UnityEngine;
using Logger = Radiation.Modules.Logger;

namespace Radiation.Components
{
	internal sealed class RadiationAwaySpawner : MonoBehaviour
	{
		public void Start()
		{
			try
			{
				int num = 0;
				while (!RadiationAway.IsRadAway(num) || savedatascript.d.toSaveStuff.ContainsKey(num) || savedatascript.d.data.farStuff.ContainsKey(num) || savedatascript.d.data.nearStuff.ContainsKey(num))
					++num;

				GameObject g = Instantiate(itemdatabase.d.gzsemle, transform.position, transform.rotation);
				Logger.Log("Instantiated during RadiationAwaySpawner.Start()");
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}
}
