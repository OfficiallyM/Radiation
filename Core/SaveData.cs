﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

namespace Radiation.Core
{
    internal enum QueueType
    {
        upsert,
        delete,
    }

    [DataContract]
    internal sealed class RadioactiveData : Savable
    {
        [DataMember] public int RadiationType { get; set; }
        [DataMember] public float RadiationLevel { get; set; }
        [DataMember] public float Distance { get; set; }
    }

	[DataContract]
	internal sealed class PoisonData : Savable
	{
		[DataMember] public float RadiationLevel { get; set; } = 0;
		[DataMember] public float RadAway { get; set; } = 0;
        [DataMember] public float RadResist { get; set; } = 0;
        [DataMember] public float RadResistLength { get; set; } = 0;
        [DataMember] public int RadResistStacks { get; set; } = 0;
		[DataMember] public bool IsNPCTransformed { get; set; } = false;
	}

    internal sealed class ToDelete : Savable { }

    internal sealed class QueueEntry
    {
        public QueueType QueueType { get; set; } = QueueType.upsert;
        public Savable Data { get; set; }
    }

    [DataContract]
    [KnownType("GetKnownTypes")]
    internal abstract class Savable
    {
        [DataMember] public int Id { get; set; }
        [DataMember] public Vector3? Position { get; set; } = null;
        [DataMember] public string Type { get; set; }

        private static IEnumerable<Type> _knownTypes;
        private static IEnumerable<Type> GetKnownTypes()
        {
            if (_knownTypes == null)
                _knownTypes = Assembly.GetExecutingAssembly()
                                        .GetTypes()
                                        .Where(t => typeof(Savable).IsAssignableFrom(t) && t.Name != "ToDelete")
                                        .ToList();
            return _knownTypes;
        }
    }

	[DataContract]
	internal sealed class SaveData
	{
		[DataMember] public List<Savable> Data { get; set; }
		[DataMember] public bool HasFoundGeigerCounter { get; set; }
	}
}
