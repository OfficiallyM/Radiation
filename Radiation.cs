using Radiation.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
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
		public override string Version => "0.2";
		public override bool UseAssetsFolder => true;
		public override bool LoadInMenu => false;

		internal static Mod mod;
		internal static bool debug = false;

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
			debug = setting.GUICheckbox(debug, "Debug mode", 10, 10);
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
			controller.AddComponent<Components.RadiationController>();

			// Attach gauges.
			if (itemdatabase.d.gww2compass.GetComponent<Components.Gauge>() == null)
				itemdatabase.d.gww2compass.AddComponent<Components.Gauge>();
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
			if (mainscript.M.player.gameObject.GetComponent<Components.RadiationPoison>() == null)
				mainscript.M.player.gameObject.AddComponent<Components.RadiationPoison>();
		}

		public override bool LoadInDB => true;
	}
}
