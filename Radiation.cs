using Radiation.Components;
using Radiation.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TLDLoader;
using UnityEngine;

//using AAAFramework;
using Logger = Radiation.Utilities.Logger;

namespace Radiation
{
	public class Radiation : Mod
	{
		internal static Texture[] textures;
		internal static GameObject RadAwayPrefab;
		internal static GameObject RadResistPrefab;
		internal static Shader RadiationAwaySicknessShader;
		internal static AudioClip RadiationAwayInjectClip;
		internal static Sprite RadiationAwaySprite;
		internal static Sprite RadiationResistSprite;

		public override string ID => nameof(Radiation);
		public override string Name => nameof(Radiation);
		public override string Author => "M-, TR";
		public override string Version => "1.1.0";
		public override bool LoadInDB => true;

		internal static Mod mod;

		// Config variables.
		private static readonly float _radiationPoisonMultiplierDefault = 0.1f;
		private static float _radiationPoisonMultiplier = 0.1f;
		private static readonly float _radiationPoisonDissipationMultiplierDefault = 0.05f;
		private static float _radiationPoisonDissipationMultiplier = 0.05f;
		internal static bool disableUntilGeigerCounter = false;
		internal static bool debug = false;
        internal static bool debugShowNearbyRadioactives = false;
		internal static bool disable = false;
		internal static bool disableForPlayer = false;

		internal static bool hasFoundGeigerCounter = false;

		private GameObject _lookingAt;

		public override void Config()
		{
			SettingAPI setting = new SettingAPI(this);
			// Actual settings.
			disableUntilGeigerCounter = setting.GUICheckbox(disableUntilGeigerCounter, "Disable radiation until you find a geiger counter", 10, 10);
			_radiationPoisonMultiplier = setting.GUISlider($"Poison multiplier (Default: {_radiationPoisonMultiplierDefault})", _radiationPoisonMultiplier, 0f, 1f, 10, 40);
			_radiationPoisonDissipationMultiplier = setting.GUISlider($"Poison dissipation multiplier (Default: {_radiationPoisonDissipationMultiplierDefault})", _radiationPoisonDissipationMultiplier, 0f, 1f, 10, 140);

			if (mainscript.M != null)
			{
				RadiationPoison.I.SetPoisonMultiplier(_radiationPoisonMultiplier);
				RadiationPoison.I.SetDissipationMultiplier(_radiationPoisonDissipationMultiplier);
			}

			// Debug stuff.
			debug = setting.GUICheckbox(debug, "Debug mode", 10, 220);
            debugShowNearbyRadioactives = setting.GUICheckbox(debugShowNearbyRadioactives, "Debug list nearby radioactive objects", 10, 240);
            disable = setting.GUICheckbox(disable, "Disable radiation completely", 10, 260);
			disableForPlayer = setting.GUICheckbox(disableForPlayer, "Disable radiation for player", 10, 280);
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
			RadAwayPrefab = assetBundle.LoadAsset<GameObject>("RadAway");
			RadResistPrefab = assetBundle.LoadAsset<GameObject>("RadResist");
			RadiationAwaySicknessShader = assetBundle.LoadAsset<Shader>("RadiationAwaySickness.shader");
			RadiationAwayInjectClip = assetBundle.LoadAsset<AudioClip>("Inject.wav");
			RadiationAwaySprite = assetBundle.LoadAsset<Sprite>("RadAway.png");
			RadiationResistSprite = assetBundle.LoadAsset<Sprite>("RadResist.png");

			if (textures == null)
			{
				List<Texture> textureList = new List<Texture>();
				foreach (string asset in assetBundle.GetAllAssetNames())
				{
					if (asset.ToLower().EndsWith("dds"))
						textureList.Add(assetBundle.LoadAsset<Texture2D>(asset));
				}
				textures = textureList.ToArray();
			}

			assetBundle.Unload(false);

			// Make mod items via replacement.
			itemdatabase.d.gzsemle.AddComponent<RadiationAway>();
			itemdatabase.d.gzsemle.AddComponent<RadiationResist>();
			itemdatabase.d.gzsemle.name = "Bread, RadAway or RadResist";

			// Create placeholders to show in M-ultiTool mod items category.
			try
			{
				GameObject radAwayPlaceholder = new GameObject("RadAwayPlaceholder");
				radAwayPlaceholder.transform.SetParent(mainscript.M.transform);
				radAwayPlaceholder.SetActive(false);
				GameObject radAway = new GameObject("RadAway");
				radAway.transform.SetParent(radAwayPlaceholder.transform, false);
				UnityEngine.Object.Instantiate(RadAwayPrefab, radAway.transform, false).transform.Rotate(0f, 180f, 0f);
				radAway.AddComponent<RadiationAwaySpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, radAway).ToArray();
				radAway.GetComponentInChildren<Collider>().enabled = false;

				GameObject radResistPlaceholder = new GameObject("RadResistPlaceholder");
				radResistPlaceholder.transform.SetParent(mainscript.M.transform);
				radResistPlaceholder.SetActive(false);
				GameObject radResist = new GameObject("RadResist");
				radResist.transform.SetParent(radResistPlaceholder.transform, false);
				UnityEngine.Object.Instantiate(RadResistPrefab, radResist.transform, false).transform.Rotate(0f, 180f, 0f);
				radResist.AddComponent<RadiationResistSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, radResist).ToArray();
				radResist.GetComponentInChildren<Collider>().enabled = false;

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
		}

		public override void OnLoad()
		{
			hasFoundGeigerCounter = Save.GetHasFoundGeigerCounter();

			// Apply radioactivity when starting a new game.
			if (mainscript.M.menu.DFMS.load) return;

			foreach (Collider collider in Physics.OverlapSphere(mainscript.M.player.transform.position, 400f))
			{
				GameObject root = collider.transform.root.gameObject;

                CreateAsSafe(root);

                // Create any children as safe too.
                foreach (tosaveitemscript save in root.GetComponentsInChildren<tosaveitemscript>())
                    CreateAsSafe(save.gameObject);
			}
		}

        /// <summary>
        /// Create a GameObject as safe.
        /// </summary>
        /// <param name="gameObject"></param>
        private void CreateAsSafe(GameObject gameObject)
        {
            string[] itemsBlacklist = new string[]
            {
                "playerragdol",
            };

            // Exclude certain items.
            if (itemsBlacklist.Contains(gameObject.name.ToLower().Replace("(clone)", string.Empty))) return;

            // Don't attempt to apply twice to the same object.
            if (gameObject.GetComponent<Radioactive>() != null) return;

            // Don't apply if object isn't a building or saveable.
            if (gameObject.GetComponent<buildingscript>() == null && gameObject.GetComponent<tosaveitemscript>() == null) return;

            Radioactive radioactive = gameObject.AddComponent<Radioactive>();
            radioactive.Init(Radioactive.RadiationType.Safe);
        }

		public override void Update()
		{
			try
			{
				Save.ExecuteQueue();
			}
			catch (Exception ex)
			{
				Logger.Log($"Error during queue execute. Details: {ex}", Logger.LogLevel.Error);
			}

			// Track hasFoundGeigerCounter.
			if (!hasFoundGeigerCounter && mainscript.M.player.pickedUp != null && mainscript.M.player.pickedUp.GetComponent<Gauge>() != null)
			{
				hasFoundGeigerCounter = true;
				Save.SetHasFoundGeigerCounter(hasFoundGeigerCounter);
			}

			// Find object the player is looking at.
			if (debug)
			{
				try
				{
					// Find object the player is looking at.
					GameObject foundObject = null;
					Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out var raycastHit, float.PositiveInfinity, mainscript.M.player.useLayer);

					tosaveitemscript save = raycastHit.transform?.gameObject?.GetComponent<tosaveitemscript>();
					if (save != null)
						foundObject = raycastHit.transform.gameObject;

					// Debug picked up if player is holding something.
					if (mainscript.M.player.pickedUp != null)
						foundObject = mainscript.M.player.pickedUp.gameObject;

					// Debug held item if something is equipped.
					if (mainscript.M.player.inHandP != null)
						foundObject = mainscript.M.player.inHandP.gameObject;
				
					if (foundObject != null)
					{
						Radioactive radioactive = foundObject.GetComponent<Radioactive>();
						if (radioactive == null)
							foundObject = null;
					}

					_lookingAt = foundObject;
				}
				catch (Exception ex)
				{
					Logger.Log($"Debug object error. Details: {ex}", Logger.LogLevel.Error);
				}
			}
		}

		public override void OnGUI()
		{
			if (!debug || _lookingAt == null) return;

			Radioactive radioactive = _lookingAt.GetComponent<Radioactive>();
			float? rads = radioactive.GetRadiationLevel(mainscript.M.player.transform.position);
			if (!rads.HasValue)
				rads = 0;

			GUI.Button(new Rect(300, 0, 300, 20), $"{_lookingAt.name}");
			GUI.Button(new Rect(300, 20, 300, 20), $"Object rads: {Math.Round((double)rads * 100, 2)}");
		}
	}
}
