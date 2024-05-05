using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace Radiation.Components
{
	[DisallowMultipleComponent]
	internal sealed class Gauge : MonoBehaviour
	{
		private meterscript _meter;
		private attachablescript _attach;

		public void Start()
		{
			tosaveitemscript component1;
			if (TryGetComponent(out component1))
			{
				if (!component1.started)
					component1.FStart();
				System.Random random = new System.Random(component1.idInSave);
				if (random.Next(0, 4) == 2)
				{
					RemoveAllModClasses();
					gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
					compassscript component2 = gameObject.GetComponent<compassscript>();
					_meter = component2.meter;
					component2.enabled = false;
					try
					{
						GameObject gameObject = new GameObject("RadiationFace");
						gameObject.transform.SetParent(this.gameObject.transform, false);
						gameObject.transform.localScale = new Vector3(-0.075f, 0.075f, 1E-06f);
						gameObject.transform.localPosition = new Vector3(0.0f, 0.0f, -0.008f);
						gameObject.AddComponent<MeshFilter>().mesh = itemdatabase.d.gerror.GetComponentInChildren<MeshFilter>().mesh;
						_meter.R = gameObject.AddComponent<MeshRenderer>();
						_meter.OffM = new Material(Shader.Find("Standard"));
						_meter.OffM.mainTexture = Radiation.textures[random.Next(0, Radiation.textures.Length)];
						_meter.OffM.SetFloat("_Mode", 2f);
						_meter.OffM.SetInt("_SrcBlend", 5);
						_meter.OffM.SetInt("_DstBlend", 10);
						_meter.OffM.SetInt("_ZWrite", 0);
						_meter.OffM.EnableKeyword("_ALPHATEST_ON");
						_meter.OffM.EnableKeyword("_ALPHABLEND_ON");
						_meter.OffM.EnableKeyword("_ALPHAPREMULTIPLY_ON");
						_meter.OffM.SetFloat("_SpecularHighlights", 0.0f);
						_meter.OffM.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
						_meter.OffM.renderQueue = 3000;
						_meter.OnM = new Material(_meter.OffM);
						_meter.OnM.EnableKeyword("_EMISSION");
						_meter.OnM.SetTexture("_EmissionMap", _meter.OnM.mainTexture);
						_meter.OnM.name = "ON_Material";
						_meter.R.material = _meter.OffM;
						_meter.R.reflectionProbeUsage = ReflectionProbeUsage.Off;
					}
					catch (Exception ex)
					{
						Debug.Log(ex.ToString());
					}
					_attach = GetComponent<attachablescript>();
					float minAngle = -155f;
					float maxAngle = 195f;
					float minValue = 0.0f;
					Color color = Color.white;
					try
					{
						string[] strArray = Path.GetFileNameWithoutExtension(_meter.R.material.mainTexture.name).Split(new char[1]
						{
							'f'
						}, StringSplitOptions.RemoveEmptyEntries);
						if (strArray.Length != 0.0)
						{
							if (strArray[0] != null)
							{
								strArray[0] = Regex.Replace(strArray[0], "[^0-9-]+", "");
								float result;
								if (float.TryParse(strArray[0], out result))
									minAngle = result;
							}
							if (strArray.Length > 1 && strArray[1] != null)
							{
								strArray[1] = Regex.Replace(strArray[1], "[^0-9-]+", "");
								float result;
								if (float.TryParse(strArray[1], out result))
									maxAngle = result;
							}
							if (strArray.Length > 2 && strArray[2] != null)
								_meter.OnM.SetColor("_EmissionColor", new Color(0.4f, 1.2f, 0.4f));
							if (strArray.Length > 3)
							{
								if (strArray[3] != null)
								{
									string lower = strArray[3].ToLower();
									color = lower.Contains("w") ? Color.white : (lower.Contains("b") ? Color.black : (lower.Contains("r") ? Color.red : Color.grey));
								}
							}
						}
					}
					catch
					{
					}
					foreach (meterscript.meterstuff meterstuff in _meter.mutatok)
					{
						meterstuff.mutato.minAngle = minAngle;
						meterstuff.mutato.maxAngle = maxAngle;
						meterstuff.mutato.minValue = minValue;
						meterstuff.mutato.OffM = new Material(meterstuff.mutato.OffM);
						meterstuff.mutato.OffM.color = color;
						meterstuff.mutato.OnM = new Material(meterstuff.mutato.OnM);
						meterstuff.mutato.OnM.color = color;
						meterstuff.mutato.OnM.SetColor("_EmissionColor", new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f));
						meterstuff.mutato.R.material = meterstuff.mutato.OffM;
						meterstuff.mutato.inputValueOffset = 0;
						meterstuff.tipus = "geigercounter";
						if (maxAngle < 0)
						{
							meterstuff.mutato.lerpAngle = false;
						}
					}
					foreach (meterscript.szamlap szamlap in _meter.szamlapok)
					{
						if (szamlap.forgo != null)
						{
							forgoszamlap.szam[] szamok = szamlap.forgo.szamok;
							for (int index = 0; index < szamok.Length; ++index)
							{
								szamok[index].r.transform.localScale = new Vector3(1f, 1f, 1f);
								szamok[index].r.enabled = false;
								gameObject.GetComponentInChildren<usablescript>().gameObject.SetActive(false);
							}
						}
					}
					if (_attach != null)
						return;
				}
			}
			enabled = false;
			Destroy(this);
		}

		public void Update()
		{
			if (_meter == null) return;

			foreach (meterscript.meterstuff meterstuff in _meter.mutatok)
			{
				meterstuff.mutato.maxValue = 1f;
				float radiation = RadiationController.I.GetRadiationLevel(gameObject.transform.position);
				// Fake the needle jumping up when entering a radiation zone by altering the value.
				if (radiation <= 0.4f)
					radiation = 0.03f;
				float flicker = UnityEngine.Random.Range(-0.3f, 0.3f);
				meterstuff.value = Mathf.Clamp(1f - radiation + flicker, meterstuff.mutato.minValue, meterstuff.mutato.maxValue);
			}
		}

		public void RemoveAllModClasses()
		{
			try
			{
				foreach (MonoBehaviour component in gameObject.GetComponents<MonoBehaviour>())
				{
					if (component != this)
					{
						string lower = component.GetType().Name.ToString().ToLower();
						if (lower.Contains("runden") || lower.Contains("tacho") || lower.Contains("ghaleas") || lower.Contains("temp") || lower.Contains("guage") || lower.Contains("fuel") || lower.Contains("guages") || lower.Contains("gauge") || lower.Contains("gauges") || lower.Contains("distance"))
						{
							component.enabled = false;
							Destroy(component);
							transform.Find("RadiationFace")?.gameObject.SetActive(false);
						}
					}
				}
			}
			catch
			{
				enabled = false;
				Destroy(this);
			}
		}
	}
}
