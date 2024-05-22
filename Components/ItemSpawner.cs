using UnityEngine;
using Logger = Radiation.Utilities.Logger;

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
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}

	internal sealed class RadiationResistSpawner : MonoBehaviour
	{
		public void Start()
		{
			try
			{
				int num = 0;
				while (!RadiationResist.IsRadResist(num) || savedatascript.d.toSaveStuff.ContainsKey(num) || savedatascript.d.data.farStuff.ContainsKey(num) || savedatascript.d.data.nearStuff.ContainsKey(num))
					++num;

				GameObject g = Instantiate(itemdatabase.d.gzsemle, transform.position, transform.rotation);
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}

	internal sealed class GaugeSpawner : MonoBehaviour
	{
		public void Start()
		{
			try
			{
				int num = 0;
				while (!Gauge.IsGauge(num) || savedatascript.d.toSaveStuff.ContainsKey(num) || savedatascript.d.data.farStuff.ContainsKey(num) || savedatascript.d.data.nearStuff.ContainsKey(num))
					++num;

				GameObject g = Instantiate(itemdatabase.d.gww2compass, transform.position, transform.rotation);
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}
}
