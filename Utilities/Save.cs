using Radiation.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

namespace Radiation.Utilities
{
	internal static class Save
	{
		private static SaveData _data;

		// Queue variables.
		private static float _lastQueueRunTime = 0;
		private static List<PoisonData> _queue = new List<PoisonData>();
		private static int _queueInterval = 2;

		/// <summary>
		/// Read/write data to game save.
		/// <para>Originally from RundensWheelPositionEditor</para>
		/// </summary>
		/// <param name="input">The string to write to the save</param>
		/// <returns>The read/written string</returns>
		internal static string ReadWriteData(string input = null)
		{
			try
			{
				save_rendszam saveRendszam = null;
				save_prefab savePrefab1;

				// Attempt to find existing plate.
				if ((savedatascript.d.data.farStuff.TryGetValue(Mathf.Abs(Radiation.mod.ID.GetHashCode()), out savePrefab1) || savedatascript.d.data.nearStuff.TryGetValue(Mathf.Abs(Radiation.mod.ID.GetHashCode()), out savePrefab1)) && savePrefab1.rendszam != null)
					saveRendszam = savePrefab1.rendszam;

				// Plate doesn't exist.
				if (saveRendszam == null)
				{
					// Create a new plate to store the input string in.
					tosaveitemscript component = itemdatabase.d.gplate.GetComponent<tosaveitemscript>();
					save_prefab savePrefab2 = new save_prefab(component.category, component.id, double.MaxValue, double.MaxValue, double.MaxValue, 0.0f, 0.0f, 0.0f);
					savePrefab2.rendszam = new save_rendszam();
					saveRendszam = savePrefab2.rendszam;
					saveRendszam.S = string.Empty;
					savedatascript.d.data.farStuff.Add(Mathf.Abs(Radiation.mod.ID.GetHashCode()), savePrefab2);
				}

				// Write the input to the plate.
				if (input != null && input != string.Empty)
					saveRendszam.S = input;

				return saveRendszam.S;
			}
			catch (Exception ex)
			{
				Logger.Log($"Save ReadWriteData() error. Details: {ex}", Logger.LogLevel.Error);
			}

			return string.Empty;
		}

		/// <summary>
		/// Unserialize existing save data.
		/// </summary>
		/// <returns>Unserialized save data</returns>
		private static SaveData Get()
		{
			try
			{
				if (_data == null)
				{
					// Save data isn't loaded, load it now.
					string existingString = ReadWriteData().Trim();
					if (existingString != null && existingString != string.Empty)
					{
						MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(existingString));
						DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(SaveData));
						_data = jsonSerializer.ReadObject(ms) as SaveData;
					}
				}

				return _data != null ? _data : new SaveData();
			}
			catch (Exception ex)
			{
				Logger.Log($"Save Get() error. Details: {ex}", Logger.LogLevel.Error);
			}

			return new SaveData();
		}

		/// <summary>
		/// Serialize save data and write to save.
		/// </summary>
		/// <param name="data">The data to serialize</param>
		private static void Set(SaveData data)
		{
			try 
			{
				// Update loaded save data.
				_data = data;

				MemoryStream ms = new MemoryStream();
				DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(SaveData));
				jsonSerializer.WriteObject(ms, _data);

				// Rewind stream.
				ms.Seek(0, SeekOrigin.Begin);

				// Convert stream to a string.
				StreamReader reader = new StreamReader(ms);
				string jsonString = reader.ReadToEnd();

				ReadWriteData(jsonString);
			}
			catch (Exception ex)
			{
				Logger.Log($"Save Set() error. Details: {ex}", Logger.LogLevel.Error);
			}
		}

		/// <summary>
		/// Set poison data.
		/// </summary>
		/// <param name="poisonData">Poison data to save</param>
		internal static void SetPoisonData(PoisonData poisonData)
		{
			PoisonData existing = _queue.Where(d => d.Id == poisonData.Id).FirstOrDefault();
			if (existing != null)
			{
				existing.RadiationLevel = poisonData.RadiationLevel;
				existing.RadAway = poisonData.RadAway;
				existing.IsNPCTransformed = poisonData.IsNPCTransformed;
			}
			else
				_queue.Add(poisonData);
		}

		/// <summary>
		/// Delete poison data for a given ID.
		/// </summary>
		/// <param name="Id">ID to delete poison data for, if it exists</param>
		internal static void DeletePoisonData(int Id)
		{
			SaveData save = Get();
			PoisonData data = save.PoisonData.Where(d => d.Id == Id).FirstOrDefault();
			save.PoisonData.Remove(data);
			Set(save);
		}

		/// <summary>
		/// Get poison data for a given ID.
		/// </summary>
		/// <param name="Id">ID to find data for</param>
		/// <returns>Poison data if exists, otherwise null</returns>
		internal static PoisonData GetPoisonData(int Id)
		{
			return Get().PoisonData?.Where(d => d.Id == Id).FirstOrDefault();
		}

		/// <summary>
		/// Wrapper for setting HasFoundGeigerCounter.
		/// </summary>
		/// <param name="hasFoundGeigerCounter">True if the player has found a geiger counter, otherwise false</param>
		internal static void SetHasFoundGeigerCounter(bool hasFoundGeigerCounter)
		{
			SaveData data = Get();
			data.HasFoundGeigerCounter = hasFoundGeigerCounter;
			Set(data);
		}

		/// <summary>
		/// Get HasFoundGeigerCounter.
		/// </summary>
		/// <returns>True if the player has found a geiger counter, otherwise false</returns>
		internal static bool GetHasFoundGeigerCounter()
		{
			return Get().HasFoundGeigerCounter;
		}

		internal static void ExecuteQueue()
		{
			int currentQueueInterval = Mathf.RoundToInt(Time.unscaledTime - _lastQueueRunTime);
			if (currentQueueInterval < _queueInterval)
				return;

			SaveData data = Get();
			foreach (PoisonData poisonData in _queue)
			{
				PoisonData existing = data.PoisonData.Where(d => d.Id == poisonData.Id).FirstOrDefault();
				if (existing != null)
				{
					existing.RadiationLevel = poisonData.RadiationLevel;
					existing.RadAway = poisonData.RadAway;
					existing.IsNPCTransformed = poisonData.IsNPCTransformed;
				}
				else
				{
					data.PoisonData.Add(poisonData);
				}
			}

			Set(data);
			_queue.Clear();
			_lastQueueRunTime = Time.unscaledTime;
		}
	}
}
