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
		private static List<QueueEntry> _queue = new List<QueueEntry>();
		private static readonly int _queueInterval = 2;

		/// <summary>
		/// Read/write data to game save.
		/// <para>Originally from RundensWheelPositionEditor</para>
		/// </summary>
		/// <param name="input">The string to write to the save</param>
		/// <returns>The read/written string</returns>
		private static string ReadWriteData(string input = null)
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
        /// Update if already exists, otherwise insert new save data.
        /// </summary>
        /// <param name="data">Data to insert</param>
        internal static void Upsert(Savable data)
        {
            QueueEntry entry = _queue.Where(q => ((data.Id != -1 && q.Data.Id == data.Id) || (data.Id == -1 && q.Data.Position == data.Position)) && q.Data.Type == data.Type).FirstOrDefault();
            if (entry != null)
                entry.Data = data;
            else
                _queue.Add(new QueueEntry() { Data = data });
        }

        /// <summary>
        /// Delete save data.
        /// </summary>
        /// <param name="id">Id of data to delete</param>
        /// <param name="type">Data type of data to delete</param>
        internal static void Delete(int id, string type)
        {
            QueueEntry entry = _queue.Where(q => q.Data.Id == id && q.Data.Type == type).FirstOrDefault();
            if (entry != null)
                entry.QueueType = QueueType.delete;
            else
                _queue.Add(new QueueEntry() { QueueType = QueueType.delete, Data = new ToDelete() { Id = id, Type = type } });
        }

        /// <summary>
        /// Get save data by id and type.
        /// </summary>
        /// <param name="id">Id to search</param>
        /// <param name="type">Data type</param>
        /// <returns>Save data if exists, otherwise null</returns>
        private static Savable GetData(int id, string type)
        {
            return Get().Data?.Where(d => d.Id == id && d.Type == type).FirstOrDefault();
        }

		/// <summary>
		/// Get poison data for a given ID.
		/// </summary>
		/// <param name="Id">ID to find data for</param>
		/// <returns>Poison data if exists, otherwise null</returns>
		internal static PoisonData GetPoisonData(int id)
		{
            return (PoisonData)GetData(id, "poison");
		}

        /// <summary>
        /// Get radioactive data for a given ID or global position.
        /// </summary>
        /// <param name="id">Id to check for or -1 to check by position</param>
        /// <param name="position">Global position to check for</param>
        /// <returns>Radioactive data if exists, otherwise null</returns>
        internal static RadioactiveData GetRadioactiveData(int id, Vector3 position)
        {
            return (RadioactiveData)Get().Data?.Where(d => ((id != -1 && id == d.Id) || (id == -1 && Vector3.Distance(d.Position.Value, position) < 2)) && d.Type == "radioactive").FirstOrDefault();
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

        /// <summary>
        /// Trigger save data queue execution.
        /// </summary>
		internal static void ExecuteQueue()
		{
			int currentQueueInterval = Mathf.RoundToInt(Time.unscaledTime - _lastQueueRunTime);
			if (currentQueueInterval < _queueInterval)
				return;

            if (_queue.Count > 0)
            {
                int upserts = _queue.Where(q => q.QueueType == QueueType.upsert).ToList().Count;
                int deletes = _queue.Where(q => q.QueueType == QueueType.delete).ToList().Count;
                Logger.Log($"Processing queue: {_queue.Count} items, {upserts} upserts, {deletes} deletes", Logger.LogLevel.Debug);

			    SaveData data = Get();
                if (data.Data == null)
                    data.Data = new List<Savable>();

			    foreach (QueueEntry entry in _queue)
			    {
                    switch (entry.QueueType)
                    {
                        case QueueType.upsert:
				            Savable existing = data.Data.Where(d => d.Id == entry.Data.Id).FirstOrDefault();
				            if (existing != null)
                                existing = entry.Data;
				            else
					            data.Data.Add(entry.Data);
                            break;
                        case QueueType.delete:
                            Savable save = data.Data.Where(d => d.Id == entry.Data.Id).FirstOrDefault();
                            if (save != null)
                                data.Data.Remove(save);
                            break;
                    }
			    }

			    Set(data);
			    _queue.Clear();
            }

			_lastQueueRunTime = Time.unscaledTime;
		}
	}
}
