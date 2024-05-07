using Radiation.Components;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using TLDLoader;
using UnityEngine;
using Logger = Radiation.Modules.Logger;

namespace Radiation
{
	public class Radiation : Mod
	{
		public static Texture[] textures;

		public override string ID => nameof(Radiation);
		public override string Name => nameof(Radiation);
		public override string Author => "M-, Trorange";
		public override string Version => "0.2.0";
		public override bool UseAssetsFolder => true;
		public override bool LoadInMenu => false;
		public override bool LoadInDB => true;

		internal static Mod mod;
		internal static bool debug = false;
		internal static bool disable = false;

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
			debug = setting.GUICheckbox(debug, "Debug mode", 10, 10);
			disable = setting.GUICheckbox(disable, "Disable radiation", 10, 30);
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
			foreach (GameObject item in itemdatabase.d.items)
			{
				// Exclude certain items.
				if (itemsBlacklist.Contains(item.name.ToLower().Replace("(clone)", string.Empty))) continue;

				if (item.GetComponent<Radioactive>() == null)
					item.AddComponent<Radioactive>();
			}
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
	}
}
