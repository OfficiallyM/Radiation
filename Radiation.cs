using Radiation.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TLDLoader;
using UnityEngine;
//using AAAFramework;
using Logger = Radiation.Modules.Logger;

namespace Radiation
{
	public class Radiation : Mod
	{
		internal static Texture[] textures;
		internal static GameObject SyringePrefab;
		internal static Shader RadiationAwaySicknessShader;
		internal static AudioClip RadiationAwayInjectClip;
		internal static Sprite RadiationAwaySprite;

		public override string ID => nameof(Radiation);
		public override string Name => nameof(Radiation);
		public override string Author => "M-, Trorange";
		public override string Version => "0.3.0";
		public override bool UseAssetsFolder => true;
		public override bool LoadInMenu => false;
		public override bool LoadInDB => true;

		internal static Mod mod;
		internal static bool debug = false;
		internal static bool disable = false;
		internal static bool disableForPlayer = false;

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
			debug = setting.GUICheckbox(debug, "Debug mode", 10, 10);
			disable = setting.GUICheckbox(disable, "Disable radiation completely", 10, 30);
			disableForPlayer = setting.GUICheckbox(disableForPlayer, "Disable radiation for player", 10, 50);
		}

		public Radiation()
		{
			mod = this;

			Logger.Init();
		}

		public override void dbLoad()
		{
			// Attach radiation controller.
			GameObject controller = new GameObject("RadiationController");
			controller.transform.SetParent(mainscript.M.transform);
			controller.AddComponent<RadiationController>();

			// Attach gauges.
			if (itemdatabase.d.gww2compass.GetComponent<Gauge>() == null)
				itemdatabase.d.gww2compass.AddComponent<Gauge>();
			if (textures == null)
			{
				List<Texture> textureList = new List<Texture>();
				foreach (string file in Directory.GetFiles(ModLoader.GetModAssetsFolder(this)))
				{
					if (file.ToLower().EndsWith("dds"))
						textureList.Add(LoadAssets.LoadTexture(this, Path.GetFileName(file)));
				}
				textures = textureList.ToArray();
			}

			// Attach player radiation poisioning.
			if (mainscript.M.player.gameObject.GetComponent<RadiationPoison>() == null)
				mainscript.M.player.gameObject.AddComponent<RadiationPoison>();

			// Attach radiation to buildings.
			string[] buildingsBlacklist = new string[]
			{
				"post",
				"phonebooth",
			};
			foreach (GameObject building in itemdatabase.d.buildings)
			{
				// Exclude certain buildings.
				if (buildingsBlacklist.Contains(building.name.ToLower())) continue;

				if (building.GetComponent<Radioactive>() == null)
				{
					Radioactive radioactive = building.AddComponent<Radioactive>();
					// Always create starter house as a safe zone.
					if (building.name == "haz02")
						radioactive.Init(Radioactive.RadiationType.Safe);
				}
			}

			// Attach radiation to desert generation buildings.
			foreach (ObjClass objClass in mainscript.M.terrainGenerationSettings.desertTowerGeneration.objTypes)
			{
				if (buildingsBlacklist.Contains(objClass.prefab.name.ToLower())) continue;

				if (objClass.prefab.GetComponent<Radioactive>() == null)
					objClass.prefab.AddComponent<Radioactive>();
			}

			// Attach radiation to items.
			string[] itemsBlacklist = new string[]
			{
				"playerragdol",
			};
			string[] npcItems = new string[]
			{
				"munkas01",
				"nyul",
			};
			foreach (GameObject item in itemdatabase.d.items)
			{
				// Exclude certain items.
				string name = item.name.ToLower().Replace("(clone)", string.Empty);
				if (itemsBlacklist.Contains(name)) continue;

				if (npcItems.Contains(name) && item.GetComponent<NPCRadiationPoison>() == null)
					item.AddComponent<NPCRadiationPoison>();
				else if (item.GetComponent<Radioactive>() == null)
					item.AddComponent<Radioactive>();
			}

			// Load assets.
			AssetBundle assetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(Radiation)}.radiation"));
			SyringePrefab = (GameObject)assetBundle.LoadAsset("Syringe");
			RadiationAwaySicknessShader = assetBundle.LoadAsset<Shader>("RadiationAwaySickness.shader");
			RadiationAwayInjectClip = assetBundle.LoadAsset<AudioClip>("Inject.wav");
			RadiationAwaySprite = assetBundle.LoadAsset<Sprite>("RadAway.png");
			assetBundle.Unload(false);

			// Make mod items via replacement.
			itemdatabase.d.gzsemle.AddComponent<RadiationAway>();
			itemdatabase.d.gzsemle.name = "Bread or RadAway";

			// Create placeholders to show in M-ultiTool mod items category.
			try
			{
				GameObject radAwayPlaceholder = new GameObject("RadAwayPlaceholder");
				radAwayPlaceholder.transform.SetParent(mainscript.M.transform);
				radAwayPlaceholder.SetActive(false);
				GameObject radAway = new GameObject("RadAway");
				radAway.transform.SetParent(radAwayPlaceholder.transform, false);
				UnityEngine.Object.Instantiate(SyringePrefab, radAway.transform, false).transform.Rotate(0f, 180f, 0f);
				radAway.AddComponent<RadiationAwaySpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, radAway).ToArray();
				radAway.GetComponentInChildren<Collider>().enabled = false;

				GameObject gaugePlaceholder = new GameObject("GaugePlaceholder");
				gaugePlaceholder.transform.SetParent(mainscript.M.transform);
				gaugePlaceholder.SetActive(false);
				GameObject gauge = new GameObject("Geiger counter");
				gauge.transform.SetParent(gaugePlaceholder.transform, false);
				UnityEngine.Object.Instantiate(itemdatabase.d.gww2compass, gauge.transform, false).transform.Rotate(0f, 180f, 0f);
				gauge.AddComponent<GaugeSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, gauge).ToArray();
				gauge.GetComponentInChildren<Collider>().enabled = false;
			}
			catch (Exception ex)
			{
				Logger.Log($"Failed to create placeholders. Details: {ex}");
			}

			// Create mod items using AAAFramework.
			//ModItem.WithGameObject()
			//	.FromEmbeddedResource("radiation", "Syringe")
			//	.SetMass(0.5f)
			//	.SetSpawnChance(0.5f)
			//	.AddPersistentComponent<RadiationAway>()
			//	.Init();
		}

		public override void OnLoad()
		{
			// Apply radioactivity when starting a new game.
			if (mainscript.M.menu.DFMS.load) return;

			string[] itemsBlacklist = new string[]
			{
				"playerragdol",
			};

			foreach (Collider collider in Physics.OverlapSphere(mainscript.M.player.transform.position, 400f))
			{
				GameObject root = collider.transform.root.gameObject;

				// Exclude certain items.
				if (itemsBlacklist.Contains(root.name.ToLower().Replace("(clone)", string.Empty))) continue;

				// Don't attempt to apply twice to the same object.
				if (root.GetComponent<Radioactive>() != null) continue;

				// Don't apply if object isn't a building or saveable.
				if (root.GetComponent<buildingscript>() == null && root.GetComponent<tosaveitemscript>() == null) continue;

				Radioactive radioactive = root.AddComponent<Radioactive>();
				radioactive.Init(Radioactive.RadiationType.Safe);
			}
		}

		public override void Update()
		{
			//if (Input.GetKeyDown(KeyCode.Semicolon))
			//	AAAFramework.AAAFramework.ItemDatabase.Where(i => i.Key == "Syringe").FirstOrDefault().Value.Spawn(mainscript.M.player.transform.position + Vector3.left * 1f);
		}
	}
}
